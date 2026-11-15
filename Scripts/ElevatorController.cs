using UnityEngine;

public class ElevatorController : MonoBehaviour
{
    [Header("Floors")]
    public int floorCount = 5;
    public float floorHeight = 3.2f;
    public int startFloor = 0;

    [Header("Doors (เรียงชั้น 1 ถึง 5)")]
    public FloorDoor[] doors;

    [Header("Cabin door (ประตูในลิฟ ขึ้นลงตามลิฟ)")]
    public FloorDoor cabinDoor;

    [Header("ตั้งค่าเสียงลิฟต์")]
public AudioSource doorSound;      // เสียงประตูเปิด-ปิด
public AudioSource moveSound;      // เสียงลิฟต์เคลื่อนที่

    [Header("Movement")]
    public float speed = 2f;
    public bool autoOpenOnArrival = true;       // ถึงชั้นแล้วเปิดประตูเอง

    [Header("Auto close (ปิดประตูอัตโนมัติ)")]
    public bool autoClose = true;
    public float autoCloseDelay = 6f;           // วินาทีที่ประตูเปิดค้างก่อนปิดเอง
    public bool waitWhilePlayerInDoorway = true; // ไม่ปิดถ้าผู้เล่นยืนอยู่หน้าประตู

    int currentFloor;
    int targetFloor;
    int requestedFloor = -1;   // ชั้นที่ถูกสั่งให้ไป (-1 = ไม่มี)
    bool requestFromHall;      // สั่งจากปุ่มด้านนอกหรือไม่
    float baseY;
    float autoCloseTimer;
    bool moving;
    bool playerInside;
    CharacterController playerController;

    public int CurrentFloor { get { return currentFloor; } }
    public bool IsMoving { get { return moving; } }

    // 1 = กำลังขึ้น, -1 = กำลังลง, 0 = หยุด
    public int Direction
    {
        get
        {
            if (!moving) return 0;
            return targetFloor > currentFloor ? 1 : -1;
        }
    }

    // ชั้นที่แสดงบนจอ (คำนวณจากความสูงจริง เปลี่ยนตามตอนลิฟวิ่งผ่าน)
    public int DisplayFloor
    {
        get
        {
            float f = (transform.position.y - baseY) / floorHeight;
            return Mathf.Clamp(Mathf.RoundToInt(f), 0, floorCount - 1);
        }
    }

    void Start()
    {
        currentFloor = startFloor;
        targetFloor = startFloor;
        baseY = transform.position.y - startFloor * floorHeight;
    }

    void Update()
    {
        if (moving)
        {
            MoveStep();
            return;
        }

        if (requestedFloor >= 0)
        {
            if (requestedFloor == currentFloor)
            {
                requestedFloor = -1;
                return;
            }

            // ปิดประตูก่อน แล้วค่อยออกตัว
            SetDoors(DoorAt(currentFloor), false);
            if (AllDoorsClosed()) StartMove(requestedFloor);
            return;
        }

        HandleAutoClose();
    }

    // ---------- ปิดประตูอัตโนมัติ ----------

    void HandleAutoClose()
    {
        FloorDoor door = DoorAt(currentFloor);

        // ปิดฟีเจอร์ หรือประตูไม่ได้เปิดอยู่ ไม่ต้องนับ
        if (!autoClose || door == null || !door.WantOpen)
        {
            autoCloseTimer = 0f;
            return;
        }

        // มีผู้เล่นยืนอยู่ที่ประตู (โซนหน้าประตูชั้นนี้) ให้รอและเริ่มนับใหม่
        if (waitWhilePlayerInDoorway && door.playerInHall)
        {
            autoCloseTimer = 0f;
            return;
        }

        autoCloseTimer += Time.deltaTime;
        if (autoCloseTimer >= autoCloseDelay)
        {
            SetDoors(door, false);   // ปิดทั้งประตูชั้นและประตูในลิฟ
            autoCloseTimer = 0f;
        }
    }

    // ---------- คำสั่งจากปุ่ม ----------

    public void PressCabinFloor(int floor)
    {
        if (moving || floor < 0 || floor >= floorCount) return;

        if (floor == currentFloor)
        {
            SetDoors(DoorAt(currentFloor), true);
            return;
        }
        requestedFloor = floor;
        requestFromHall = false;
    }

    public void PressHall(int floor)
    {
        if (moving || floor < 0 || floor >= floorCount) return;

        if (floor == currentFloor)
        {
            SetDoors(DoorAt(currentFloor), true);
            return;
        }
        requestedFloor = floor;
        requestFromHall = true;
    }

    public void PressOpen()
    {
        if (moving) return;
        SetDoors(DoorAt(currentFloor), true);
    }

    public void PressClose()
    {
        if (moving) return;
        SetDoors(DoorAt(currentFloor), false);
    }

    // ปุ่มนี้ควรสว่างหรือไม่ (ElevatorButton เรียกใช้)
    public bool IsButtonLit(ElevatorButtonType type, int floor)
    {
        FloorDoor door = DoorAt(currentFloor);

        switch (type)
        {
            case ElevatorButtonType.CabinFloor:
                return requestedFloor == floor && !requestFromHall;
            case ElevatorButtonType.HallUp:
            case ElevatorButtonType.HallDown:
                return requestedFloor == floor && requestFromHall;
            case ElevatorButtonType.Open:
                return door != null && door.WantOpen;
            case ElevatorButtonType.Close:
                // สว่างระหว่างประตูกำลังปิด และดับเมื่อปิดสนิท
                return door != null && !door.WantOpen && !door.FullyClosed;
            default:
                return false;
        }
    }

    // ---------- ภายใน ----------

    FloorDoor DoorAt(int floor)
    {
        if (doors == null || floor < 0 || floor >= doors.Length) return null;
        return doors[floor];
    }

    // เปิด/ปิดประตูชั้นที่ลิฟจอดอยู่ พร้อมประตูในลิฟ
    void SetDoors(FloorDoor hallDoor, bool open)
    {
        if (open) autoCloseTimer = 0f;   // สั่งเปิดทุกครั้ง เริ่มนับเวลาปิดใหม่
        if (doorSound != null) doorSound.Play();
        if (hallDoor != null) hallDoor.SetOpen(open);
        if (cabinDoor != null) cabinDoor.SetOpen(open);
    }

    bool AllDoorsClosed()
    {
        if (cabinDoor != null && !cabinDoor.FullyClosed) return false;
        if (doors == null) return true;
        for (int i = 0; i < doors.Length; i++)
            if (doors[i] != null && !doors[i].FullyClosed) return false;
        return true;
    }

    void StartMove(int floor)
    {
        targetFloor = floor;
        moving = true;
        if (moveSound != null && !moveSound.isPlaying) moveSound.Play();
    }

    void MoveStep()
    {
        float targetY = baseY + targetFloor * floorHeight;
        Vector3 pos = transform.position;
        float newY = Mathf.MoveTowards(pos.y, targetY, speed * Time.deltaTime);
        float dy = newY - pos.y;

        transform.position = new Vector3(pos.x, newY, pos.z);

        // พาผู้เล่นเคลื่อนที่ไปพร้อมลิฟ
        if (playerInside && playerController != null)
            playerController.Move(Vector3.up * dy);

        if (Mathf.Approximately(newY, targetY))
        {
            moving = false;
            currentFloor = targetFloor;
            requestedFloor = -1;
            if (moveSound != null) moveSound.Stop();
            if (autoOpenOnArrival) SetDoors(DoorAt(currentFloor), true);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        CharacterController cc = other.GetComponentInParent<CharacterController>();
        if (cc != null)
        {
            playerInside = true;
            playerController = cc;
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.GetComponentInParent<CharacterController>() != null)
        {
            playerInside = false;
            playerController = null;
        }
    }
}