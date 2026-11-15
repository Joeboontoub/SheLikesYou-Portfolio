using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.SceneManagement;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem; // เพิ่มบรรทัดนี้เข้ามาเพื่อรองรับระบบปุ่มกดแบบใหม่
#endif

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    [Header("ตั้งค่า UI")]
    public TextMeshProUGUI centerText; // กล่องข้อความกลางจอ (สำหรับ Intro)
    public TextMeshProUGUI cornerMissionText; // กล่องข้อความเล็กๆ มุมจอ (สร้างเพิ่มทีหลัง)

    [Header("ตั้งค่าภารกิจ")]
    public string[] itemsToFind = new string[9];
    
    [Header("ระบบผีหลอก (Ghost System)")]
    public GameObject ghost;
    public Transform[] ghostSpawnPoints;

    [Header("ประตูทางออก (Exit Doors)")]
    public InteractableProp[] exitDoors;

    private int currentIndex = 0;
    private bool isGameStarted = false; // ตัวเช็คว่าเกมเริ่มหรือยัง
    private bool isIntroShowing = false; // กำลังโชว์ข้อความ Intro อยู่ (รอกดปุ่มเริ่มเกม)
    private int introStartFrame;         // เฟรมที่เริ่มโชว์ Intro (กันไม่ให้การกดปุ่มเมนูข้าม Intro ไปเลย)

    [Header("เมนูก่อนเข้าเกม (Start Menu)")]
    public bool showStartMenu = true;
    public string titleThai = "เธอ ชอบ คุณ";
    public string gameSubtitle = "She likes you.";
    public Color titleColor = new Color(0.85f, 0.05f, 0.05f, 1f);
    [Tooltip("จุดวางกล้องตอนอยู่หน้าเมนู (เว้นว่าง = ใช้มุมมองเริ่มต้นของผู้เล่น)")]
    public Transform menuCameraPoint;

    [Header("มือถือ")]
    [Tooltip("ติ๊กเพื่อทดสอบปุ่มมือถือใน Unity Editor (ใช้เมาส์แทนนิ้ว) - ไม่มีผลกับเกมที่ build แล้ว")]
    public bool testMobileControlsInEditor = false;
    MobileControls mobileControls;
    public bool UseMobileControls => Application.isMobilePlatform || (Application.isEditor && testMobileControlsInEditor);

    [Header("ข้อความ Intro (ชิดซ้ายของจอ)")]
    [Range(0f, 0.5f)] public float introLeftMargin = 0.06f;  // ระยะจากขอบซ้าย (สัดส่วนของจอ)
    [Range(0.3f, 0.9f)] public float introRightEdge = 0.58f; // ขอบขวาของกล่องข้อความ (ไม่ให้ทับป้ายปุ่มควบคุม)

    // เก็บค่าเดิมของ centerText ไว้คืนหลังจบ Intro (ข้อความแจ้งเตือนกลางจอยังอยู่ตรงกลางเหมือนเดิม)
    Vector2 ctAnchorMin, ctAnchorMax, ctOffsetMin, ctOffsetMax, ctPivot;
    TextAlignmentOptions ctAlignment;

    [Header("ฉากจบ (เปิดประตูทางออกหลังเก็บของครบ)")]
    [TextArea(3, 8)]
    public string endingMessage = "คุณหนีออกมาได้แล้ว...\n\nแต่ไม่ว่าคุณจะไปที่ไหน\n'เธอ' ก็ยังชอบคุณเสมอ...";
    public string endingPrompt = "[กดปุ่มใดๆ เพื่อกลับสู่เมนูหลัก]";
    public float endingDelay = 1.5f;   // รอให้เห็นประตูเปิดก่อน แล้วค่อยมืดลง (วินาที)
    bool isEnding;

    [Header("เสียงพิมพ์ดีด (ตอนข้อความขึ้นทีละตัว)")]
    public AudioClip typeSound;                         // ลาก Keyboard_1 จาก Assets/Sound_1 มาใส่
    [Range(0f, 1f)] public float typeVolume = 0.7f;
    [Range(0f, 0.3f)] public float typePitchVariation = 0.06f; // สุ่มเสียงสูงต่ำเล็กน้อย ให้เหมือนพิมพ์ดีดจริง
    AudioSource typeSource;
    Coroutine introRoutine;
    Coroutine alertRoutine;

    bool isPaused;
    bool cursorWasLocked;

    // ให้ระบบอื่น (เช่น AmbienceManager) อ่านสถานะเกมได้
    public float Progress => itemsToFind.Length == 0 ? 0f : Mathf.Clamp01((float)currentIndex / itemsToFind.Length); // 0 = ยังไม่เก็บ, 1 = เก็บครบ
    public bool IsPlaying => isGameStarted && !isPaused && !isEnding;
    public bool IsEnding => isEnding;   // ใช้ตรวจว่าเมาส์หลุดล็อกบนเว็บ (กด ESC ในเบราว์เซอร์)

    GameMenuUI menu;
    Behaviour cinemachineBrain;   // ปิดชั่วคราวตอนใช้มุมกล้องเมนู
    Behaviour playerInput;        // (PlayerInput) ปิดชั่วคราวตอนอยู่ในเมนู/Intro ไม่ให้เมาส์หมุนกล้อง

    [TextArea]
    public string introMessage = "เมื่อคุณตื่นขึ้นมาในตึกร้าง...\nหาของส่วนตัว 9 ชิ้นเพื่อหนีออกไป\nแต่!!\nระวัง 'มัน' เอาไว้ให้ดี...";

    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        if (ghost != null) ghost.SetActive(false);
        Time.timeScale = 0f; 
        isGameStarted = false;
        cornerMissionText.gameObject.SetActive(false); // ซ่อนภารกิจมุมจอก่อน

#if ENABLE_INPUT_SYSTEM
        playerInput = FindFirstObjectByType<PlayerInput>();
#endif
        // มือถือ: สร้างปุ่มบนจอ (บนคอมจะไม่สร้าง)
        if (UseMobileControls)
            mobileControls = MobileControls.Create(centerText != null ? centerText.font : null, FixThaiText);

        SetPlayerControl(false); // ยังไม่ให้ผู้เล่นขยับ/หมุนกล้อง จนกว่าจะเริ่มเกม

        // สร้างเมนู + ป้ายปุ่มควบคุม (ใช้ฟอนต์เดียวกับข้อความ Intro ซึ่งรองรับภาษาไทย)
        menu = gameObject.AddComponent<GameMenuUI>();
        menu.onStart = OnStartButtonPressed;
        menu.onResume = ResumeGame;
        menu.onMainMenu = BackToMainMenu;
        menu.onTypeChar = PlayTypeSound;

        // AudioSource แบบ 2D สำหรับเสียงพิมพ์ดีด
        typeSource = gameObject.AddComponent<AudioSource>();
        typeSource.playOnAwake = false;
        typeSource.spatialBlend = 0f;
        menu.Build(centerText != null ? centerText.font : null, FixThaiText, titleThai, gameSubtitle, titleColor, showStartMenu, UseMobileControls);

        if (showStartMenu)
        {
            centerText.gameObject.SetActive(false); // ซ่อนข้อความ Intro ไว้ก่อน รอผู้เล่นกด START
            UseMenuCamera(true);
        }
        else
        {
            BeginIntro();
        }
    }

    // เริ่มโชว์ข้อความ "เมื่อคุณตื่นขึ้นมาในตึกร้าง..." พร้อมป้ายปุ่มควบคุม
    void BeginIntro()
    {
        menu.HideMenu();
        menu.ShowControls(true);
        centerText.gameObject.SetActive(true);
        SetIntroLayout(true);
        isIntroShowing = true;
        introStartFrame = Time.frameCount;
        introRoutine = StartCoroutine(TypeIntroText());
    }

    // ถูกเรียกเมื่อผู้เล่นกด START บนเมนู
    void OnStartButtonPressed()
    {
        UseMenuCamera(false);
        BeginIntro();
    }

    // ย้ายกล้องไปจุดเมนู (ปิด Cinemachine ชั่วคราว) หรือคืนกล้องให้ผู้เล่น
    void UseMenuCamera(bool on)
    {
        if (menuCameraPoint == null || Camera.main == null) return;

        if (cinemachineBrain == null)
            foreach (var b in Camera.main.GetComponents<Behaviour>())
                if (b.GetType().Name == "CinemachineBrain") cinemachineBrain = b;

        if (cinemachineBrain != null) cinemachineBrain.enabled = !on;
        if (on) Camera.main.transform.SetPositionAndRotation(menuCameraPoint.position, menuCameraPoint.rotation);
    }

    // จัดข้อความ Intro ให้อยู่ชิดซ้ายของจอ (on) หรือคืนตำแหน่งเดิม (off)
    void SetIntroLayout(bool on)
    {
        RectTransform rt = centerText.rectTransform;
        if (on)
        {
            ctAnchorMin = rt.anchorMin; ctAnchorMax = rt.anchorMax;
            ctOffsetMin = rt.offsetMin; ctOffsetMax = rt.offsetMax;
            ctPivot = rt.pivot; ctAlignment = centerText.alignment;

            rt.anchorMin = new Vector2(introLeftMargin, 0.08f);
            rt.anchorMax = new Vector2(introRightEdge, 0.92f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            centerText.alignment = TextAlignmentOptions.MidlineLeft;
        }
        else
        {
            rt.anchorMin = ctAnchorMin; rt.anchorMax = ctAnchorMax;
            rt.pivot = ctPivot;
            rt.offsetMin = ctOffsetMin; rt.offsetMax = ctOffsetMax;
            centerText.alignment = ctAlignment;
        }
    }

    void SetPlayerControl(bool on)
    {
        if (mobileControls != null)
        {
            // มือถือ: ปิด PlayerInput ตลอด (ไม่ให้การแตะจอไปหมุนกล้องเอง) แล้วใช้ปุ่มบนจอแทน
            if (playerInput != null) playerInput.enabled = false;
            mobileControls.SetActive(on);
            return;
        }
        if (playerInput != null) playerInput.enabled = on;
    }

    void Update()
    {
        // รอให้ผู้เล่นกดปุ่ม เพื่อเริ่มเกมหลังจากจบ Intro
        // (ต้องเป็นเฟรมหลังจากที่ Intro เริ่มแล้ว เพื่อไม่ให้การกดปุ่มเมนูไปข้าม Intro ทันที)
        if (!isGameStarted && isIntroShowing && Time.frameCount > introStartFrame)
        {
            bool isPressed = false;

#if ENABLE_INPUT_SYSTEM
            // สำหรับระบบ Input แบบใหม่
            if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame) isPressed = true;
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) isPressed = true;
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame) isPressed = true; // แตะจอ (มือถือ)
#else
            // สำหรับระบบ Input แบบเก่า
            if (Input.anyKeyDown || Input.GetMouseButtonDown(0)) isPressed = true;
#endif

            if (isPressed)
            {
                StartGame();
            }
        }

        else if (isGameStarted && !isEnding && menu != null)
        {
            // กด ESC เพื่อหยุดเกม / เล่นต่อ
            if (EscPressed())
            {
#if UNITY_WEBGL && !UNITY_EDITOR
                // บนเว็บ ESC ใช้หยุดเกมอย่างเดียว (เล่นต่อด้วยปุ่ม "เล่นต่อ") กันเกมหยุดแล้วเล่นต่อเองทันที
                if (!isPaused) PauseGame();
#else
                if (isPaused) ResumeGame();
                else PauseGame();
#endif
            }
            // ระหว่างเล่น กด H เพื่อเปิด/ปิดป้ายปุ่มควบคุม
            else if (!isPaused && HPressed())
            {
                menu.ToggleControls();
            }

#if UNITY_WEBGL && !UNITY_EDITOR
            // บนเว็บ เบราว์เซอร์จะใช้ปุ่ม ESC ปลดล็อกเมาส์เอง (เกมอาจไม่ได้รับปุ่ม ESC)
            // จึงหยุดเกมอัตโนมัติเมื่อเมาส์หลุดล็อก
            if (!isPaused)
            {
                if (Cursor.lockState == CursorLockMode.Locked) cursorWasLocked = true;
                else if (cursorWasLocked) PauseGame();
            }
#endif
        }
    }

    // ================= เสียงพิมพ์ดีด =================
    public void PlayTypeSound(char c)
    {
        if (typeSound == null || typeSource == null) return;
        if (char.IsWhiteSpace(c) || IsThaiCombiningMark(c)) return; // ไม่มีเสียงตอนเว้นวรรค/ขึ้นบรรทัด และสระบน-ล่าง/วรรณยุกต์ (กันเสียงซ้อนกับพยัญชนะ)
        typeSource.pitch = 1f + Random.Range(-typePitchVariation, typePitchVariation);
        typeSource.PlayOneShot(typeSound, typeVolume);
    }

    static bool IsThaiCombiningMark(char c)
    {
        return c == 'ั' || (c >= 'ิ' && c <= 'ฺ') || (c >= '็' && c <= '๎');
    }

    // ================= ฉากจบ =================
    // PlayerInteractor เรียกทุกครั้งที่ผู้เล่นเปิด/ปิดประตู
    public void OnDoorOpened(InteractableProp door)
    {
        if (!isGameStarted || isEnding) return;
        if (currentIndex < itemsToFind.Length) return;                  // ยังเก็บของไม่ครบ
        if (exitDoors == null || System.Array.IndexOf(exitDoors, door) < 0) return; // ไม่ใช่ประตูทางออก
        StartCoroutine(EndingRoutine());
    }

    IEnumerator EndingRoutine()
    {
        isEnding = true;
        SetPlayerControl(false);
        if (menu != null) { menu.ShowHint(false); menu.ShowControls(false); }

        // หนีพ้นแล้ว: ผีที่ไล่/คลานตามหยุดและหายไปทันที (กันโดนจับระหว่างฉากจบ)
        foreach (var chase in FindObjectsByType<ChaseStartTrigger>(FindObjectsSortMode.None)) chase.EndForEnding();
        foreach (var crawl in FindObjectsByType<GhostCeilingChase>(FindObjectsSortMode.None)) crawl.EndForEnding();

        // ค่อยๆ ลดเสียงทุกเสียงในเกม (เสียงไล่ล่า เสียงคลาน ฯลฯ) จนเงียบ ไปพร้อมกับจอที่มืดลง
        StartCoroutine(FadeOutWorldAudio(endingDelay + 2f));

        yield return new WaitForSeconds(endingDelay); // ให้เห็นประตูเปิดออกก่อน

        if (ghost != null) ghost.SetActive(false);
        cornerMissionText.gameObject.SetActive(false);
        centerText.gameObject.SetActive(false);
        Time.timeScale = 0f;

        yield return menu.PlayEnding(FixThaiText(endingMessage), FixThaiText(endingPrompt));

        // รอให้ผู้เล่นกดปุ่มใดๆ แล้วกลับไปหน้า START
        yield return null;
        while (!AnyInputPressed()) yield return null;
        BackToMainMenu();
    }

    // ลดเสียงทุก AudioSource ที่กำลังเล่นอยู่ (ยกเว้นเสียงพิมพ์ดีด) จนเงียบแล้วหยุด
    IEnumerator FadeOutWorldAudio(float duration)
    {
        var sources = new System.Collections.Generic.List<AudioSource>();
        var startVol = new System.Collections.Generic.List<float>();
        foreach (var s in FindObjectsByType<AudioSource>(FindObjectsSortMode.None))
        {
            if (s == typeSource || !s.isPlaying) continue;
            sources.Add(s);
            startVol.Add(s.volume);
        }

        for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
        {
            float k = 1f - t / duration;
            for (int i = 0; i < sources.Count; i++)
                if (sources[i] != null) sources[i].volume = startVol[i] * k;
            yield return null;
        }

        foreach (var s in sources) if (s != null) s.Stop();
    }

    bool AnyInputPressed()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame) return true;
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) return true;
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame) return true;
        return false;
#else
        return Input.anyKeyDown || Input.GetMouseButtonDown(0);
#endif
    }

    // ================= ระบบหยุดเกม =================
    void PauseGame()
    {
        isPaused = true;
        cursorWasLocked = false;
        Time.timeScale = 0f;
        AudioListener.pause = true;     // หยุดเสียงทั้งหมดในเกม
        SetPlayerControl(false);
        menu.ShowHint(false);
        menu.ShowControls(false);
        menu.ShowPause(true);
    }

    void ResumeGame()
    {
        isPaused = false;
        Time.timeScale = 1f;
        AudioListener.pause = false;
        SetPlayerControl(true);
        menu.ShowPause(false);
        menu.ShowControls(false);
        menu.ShowHint(true);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void BackToMainMenu()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex); // โหลดฉากใหม่ = กลับไปหน้าเมนู
    }

    bool EscPressed()
    {
        if (MobileInput.ConsumePause()) return true; // ปุ่ม "II" บนมือถือ
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.Escape);
#endif
    }

    bool HPressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.hKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.H);
#endif
    }

    IEnumerator TypeIntroText()
    {
        // 1. จัดเตรียมข้อความและซ่อมสระไทยให้เสร็จสมบูรณ์
        string fullMessage = FixThaiText(introMessage + "\n\n(ชิ้นแรกที่ต้องหา: " + itemsToFind[0] + ")\n\n" + (UseMobileControls ? "[แตะหน้าจอเพื่อเริ่มเกม]" : "[กดปุ่มใดๆ เพื่อเริ่มเกม]"));
        
        // 2. โยนข้อความทั้งหมดใส่ TextMeshPro ในครั้งเดียว (เพื่อให้มันอ่าน Tag voffset ได้สมบูรณ์)
        centerText.text = fullMessage;
        
        // 3. บังคับให้อัปเดตข้อความ เพื่อให้โปรแกรมเตรียมนับจำนวนตัวอักษร (ไม่นับพวก Tag คำสั่ง)
        centerText.ForceMeshUpdate();
        
        int totalCharacters = centerText.textInfo.characterCount;
        
        // 4. สั่งให้มองไม่เห็นตัวอักษรเลยในตอนแรก (ค่าเป็น 0)
        centerText.maxVisibleCharacters = 0;

        // 5. ค่อยๆ เพิ่มจำนวนตัวอักษรที่มองเห็นทีละ 1 ตัว
        for (int i = 0; i <= totalCharacters; i++)
        {
            centerText.maxVisibleCharacters = i;
            if (i > 0) PlayTypeSound(centerText.textInfo.characterInfo[i - 1].character); // เสียงพิมพ์ดีดของตัวที่เพิ่งขึ้น
            yield return new WaitForSecondsRealtime(0.05f); // ความเร็วในการพิมพ์
        }
    }

    void StartGame()
    {
        isGameStarted = true;
        isIntroShowing = false;
        if (introRoutine != null) { StopCoroutine(introRoutine); introRoutine = null; } // หยุดพิมพ์ (และเสียงพิมพ์ดีด) ถ้ากดข้าม Intro
        Time.timeScale = 1f; // ให้เวลาเดินปกติ
        centerText.gameObject.SetActive(false); // ปิดข้อความกลางจอ
        SetIntroLayout(false);                  // คืนตำแหน่งกลางจอให้ข้อความแจ้งเตือน
        centerText.maxVisibleCharacters = 99999; // ถ้ากดข้าม Intro กลางคัน ให้ข้อความต่อๆ ไปแสดงครบ
        ScareFX.Warmup();                        // เตรียมระบบเอฟเฟกต์ไว้ก่อน กันภาพกระตุกตอนผีโผล่ครั้งแรก
        if (menu != null) menu.ShowHint(true);  // โชว์ "ESC หยุดเกม / H ปุ่มควบคุม" มุมขวาล่าง
        if (menu != null) menu.ShowControls(false); // ซ่อนป้ายปุ่มควบคุม (กด H ดูซ้ำได้)

        // ให้ผู้เล่นควบคุมตัวละครได้ และล็อกเมาส์ไว้กลางจอ
        SetPlayerControl(true);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // เปิดข้อความภารกิจมุมจอ
        cornerMissionText.gameObject.SetActive(true); 
        UpdateCornerMissionUI();
    }

    // เปลี่ยนจาก void เป็น bool เพื่อให้มันตอบกลับได้ว่า "เก็บถูกชิ้นไหม"
    public bool CollectItem(string collectedName) 
    {
        if (!isGameStarted) return false; // เปลี่ยนเป็น return false;

        if (collectedName == itemsToFind[currentIndex])
        {
            if (ghost != null && ghostSpawnPoints.Length > currentIndex && ghostSpawnPoints[currentIndex] != null)
            {
                ghost.SetActive(true);
                ghost.transform.position = ghostSpawnPoints[currentIndex].position;
                ghost.transform.rotation = ghostSpawnPoints[currentIndex].rotation;
            }

            currentIndex++; 
            
            if (currentIndex < itemsToFind.Length)
            {
                UpdateCornerMissionUI();
                // ครอบ FixThaiText ตรงนี้
                ShowCenterAlert(FixThaiText("ชิ้นต่อไป: " + itemsToFind[currentIndex])); 
            }
            else
            {
                cornerMissionText.text = FixThaiText("ภารกิจ: หาทางออก!");
                ShowCenterAlert(FixThaiText("ได้กุญแจแล้ว! รีบหนี!!"));
                
                // วนลูปปลดล็อกประตูทางออกทุกบาน (ทั้งซ้ายและขวา)
                foreach (InteractableProp door in exitDoors)
                {
                    if (door != null)
                    {
                        door.isLocked = false; 
                    }
                }
            }
            
            return true; // <--- เก็บถูกชิ้น ส่งค่าบอกว่าสำเร็จ
        }
        
        return false; // <--- ชื่อไม่ตรง หรือผิดลำดับ ส่งค่าบอกว่าไม่สำเร็จ
    }

    void UpdateCornerMissionUI()
    {
      cornerMissionText.text = FixThaiText("ภารกิจ: " + itemsToFind[currentIndex] + " (" + (currentIndex + 1) + "/9)"); 
    }

    // ฟังก์ชันสำหรับโชว์ข้อความเตือนชั่วคราวกลางจอตอนเก็บของได้
    public void ShowCenterAlert(string message)
    {
        // หยุดข้อความเก่าก่อน ไม่ให้มันมาซ่อนข้อความใหม่ก่อนเวลา
        if (alertRoutine != null) StopCoroutine(alertRoutine);
        alertRoutine = StartCoroutine(AlertRoutine(message));
    }

    IEnumerator AlertRoutine(string message)
    {
        centerText.gameObject.SetActive(true);
        centerText.maxVisibleCharacters = 99999; // แสดงครบทุกตัว (กันค่าค้างจากตอนพิมพ์ Intro)
        centerText.text = message;
        yield return new WaitForSeconds(3f); // โชว์ค้างไว้ 3 วินาที
        centerText.gameObject.SetActive(false);
    }
    // ฟังก์ชันสำหรับแก้ปัญหาวรรณยุกต์ซ้อนสระบน (ใช้ระบบดันข้อความของ TextMeshPro)
    public string FixThaiText(string text)
    {
        char[] upperVowels = { 'ิ', 'ี', 'ึ', 'ื', 'ั', 'ํ' };
        char[] tones = { '่', '้', '๊', '๋', '์' };

        for (int i = 0; i < upperVowels.Length; i++)
        {
            for (int j = 0; j < tones.Length; j++)
            {
                // จับคู่สระบนและวรรณยุกต์
                string original = upperVowels[i].ToString() + tones[j].ToString();
                
                // ใช้ Tag <voffset> ของ TextMeshPro เพื่อดันเฉพาะวรรณยุกต์ให้ลอยสูงขึ้น
                string shifted = upperVowels[i].ToString() + "<voffset=0.25em>" + tones[j].ToString() + "</voffset>";
                
                text = text.Replace(original, shifted);
            }
        }
        return text;
    }
}