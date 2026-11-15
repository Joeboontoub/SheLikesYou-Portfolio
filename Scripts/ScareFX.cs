using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

// เอฟเฟกต์ตอนผีโผล่ ใช้ร่วมกันทุกจุด (ไม่ต้องวางในฉาก ระบบสร้างตัวเองอัตโนมัติ)
//   ScareFX.Hit(แรง)            -> จอสั่น + ขอบจอมืดแดงวูบ
//   ScareFX.FlickerNear(จุด, วิ) -> ไฟแถวนั้นกะพริบรัวๆ
//   ScareFX.Quiet(วินาที)        -> เสียงบรรยากาศเงียบลงชั่วคราว (ความเงียบก่อนผีโผล่)
//   ScareFX.PushQuiet/PopQuiet   -> เงียบตลอดช่วง (เช่น ตอนผีไล่)
public class ScareFX : MonoBehaviour
{
    // ---------- เรียกใช้จากสคริปต์ผี ----------
    public static void Hit(float strength = 1f) { Get().DoHit(strength); }

    // สร้างระบบไว้ล่วงหน้า (เรียกตอนเริ่มเกม) กันภาพกระตุกตอนผีโผล่ครั้งแรก
    public static void Warmup() { Get(); }

    // ไฟกะพริบตรงจุดนั้น: ไฟในฉากเป็นแบบ Baked สั่งกะพริบจริงไม่ได้
    // จึงทำให้ "จอมืดวูบ" แทน ถ้าผู้เล่นอยู่ในรัศมี (+ กะพริบไฟจริงด้วยถ้าเป็นไฟ Realtime)
    public static void FlickerNear(Vector3 position, float duration, float radius = 10f)
    {
        var fx = Get();
        Camera cam = Camera.main;
        if (cam != null && (cam.transform.position - position).sqrMagnitude <= radius * radius)
            fx.StartCoroutine(fx.ScreenFlickerRoutine(duration, 0.85f));
        fx.StartCoroutine(fx.FlickerRoutine(position, duration, radius));
    }

    // จอมืดวูบเหมือนไฟกะพริบ (strength 0-1 = มืดสุดแค่ไหน)
    public static void ScreenFlicker(float duration, float strength = 0.8f)
    {
        var fx = Get();
        fx.StartCoroutine(fx.ScreenFlickerRoutine(duration, strength));
    }

    public static void Quiet(float seconds) { if (AmbienceManager.instance != null) AmbienceManager.instance.QuietFor(seconds); }
    public static void PushQuiet() { if (AmbienceManager.instance != null) AmbienceManager.instance.PushQuiet(); }
    public static void PopQuiet() { if (AmbienceManager.instance != null) AmbienceManager.instance.PopQuiet(); }

    // ---------- ภายใน ----------
    static ScareFX inst;
    static ScareFX Get()
    {
        if (inst == null) inst = new GameObject("ScareFX").AddComponent<ScareFX>();
        return inst;
    }

    float shake;          // ความแรงจอสั่นตอนนี้ (ค่อยๆ ลดลง)
    float vignetteAlpha;  // ความทึบขอบจอตอนนี้ (ค่อยๆ จางลง)
    Image vignette;
    Image darkness;       // แผ่นดำทั้งจอ ใช้ทำ "ไฟดับวูบ"
    float darknessAlpha;
    int flickerRunning;

    Camera shakenCam;
    Vector3 savedPos;
    Quaternion savedRot;
    bool shakeApplied;

    void Awake()
    {
        // ขอบจอมืดแดง (อยู่ใต้เมนู แต่อยู่บนภาพเกม)
        var canvasGO = new GameObject("ScareFXCanvas", typeof(RectTransform), typeof(Canvas));
        canvasGO.transform.SetParent(transform, false);
        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 900;

        var dark = new GameObject("Darkness", typeof(RectTransform), typeof(Image));
        dark.transform.SetParent(canvasGO.transform, false);
        var drt = dark.GetComponent<RectTransform>();
        drt.anchorMin = Vector2.zero; drt.anchorMax = Vector2.one;
        drt.offsetMin = drt.offsetMax = Vector2.zero;
        darkness = dark.GetComponent<Image>();
        darkness.color = new Color(0f, 0f, 0f, 0f);
        darkness.raycastTarget = false;
        darkness.enabled = false;

        var img = new GameObject("Vignette", typeof(RectTransform), typeof(Image));
        img.transform.SetParent(canvasGO.transform, false);
        var rt = img.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        vignette = img.GetComponent<Image>();
        vignette.sprite = UIIconFactory.Vignette();
        vignette.raycastTarget = false;
        vignette.enabled = false;

        RenderPipelineManager.beginCameraRendering += OnBeginCamera;
        RenderPipelineManager.endCameraRendering += OnEndCamera;
    }

    void OnDestroy()
    {
        RenderPipelineManager.beginCameraRendering -= OnBeginCamera;
        RenderPipelineManager.endCameraRendering -= OnEndCamera;
        if (inst == this) inst = null;
    }

    void DoHit(float strength)
    {
        shake = Mathf.Max(shake, 0.07f * strength);
        vignetteAlpha = Mathf.Max(vignetteAlpha, Mathf.Clamp01(0.5f + 0.35f * strength));
    }

    void Update()
    {
        float dt = Time.deltaTime; // หยุดเกม = เอฟเฟกต์หยุดด้วย
        shake = Mathf.MoveTowards(shake, 0f, dt * 0.09f);
        vignetteAlpha = Mathf.MoveTowards(vignetteAlpha, 0f, dt * 0.45f);

        vignette.enabled = vignetteAlpha > 0.001f;
        if (vignette.enabled) vignette.color = new Color(0.22f, 0f, 0f, vignetteAlpha);

        if (flickerRunning <= 0) darknessAlpha = 0f;
        darkness.enabled = darknessAlpha > 0.001f;
        if (darkness.enabled) darkness.color = new Color(0f, 0f, 0f, darknessAlpha);
    }

    IEnumerator ScreenFlickerRoutine(float duration, float strength)
    {
        flickerRunning++;
        float t = 0f;
        while (t < duration)
        {
            // ส่วนใหญ่ดับวูบ บางจังหวะสว่างกลับมา เหมือนหลอดไฟจะขาด
            darknessAlpha = Random.value < 0.55f ? strength * Random.Range(0.7f, 1f) : strength * Random.Range(0f, 0.3f);
            float wait = Random.Range(0.04f, 0.14f);
            t += wait;
            yield return new WaitForSeconds(wait);
        }
        flickerRunning--;
    }

    // จอสั่น: ขยับกล้องเฉพาะตอนเรนเดอร์ แล้วคืนตำแหน่งทันที (ไม่ไปรบกวนระบบกล้องของผู้เล่น)
    void OnBeginCamera(ScriptableRenderContext ctx, Camera cam)
    {
        if (shake <= 0.0001f || cam != Camera.main) return;
        shakenCam = cam;
        savedPos = cam.transform.position;
        savedRot = cam.transform.rotation;
        // ใช้ Perlin noise ให้สั่นแบบนุ่มต่อเนื่อง (สุ่มใหม่ทุกเฟรมจะดูเหมือนภาพกระตุก)
        float t = Time.time * 14f;
        Vector3 n = new Vector3(Noise(t, 0f), Noise(t, 10f), Noise(t, 20f));
        Vector3 r = new Vector3(Noise(t, 30f), Noise(t, 40f), Noise(t, 50f));
        Vector3 posJitter = n * shake * 0.5f;
        Quaternion rotJitter = Quaternion.Euler(r.x * shake * 22f, r.y * shake * 22f, r.z * shake * 10f);
        cam.transform.SetPositionAndRotation(savedPos + posJitter, savedRot * rotJitter);
        shakeApplied = true;
    }

    static float Noise(float t, float seed) => (Mathf.PerlinNoise(t, seed) - 0.5f) * 2f; // -1..1

    void OnEndCamera(ScriptableRenderContext ctx, Camera cam)
    {
        if (!shakeApplied || cam != shakenCam) return;
        cam.transform.SetPositionAndRotation(savedPos, savedRot);
        shakeApplied = false;
    }

    // ไฟในรัศมีกะพริบรัวๆ แล้วกลับเป็นปกติ
    IEnumerator FlickerRoutine(Vector3 position, float duration, float radius)
    {
        // เฉพาะไฟที่เปิดอยู่และเป็นแบบ Realtime/Mixed (ไฟ Baked สั่งแล้วภาพไม่เปลี่ยน)
        var list = new List<FlickerLight>();
        var wasEnabled = new List<bool>();
        foreach (var f in FindObjectsByType<FlickerLight>(FindObjectsSortMode.None))
            if (f.targetLight != null && f.targetLight.enabled
                && !(f.targetLight.bakingOutput.isBaked && f.targetLight.bakingOutput.lightmapBakeType == LightmapBakeType.Baked)
                && (f.transform.position - position).sqrMagnitude <= radius * radius)
            {
                list.Add(f);
                wasEnabled.Add(f.enabled);
            }
        if (list.Count == 0) yield break;

        foreach (var f in list) f.enabled = false; // หยุดการกะพริบปกติไว้ก่อน

        float t = 0f;
        while (t < duration)
        {
            foreach (var f in list)
                if (f != null) f.targetLight.intensity = Random.value < 0.6f ? 0f : f.normalIntensity * Random.Range(0.2f, 1f);
            float wait = Random.Range(0.03f, 0.12f);
            t += wait;
            yield return new WaitForSeconds(wait);
        }

        for (int i = 0; i < list.Count; i++)
        {
            if (list[i] == null) continue;
            list[i].enabled = wasEnabled[i];                          // คืนสถานะสคริปต์เดิม
            list[i].targetLight.intensity = list[i].normalIntensity;  // คืนความสว่างปกติ
        }
    }
}
