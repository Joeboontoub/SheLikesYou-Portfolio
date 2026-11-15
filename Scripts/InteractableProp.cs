using UnityEngine;

public class InteractableProp : MonoBehaviour
{
    [Header("ประเภทการโต้ตอบ (เลือกได้หลายอัน)")]
    public bool isOpenableDoor;   // หมุนเปิด (เช่น ประตูตู้เย็น, ไมโครเวฟ)
    public bool isOpenableDrawer; // เลื่อนเปิด (เช่น ลิ้นชัก)
    public bool canGrab;          // จับโยกย้ายได้ (วัตถุต้องมี Rigidbody)
    public bool isEdible;         // กินได้ (กด E)
    [Header("ระบบภารกิจค้นหา")]
    public bool isQuestItem;      // ติ๊กว่าเป็นของทำภารกิจ
    public string questItemName;  // ตั้งชื่อของให้ตรงกับใน GameManager

    [Header("ตั้งค่าการเปิด/ปิด (สำหรับ Door และ Drawer)")]
    [Tooltip("ใส่องศาที่จะหมุน (Door) หรือระยะที่จะเลื่อน (Drawer) เช่น X, Y หรือ Z")]
    public Vector3 openAmount;    
    public float speed = 5f;
    public AudioSource openSound;


    [Header("ระบบประตูล็อก")]
    public bool isLocked; // ติ๊กเพื่อล็อกประตู
    public string lockedMessage = "ประตูล็อกอยู่... ต้องหากุญแจก่อน!";

    [Header("อีเวนต์ผีเดินผ่าน (ใส่หรือไม่ใส่ก็ได้)")]
    public GhostWalkBy ghostWalkEvent;

    bool isOpen;
    Vector3 closedPos, openPos;
    Quaternion closedRot, openRot;

    void Start()
    {
        closedPos = transform.localPosition;
        closedRot = transform.localRotation;
        
        // คำนวณจุดที่เปิดสุดเตรียมไว้
        openPos = closedPos + openAmount;
        openRot = closedRot * Quaternion.Euler(openAmount);
    }

    void Update()
    {
        // ทำงานเมื่อมีการคลิกสั่งเปิด/ปิด
        if (isOpenableDrawer)
            transform.localPosition = Vector3.Lerp(transform.localPosition, isOpen ? openPos : closedPos, Time.deltaTime * speed);
        
        if (isOpenableDoor)
            transform.localRotation = Quaternion.Lerp(transform.localRotation, isOpen ? openRot : closedRot, Time.deltaTime * speed);
    }

    public void ToggleOpen()
    {
        isOpen = !isOpen;

        // เสียงเปิดประตู (ใช้ AudioSource ปกติ)
        if (openSound != null)
        {
            openSound.Play();
        }
    }
}