using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// ลดขนาด texture เฉพาะตอน build WebGL (ตั้งค่า override ของแพลตฟอร์ม WebGL)
// - ไฟล์รูปต้นฉบับไม่ถูกแก้ ใช้กับแพลตฟอร์มอื่นได้ตามเดิม
// - ย้อนกลับได้ด้วยเมนู Tools > Restore Web Texture Sizes
// กฎ: ผี (DemonDoll) 2048 | ตัวผู้เล่น (Robot) 512 | อื่นๆ 1024 | ไม่แตะ UI/ฟอนต์/Lightmap
public static class WebTextureOptimizer
{
    const string ScenePath = "Assets/Scenes/SampleScene.unity";
    const string Platform = "WebGL";
    const string Tag = "SLY_WebOpt"; // ป้ายบอกว่าเครื่องมือนี้เป็นคนตั้ง (ใช้ตอนย้อนกลับ)

    static int CapFor(string path, TextureImporter ti)
    {
        if (ti.textureType == TextureImporterType.Sprite || ti.textureType == TextureImporterType.GUI ||
            ti.textureType == TextureImporterType.Cursor || ti.textureType == TextureImporterType.SingleChannel)
            return 0;                                         // UI / พิเศษ: ไม่แตะ
        if (path.Contains("/DemonDoll/")) return 2048;        // ผี: เห็นใกล้ๆ ตอนหลอก ต้องชัด
        if (path.Contains("/Robot_Character/")) return 512;   // ตัวผู้เล่น: แทบมองไม่เห็น
        return 1024;                                          // ฉาก/ของทั่วไป
    }

    [MenuItem("Tools/Optimize Textures for Web")]
    static void RunMenu()
    {
        if (!EditorUtility.DisplayDialog("Optimize Textures for Web",
            "ลดขนาด texture ที่ใช้ในฉาก เฉพาะตอน build WebGL\n\n" +
            "• ผี 2048 • ตัวผู้เล่น 512 • อื่นๆ 1024\n" +
            "• ไฟล์รูปต้นฉบับไม่ถูกแก้\n" +
            "• ย้อนกลับได้ที่ Tools > Restore Web Texture Sizes\n\n" +
            "อาจใช้เวลาหลายนาที (Unity ต้อง import texture ใหม่)", "เริ่ม", "ยกเลิก"))
            return;
        Run();
    }

    // เรียกจาก command line ได้: -executeMethod WebTextureOptimizer.Run
    public static void Run()
    {
        var changed = new List<string>();
        AssetDatabase.StartAssetEditing();
        try
        {
            foreach (string path in AssetDatabase.GetDependencies(ScenePath, true))
            {
                if (path.StartsWith("Assets/Scenes/") || path.Contains("/TextMesh Pro/") || path.StartsWith("Packages/")) continue;
                var ti = AssetImporter.GetAtPath(path) as TextureImporter;
                if (ti == null) continue;

                int cap = CapFor(path, ti);
                if (cap <= 0) continue;

                var ps = ti.GetPlatformTextureSettings(Platform);
                int current = ps.overridden ? ps.maxTextureSize : ti.maxTextureSize;
                if (current <= cap) continue;

                // จำค่าเดิมไว้ย้อนกลับ
                if (!HasTag(ti)) ti.userData = AddTag(ti.userData, ps.overridden ? current.ToString() : "none");

                if (!ps.overridden)
                {
                    ps.overridden = true;
                    ps.format = TextureImporterFormat.Automatic;
                    ps.textureCompression = TextureImporterCompression.Compressed;
                }
                ps.maxTextureSize = cap;
                ti.SetPlatformTextureSettings(ps);
                ti.SaveAndReimport();
                changed.Add($"{current} -> {cap}  {path}");
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
        }

        Debug.Log($"[WebTextureOptimizer] ลดขนาด {changed.Count} texture\n" + string.Join("\n", changed));
        if (!Application.isBatchMode)
            EditorUtility.DisplayDialog("Optimize Textures for Web", $"เสร็จแล้ว: ลดขนาด {changed.Count} texture\n(ดูรายชื่อใน Console)", "OK");
    }

    [MenuItem("Tools/Restore Web Texture Sizes")]
    static void Restore()
    {
        int n = 0;
        AssetDatabase.StartAssetEditing();
        try
        {
            foreach (string guid in AssetDatabase.FindAssets("t:Texture"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var ti = AssetImporter.GetAtPath(path) as TextureImporter;
                if (ti == null || !HasTag(ti)) continue;

                string prev = GetTag(ti.userData);
                var ps = ti.GetPlatformTextureSettings(Platform);
                if (prev == "none") ps.overridden = false;
                else if (int.TryParse(prev, out int size)) ps.maxTextureSize = size;
                ti.SetPlatformTextureSettings(ps);
                ti.userData = RemoveTag(ti.userData);
                ti.SaveAndReimport();
                n++;
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
        }
        Debug.Log($"[WebTextureOptimizer] คืนค่าเดิม {n} texture");
        EditorUtility.DisplayDialog("Restore Web Texture Sizes", $"คืนค่าเดิม {n} texture แล้ว", "OK");
    }

    // ---- ป้ายใน userData: "SLY_WebOpt=<ค่าเดิม>" ----
    static bool HasTag(TextureImporter ti) => ti.userData != null && ti.userData.Contains(Tag + "=");
    static string AddTag(string data, string prev) => (string.IsNullOrEmpty(data) ? "" : data + ";") + Tag + "=" + prev;
    static string GetTag(string data)
    {
        foreach (var part in data.Split(';'))
            if (part.StartsWith(Tag + "=")) return part.Substring(Tag.Length + 1);
        return "none";
    }
    static string RemoveTag(string data)
    {
        var keep = new List<string>();
        foreach (var part in data.Split(';'))
            if (!part.StartsWith(Tag + "=") && part.Length > 0) keep.Add(part);
        return string.Join(";", keep);
    }
}
