using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// เครื่องมือเปลี่ยนไฟล์เสียงของ AudioSource ทีละหลายอันในฉากที่เปิดอยู่
// เปิดได้จากเมนู Tools > Replace Audio Clip (กด Ctrl+Z ย้อนกลับได้)
public class ReplaceAudioClipTool : EditorWindow
{
    AudioClip fromClip;
    AudioClip toClip;
    string filter = "toilet_cubicle";

    [MenuItem("Tools/Replace Audio Clip")]
    static void Open()
    {
        var w = GetWindow<ReplaceAudioClipTool>("Replace Audio Clip");
        w.fromClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sound_1/open_door_B.mp3");
        w.toClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sound_1/open_door_B_2.MP3");
    }

    void OnGUI()
    {
        fromClip = (AudioClip)EditorGUILayout.ObjectField("เสียงเดิม (From)", fromClip, typeof(AudioClip), false);
        toClip = (AudioClip)EditorGUILayout.ObjectField("เสียงใหม่ (To)", toClip, typeof(AudioClip), false);
        filter = EditorGUILayout.TextField(new GUIContent("กรองชื่อ (Filter)",
            "เปลี่ยนเฉพาะวัตถุที่ชื่อ หรือชื่อ Prefab ต้นทาง มีคำนี้อยู่ (เว้นว่าง = ทุกอันในฉาก)"), filter);

        var matches = FindMatches();
        EditorGUILayout.HelpBox($"พบ AudioSource ที่ตรงเงื่อนไข: {matches.Length} อัน", MessageType.Info);

        GUI.enabled = fromClip != null && toClip != null && matches.Length > 0;
        if (GUILayout.Button("Replace"))
        {
            Undo.RecordObjects(matches, "Replace Audio Clip");
            foreach (var src in matches)
            {
                src.clip = toClip;
                PrefabUtility.RecordPrefabInstancePropertyModifications(src);
            }
            EditorSceneManager.MarkAllScenesDirty();
            Debug.Log($"[ReplaceAudioClip] เปลี่ยน {matches.Length} อัน: {fromClip.name} -> {toClip.name}");
        }
        GUI.enabled = true;
    }

    AudioSource[] FindMatches()
    {
        if (fromClip == null) return new AudioSource[0];
        var all = FindObjectsByType<AudioSource>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        var list = new System.Collections.Generic.List<AudioSource>();
        foreach (var src in all)
        {
            if (src.clip != fromClip) continue;
            if (!string.IsNullOrEmpty(filter))
            {
                string prefabPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(src.gameObject) ?? "";
                bool hit = src.gameObject.name.Contains(filter) || prefabPath.Contains(filter);
                if (!hit) continue;
            }
            list.Add(src);
        }
        return list.ToArray();
    }
}
