using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// เครื่องมือตั้ง Spatial Blend ให้ AudioSource หลายอันพร้อมกันในฉากที่เปิดอยู่
// เปิดได้จากเมนู Tools > Spatial Blend Fixer (กด Ctrl+Z ย้อนกลับได้)
public class SpatialBlendTool : EditorWindow
{
    float targetBlend = 1f;          // 1 = 3D เต็มที่
    bool setRolloff = false;
    AudioRolloffMode rolloff = AudioRolloffMode.Logarithmic;
    float minDistance = 1f;
    float maxDistance = 20f;

    List<AudioSource> sources = new List<AudioSource>();
    Dictionary<AudioSource, bool> selected = new Dictionary<AudioSource, bool>();
    Vector2 scroll;

    [MenuItem("Tools/Spatial Blend Fixer")]
    static void Open()
    {
        GetWindow<SpatialBlendTool>("Spatial Blend Fixer").Refresh();
    }

    void OnFocus() { Refresh(); }
    void OnHierarchyChange() { Refresh(); Repaint(); }

    void Refresh()
    {
        sources.Clear();
        foreach (var s in FindObjectsByType<AudioSource>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (EditorUtility.IsPersistent(s)) continue;
            if (s.spatialBlend < 1f) sources.Add(s);
        }
        sources.Sort((a, b) => string.Compare(GetPath(a.transform), GetPath(b.transform), System.StringComparison.Ordinal));

        var old = selected;
        selected = new Dictionary<AudioSource, bool>();
        foreach (var s in sources)
            selected[s] = old.TryGetValue(s, out bool v) ? v : true;
    }

    void OnGUI()
    {
        EditorGUILayout.LabelField("ตั้งค่าที่จะใช้", EditorStyles.boldLabel);
        targetBlend = EditorGUILayout.Slider(new GUIContent("Spatial Blend", "0 = 2D, 1 = 3D"), targetBlend, 0f, 1f);
        setRolloff = EditorGUILayout.Toggle(new GUIContent("ตั้งระยะเสียงด้วย", "ตั้ง Rolloff / Min / Max Distance ไปพร้อมกัน"), setRolloff);
        if (setRolloff)
        {
            EditorGUI.indentLevel++;
            rolloff = (AudioRolloffMode)EditorGUILayout.EnumPopup("Volume Rolloff", rolloff);
            minDistance = EditorGUILayout.FloatField("Min Distance", minDistance);
            maxDistance = EditorGUILayout.FloatField("Max Distance", maxDistance);
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField($"AudioSource ที่ยังไม่เป็น 3D: {sources.Count} อัน (ติ๊กออกอันที่ควรเป็น 2D เช่น เพลงพื้นหลัง)", EditorStyles.wordWrappedLabel);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("เลือกทั้งหมด")) SetAll(true);
        if (GUILayout.Button("ไม่เลือกเลย")) SetAll(false);
        if (GUILayout.Button("รีเฟรช")) Refresh();
        EditorGUILayout.EndHorizontal();

        scroll = EditorGUILayout.BeginScrollView(scroll);
        foreach (var s in sources)
        {
            if (s == null) continue;
            EditorGUILayout.BeginHorizontal();
            selected[s] = EditorGUILayout.Toggle(selected[s], GUILayout.Width(18));
            string clipName = s.clip != null ? s.clip.name : "(ไม่มีเสียง)";
            EditorGUILayout.LabelField($"{GetPath(s.transform)}", GUILayout.MinWidth(150));
            EditorGUILayout.LabelField($"{clipName}  [{s.spatialBlend:0.##}]", GUILayout.Width(200));
            if (GUILayout.Button("ดู", GUILayout.Width(36)))
            {
                Selection.activeGameObject = s.gameObject;
                EditorGUIUtility.PingObject(s.gameObject);
            }
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndScrollView();

        int count = 0;
        foreach (var kv in selected) if (kv.Value && kv.Key != null) count++;

        GUI.enabled = count > 0;
        if (GUILayout.Button($"Apply กับ {count} อันที่เลือก", GUILayout.Height(28)))
            Apply();
        GUI.enabled = true;
    }

    void SetAll(bool v)
    {
        foreach (var s in sources) selected[s] = v;
    }

    void Apply()
    {
        var targets = new List<AudioSource>();
        foreach (var kv in selected) if (kv.Value && kv.Key != null) targets.Add(kv.Key);

        Undo.RecordObjects(targets.ToArray(), "Set Spatial Blend");
        foreach (var s in targets)
        {
            s.spatialBlend = targetBlend;
            if (setRolloff)
            {
                s.rolloffMode = rolloff;
                s.minDistance = minDistance;
                s.maxDistance = maxDistance;
            }
            PrefabUtility.RecordPrefabInstancePropertyModifications(s);
        }
        EditorSceneManager.MarkAllScenesDirty();
        Debug.Log($"[SpatialBlendFixer] ตั้ง Spatial Blend = {targetBlend} ให้ {targets.Count} อัน");
        Refresh();
    }

    static string GetPath(Transform t)
    {
        string p = t.name;
        while (t.parent != null) { t = t.parent; p = t.name + "/" + p; }
        return p;
    }
}
