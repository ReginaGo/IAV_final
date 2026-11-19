using System;
using System.Collections;
using System.IO;
using System.Linq;
using MonsterSandbox;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class MonsterSandboxValidation
{
    private static IEnumerator checks;
    private static readonly System.Text.StringBuilder results = new System.Text.StringBuilder();
    static MonsterSandboxValidation()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool("MonsterValidation", false))
            {
                checks = CheckRuntime();
                EditorApplication.update += Tick;
            }
        };
    }
    public static void Run()
    {
        if (!Application.isBatchMode) throw new Exception("Run the automated checks in batch mode to preserve open scenes.");
        MonsterSandboxBuilder.Validate();
        EditorSceneManager.OpenScene(MonsterSandboxBuilder.ScenePath);
        SessionState.SetBool("MonsterValidation", true);
        EditorApplication.isPlaying = true;
    }
    private static void Tick()
    {
        try { if (!checks.MoveNext()) Finish(0); }
        catch (Exception exception) { results.AppendLine("FAIL: " + exception); Finish(1); }
    }
    private static void Finish(int code)
    {
        EditorApplication.update -= Tick;
        SessionState.SetBool("MonsterValidation", false);
        File.WriteAllText("Logs/monster-playmode-validation.txt", results.ToString());
        Debug.Log(results.ToString());
        EditorApplication.Exit(code);
    }
    private static void Require(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
        results.AppendLine("PASS: " + message);
    }
    private static IEnumerator CheckRuntime()
    {
        for (int i = 0; i < 20; i++) yield return null;
        var player = UnityEngine.Object.FindFirstObjectByType<SandboxPlayer>();
        var coil = UnityEngine.Object.FindFirstObjectByType<CoilHeadAI>();
        var bracken = UnityEngine.Object.FindFirstObjectByType<BrackenAI>();
        var thumper = UnityEngine.Object.FindFirstObjectByType<ThumperAI>();
        coil.enabled = bracken.enabled = thumper.enabled = false;
        foreach (var motor in player.monsters) motor.Stop();
        var cm = coil.GetComponent<MonsterMotor>();
        cm.Agent.Warp(new Vector3(0, 0, -10));
        coil.enabled = true;
        Vector3 start = coil.transform.position;
        float until = Time.time + .6f;
        while (Time.time < until) yield return null;
        // NavMesh may snap height after Warp; freezing concerns locomotion across the floor.
        Require(cm.IsObserved() && Vector3.ProjectOnPlane(start - coil.transform.position, Vector3.up).magnitude < .03f, "Coil-Head freezes while visible");
        player.transform.rotation = Quaternion.Euler(0, 180, 0);
        until = Time.time + .6f;
        while (Time.time < until) yield return null;
        Require(Vector3.Distance(start, coil.transform.position) > .5f, "Coil-Head moves when player looks away");
        coil.enabled = false; cm.Stop();

        TeleportPlayer(player, new Vector3(-7, .1f, -8), 0);
        cm.Agent.Warp(new Vector3(-7, 0, 8));
        Physics.SyncTransforms();
        Require(!cm.IsObserved(), "Cover occludes gaze detection");

        TeleportPlayer(player, new Vector3(0, .1f, -18), 0);
        var bm = bracken.GetComponent<MonsterMotor>();
        bm.Agent.Warp(new Vector3(0, 0, -9));
        bracken.ResetBehaviour(); bracken.enabled = true;
        until = Time.time + .25f;
        while (Time.time < until) yield return null;
        Require(bracken.CurrentState == BrackenAI.Behaviour.Retreating, "Bracken retreats when spotted");
        until = Time.time + 2.8f;
        while (Time.time < until) yield return null;
        Require(bracken.CurrentState == BrackenAI.Behaviour.Enraged, "Bracken becomes aggressive after sustained gaze");
        bracken.ResetBehaviour();
        player.transform.rotation = Quaternion.Euler(0, 180, 0);
        until = Time.time + .2f;
        while (Time.time < until) yield return null;
        Require(bracken.CurrentState == BrackenAI.Behaviour.Stalking, "Bracken stalks when unobserved");
        bracken.enabled = false; bm.Stop();

        TeleportPlayer(player, new Vector3(14, .1f, -18), 0);
        var tm = thumper.GetComponent<MonsterMotor>();
        tm.Agent.Warp(new Vector3(14, 0, 2));
        thumper.transform.rotation = Quaternion.Euler(0, 180, 0);
        thumper.ResetBehaviour(); thumper.enabled = true;
        until = Time.time + 1.3f;
        while (Time.time < until) yield return null;
        Require(thumper.CurrentState == ThumperAI.Behaviour.Chasing && tm.Agent.speed > 3.5f, "Thumper detects player and accelerates on a straight path");
        TeleportPlayer(player, new Vector3(22, .1f, thumper.transform.position.z - 5), 0);
        until = Time.time + .15f;
        while (Time.time < until) yield return null;
        Require(tm.Agent.speed < 4.8f, "Thumper slows for a sharp turn");
        TeleportPlayer(player, new Vector3(-7, .1f, -8), 0);
        tm.Agent.Warp(new Vector3(-7, 0, 8));
        Physics.SyncTransforms();
        until = Time.time + .2f;
        while (Time.time < until) yield return null;
        Require(thumper.CurrentState == ThumperAI.Behaviour.Searching, "Thumper searches last seen position after losing sight");
        thumper.enabled = false; tm.Stop();

        player.TakeDamage(100, "validation");
        Require(player.IsDead, "Enemy damage can defeat the player");
        player.SendMessage("ResetSandbox");
        Require(!player.IsDead, "Restart restores player health");
        foreach (var m in player.monsters) m.Stop();
        Capture(player.View, "Logs/monster-player.png");
        var overview = new GameObject("Validation overview").AddComponent<Camera>();
        overview.transform.position = new Vector3(40, 38, -42);
        overview.transform.LookAt(new Vector3(0, 0, 2));
        overview.clearFlags = CameraClearFlags.SolidColor;
        overview.backgroundColor = new Color(.38f,.48f,.57f);
        Capture(overview, "Logs/monster-overview.png");
    }
    private static void TeleportPlayer(SandboxPlayer player, Vector3 position, float yaw)
    {
        var controller = player.GetComponent<CharacterController>();
        controller.enabled = false;
        player.transform.SetPositionAndRotation(position, Quaternion.Euler(0,yaw,0));
        controller.enabled = true;
    }
    private static void Capture(Camera camera, string path)
    {
        var target = new RenderTexture(1280, 720, 24);
        camera.targetTexture = target;
        camera.Render();
        var previous = RenderTexture.active;
        RenderTexture.active = target;
        var image = new Texture2D(1280, 720, TextureFormat.RGB24, false);
        image.ReadPixels(new Rect(0,0,1280,720),0,0); image.Apply();
        File.WriteAllBytes(path, image.EncodeToPNG());
        camera.targetTexture = null; RenderTexture.active = previous;
        UnityEngine.Object.Destroy(image); target.Release(); UnityEngine.Object.Destroy(target);
    }
}
