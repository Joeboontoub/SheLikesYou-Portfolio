using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// เมนู Tools > Setup Ambience : สร้าง AmbienceManager ในฉาก พร้อมใส่ไฟล์เสียงให้อัตโนมัติ
public static class AmbienceSetupTool
{
    const string Dir = "Assets/Sound _Effects/";

    [MenuItem("Tools/Setup Ambience")]
    static void Setup()
    {
        var mgr = Object.FindFirstObjectByType<AmbienceManager>(FindObjectsInactive.Include);
        if (mgr == null)
        {
            var go = new GameObject("AmbienceManager");
            Undo.RegisterCreatedObjectUndo(go, "Create AmbienceManager");
            mgr = Undo.AddComponent<AmbienceManager>(go);
        }
        else
        {
            if (!EditorUtility.DisplayDialog("Setup Ambience", "มี AmbienceManager อยู่แล้ว ต้องการใส่ไฟล์เสียงชุดเริ่มต้นใหม่ทับหรือไม่?", "ใส่ใหม่", "ยกเลิก"))
                return;
            Undo.RecordObject(mgr, "Setup Ambience");
        }

        mgr.ambientLoop = Load("fronbondi_skegs-pad-creepy-and-ghostly-horror-background-sound-effect-359406.mp3");
        mgr.randomSounds.Clear();
        //   ไฟล์                                                              ความดัง  เริ่มเมื่อ  3D
        Add(mgr, "fronbondi_skegs-perc-horror-ambient-background-knocks-sound-effect-452814.mp3", 0.7f, 0.00f, true);  // เสียงเคาะ
        Add(mgr, "freesound_community-nervous-abrupt-heh-103134.mp3",                              0.6f, 0.00f, true);  // เสียง "เฮะ" สั้นๆ
        Add(mgr, "freesound_community-electricity-101990.mp3",                                     0.4f, 0.00f, true, true); // ไฟฟ้าช็อต + จอมืดวูบทุกครั้ง
        Add(mgr, "lexieva-girl-laugh-149491.mp3",                                                  0.6f, 0.20f, true);  // เด็กผู้หญิงหัวเราะ
        Add(mgr, "scottishperson-sound-effect-woman-giggling-and-laughing-320487.mp3",             0.55f, 0.35f, true); // ผู้หญิงหัวเราะคิกคัก
        Add(mgr, "freesound_community-possessed-laugh-94851.mp3",                                  0.55f, 0.60f, true); // หัวเราะแบบถูกสิง
        Add(mgr, "liecio-terror-building-rise-and-fall-132286.mp3",                                0.45f, 0.75f, false); // ดนตรีหลอนพุ่งขึ้น

        EditorUtility.SetDirty(mgr);
        EditorSceneManager.MarkSceneDirty(mgr.gameObject.scene);
        Selection.activeGameObject = mgr.gameObject;
        Debug.Log("[Ambience] ตั้งค่าเสร็จแล้ว อย่าลืมกด Ctrl+S เพื่อเซฟฉาก");
    }

    static AudioClip Load(string file)
    {
        var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(Dir + file);
        if (clip == null) Debug.LogWarning("[Ambience] ไม่พบไฟล์เสียง: " + Dir + file);
        return clip;
    }

    static void Add(AmbienceManager mgr, string file, float volume, float minProgress, bool play3D, bool alwaysFlicker = false)
    {
        mgr.randomSounds.Add(new AmbienceManager.RandomSound
        {
            clip = Load(file),
            volume = volume,
            minProgress = minProgress,
            play3D = play3D,
            alwaysFlicker = alwaysFlicker
        });
    }
}
