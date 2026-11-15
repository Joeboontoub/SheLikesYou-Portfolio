using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// หน้าเมนูก่อนเข้าเกม (START / MOVE / EXIT) + ป้ายบอกปุ่มควบคุม + หน้าจอออกจากเกม
// ถูกสร้างและสั่งงานโดย GameManager (ไม่ต้องจัด UI ในฉากเอง)
public class GameMenuUI : MonoBehaviour
{
    public Action onStart;            // GameManager ใส่ฟังก์ชันเริ่มเกมไว้ที่นี่
    public float controlsScale = 0.8f; // ขนาดป้ายปุ่มควบคุม (1 = ขนาดเต็ม, 0.8 = เล็กลง 20%)

    TMP_FontAsset font;
    Func<string, string> fixThai;

    public Action onResume;           // ปุ่ม "เล่นต่อ" ในเมนูหยุดเกม
    public Action onMainMenu;         // ปุ่ม "กลับเมนูหลัก" ในเมนูหยุดเกม
    public Action<char> onTypeChar;   // เรียกทุกครั้งที่ข้อความจบขึ้น 1 ตัว (ใช้เล่นเสียงพิมพ์ดีด)

    GameObject canvasGO, menuRoot, controlsPanel, exitScreen, pauseRoot, hint;

    public bool IsMenuOpen => menuRoot != null && menuRoot.activeSelf;
    public bool IsPauseOpen => pauseRoot != null && pauseRoot.activeSelf;
    public bool IsControlsOpen => controlsPanel != null && controlsPanel.activeSelf;

    bool mobile; // เปิดจากมือถือ: ป้ายควบคุมแบบปุ่มบนจอ และไม่โชว์คำแนะนำคีย์บอร์ด

    public void Build(TMP_FontAsset font, Func<string, string> fixThai, string titleTh, string titleEn, Color titleColor, bool showMenu, bool mobile = false)
    {
        this.mobile = mobile;
        this.font = font;
        this.fixThai = fixThai ?? (s => s);

        // Canvas หลักของเมนู (อยู่บนสุด)
        canvasGO = new GameObject("GameMenuCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;
        var scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        BuildMenu(titleTh, titleEn, titleColor);
        BuildPauseMenu();
        BuildHint();
        BuildControlsPanel();   // สร้างทีหลังเมนู เพื่อให้ป้ายอยู่ด้านบนของพื้นมืด
        BuildEndingScreen();
        BuildExitScreen();      // สร้างสุดท้าย ให้บังทุกอย่าง
        endingScreen.SetActive(false);

        menuRoot.SetActive(showMenu);
        pauseRoot.SetActive(false);
        hint.SetActive(false);
        controlsPanel.SetActive(false);
        exitScreen.SetActive(false);
    }

    public void HideMenu() { if (menuRoot != null) menuRoot.SetActive(false); }
    public void ShowPause(bool show) { if (pauseRoot != null) pauseRoot.SetActive(show); }
    public void ShowHint(bool show) { if (hint != null) hint.SetActive(show && !mobile); } // มือถือมีปุ่ม II อยู่แล้ว
    public void ShowControls(bool show) { if (controlsPanel != null) controlsPanel.SetActive(show); }
    public void ToggleControls() { ShowControls(!IsControlsOpen); }

    void LateUpdate()
    {
        // ระหว่างอยู่ในเมนู ต้องเห็นลูกศรเมาส์เสมอ (ตัวควบคุมผู้เล่นชอบล็อกเมาส์อัตโนมัติ)
        if (IsMenuOpen || IsPauseOpen || (exitScreen != null && exitScreen.activeSelf))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    // ================= เมนูหลัก =================
    void BuildMenu(string titleTh, string titleEn, Color titleColor)
    {
        menuRoot = NewUI("Menu", canvasGO.transform);
        Stretch(menuRoot);

        // เงามืดด้านซ้าย ให้ตัวหนังสืออ่านง่ายบนฉากจริง
        var fade = NewUI("LeftFade", menuRoot.transform);
        var frt = fade.GetComponent<RectTransform>();
        frt.anchorMin = new Vector2(0, 0); frt.anchorMax = new Vector2(0.6f, 1);
        frt.offsetMin = frt.offsetMax = Vector2.zero;
        var fimg = fade.AddComponent<Image>();
        fimg.sprite = UIIconFactory.LeftFade();
        fimg.raycastTarget = false;

        const float left = 0.115f; // ระยะจากขอบซ้าย (สัดส่วนของจอ)

        var t1 = MakeText("TitleTH", menuRoot.transform, titleTh, 120, titleColor, TextAlignmentOptions.BottomLeft);
        Place(t1, left, 0.70f, 1100, 170, new Vector2(0, 0));

        var t2 = MakeText("TitleEN", menuRoot.transform, titleEn, 52, titleColor, TextAlignmentOptions.TopLeft);
        Place(t2, left + 0.004f, 0.695f, 1100, 80, new Vector2(0, 1));

        MakeMenuButton(menuRoot.transform, "START", 0.52f, left, () => onStart?.Invoke());
        MakeMenuButton(menuRoot.transform, "MOVE", 0.44f, left, ToggleControls);
        MakeMenuButton(menuRoot.transform, "EXIT", 0.36f, left, ExitGame);

        // เปิดจากมือถือ: ปุ่มเต็มจอ + คำแนะนำ
        if (mobile)
        {
            // ปุ่มเต็มจอ (มือถือ Android / iPhone ไม่รองรับ)
            MakeMenuButton(menuRoot.transform, "FULL SCREEN", 0.28f, left, MobileControls.ToggleFullscreen);

            var note = MakeText("MobileNote", menuRoot.transform,
                "เล่นบนมือถือ: ใช้ปุ่มบนจอ (กด MOVE เพื่อดูวิธีเล่น)\nแนะนำให้หมุนจอแนวนอนและใส่หูฟัง", 34, new Color(1f, 0.75f, 0.3f, 0.9f), TextAlignmentOptions.TopLeft);
            Place(note, left, 0.21f, 1400, 120, new Vector2(0, 1));
        }
    }

    // ================= เมนูหยุดเกม (กด ESC) =================
    void BuildPauseMenu()
    {
        pauseRoot = NewUI("PauseMenu", canvasGO.transform);
        Stretch(pauseRoot);
        var dim = pauseRoot.AddComponent<Image>();
        dim.color = new Color(0f, 0f, 0f, 0.65f); // ทำให้ภาพเกมมืดลง และกันการคลิกด้านหลัง

        const float left = 0.115f;
        var title = MakeText("PauseTitle", pauseRoot.transform, "หยุดเกม", 96, Color.white, TextAlignmentOptions.BottomLeft);
        Place(title, left, 0.64f, 900, 150, new Vector2(0, 0));

        MakeMenuButton(pauseRoot.transform, "เล่นต่อ", 0.56f, left, () => onResume?.Invoke());
        MakeMenuButton(pauseRoot.transform, "ปุ่มควบคุม", 0.48f, left, ToggleControls);
        MakeMenuButton(pauseRoot.transform, "กลับเมนูหลัก", 0.40f, left, () => onMainMenu?.Invoke());
        MakeMenuButton(pauseRoot.transform, "ออกจากเกม", 0.32f, left, ExitGame);
    }

    // ข้อความเล็กๆ มุมขวาล่างระหว่างเล่น บอกว่ากด ESC หยุดเกมได้
    void BuildHint()
    {
        hint = NewUI("Hint", canvasGO.transform);
        var rt = hint.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(1f, 0f);
        rt.pivot = new Vector2(1f, 0f);
        rt.anchoredPosition = new Vector2(-40f, 30f);
        rt.sizeDelta = new Vector2(800f, 50f);

        var t = MakeText("Text", hint.transform, "ESC  หยุดเกม     H  ปุ่มควบคุม", 30, new Color(1f, 1f, 1f, 0.6f), TextAlignmentOptions.BottomRight);
        Stretch(t.gameObject);
    }

    void MakeMenuButton(Transform parent, string label, float y, float x, Action onClick)
    {
        var go = NewUI("Button_" + label, parent);
        Place(go.GetComponent<RectTransform>(), x, y, 520, 72, new Vector2(0, 0.5f));

        var hit = go.AddComponent<Image>();
        hit.color = new Color(0, 0, 0, 0); // พื้นที่กดล่องหน

        var txt = MakeText("Text", go.transform, label, 54, Color.white, TextAlignmentOptions.Left);
        Stretch(txt.gameObject);
        txt.fontStyle = FontStyles.Normal;
        txt.characterSpacing = 4;

        var btn = go.AddComponent<Button>();
        btn.targetGraphic = txt;
        var cb = btn.colors;
        cb.normalColor = new Color(0.78f, 0.78f, 0.78f, 1f);
        cb.highlightedColor = Color.white;
        cb.selectedColor = new Color(0.78f, 0.78f, 0.78f, 1f);
        cb.pressedColor = new Color(0.55f, 0.55f, 0.55f, 1f);
        cb.fadeDuration = 0.08f;
        btn.colors = cb;
        btn.onClick.AddListener(() => onClick());
    }

    // ================= ป้ายบอกปุ่มควบคุม =================
    void BuildControlsPanel()
    {
        controlsPanel = NewUI("ControlsPanel", canvasGO.transform);
        var rt = controlsPanel.GetComponent<RectTransform>();

        const float rowH = 84f;
        const float iconW = 150f;
        const float panelW = 640f;
        const float pad = 26f;

        // (ไอคอน, ข้อความ)
        var rows = new (Action<Transform> icon, string label)[]
        {
            (p => WasdIcon(p),                                   "เดิน"),
            (p => MouseIcon(p, UIIconFactory.MouseLook(), 130),  "มองรอบตัว"),
            (p => KeyIcon(p, "Shift", 84),                       "วิ่ง"),
            (p => KeyIcon(p, "Space", 92),                       "กระโดด"),
            (p => MouseIcon(p, UIIconFactory.MouseLeftClick(), 62), "เปิดประตู / กดปุ่ม"),
            (p => MouseIcon(p, UIIconFactory.MouseLeftClick(), 62, "ค้าง"), "ถือของ (กดค้าง)"),
            (p => KeyIcon(p, "E", 54),                           "เก็บของ / กิน"),
            (p => KeyIcon(p, "L", 54),                           "ไฟฉาย"),
            (p => KeyIcon(p, "H", 54),                           "เปิด/ปิดป้ายนี้"),
            (p => KeyIcon(p, "ESC", 64),                         "หยุดเกม"),
        };
        if (mobile)
        {
            // มือถือ: อธิบายปุ่มบนจอแทนคีย์บอร์ด
            rows = new (Action<Transform> icon, string label)[]
            {
                (p => TouchIcon(p, "จอยซ้าย", false),  "เดิน (ดันสุด = วิ่ง)"),
                (p => TouchIcon(p, "วงขวา", false),    "ลากนิ้ว = หัน / มองขึ้นลง"),
                (p => TouchIcon(p, "แตะ", true),       "แตะ = เปิดประตู / กดปุ่ม"),
                (p => TouchIcon(p, "แตะ", true),       "กดค้าง = ถือของ"),
                (p => TouchIcon(p, "เก็บ", true),      "เก็บของ / กิน"),
                (p => TouchIcon(p, "ไฟฉาย", true),     "เปิด/ปิดไฟฉาย"),
                (p => TouchIcon(p, "กระโดด", true),    "กระโดด"),
                (p => TouchIcon(p, "II", true),        "หยุดเกม"),
                (p => TouchIcon(p, "เต็มจอ", true),    "เล่นเต็มจอ (Android)"),
            };
        }

        float panelH = rows.Length * rowH + pad * 2;
        rt.anchorMin = rt.anchorMax = new Vector2(1f, 0.5f);
        rt.pivot = new Vector2(1f, 0.5f);
        rt.anchoredPosition = new Vector2(-110f, 0f);
        rt.sizeDelta = new Vector2(panelW, panelH);
        rt.localScale = Vector3.one * controlsScale; // ย่อทั้งป้าย (ขอบขวายังอยู่ที่เดิม)

        // พื้นหลังโปร่งแสงบางๆ ให้อ่านง่าย
        var bg = controlsPanel.AddComponent<Image>();
        bg.sprite = UIIconFactory.RoundedRect();
        bg.type = Image.Type.Sliced;
        bg.color = new Color(0f, 0f, 0f, 0.45f);
        bg.raycastTarget = false;

        for (int i = 0; i < rows.Length; i++)
        {
            float yTop = -pad - i * rowH;

            var iconBox = NewUI("Icon" + i, controlsPanel.transform);
            var irt = iconBox.GetComponent<RectTransform>();
            irt.anchorMin = irt.anchorMax = new Vector2(0, 1);
            irt.pivot = new Vector2(0.5f, 0.5f);
            irt.anchoredPosition = new Vector2(pad + iconW / 2f, yTop - rowH / 2f);
            irt.sizeDelta = new Vector2(iconW, rowH);
            rows[i].icon(iconBox.transform);

            var label = MakeText("Label" + i, controlsPanel.transform, rows[i].label, 44, Color.white, TextAlignmentOptions.Left);
            label.enableAutoSizing = true; // ข้อความยาว (เช่น แบบมือถือ) ย่อให้พอดีป้าย
            label.fontSizeMax = 44;
            label.fontSizeMin = 24;
            var lrt = label.rectTransform;
            lrt.anchorMin = lrt.anchorMax = new Vector2(0, 1);
            lrt.pivot = new Vector2(0, 0.5f);
            lrt.anchoredPosition = new Vector2(pad + iconW + 22f, yTop - rowH / 2f);
            lrt.sizeDelta = new Vector2(panelW - iconW - pad * 2 - 22f, rowH);
        }
    }

    // ปุ่มคีย์บอร์ดเดี่ยว (สี่เหลี่ยมขาวมุมมน ตัวหนังสือดำ)
    void KeyIcon(Transform parent, string key, float width, float height = 54, float fontSize = 0, Vector2? pos = null)
    {
        var go = NewUI("Key_" + key, parent);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(width, height);
        rt.anchoredPosition = pos ?? Vector2.zero;

        var img = go.AddComponent<Image>();
        img.sprite = UIIconFactory.RoundedRect();
        img.type = Image.Type.Sliced;
        img.pixelsPerUnitMultiplier = height < 40 ? 2.2f : 1.4f;
        img.color = Color.white;
        img.raycastTarget = false;

        if (fontSize <= 0) fontSize = key.Length > 1 ? 24 : 30;
        var txt = MakeText("Text", go.transform, key, fontSize, new Color(0.1f, 0.1f, 0.1f), TextAlignmentOptions.Center, false);
        txt.fontStyle = FontStyles.Bold;
        Stretch(txt.gameObject);
    }

    // ไอคอนปุ่มบนจอมือถือ: วงกลม (ปุ่ม) หรือกรอบมน (การแตะ/ลาก) + ข้อความไทย
    void TouchIcon(Transform parent, string text, bool round)
    {
        var go = NewUI("Touch_" + text, parent);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = round ? new Vector2(70, 70) : new Vector2(140, 54);
        var img = go.AddComponent<Image>();
        img.sprite = round ? UIIconFactory.Circle() : UIIconFactory.RoundedRect();
        if (!round) img.type = Image.Type.Sliced;
        img.color = new Color(1f, 1f, 1f, 0.25f);
        img.raycastTarget = false;
        var t = MakeText("Text", go.transform, text, round ? 22 : 24, Color.white, TextAlignmentOptions.Center);
        Stretch(t.gameObject);
    }

    // กลุ่มปุ่ม W A S D
    void WasdIcon(Transform parent)
    {
        const float k = 34f, gap = 4f;
        KeyIcon(parent, "W", k, k, 18, new Vector2(0, (k + gap) / 2f));
        KeyIcon(parent, "A", k, k, 18, new Vector2(-(k + gap), -(k + gap) / 2f));
        KeyIcon(parent, "S", k, k, 18, new Vector2(0, -(k + gap) / 2f));
        KeyIcon(parent, "D", k, k, 18, new Vector2(k + gap, -(k + gap) / 2f));
    }

    void MouseIcon(Transform parent, Sprite sprite, float width, string badge = null)
    {
        var go = NewUI("Mouse", parent);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(width, 72);
        var img = go.AddComponent<Image>();
        img.sprite = sprite;
        img.preserveAspect = true;
        img.raycastTarget = false;

        if (!string.IsNullOrEmpty(badge))
        {
            var b = MakeText("Badge", parent, badge, 22, Color.white, TextAlignmentOptions.Left);
            var brt = b.rectTransform;
            brt.anchorMin = brt.anchorMax = new Vector2(0.5f, 0.5f);
            brt.pivot = new Vector2(0, 0.5f);
            brt.anchoredPosition = new Vector2(34, -18);
            brt.sizeDelta = new Vector2(60, 30);
        }
    }

    // ================= ฉากจบ (หนีออกมาได้) =================
    GameObject endingScreen;
    CanvasGroup endingGroup;
    TextMeshProUGUI endingText, endingPrompt;

    void BuildEndingScreen()
    {
        endingScreen = NewUI("EndingScreen", canvasGO.transform);
        Stretch(endingScreen);
        endingScreen.AddComponent<Image>().color = Color.black;
        endingGroup = endingScreen.AddComponent<CanvasGroup>();

        endingText = MakeText("Text", endingScreen.transform, "", 64, Color.white, TextAlignmentOptions.Center);
        var rt = endingText.rectTransform;
        rt.anchorMin = new Vector2(0.1f, 0.2f); rt.anchorMax = new Vector2(0.9f, 0.85f);
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        endingText.textWrappingMode = TextWrappingModes.Normal;

        endingPrompt = MakeText("Prompt", endingScreen.transform, "", 32, new Color(1f, 1f, 1f, 0.6f), TextAlignmentOptions.Bottom);
        var prt = endingPrompt.rectTransform;
        prt.anchorMin = new Vector2(0f, 0.06f); prt.anchorMax = new Vector2(1f, 0.16f);
        prt.offsetMin = prt.offsetMax = Vector2.zero;
    }

    // ค่อยๆ มืดลง -> พิมพ์ข้อความจบทีละตัว -> โชว์ข้อความให้กดปุ่ม (ข้อความต้องผ่าน FixThaiText มาแล้ว)
    public System.Collections.IEnumerator PlayEnding(string message, string prompt, float fadeTime = 2f)
    {
        menuRoot.SetActive(false);
        pauseRoot.SetActive(false);
        hint.SetActive(false);
        controlsPanel.SetActive(false);

        endingScreen.SetActive(true);
        endingPrompt.gameObject.SetActive(false);
        endingText.text = message;
        endingText.maxVisibleCharacters = 0;

        for (float t = 0f; t < fadeTime; t += Time.unscaledDeltaTime)
        {
            endingGroup.alpha = t / fadeTime;
            yield return null;
        }
        endingGroup.alpha = 1f;

        endingText.ForceMeshUpdate();
        int total = endingText.textInfo.characterCount;
        for (int i = 0; i <= total; i++)
        {
            endingText.maxVisibleCharacters = i;
            if (i > 0) onTypeChar?.Invoke(endingText.textInfo.characterInfo[i - 1].character); // เสียงพิมพ์ดีด
            yield return new WaitForSecondsRealtime(0.06f);
        }

        yield return new WaitForSecondsRealtime(1.5f);
        endingPrompt.text = prompt;
        endingPrompt.gameObject.SetActive(true);
    }

    // ================= ออกจากเกม =================
    void BuildExitScreen()
    {
        exitScreen = NewUI("ExitScreen", canvasGO.transform);
        Stretch(exitScreen);
        var img = exitScreen.AddComponent<Image>();
        img.color = Color.black; // บังทุกอย่าง และกันการคลิกด้านหลัง

        var txt = MakeText("Text", exitScreen.transform,
            "ขอบคุณที่เล่น\n\n<size=60%>คุณสามารถปิดแท็บนี้ได้เลย</size>", 72, Color.white, TextAlignmentOptions.Center);
        Stretch(txt.gameObject);
    }

    void ExitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#elif UNITY_WEBGL
        // บนเว็บ เบราว์เซอร์ไม่ยอมให้เกมปิดแท็บเอง จึงโชว์หน้าขอบคุณแทน
        menuRoot.SetActive(false);
        pauseRoot.SetActive(false);
        hint.SetActive(false);
        controlsPanel.SetActive(false);
        endingScreen.SetActive(false);
        exitScreen.SetActive(true);
        AudioListener.volume = 0f;
#else
        Application.Quit();
#endif
    }

    // ================= ตัวช่วย =================
    static GameObject NewUI(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    static void Stretch(GameObject go)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    static void Place(Component c, float ax, float ay, float w, float h, Vector2 pivot)
    {
        var rt = c.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(ax, ay);
        rt.pivot = pivot;
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(w, h);
    }

    TextMeshProUGUI MakeText(string name, Transform parent, string text, float size, Color color, TextAlignmentOptions align, bool thai = true)
    {
        var go = NewUI(name, parent);
        var t = go.AddComponent<TextMeshProUGUI>();
        if (font != null) t.font = font;
        t.text = thai ? fixThai(text) : text;
        t.fontSize = size;
        t.color = color;
        t.alignment = align;
        t.textWrappingMode = TextWrappingModes.NoWrap;
        t.raycastTarget = false;
        return t;
    }
}
