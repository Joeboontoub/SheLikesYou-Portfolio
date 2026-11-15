using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[DefaultExecutionOrder(100)]
public class Flashlight : MonoBehaviour
{
    public Light spot;               // Spot Light ของไฟฉาย
    public bool startOn = false;     // เริ่มเกมโดยเปิดไฟหรือไม่
    public float intensity = 10f;
    public AudioSource clickSound;   // เสียงคลิกเปิดปิด (ไม่ใส่ก็ได้)

    Transform cam;

    void Start()
    {
        if (spot == null) spot = GetComponent<Light>();
        if (Camera.main != null) cam = Camera.main.transform;

        spot.intensity = intensity;
        spot.enabled = startOn;
    }

    void Update()
    {
        if (TogglePressed())
        {
            spot.enabled = !spot.enabled;
            if (clickSound != null) clickSound.Play();
        }
    }

    void LateUpdate()
    {
        // หันไปทางเดียวกับกล้อง ให้ส่องตามที่ผู้เล่นมอง
        if (cam != null)
            transform.rotation = cam.rotation;
    }

    bool TogglePressed()
    {
        if (MobileInput.ConsumeFlashlight()) return true; // ปุ่ม "ไฟฉาย" บนมือถือ
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.lKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.L);
#endif
    }
}