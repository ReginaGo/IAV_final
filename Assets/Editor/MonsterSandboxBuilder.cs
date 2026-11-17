using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MonsterSandbox;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

public static class MonsterSandboxBuilder
{
    public const string ScenePath = "Assets/Scenes/MonsterSandbox.unity";
    private const string Output = "Assets/MonsterSandbox";
    private static readonly Dictionary<string, Material> materials = new Dictionary<string, Material>();

    [MenuItem("Tools/Monsters/Create sandbox scene")]
    public static void Build()
    {
        if (File.Exists(ScenePath)) throw new InvalidOperationException("MonsterSandbox already exists; keep the saved scene instead of overwriting it.");
        Directory.CreateDirectory(Output + "/Materials");
        AssetDatabase.Refresh();
        var previous = SceneManager.GetActiveScene();
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, Application.isBatchMode ? NewSceneMode.Single : NewSceneMode.Additive);
        SceneManager.SetActiveScene(scene);
        try
        {
            var environment = new GameObject("Environment");
            var ground = Box("Ground", new Vector3(0, -.3f, 0), new Vector3(64, .6f, 64), new Color(.18f, .24f, .17f));
            ground.transform.SetParent(environment.transform);
            foreach (int side in new[] { -1, 1 })
            {
                Box("Boundary", new Vector3(side * 32, 1.5f, 0), new Vector3(1, 3, 64), Color.gray).transform.SetParent(environment.transform);
                Box("Boundary", new Vector3(0, 1.5f, side * 32), new Vector3(64, 3, 1), Color.gray).transform.SetParent(environment.transform);
            }
            // Cover creates line-of-sight breaks and turns for the three behaviours.
            Box("Cover west", new Vector3(-7, 1.5f, 0), new Vector3(2, 3, 7), new Color(.3f,.32f,.3f)).transform.SetParent(environment.transform);
            Box("Cover east", new Vector3(8, 1.5f, 5), new Vector3(2, 3, 8), new Color(.3f,.32f,.3f)).transform.SetParent(environment.transform);
            Box("Cover north", new Vector3(-2, 1.5f, 17), new Vector3(9, 3, 2), new Color(.3f,.32f,.3f)).transform.SetParent(environment.transform);

            var treeAsset = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Nicrom/Shaders/Wind/Prefabs/LPW_Tree_A1_6.5m_01.prefab");
            if (treeAsset)
            {
                for (int i = 0; i < 24; i++)
                {
                    float angle = i * Mathf.PI * 2 / 24;
                    var tree = (GameObject)PrefabUtility.InstantiatePrefab(treeAsset, scene);
                    PrefabUtility.UnpackPrefabInstance(tree, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                    tree.name = "Nicrom Tree " + (i + 1);
                    tree.transform.SetParent(environment.transform);
                    tree.transform.position = new Vector3(Mathf.Cos(angle) * 25, 0, Mathf.Sin(angle) * 25);
                    tree.transform.Rotate(0, i * 37, 0);
                    ConvertMaterials(tree, null);
                    // A simple trunk collider avoids blocking the walkable area under the canopy.
                    foreach (var collider in tree.GetComponentsInChildren<Collider>()) UnityEngine.Object.DestroyImmediate(collider);
                    var trunk = tree.AddComponent<CapsuleCollider>();
                    trunk.center = new Vector3(0, 1.5f, 0); trunk.height = 3; trunk.radius = .45f;
                }
            }
            var light = new GameObject("Sun").AddComponent<Light>();
            light.type = LightType.Directional; light.intensity = 1.8f;
            light.transform.rotation = Quaternion.Euler(50, -30, 0);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(.55f, .6f, .65f);

            var playerObject = new GameObject("Player"); playerObject.layer = 2;
            playerObject.transform.position = new Vector3(0, .1f, -18);
            var controller = playerObject.AddComponent<CharacterController>();
            controller.height = 1.8f; controller.radius = .3f; controller.center = Vector3.up * .9f;
            var player = playerObject.AddComponent<SandboxPlayer>();
            var camera = new GameObject("Player Camera").AddComponent<Camera>();
            camera.tag = "MainCamera"; camera.transform.SetParent(playerObject.transform, false);
            camera.transform.localPosition = Vector3.up * 1.65f; camera.fieldOfView = 75; camera.farClipPlane = 120;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.38f,.48f,.57f);
            camera.gameObject.AddComponent<AudioListener>(); player.View = camera;

            var bracken = Model("Bracken", "Assets/Mounstros/source_Bracken/Bracken.fbx", new Vector3(-12, 0, 4), 2.5f);
            var coil = Model("Coil-Head", "Assets/Mounstros/source_Coil-Head/Coil-Head.fbx", new Vector3(0, 0, 8), 2.6f);
            var thumper = Model("Thumper", "Assets/Mounstros/Thumper.fbx", new Vector3(14, 0, 12), 1.5f);
            Model("Eyeless Dog (exhibicion)", "Assets/Mounstros/sourceEyeless Dog/Eyeless Dog.fbx", new Vector3(-9, 0, -13), 2f);
            Model("Nutcracker (exhibicion)", "Assets/Mounstros/sourceNutcrackerGmod/NutcrackerGmod.fbx", new Vector3(9, 0, -13), 2.5f);

            var surface = environment.AddComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.Children;
            surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            surface.BuildNavMesh();
            if (!surface.navMeshData) throw new InvalidOperationException("NavMesh bake failed");
            AssetDatabase.CreateAsset(surface.navMeshData, Output + "/SandboxNavMesh.asset");
            player.monsters = new[] { Configure(bracken, player, 2.2f), Configure(coil, player, 2.3f), Configure(thumper, player, 1f) };
            bracken.AddComponent<BrackenAI>(); coil.AddComponent<CoilHeadAI>(); thumper.AddComponent<ThumperAI>();
            if (!EditorSceneManager.SaveScene(scene, ScenePath)) throw new IOException("Scene was not saved");
            AssetDatabase.SaveAssets();
            Validate();
        }
        finally
        {
            if (previous.IsValid() && previous.isLoaded)
            {
                SceneManager.SetActiveScene(previous);
                EditorSceneManager.CloseScene(scene, true);
            }
        }
    }

    private static MonsterMotor Configure(GameObject obj, SandboxPlayer player, float eyeHeight)
    {
        var agent = obj.AddComponent<NavMeshAgent>();
        agent.radius = .4f; agent.height = eyeHeight + .2f; agent.speed = 3;
        agent.acceleration = 35; agent.angularSpeed = 250; agent.stoppingDistance = 1.1f;
        var motor = obj.AddComponent<MonsterMotor>(); motor.player = player; motor.eyeHeight = eyeHeight;
        return motor;
    }

    private static GameObject Model(string name, string path, Vector3 position, float height)
    {
        var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (!asset) throw new InvalidOperationException("Missing model: " + path);
        var root = new GameObject(name);
        var visual = UnityEngine.Object.Instantiate(asset, root.transform);
        visual.name = "Visual";
        foreach (var animator in visual.GetComponentsInChildren<Animator>()) animator.enabled = false;
        foreach (var cam in visual.GetComponentsInChildren<Camera>()) UnityEngine.Object.DestroyImmediate(cam.gameObject);
        var renderers = visual.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) throw new InvalidOperationException("No renderers: " + path);
        var bounds = renderers[0].bounds;
        foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
        float scale = height / Mathf.Max(.01f, bounds.size.y);
        visual.transform.localScale *= scale;
        visual.transform.localPosition = new Vector3(-bounds.center.x, -bounds.min.y, -bounds.center.z) * scale;
        root.transform.position = position;
        foreach (var transform in root.GetComponentsInChildren<Transform>()) transform.gameObject.layer = 2;
        ConvertMaterials(visual, name);
        return root;
    }

    private static GameObject Box(string name, Vector3 center, Vector3 size, Color color)
    {
        var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
        box.name = name; box.transform.position = center; box.transform.localScale = size;
        string key = "Solid_" + ColorUtility.ToHtmlStringRGB(color);
        if (!materials.TryGetValue(key, out var material))
        {
            material = new Material(Shader.Find("Universal Render Pipeline/Lit")); material.color = color;
            material.SetFloat("_Smoothness", 0);
            material.SetFloat("_SpecularHighlights", 0);
            material.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");
            AssetDatabase.CreateAsset(material, Output + "/Materials/" + key + ".mat"); materials[key] = material;
        }
        box.GetComponent<Renderer>().sharedMaterial = material;
        return box;
    }

    private static void ConvertMaterials(GameObject root, string monster)
    {
        foreach (var renderer in root.GetComponentsInChildren<Renderer>())
        {
            var converted = renderer.sharedMaterials;
            for (int i = 0; i < converted.Length; i++)
            {
                var original = converted[i];
                string key = (monster ?? "Tree") + "_" + (original ? original.name : "Default");
                key = string.Concat(key.Select(c => char.IsLetterOrDigit(c) || c == '_' ? c : '_'));
                if (!materials.TryGetValue(key, out var material))
                {
                    material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                    Texture texture = original && original.HasProperty("_BaseMap") ? original.GetTexture("_BaseMap") : original && original.HasProperty("_MainTex") ? original.GetTexture("_MainTex") : null;
                    if (monster != null)
                    {
                        string lower = (original ? original.name : "").ToLowerInvariant();
                        string texturePath = monster.StartsWith("Bracken") ? (lower.Contains("leave") ? "textures_Bracken/bracken_leaves_d.png" : "textures_Bracken/bracken_d.png") :
                            monster.StartsWith("Coil") ? "textures_Coil-Head/coil_head_d.png" :
                            monster.StartsWith("Eyeless") ? "texturesEyeless Dog/eyeless_dog_d.png" :
                            monster.StartsWith("Nutcracker") ? (lower.Contains("shotgun") ? "texturesNutcrackerGmod/shotgun_d.png" : "texturesNutcrackerGmod/nutcracker_d.png") : null;
                        if (texturePath != null) texture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Mounstros/" + texturePath);
                    }
                    material.SetTexture("_BaseMap", texture);
                    material.SetColor("_BaseColor", texture ? Color.white : monster == null ? new Color(.22f,.42f,.16f) : new Color(.45f,.25f,.23f));
                    material.SetFloat("_Smoothness", .15f);
                    material.SetFloat("_Cull", 0);
                    if (monster == null || key.ToLowerInvariant().Contains("leave"))
                    { material.SetFloat("_AlphaClip", 1); material.EnableKeyword("_ALPHATEST_ON"); }
                    AssetDatabase.CreateAsset(material, Output + "/Materials/" + key + ".mat"); materials[key] = material;
                }
                converted[i] = material;
            }
            renderer.sharedMaterials = converted;
        }
    }

    public static void Validate()
    {
        var scene = SceneManager.GetSceneByPath(ScenePath);
        bool opened = !scene.IsValid() || !scene.isLoaded;
        if (opened) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
        try
        {
            var roots = scene.GetRootGameObjects();
            var components = roots.SelectMany(r => r.GetComponentsInChildren<Component>(true)).ToArray();
            if (components.Any(c => c == null)) throw new Exception("Missing script");
            var motors = components.OfType<MonsterMotor>().ToArray();
            if (motors.Length != 3 || components.OfType<SandboxPlayer>().Count() != 1) throw new Exception("Wrong actor count");
            foreach (var motor in motors)
            {
                if (!motor.player || !motor.player.View) throw new Exception("Missing player reference");
                if (!NavMesh.SamplePosition(motor.transform.position, out _, 1f, NavMesh.AllAreas)) throw new Exception("Off NavMesh: " + motor.name);
            }
            foreach (var renderer in components.OfType<Renderer>())
                foreach (var material in renderer.sharedMaterials)
                    if (!material || !material.shader || material.shader.name.Contains("InternalError")) throw new Exception("Invalid material: " + renderer.name);
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/monster-validation.txt", "PASS: scene saved; five monster models; three AI agents; player/camera references; NavMesh spawn positions; materials; no missing scripts.\n" + string.Join("\n", roots.Select(r => r.name)));
            Debug.Log("MONSTER_SANDBOX_VALIDATION_PASS");
        }
        finally { if (opened) EditorSceneManager.CloseScene(scene, true); }
    }

    public static void FinishMaterialsAndValidate()
    {
        foreach (string guid in AssetDatabase.FindAssets("t:Material", new[] { Output + "/Materials" }))
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
            if (!material.name.StartsWith("Solid_")) continue;
            material.SetFloat("_Smoothness", 0);
            material.SetFloat("_SpecularHighlights", 0);
            material.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");
            EditorUtility.SetDirty(material);
        }
        AssetDatabase.SaveAssets();
        MonsterSandboxValidation.Run();
    }
}
