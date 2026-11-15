using UnityEngine;

public class FloorDoor : MonoBehaviour
{
    public Transform panelA;             // บานที่ Z น้อย (เลื่อนไปทาง -Z)
    public Transform panelB;             // บานที่ Z มาก (เลื่อนไปทาง +Z)
    public float slideDistance = 0.45f;  // ระยะที่บานเลื่อนออก
    public float openTime = 1f;          // วินาทีที่ใช้เปิด/ปิด

    [HideInInspector] public bool playerInHall;  // ตัวละครอยู่หน้าประตูชั้นนี้

    float amount;                        // 0 = ปิดสนิท, 1 = เปิดสุด
    bool wantOpen;
    Vector3 aClosed, bClosed;

    public bool WantOpen { get { return wantOpen; } }
    public bool FullyClosed { get { return !wantOpen && amount <= 0.001f; } }

    void Start()
    {
        aClosed = panelA.localPosition;
        bClosed = panelB.localPosition;
    }

    void Update()
    {
        amount = Mathf.MoveTowards(amount, wantOpen ? 1f : 0f, Time.deltaTime / openTime);
        float t = Mathf.SmoothStep(0f, 1f, amount);
        panelA.localPosition = aClosed + new Vector3(0f, 0f, -slideDistance * t);
        panelB.localPosition = bClosed + new Vector3(0f, 0f, slideDistance * t);
    }

    public void SetOpen(bool open)
    {
        wantOpen = open;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.GetComponentInParent<CharacterController>() != null)
            playerInHall = true;
    }

    void OnTriggerExit(Collider other)
    {
        if (other.GetComponentInParent<CharacterController>() != null)
            playerInHall = false;
    }
}