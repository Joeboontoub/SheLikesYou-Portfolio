using System.Collections.Generic;
using UnityEngine;

// ระบบเสียงบรรยากาศ:
// 1) เสียงหลอนวนเบาๆ ตลอดเกม
// 2) สุ่มเสียงเล็กๆ ดังจากทิศรอบตัวผู้เล่น (ใส่หูฟังจะได้ยินเหมือนมีอะไรอยู่ข้างหลัง)
// 3) ยิ่งเก็บของได้มาก เสียงยิ่งดัง ยิ่งถี่ เสียงที่น่ากลัวกว่าเริ่มโผล่ และจอมืดวูบบ่อยขึ้น
// สร้างได้จากเมนู Tools > Setup Ambience (ใส่ไฟล์เสียงให้อัตโนมัติ)
public class AmbienceManager : MonoBehaviour
{
    [System.Serializable]
    public class RandomSound
    {
        public AudioClip clip;
        [Range(0f, 1f)] public float volume = 0.7f;
        [Tooltip("เริ่มสุ่มได้เมื่อเก็บของถึงสัดส่วนนี้ (0 = ตั้งแต่แรก, 0.5 = เก็บได้ครึ่งหนึ่ง)")]
        [Range(0f, 1f)] public float minProgress = 0f;
        [Tooltip("ติ๊ก = เสียงดังมาจากทิศรอบตัว / ไม่ติ๊ก = ดังในหัวทั้งสองข้าง (เหมาะกับเสียงดนตรีหลอน)")]
        public bool play3D = true;
        [Tooltip("ติ๊ก = จอมืดวูบทุกครั้งที่เสียงนี้ดัง (เช่น เสียงไฟฟ้าช็อต)")]
        public bool alwaysFlicker = false;
    }

    [Header("เสียงบรรยากาศวนตลอดเกม")]
    public AudioClip ambientLoop;
    [Range(0f, 1f)] public float loopVolumeStart = 0.15f; // ความดังตอนเริ่มเกม
    [Range(0f, 1f)] public float loopVolumeEnd = 0.45f;   // ความดังตอนเก็บของเกือบครบ
    public bool playOnMenu = true;                        // เปิดเสียงบรรยากาศตั้งแต่หน้าเมนู

    [Header("เสียงสุ่มรอบตัวผู้เล่น")]
    public List<RandomSound> randomSounds = new List<RandomSound>();
    public float firstSoundDelay = 20f;                   // เสียงแรกดังหลังเริ่มเล่นกี่วินาที
    [Tooltip("ช่วงเวลาสุ่มระหว่างเสียง (วินาที) ตอนเริ่มเกม")]
    public Vector2 intervalStart = new Vector2(35f, 60f);
    [Tooltip("ช่วงเวลาสุ่มระหว่างเสียง (วินาที) ตอนเก็บของเกือบครบ")]
    public Vector2 intervalEnd = new Vector2(12f, 25f);
    public Vector2 distance = new Vector2(4f, 10f);       // ระยะห่างจากผู้เล่น (เมตร)
    [Range(0f, 1f)] public float behindChance = 0.6f;     // โอกาสที่เสียงจะดังมาจากด้านหลัง

    [Header("ไฟดับวูบ (จอมืดวูบ เพราะไฟในฉากเป็นแบบ Baked)")]
    [Tooltip("โอกาสที่จอจะมืดวูบพร้อมเสียงสุ่ม: ตอนเริ่มเกม / ตอนเก็บของเกือบครบ")]
    public Vector2 flickerChance = new Vector2(0.15f, 0.5f);
    [Tooltip("ความมืดตอนวูบ: ตอนเริ่มเกม / ตอนเก็บของเกือบครบ (1 = มืดสนิท)")]
    public Vector2 flickerStrength = new Vector2(0.5f, 0.85f);
    public Vector2 flickerDuration = new Vector2(0.4f, 1.2f); // วูบนานกี่วินาที (สุ่มระหว่าง)

    [Header("ช่วงเงียบ (ก่อน/ระหว่างผีโผล่)")]
    [Range(0f, 1f)] public float quietLoopFactor = 0.15f; // เสียงวนเหลือกี่ส่วนตอนเงียบ
    public float afterQuietDelay = 15f;                  // หลังผีหายไป รออย่างน้อยกี่วินาทีก่อนสุ่มเสียงต่อ

    public static AmbienceManager instance;
    int quietCount;
    float quietUntil;
    public bool IsQuiet => quietCount > 0 || Time.time < quietUntil;

    public void QuietFor(float seconds) { quietUntil = Mathf.Max(quietUntil, Time.time + seconds); }
    public void PushQuiet() { quietCount++; }
    public void PopQuiet() { quietCount = Mathf.Max(0, quietCount - 1); }

    void Awake() { instance = this; }
    void OnDestroy() { if (instance == this) instance = null; }

    AudioSource loopSource;
    float timer;
    AudioClip lastClip;

    void Start()
    {
        timer = firstSoundDelay;

        loopSource = gameObject.AddComponent<AudioSource>();
        loopSource.clip = ambientLoop;
        loopSource.loop = true;
        loopSource.spatialBlend = 0f;
        loopSource.playOnAwake = false;
        loopSource.volume = 0f; // ค่อยๆ ดังขึ้น
        if (ambientLoop != null && playOnMenu) loopSource.Play();
    }

    void Update()
    {
        GameManager gm = GameManager.instance;
        if (gm == null) return;
        float p = gm.Progress;

        // --- เสียงวน: ค่อยๆ ปรับความดังตามความคืบหน้า และเงียบลงตอนฉากจบ ---
        if (ambientLoop != null)
        {
            if (!loopSource.isPlaying && gm.IsPlaying) loopSource.Play();
            float target = gm.IsEnding ? 0f : Mathf.Lerp(loopVolumeStart, loopVolumeEnd, p);
            if (IsQuiet) target *= quietLoopFactor;
            float speed = IsQuiet ? 0.6f : 0.1f; // เงียบลงเร็ว แต่กลับมาช้าๆ
            loopSource.volume = Mathf.MoveTowards(loopSource.volume, target, Time.unscaledDeltaTime * speed);
        }

        // --- เสียงสุ่ม: เฉพาะตอนกำลังเล่น (ไม่ดังตอนอยู่เมนู/หยุดเกม/ฉากจบ) ---
        if (!gm.IsPlaying) return;
        if (IsQuiet) { timer = Mathf.Max(timer, afterQuietDelay); return; } // ไม่สุ่มเสียงทับตอนผีโผล่
        timer -= Time.deltaTime;
        if (timer > 0f) return;

        PlayRandomSound(p);
        timer = Random.Range(Mathf.Lerp(intervalStart.x, intervalEnd.x, p), Mathf.Lerp(intervalStart.y, intervalEnd.y, p));
    }

    void PlayRandomSound(float progress)
    {
        var candidates = new List<RandomSound>();
        foreach (var s in randomSounds)
            if (s.clip != null && progress >= s.minProgress) candidates.Add(s);
        if (candidates.Count == 0) return;
        if (candidates.Count > 1) candidates.RemoveAll(s => s.clip == lastClip); // ไม่เล่นเสียงเดิมซ้ำติดกัน

        RandomSound pick = candidates[Random.Range(0, candidates.Count)];
        lastClip = pick.clip;

        Transform listener = Camera.main != null ? Camera.main.transform : null;
        Vector3 pos = listener != null ? listener.position : transform.position;

        if (pick.play3D && listener != null)
        {
            // เลือกทิศ: ส่วนใหญ่มาจากด้านหลังผู้เล่น
            Vector3 fwd = listener.forward; fwd.y = 0f;
            if (fwd.sqrMagnitude < 0.01f) fwd = Vector3.forward;
            fwd.Normalize();
            Vector3 dir = Random.value < behindChance
                ? Quaternion.Euler(0f, Random.Range(-60f, 60f), 0f) * -fwd
                : Quaternion.Euler(0f, Random.Range(0f, 360f), 0f) * fwd;
            pos += dir * Random.Range(distance.x, distance.y) + Vector3.up * Random.Range(-0.3f, 1.5f);
        }

        var go = new GameObject("AmbientSFX_" + pick.clip.name);
        go.transform.position = pos;
        var src = go.AddComponent<AudioSource>();
        src.clip = pick.clip;
        src.volume = pick.volume;
        src.spatialBlend = pick.play3D ? 1f : 0f;
        src.rolloffMode = AudioRolloffMode.Linear;
        src.minDistance = 1f;
        src.maxDistance = 25f;
        src.dopplerLevel = 0f;
        src.pitch = Random.Range(0.92f, 1.05f);
        src.Play();
        Destroy(go, pick.clip.length / src.pitch + 0.2f);

        // จอมืดวูบเหมือนไฟจะดับ (ยิ่งเก็บของมาก ยิ่งบ่อยและมืดขึ้น)
        if (pick.alwaysFlicker || Random.value < Mathf.Lerp(flickerChance.x, flickerChance.y, progress))
            ScareFX.ScreenFlicker(Random.Range(flickerDuration.x, flickerDuration.y),
                                  Mathf.Lerp(flickerStrength.x, flickerStrength.y, progress));
    }
}
