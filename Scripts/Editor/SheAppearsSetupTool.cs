using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// เมนู Tools > Setup She Appears : สร้างระบบ "เธอ" โผล่ข้างหลัง พร้อมใส่โมเดลและเสียงให้อัตโนมัติ
public static class SheAppearsSetupTool
{
    const string Sfx = "Assets/Sound _Effects/";

    [MenuItem("Tools/Setup She Appears")]
    static void Setup()
    {
        var she = Object.FindFirstObjectByType<SheAppears>(FindObjectsInactive.Include);
        if (she == null)
        {
            var go = new GameObject("SheAppears");
            Undo.RegisterCreatedObjectUndo(go, "Create SheAppears");
            she = Undo.AddComponent<SheAppears>(go);
        }
        else
        {
            if (!EditorUtility.DisplayDialog("Setup She Appears", "มี SheAppears อยู่แล้ว ต้องการใส่โมเดลและเสียงชุดเริ่มต้นใหม่ทับหรือไม่?", "ใส่ใหม่", "ยกเลิก"))
                return;
            Undo.RecordObject(she, "Setup She Appears");
        }

        she.ghostPrefab = Load<GameObject>("Assets/DemonDoll/Prefab/SKM_DemonDoll_Var1 (1).prefab");
        she.poseController = Load<RuntimeAnimatorController>("Assets/Animations/GhostWalkController.controller");
        she.callSounds = new[]
        {
            Load<AudioClip>(Sfx + "freesound_community-nervous-abrupt-heh-103134.mp3"),
            Load<AudioClip>(Sfx + "lexieva-girl-laugh-149491.mp3"),
        };
        she.vanishSound = Load<AudioClip>(Sfx + "universfield-eerie-sudden-shock-454675.mp3");

        EditorUtility.SetDirty(she);
        EditorSceneManager.MarkSceneDirty(she.gameObject.scene);
        Selection.activeGameObject = she.gameObject;
        Debug.Log("[SheAppears] ตั้งค่าเสร็จแล้ว อย่าลืมกด Ctrl+S เพื่อเซฟฉาก");
    }

    static T Load<T>(string path) where T : Object
    {
        var a = AssetDatabase.LoadAssetAtPath<T>(path);
        if (a == null) Debug.LogWarning("[SheAppears] ไม่พบไฟล์: " + path);
        return a;
    }
}
