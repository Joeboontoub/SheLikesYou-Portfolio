using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// ยิงเส้นจากกลางจอ ถ้าชี้ปุ่มลิฟต์ หรือของที่โต้ตอบได้ จะขึ้นจุดแดง 
// คลิกซ้ายสั้นๆ เพื่อกด/เปิด, กดซ้ายค้างเพื่อถือของ, กด E เพื่อกิน
public class PlayerInteractor : MonoBehaviour
{
    public Camera cam;
    public float maxDistance = 3f;
    public LayerMask mask = ~0;
    
    [Header("UI Settings")]
    public float dotSize = 10f;
    public Color dotColor = Color.red;
    
    [Header("Grab Settings")]
    public float grabDistance = 1.5f; // ระยะห่างจากหน้ากล้องตอนถือของ
    public float grabSmooth = 10f;    // ความหนืดตอนถือ

    [Header("Sound Settings")]
    public AudioClip pickupSound;     // เสียงตอนกด E เก็บของ (ลากไฟล์ swish จาก Assets/Sound_1 มาใส่)
    [Range(0f, 1f)] public float pickupVolume = 1f;

    AudioSource sfxSource;
    GameObject exactTarget;
    GameObject nearbyTarget;
    
    // สถานะถือของ
    Rigidbody grabbedRb;
    bool isGrabbing;

    // สถานะกิน
    GameObject eatingObj;
    float eatTimer;

    Texture2D dot;
    static readonly RaycastHit[] hits = new RaycastHit[16];

    void Start()
    {
        if (cam == null) cam = GetComponent<Camera>();
        if (cam == null) cam = Camera.main;
        dot = MakeDot(32);

        // AudioSource แบบ 2D สำหรับเสียงเอฟเฟกต์ของผู้เล่น (เสียงจะไม่หายไปพร้อมของที่ถูกทำลาย)
        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.playOnAwake = false;
        sfxSource.spatialBlend = 0f;
    }

    void PlayPickupSound()
    {
        if (pickupSound != null && sfxSource != null)
            sfxSource.PlayOneShot(pickupSound, pickupVolume);
    }

    void Update()
    {
        if (Time.timeScale == 0f) return;
        // 1. ถ้ากำลังกินอยู่ ให้เล่นอนิเมชันเอาเข้าปากแล้วจบเลย
        if (eatingObj != null)
        {
            AnimateEating();
            return;
        }

        // 2. ถ้ากำลังกดค้างเพื่อถือของอยู่
        if (isGrabbing && grabbedRb != null)
        {
            if (ClickReleased()) Drop();
            else HandleGrabbing();
            return;
        }

        // 3. หาสิ่งของที่อยู่ตรงหน้า
        FindTargets();

        // 4. ระบบกดปุ่มและโต้ตอบ
        if (exactTarget != null)
        {
            ElevatorButton btn = exactTarget.GetComponent<ElevatorButton>();
            InteractableProp prop = exactTarget.GetComponent<InteractableProp>();
            Rigidbody rb = exactTarget.GetComponent<Rigidbody>();

            // เมื่อคลิกเมาส์ซ้าย
            if (ClickDown())
            {
                if (btn != null) btn.Press(); // กดลิฟต์
               else if (prop != null && (prop.isOpenableDoor || prop.isOpenableDrawer)) 
                {
                    if (prop.isLocked)
                    {
                        // ถ้าประตูล็อกอยู่ ให้โชว์ข้อความเตือน (ผ่านระบบแก้สระไทยแล้ว)
                        GameManager.instance.ShowCenterAlert(GameManager.instance.FixThaiText(prop.lockedMessage));
                    }
                    else
                    {
                        prop.ToggleOpen(); // เปิดได้ตามปกติ
                        GameManager.instance.OnDoorOpened(prop); // ถ้าเป็นประตูทางออกและเก็บของครบ -> ฉากจบ
                        
                        // เพิ่มตรงนี้: ถ้าประตูบานนี้มีสคริปต์ผีเดินผ่านอยู่ ให้สั่งผีทำงาน
                        if (prop.ghostWalkEvent != null)
                        {
                            prop.ghostWalkEvent.TriggerWalk();
                        }
                    }
                }
                else if (rb != null && (prop == null || prop.canGrab)) StartGrab(rb); // หยิบของ
            }

             // เมื่อกดปุ่ม E บนคีย์บอร์ด
            if (EPressed() && prop != null)
            {
                if (prop.isEdible) 
                {
                    StartEating(exactTarget);
                }
                else if (prop.isQuestItem) // ถ้าเป็นของภารกิจ
                {
                    // ส่งชื่อไปเช็คว่าใช่ชิ้นที่ต้องหาตอนนี้ไหม
                    bool isCorrect = GameManager.instance.CollectItem(prop.questItemName);
                    
                    if (isCorrect)
                    {
                        PlayPickupSound();
                        Destroy(exactTarget); // ถ้าถูกชิ้น ค่อยลบของทิ้ง
                        exactTarget = null;
                    }
                    else
                    {
                        // ถ้าหยิบผิดชิ้น หรือสะกดชื่อผิด ให้โชว์ข้อความเตือนกลางจอ
                        GameManager.instance.ShowCenterAlert("ยังไม่ใช่ชิ้นนี้ หรือยังไม่ถึงเวลาเก็บ!");
                    }
                }
            }
        }
    }

   void FindTargets()
    {
        exactTarget = null;
        nearbyTarget = null;
        if (cam == null) return;

        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        float bestDistance = float.MaxValue;

        // 1. ใช้เลเซอร์เส้นตรง (Raycast) ยิงหาแบบเป๊ะๆ ก่อนเป็นอันดับแรก
        int count = Physics.RaycastNonAlloc(ray, hits, maxDistance, mask, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < count; i++)
        {
            Collider c = hits[i].collider;
            if (c.GetComponentInParent<CharacterController>() != null) continue; // ข้ามตัวเอง

            if (c.GetComponent<ElevatorButton>() != null || c.GetComponent<InteractableProp>() != null || c.GetComponent<Rigidbody>() != null)
            {
                if (hits[i].distance < bestDistance)
                {
                    bestDistance = hits[i].distance;
                    exactTarget = c.gameObject;
                    nearbyTarget = c.gameObject;
                }
            }
        }

        // 2. ถ้าเลเซอร์เส้นตรงยิงไม่โดนของที่โต้ตอบได้เลย ค่อยใช้รัศมีกว้างๆ (SphereCast) ช่วยกวาดหาอีกรอบ
        if (exactTarget == null)
        {
            count = Physics.SphereCastNonAlloc(ray, 0.15f, hits, maxDistance, mask, QueryTriggerInteraction.Ignore);
            bestDistance = float.MaxValue;
            for (int i = 0; i < count; i++)
            {
                Collider c = hits[i].collider;
                if (c.GetComponentInParent<CharacterController>() != null) continue;

                if (c.GetComponent<ElevatorButton>() != null || c.GetComponent<InteractableProp>() != null || c.GetComponent<Rigidbody>() != null)
                {
                    if (hits[i].distance < bestDistance)
                    {
                        bestDistance = hits[i].distance;
                        exactTarget = c.gameObject;
                        nearbyTarget = c.gameObject;
                    }
                }
            }
        }
    }

    // --- ระบบหยิบของ ---
    void StartGrab(Rigidbody rb)
    {
        grabbedRb = rb;
        grabbedRb.useGravity = false; // ปิดแรงโน้มถ่วงชั่วคราว
        grabbedRb.linearDamping = 10f;         // ทำให้เคลื่อนไหวหนืดๆ สมูทขึ้น
        grabbedRb.freezeRotation = true; // ไม่ให้ของหมุนมั่วตอนถือ
        isGrabbing = true;
    }

    void HandleGrabbing()
    {
        // ใช้ Velocity ดึงวัตถุมาไว้ตรงหน้ากล้อง เพื่อป้องกันวัตถุทะลุกำแพง
        Vector3 targetPos = cam.transform.position + cam.transform.forward * grabDistance;
        grabbedRb.linearVelocity = (targetPos - grabbedRb.position) * grabSmooth;
    }

    void Drop()
    {
        if (grabbedRb != null)
        {
            grabbedRb.useGravity = true;
            grabbedRb.linearDamping = 0f;
            grabbedRb.freezeRotation = false;
        }
        isGrabbing = false;
        grabbedRb = null;
    }

    // --- ระบบกินของ ---
    void StartEating(GameObject food)
    {
        eatingObj = food;
        eatTimer = 0f;
        
        Rigidbody rb = food.GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true; // หยุดฟิสิกส์ทั้งหมด
        
        Collider col = food.GetComponent<Collider>();
        if (col != null) col.enabled = false;  // ปิดการชน
    }

    void AnimateEating()
    {
        eatTimer += Time.deltaTime * 2.5f; // ความเร็วยกเข้าปาก
        // เลื่อนเข้าหากล้อง และย่อขนาดลงจนหายไป
        eatingObj.transform.position = Vector3.Lerp(eatingObj.transform.position, cam.transform.position - new Vector3(0, 0.2f, 0), eatTimer);
        eatingObj.transform.localScale = Vector3.Lerp(eatingObj.transform.localScale, Vector3.zero, eatTimer);

        if (eatTimer >= 1f)
        {
            Destroy(eatingObj); // กินเสร็จทำลายทิ้ง
            eatingObj = null;
        }
    }

    // --- ระบบตรวจสอบ Input ---
    bool ClickDown()
    {
        if (MobileInput.ConsumeInteractDown()) return true; // ปุ่ม "แตะ" บนมือถือ
#if ENABLE_INPUT_SYSTEM
        return Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
#else
        return Input.GetMouseButtonDown(0);
#endif
    }

    bool ClickReleased()
    {
        if (MobileInput.ConsumeInteractUp()) return true; // ปล่อยปุ่ม "แตะ" บนมือถือ
#if ENABLE_INPUT_SYSTEM
        return Mouse.current != null && Mouse.current.leftButton.wasReleasedThisFrame;
#else
        return Input.GetMouseButtonUp(0);
#endif
    }

    bool EPressed()
    {
        if (MobileInput.ConsumeCollect()) return true; // ปุ่ม "เก็บ" บนมือถือ
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.E);
#endif
    }

    // --- ระบบวาดหน้าจอ ---
    void OnGUI()
    {
        if (isGrabbing || eatingObj != null) return; // ปิด UI ตอนถือของ/กินข้าว

        if (nearbyTarget != null && dot != null)
        {
            // วาดจุดแดง
            GUI.color = dotColor;
            GUI.DrawTexture(new Rect((Screen.width - dotSize) / 2f, (Screen.height - dotSize) / 2f, dotSize, dotSize), dot);
            GUI.color = Color.white;

            // ถ้ายืนยันเป้าหมายและสิ่งนั้นกินได้
            if (exactTarget == nearbyTarget)
            {
               InteractableProp prop = exactTarget.GetComponent<InteractableProp>();
                if (prop != null)
                {
                    string promptText = "";
                    if (prop.isEdible) promptText = "กิน (E)";
                    else if (prop.isQuestItem) promptText = "เก็บ (E)";

                    if (promptText != "")
                    {
                        GUIStyle style = new GUIStyle();
                        style.fontSize = 24;
                        style.normal.textColor = Color.yellow;
                        style.alignment = TextAnchor.MiddleCenter;
                        
                        GUIStyle shadow = new GUIStyle(style);
                        shadow.normal.textColor = Color.black;
                        
                        Rect rect = new Rect(0, Screen.height / 2f + 30f, Screen.width, 50);
                        Rect shadowRect = new Rect(2, Screen.height / 2f + 32f, Screen.width, 50);
                        
                        GUI.Label(shadowRect, promptText, shadow);
                        GUI.Label(rect, promptText, style);
                    }
                }
            }
        }
    }

    Texture2D MakeDot(int size)
    {
        Texture2D t = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float r = size / 2f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(r, r));
                t.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(r - d)));
            }
        }
        t.Apply();
        return t;
    }
}