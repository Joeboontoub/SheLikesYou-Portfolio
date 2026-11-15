using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using StarterAssets;

// ปุ่มบนจอสำหรับมือถือ (สร้างเฉพาะตอนเปิดจากมือถือ บนคอมจะไม่มีสิ่งนี้เลย)
//   ฝั่งซ้าย: จอยเดินแบบลอย (แตะตรงไหนในโซน จอยไปอยู่ตรงนั้น / ดันสุด = วิ่ง)
//   ฝั่งขวา: ไข่ดาว = วางนิ้วแล้วลากเพื่อหัน/มองขึ้นลง
//            + ปุ่ม แตะ (แตะ = เปิด/กด, กดค้าง = ถือของ), เก็บ, ไฟฉาย, กระโดด
//   มุมขวาบน: เต็มจอ, หยุดเกม
// ส่งค่าเดิน/มองเข้า StarterAssetsInputs โดยตรง (ปิด PlayerInput ไว้ ไม่ให้การแตะจอไปหมุนกล้องเอง)
public class MobileControls : MonoBehaviour
{
    public float lookSensitivity = 1f;   // ความไวการลากมอง
    public float sprintThreshold = 0.9f; // ดันจอยเดินเกินนี้ = วิ่ง

    StarterAssetsInputs inputs;
    ThirdPersonController tpc;
    PropertyInfo mouseSchemeProp;

    TMP_FontAsset font;
    Func<string, string> fixThai;

    GameObject root, rotateHint;
    RectTransform moveZone, moveBase, moveKnob;
    RectTransform lookBase, lookKnob;
    Vector2 move, lookAccum, lookStart;
    bool active;
    const float MoveRadius = 120f;
    const float LookRadius = 110f;

    public static MobileControls Create(TMP_FontAsset font, Func<string, string> fixThai)
    {
        var mc = new GameObject("MobileControls").AddComponent<MobileControls>();
        mc.font = font;
        mc.fixThai = fixThai ?? (s => s);
        mc.Build();
        return mc;
    }

    // สลับโหมดเต็มจอ (ใช้ได้บน Android / iPhone ไม่รองรับ)
    public static void ToggleFullscreen() { Screen.fullScreen = !Screen.fullScreen; }

    void Start()
    {
        inputs = FindFirstObjectByType<StarterAssetsInputs>();
        tpc = FindFirstObjectByType<ThirdPersonController>();
        if (tpc != null)
            mouseSchemeProp = typeof(ThirdPersonController).GetProperty("IsCurrentDeviceMouse", BindingFlags.Instance | BindingFlags.NonPublic);
        if (inputs != null) inputs.cursorLocked = false; // มือถือไม่ต้องล็อกเมาส์
    }

    // เปิด/ปิดปุ่ม (เปิดเฉพาะตอนกำลังเล่น)
    public void SetActive(bool on)
    {
        active = on;
        if (root != null) root.SetActive(on);
        move = Vector2.zero;
        lookAccum = Vector2.zero;
        if (moveKnob != null) moveKnob.anchoredPosition = Vector2.zero;
        if (lookKnob != null) lookKnob.anchoredPosition = Vector2.zero;
        MobileInput.ResetAll();
        if (inputs != null)
        {
            inputs.MoveInput(Vector2.zero);
            inputs.LookInput(Vector2.zero);
            inputs.SprintInput(false);
            inputs.JumpInput(false);
        }
    }

    void Update()
    {
        if (rotateHint != null) rotateHint.SetActive(Screen.height > Screen.width); // จอแนวตั้ง -> บอกให้หมุนจอ
        if (!active || inputs == null) return;

        // มอง: แปลงระยะลากนิ้ว (พิกเซล) เป็นองศา อิงความสูงจอ ให้ความไวเท่ากันทุกขนาดจอ
        Vector2 deg = lookAccum / Mathf.Max(1, Screen.height) * 150f * lookSensitivity;
        lookAccum = Vector2.zero;
        Vector2 look = new Vector2(deg.x, -deg.y); // ลากขึ้น = มองขึ้น
        // ตัวควบคุมจะคูณ deltaTime เองถ้าไม่ใช่เมาส์ จึงหารไว้ก่อนให้ได้ผลเท่ากับระยะที่ลากจริง
        if (!IsMouseScheme()) look /= Mathf.Max(Time.deltaTime, 0.0001f);

        inputs.MoveInput(move);
        inputs.SprintInput(move.magnitude >= sprintThreshold);
        inputs.LookInput(look);
    }

    bool IsMouseScheme()
    {
        if (mouseSchemeProp == null || tpc == null) return false;
        try { return (bool)mouseSchemeProp.GetValue(tpc); } catch { return false; }
    }

    // ================= สร้าง UI =================
    void Build()
    {
        var canvasGO = new GameObject("MobileCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGO.transform.SetParent(transform, false);
        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 800; // ใต้เมนู/เอฟเฟกต์
        var scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        root = NewUI("Controls", canvasGO.transform);
        Stretch(root);

        // ---------- ฝั่งซ้าย: จอยเดินแบบลอย ----------
        var zone = NewUI("MoveZone", root.transform);
        Anchor(zone, new Vector2(0f, 0f), new Vector2(0.45f, 0.8f));
        zone.AddComponent<Image>().color = new Color(0, 0, 0, 0);
        moveZone = zone.GetComponent<RectTransform>();

        moveBase = MakeStick(zone.transform, "MoveStick", new Vector2(0f, 0f), new Vector2(280f, 260f), MoveRadius, out moveKnob);
        Vector2 moveDefault = moveBase.anchoredPosition;
        var mz = zone.AddComponent<TouchArea>();
        mz.onDown = pos => { moveBase.anchoredPosition = ScreenToZone(pos); UpdateMove(pos); };
        mz.onMove = pos => UpdateMove(pos);
        mz.onUp = () => { move = Vector2.zero; moveKnob.anchoredPosition = Vector2.zero; moveBase.anchoredPosition = moveDefault; };

        // ---------- ฝั่งขวา: กลุ่มปุ่มตามภาพร่าง ----------
        //            [เก็บ]     [แตะ]
        //     [ไฟฉาย]
        // [กระโดด]           (( ไข่ดาว ))
        var lookGO = NewUI("LookDragArea", root.transform);
        var lrt = lookGO.GetComponent<RectTransform>();
        lrt.anchorMin = lrt.anchorMax = new Vector2(1f, 0f);
        lrt.sizeDelta = new Vector2(LookRadius * 2.9f, LookRadius * 2.9f); // ใหญ่กว่ารูปไข่ดาวเล็กน้อย
        lrt.anchoredPosition = new Vector2(-215f, 215f);
        lookGO.AddComponent<Image>().color = new Color(0, 0, 0, 0);
        lookBase = MakeStick(lookGO.transform, "LookEgg", new Vector2(0.5f, 0.5f), Vector2.zero, LookRadius, out lookKnob);
        var lz = lookGO.AddComponent<TouchArea>();
        lz.onDown = pos => { lookStart = pos; };
        lz.onDrag = d => lookAccum += d;
        lz.onMove = pos =>
        {
            // วงในขยับตามนิ้วเล็กน้อย ให้รู้ว่ากำลังลากอยู่
            Vector2 offset = (pos - lookStart) / Mathf.Max(0.01f, lookBase.lossyScale.x);
            lookKnob.anchoredPosition = Vector2.ClampMagnitude(offset * 0.35f, LookRadius * 0.6f);
        };
        lz.onUp = () => { lookKnob.anchoredPosition = Vector2.zero; };

        MakeButton("แตะ", new Vector2(-160f, 560f), 190f, 46,
            () => MobileInput.PressInteract(), () => MobileInput.ReleaseInteract());
        MakeButton("เก็บ", new Vector2(-390f, 520f), 150f, 38, () => MobileInput.PressCollect(), null);
        MakeButton("ไฟฉาย", new Vector2(-520f, 335f), 135f, 30, () => MobileInput.PressFlashlight(), null);
        MakeButton("กระโดด", new Vector2(-580f, 125f), 130f, 30, () => { if (inputs != null) inputs.JumpInput(true); }, null);

        // ---------- มุมขวาบน: เต็มจอ + หยุดเกม ----------
        var pause = MakeButton("II", Vector2.zero, 110f, 40, () => MobileInput.PressPause(), null);
        var prt = pause.GetComponent<RectTransform>();
        prt.anchorMin = prt.anchorMax = new Vector2(1f, 1f);
        prt.anchoredPosition = new Vector2(-90f, -90f);

        var full = MakeButton("เต็มจอ", Vector2.zero, 110f, 26, ToggleFullscreen, null);
        var frt = full.GetComponent<RectTransform>();
        frt.anchorMin = frt.anchorMax = new Vector2(1f, 1f);
        frt.anchoredPosition = new Vector2(-220f, -90f);

        root.SetActive(false);

        // ---------- จอแนวตั้ง: บอกให้หมุนจอ (บังทุกอย่าง) ----------
        var hintCanvasGO = new GameObject("RotateHintCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        hintCanvasGO.transform.SetParent(transform, false);
        var hc = hintCanvasGO.GetComponent<Canvas>();
        hc.renderMode = RenderMode.ScreenSpaceOverlay;
        hc.sortingOrder = 2000;
        var hs = hintCanvasGO.GetComponent<CanvasScaler>();
        hs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        hs.referenceResolution = new Vector2(1080, 1920);
        hs.matchWidthOrHeight = 0.5f;
        rotateHint = NewUI("RotateHint", hintCanvasGO.transform);
        Stretch(rotateHint);
        rotateHint.AddComponent<Image>().color = Color.black;
        var ht = MakeLabel(rotateHint.transform, "กรุณาหมุนมือถือเป็นแนวนอน\n\n<size=70%>เพื่อเล่นเกม</size>", 64);
        Stretch(ht.gameObject);
        rotateHint.SetActive(false);
    }

    // วงนอก (ring) + วงใน (knob)
    RectTransform MakeStick(Transform parent, string name, Vector2 anchor, Vector2 pos, float radius, out RectTransform knob)
    {
        var b = NewUI(name, parent);
        var rt = b.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = anchor;
        rt.sizeDelta = new Vector2(radius * 2.2f, radius * 2.2f);
        rt.anchoredPosition = pos;
        var bimg = b.AddComponent<Image>();
        bimg.sprite = UIIconFactory.Ring();
        bimg.color = new Color(1, 1, 1, 0.4f);
        bimg.raycastTarget = false;

        var k = NewUI("Knob", b.transform);
        knob = k.GetComponent<RectTransform>();
        knob.sizeDelta = new Vector2(radius * 1.0f, radius * 1.0f);
        var kimg = k.AddComponent<Image>();
        kimg.sprite = UIIconFactory.Circle();
        kimg.color = new Color(1, 1, 1, 0.45f);
        kimg.raycastTarget = false;
        return rt;
    }

    void UpdateMove(Vector2 screenPos)
    {
        Vector2 local = ScreenToZone(screenPos) - moveBase.anchoredPosition;
        Vector2 clamped = Vector2.ClampMagnitude(local, MoveRadius);
        moveKnob.anchoredPosition = clamped;
        move = clamped / MoveRadius;
    }

    // ตำแหน่งบนจอ -> พิกัดในโซนจอยเดิน นับจากมุมซ้ายล่างของโซน (ตรงกับ anchor ของจอย)
    Vector2 ScreenToZone(Vector2 screenPos)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(moveZone, screenPos, null, out Vector2 local);
        Rect r = moveZone.rect;
        return new Vector2(local.x - r.xMin, local.y - r.yMin);
    }

    GameObject MakeButton(string label, Vector2 pos, float size, float fontSize, Action onDown, Action onUp)
    {
        var go = NewUI("Btn_" + label, root.transform);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(1f, 0f); // นับจากมุมขวาล่าง
        rt.sizeDelta = new Vector2(size, size);
        rt.anchoredPosition = pos;

        var img = go.AddComponent<Image>();
        img.sprite = UIIconFactory.Circle();
        Color normal = new Color(1f, 1f, 1f, 0.2f), pressed = new Color(1f, 1f, 1f, 0.45f);
        img.color = normal;

        var ring = NewUI("Ring", go.transform);
        Stretch(ring);
        var rimg = ring.AddComponent<Image>();
        rimg.sprite = UIIconFactory.Ring();
        rimg.color = new Color(1f, 1f, 1f, 0.5f);
        rimg.raycastTarget = false;

        var t = MakeLabel(go.transform, label, fontSize);
        Stretch(t.gameObject);

        var ta = go.AddComponent<TouchArea>();
        ta.onDown = _ => { img.color = pressed; onDown?.Invoke(); };
        ta.onUp = () => { img.color = normal; onUp?.Invoke(); };
        return go;
    }

    TextMeshProUGUI MakeLabel(Transform parent, string text, float size)
    {
        var go = NewUI("Label", parent);
        var t = go.AddComponent<TextMeshProUGUI>();
        if (font != null) t.font = font;
        t.text = fixThai(text);
        t.fontSize = size;
        t.color = Color.white;
        t.alignment = TextAlignmentOptions.Center;
        t.textWrappingMode = TextWrappingModes.NoWrap;
        t.raycastTarget = false;
        return t;
    }

    static GameObject NewUI(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    static void Stretch(GameObject go) => Anchor(go, Vector2.zero, Vector2.one);

    static void Anchor(GameObject go, Vector2 min, Vector2 max)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = min; rt.anchorMax = max;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }
}

// พื้นที่รับนิ้ว: กด / ลาก / ปล่อย (รองรับหลายนิ้วพร้อมกัน แต่ละพื้นที่จำนิ้วของตัวเอง)
public class TouchArea : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    public Action<Vector2> onDown;   // ตำแหน่งบนจอ
    public Action<Vector2> onMove;   // ตำแหน่งบนจอ
    public Action<Vector2> onDrag;   // ระยะที่ลาก (พิกเซล)
    public Action onUp;
    int pointerId = int.MinValue;

    public void OnPointerDown(PointerEventData e)
    {
        if (pointerId != int.MinValue) return; // มีนิ้วอื่นใช้อยู่
        pointerId = e.pointerId;
        onDown?.Invoke(e.position);
    }

    public void OnDrag(PointerEventData e)
    {
        if (e.pointerId != pointerId) return;
        onMove?.Invoke(e.position);
        onDrag?.Invoke(e.delta);
    }

    public void OnPointerUp(PointerEventData e)
    {
        if (e.pointerId != pointerId) return;
        pointerId = int.MinValue;
        onUp?.Invoke();
    }

    void OnDisable()
    {
        if (pointerId != int.MinValue) { pointerId = int.MinValue; onUp?.Invoke(); }
    }
}
