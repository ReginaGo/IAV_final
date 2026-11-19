using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class MonsterAssetAudit
{
    static MonsterAssetAudit() { EditorApplication.delayCall += Audit; }
    [MenuItem("Tools/Monsters/Audit imported models")]
    public static void Audit()
    {
        if (!Application.isBatchMode && !File.Exists("Temp/monster-audit.request")) return;
        if (File.Exists("Temp/monster-audit.request")) File.Delete("Temp/monster-audit.request");
        var report = new StringBuilder();
        foreach (var guid in AssetDatabase.FindAssets("t:GameObject", new[] { "Assets/Mounstros" }))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            report.AppendLine(path + " => " + (model ? model.name : "NULL"));
            if (!model) continue;
            foreach (var t in model.GetComponentsInChildren<Transform>(true))
                report.AppendLine("  " + t.name);
        }
        Directory.CreateDirectory("Logs");
        File.WriteAllText("Logs/monster-audit.txt", report.ToString());
    }
}
