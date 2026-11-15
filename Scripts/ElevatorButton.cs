using UnityEngine;

public enum ElevatorButtonType { CabinFloor, Open, Close, HallUp, HallDown }

public class ElevatorButton : MonoBehaviour
{
    public ElevatorController elevator;
    public ElevatorButtonType type;
    public int floor;                    // 0 = ชั้น 1 ... 4 = ชั้น 5 (ใช้กับ CabinFloor, HallUp, HallDown)

    public Renderer buttonRenderer;      // ถ้าว่างจะใช้ Renderer ของวัตถุนี้
    public Material normalMaterial;      // วัสดุตอนปกติ (Button_Mat)
    public Material litMaterial;         // วัสดุตอนสว่าง (Button_Lit_Mat)
    public float flashSeconds = 0.25f;   // สว่างวูบทุกครั้งที่กด

    [Header("ตั้งค่าเสียง")]
public AudioSource buttonSound; // ลำโพงเสียงกดปุ่ม

    bool lit;
    float flashTimer;

    void Start()
    {
        if (buttonRenderer == null) buttonRenderer = GetComponent<Renderer>();
        SetLit(false);
    }

    void Update()
    {
        if (elevator == null) return;

        if (flashTimer > 0f) flashTimer -= Time.deltaTime;

        bool want = flashTimer > 0f || elevator.IsButtonLit(type, floor);
        if (want != lit) SetLit(want);
    }

    void SetLit(bool value)
    {
        lit = value;
        if (buttonRenderer != null && normalMaterial != null && litMaterial != null)
            buttonRenderer.sharedMaterial = value ? litMaterial : normalMaterial;
    }

    public void Press()
    {
        if (elevator == null) return;

        if (buttonSound != null) buttonSound.Play();

        flashTimer = flashSeconds;

        switch (type)
        {
            case ElevatorButtonType.CabinFloor:
                elevator.PressCabinFloor(floor);
                break;
            case ElevatorButtonType.HallUp:
            case ElevatorButtonType.HallDown:
                elevator.PressHall(floor);
                break;
            case ElevatorButtonType.Open:
                elevator.PressOpen();
                break;
            case ElevatorButtonType.Close:
                elevator.PressClose();
                break;
        }
    }
}