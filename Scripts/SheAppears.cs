using UnityEngine;

// "เธอ" โผล่มาหมอบอยู่ข้างหลังผู้เล่นแบบสุ่ม จ้องมอง แล้วหายไปทันทีที่ผู้เล่นหันมาเห็น
// - มีเสียงเรียกเบาๆ จากตำแหน่งของเธอ ให้ผู้เล่นหันไปมอง
// - พอเห็น: จอมืดวูบ เธอหายไป + เสียงกระตุก
// - ถ้าผู้เล่นไม่หันมา สักพักเธอจะหายไปเงียบๆ (ผู้เล่นไม่รู้ด้วยซ้ำว่าเธอเคยอยู่ตรงนั้น)
// สร้างได้จากเมนู Tools > Setup She Appears
public class SheAppears : MonoBehaviour
{
    [Header("โมเดล")]
    public GameObject ghostPrefab;                   // DemonDoll
    public RuntimeAnimatorController poseController; // ใช้ท่าจากแอนิเมชันคลาน แล้วหยุดค้างไว้
    public string poseState = "Running Crawl";
    [Range(0f, 1f)] public float poseFrame = 0.3f;   // หยุดที่ช่วงไหนของท่า (ลองปรับให้ได้ท่าที่หลอนที่สุด)

    [Header("เมื่อไหร่จะโผล่")]
    [Range(0f, 1f)] public float minProgress = 0.1f; // เริ่มโผล่เมื่อเก็บของถึงสัดส่วนนี้
    public float firstDelay = 60f;                   // ครั้งแรกหลังเริ่มเกมอย่างน้อยกี่วินาที
    public Vector2 intervalStart = new Vector2(90f, 150f); // เว้นระยะ (วินาที) ตอนต้นเกม
    public Vector2 intervalEnd = new Vector2(40f, 70f);    // เว้นระยะ (วินาที) ตอนเก็บของเกือบครบ
    public int maxAppearances = 6;                   // โผล่สูงสุดกี่ครั้งต่อเกม (0 = ไม่จำกัด)

    [Header("ตำแหน่ง")]
    public Vector2 distance = new Vector2(3.5f, 7f); // ห่างจากผู้เล่น (เมตร)
    [Range(10f, 90f)] public float behindAngle = 50f; // กระจายซ้าย-ขวาจากด้านหลังกี่องศา
    public LayerMask obstacleMask = ~0;

    [Header("หายไปเมื่อ")]
    [Range(5f, 45f)] public float lookAngle = 18f;   // ผู้เล่นหันมามองใกล้แค่ไหนถึงนับว่า "เห็น"
    public float vanishDelay = 0.25f;                // เห็นแล้วค้างไว้ให้ตกใจกี่วินาที
    public float tooClose = 2f;                      // เดินเข้าใกล้เกินนี้ = หาย
    public float maxLifetime = 12f;                  // ไม่มีใครเห็น = หายเงียบๆ หลังกี่วินาที

    [Header("เสียง")]
    public AudioClip[] callSounds;                   // เสียงเรียกเบาๆ จากตัวเธอ
    [Range(0f, 1f)] public float callVolume = 0.6f;
    public float callDelay = 1.5f;
    public AudioClip vanishSound;                    // เสียงตอนหาย
    [Range(0f, 1f)] public float vanishVolume = 0.7f;
    [Range(0f, 2f)] public float vanishScare = 0.35f; // จอสั่น/ขอบจอแดงตอนหาย

    GameObject ghost;
    CharacterController playerBody;
    float timer;
    float aliveTime, seenTime;
    bool called, seen;
    int count;

    void Start() { timer = firstDelay; }

    void Update()
    {
        GameManager gm = GameManager.instance;
        if (gm == null || !gm.IsPlaying) return;
        bool quiet = AmbienceManager.instance != null && AmbienceManager.instance.IsQuiet;

        if (ghost != null)
        {
            if (quiet) { Remove(); return; } // มีผีตัวอื่นกำลังหลอกอยู่ ไม่ซ้อนกัน
            UpdateShowing();
            return;
        }

        if (quiet || gm.Progress < minProgress) return;
        if (maxAppearances > 0 && count >= maxAppearances) return;

        timer -= Time.deltaTime;
        if (timer > 0f) return;

        float p = gm.Progress;
        timer = Random.Range(Mathf.Lerp(intervalStart.x, intervalEnd.x, p), Mathf.Lerp(intervalStart.y, intervalEnd.y, p));
        if (!TrySpawn()) timer = 8f; // หาที่ไม่ได้ ลองใหม่เร็วๆ
    }

    void UpdateShowing()
    {
        Camera cam = Camera.main;
        if (cam == null) { Remove(); return; }
        aliveTime += Time.deltaTime;

        Vector3 head = ghost.transform.position + Vector3.up * 0.5f;
        Vector3 toGhost = head - cam.transform.position;

        // เสียงเรียกเบาๆ ให้หันมา
        if (!called && aliveTime >= callDelay)
        {
            called = true;
            if (callSounds != null && callSounds.Length > 0)
            {
                var clip = callSounds[Random.Range(0, callSounds.Length)];
                if (clip != null) PlayAt(clip, head, callVolume, 1f);
            }
        }

        // ผู้เล่นหันมาเห็น / เดินเข้าใกล้เกินไป
        if (!seen)
        {
            bool looking = Vector3.Angle(cam.transform.forward, toGhost) < lookAngle && !Blocked(cam.transform.position, head);
            if (looking || toGhost.magnitude < tooClose) { seen = true; seenTime = 0f; }
        }

        if (seen)
        {
            seenTime += Time.deltaTime;
            if (seenTime >= vanishDelay) Vanish();
        }
        else if (aliveTime > maxLifetime)
        {
            Remove(); // ไม่มีใครเห็น หายไปเงียบๆ
        }
    }

    bool TrySpawn()
    {
        Camera cam = Camera.main;
        if (cam == null || ghostPrefab == null) return false;

        Vector3 eye = cam.transform.position;
        Vector3 back = -cam.transform.forward; back.y = 0f;
        if (back.sqrMagnitude < 0.01f) return false;
        back.Normalize();
        if (playerBody == null) playerBody = FindFirstObjectByType<CharacterController>();
        float feetY = playerBody != null ? playerBody.transform.position.y : eye.y - 1.6f; // ระดับพื้นที่ผู้เล่นยืน

        for (int i = 0; i < 12; i++)
        {
            Vector3 dir = Quaternion.Euler(0f, Random.Range(-behindAngle, behindAngle), 0f) * back;
            Vector3 point = eye + dir * Random.Range(distance.x, distance.y);

            // ต้องมองเห็นได้จากตำแหน่งผู้เล่น (ไม่มีผนังกั้น) เวลาหันกลับไป
            if (Blocked(eye, point)) continue;

            // หาพื้น และต้องอยู่ชั้นเดียวกับผู้เล่น
            if (!Physics.Raycast(point, Vector3.down, out RaycastHit floor, 3f, obstacleMask, QueryTriggerInteraction.Ignore)) continue;
            if (Mathf.Abs(floor.point.y - feetY) > 0.5f) continue; // กันไปโผล่บนโต๊ะ/ชั้นอื่น

            // ต้องมีที่ว่างพอ (ไม่จมในโต๊ะ/ผนัง)
            Vector3 f = floor.point;
            if (Physics.CheckCapsule(f + Vector3.up * 0.45f, f + Vector3.up * 1.2f, 0.3f, obstacleMask, QueryTriggerInteraction.Ignore)) continue;

            // ต้องอยู่นอกสายตาตอนโผล่
            if (Vector3.Angle(cam.transform.forward, f - eye) < 75f) continue;

            Spawn(f, eye);
            return true;
        }
        return false;
    }

    void Spawn(Vector3 pos, Vector3 lookAt)
    {
        Vector3 face = lookAt - pos; face.y = 0f;
        ghost = Instantiate(ghostPrefab, pos, Quaternion.LookRotation(face));
        ghost.name = "SheAppears_Ghost";

        // เอาไว้ให้เห็นอย่างเดียว: ตัดการชน สคริปต์ ฟิสิกส์ และเสียงเดิมออก
        foreach (var c in ghost.GetComponentsInChildren<Collider>(true)) Destroy(c);
        foreach (var m in ghost.GetComponentsInChildren<MonoBehaviour>(true)) Destroy(m);
        foreach (var r in ghost.GetComponentsInChildren<Rigidbody>(true)) Destroy(r);
        foreach (var a in ghost.GetComponentsInChildren<AudioSource>(true)) Destroy(a);

        // ท่าหมอบค้าง (ใช้เฟรมหนึ่งของท่าคลานแล้วหยุดไว้)
        var anim = ghost.GetComponentInChildren<Animator>();
        if (anim != null)
        {
            if (poseController != null) anim.runtimeAnimatorController = poseController;
            anim.applyRootMotion = false;
            if (anim.runtimeAnimatorController != null)
            {
                anim.Play(poseState, 0, poseFrame);
                anim.Update(0f);
                anim.speed = 0f;
            }
        }

        aliveTime = 0f; called = false; seen = false;
        count++;
    }

    void Vanish()
    {
        if (ghost == null) return;
        ScareFX.ScreenFlicker(0.35f, 0.95f);
        if (vanishScare > 0f) ScareFX.Hit(vanishScare);
        if (vanishSound != null) PlayAt(vanishSound, Camera.main != null ? Camera.main.transform.position : ghost.transform.position, vanishVolume, 0f);
        ScareFX.Quiet(4f);
        Destroy(ghost, 0.06f); // หายตอนจอมืดพอดี
        ghost = null;
    }

    void Remove()
    {
        if (ghost != null) Destroy(ghost);
        ghost = null;
    }

    // มีอะไรบังระหว่างสองจุดไหม (ไม่นับตัวผู้เล่นเอง)
    bool Blocked(Vector3 from, Vector3 to)
    {
        Vector3 d = to - from;
        foreach (var h in Physics.RaycastAll(from, d.normalized, d.magnitude, obstacleMask, QueryTriggerInteraction.Ignore))
        {
            if (h.collider.GetComponentInParent<CharacterController>() != null) continue;
            if (ghost != null && h.collider.transform.IsChildOf(ghost.transform)) continue;
            return true;
        }
        return false;
    }

    static void PlayAt(AudioClip clip, Vector3 pos, float volume, float spatial)
    {
        var go = new GameObject("SheAppears_SFX");
        go.transform.position = pos;
        var src = go.AddComponent<AudioSource>();
        src.clip = clip;
        src.volume = volume;
        src.spatialBlend = spatial;
        src.rolloffMode = AudioRolloffMode.Linear;
        src.minDistance = 1f;
        src.maxDistance = 20f;
        src.dopplerLevel = 0f;
        src.Play();
        Destroy(go, clip.length + 0.2f);
    }
}
