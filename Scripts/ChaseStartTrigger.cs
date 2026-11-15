using UnityEngine;

public class ChaseStartTrigger : MonoBehaviour
{
    [Header("1. เงื่อนไขการทำงาน")]
    public GameObject keyItem; // กุญแจที่ต้องหยิบก่อนถึงจะเริ่มไล่ล่า
    
    [Header("2. ตั้งค่าผีและการไล่ล่า")]
    public GameObject ghost;         // ตัวผีที่จะให้วิ่งไล่
    public Transform pointStart;     // จุดที่ผีจะวาร์ปมาโผล่ตอนเริ่ม
    public float chaseSpeed = 3.5f;  // ปรับความเร็วการวิ่ง/คลาน
    public AudioSource chaseSound;   // ลำโพงเสียงวิ่งไล่ล่า
    
    [Header("3. รูปแบบของผี")]
    public bool isCeilingGhost = false; // ติ๊กถูกถ้าใช้กับผีห้อยหัวบนเพดาน

    private bool isChasing = false;
    private bool hasTriggered = false;
    private bool quietPushed = false; // สั่งให้เสียงบรรยากาศเงียบอยู่หรือไม่
    private Transform player;

    void Start()
    {
        // ค้นหาผู้เล่นอัตโนมัติ
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;
        
        // ซ่อนผีไว้ก่อนตอนเริ่มเกม
        if (ghost != null) ghost.SetActive(false); 
    }

    private void OnTriggerEnter(Collider other)
    {
        // ถ้ายูสเซอร์เดินมาชน และสคริปต์นี้ยังไม่เคยทำงาน
        if (!hasTriggered && other.CompareTag("Player"))
        {
            // ตรวจสอบว่ากุญแจถูกหยิบไปแล้ว (null หรือปิด active ไปแล้ว)
            if (keyItem == null || !keyItem.activeInHierarchy)
            {
                hasTriggered = true;
                StartChase();
            }
        }
    }

    void StartChase()
    {
        if (ghost != null)
        {
            // 1. ย้ายผีไปรอที่ Point_Start
            ghost.transform.position = pointStart.position;
            ghost.transform.rotation = pointStart.rotation;
            
            // 2. เปิดตัวผีให้โผล่มา และเล่นเสียง
            ghost.SetActive(true);

            // เอฟเฟกต์: จอสั่น ไฟตรงจุดที่ผีโผล่กะพริบ และปิดเสียงบรรยากาศตลอดการไล่
            ScareFX.Hit(0.8f);
            ScareFX.FlickerNear(pointStart.position, 1.5f);
            if (!quietPushed) { ScareFX.PushQuiet(); quietPushed = true; }
            if (chaseSound != null) chaseSound.Play();

            // 3. สั่งให้เริ่มวิ่งไล่
            isChasing = true;
        }
    }

    void Update()
    {
        if (isChasing && ghost != null && player != null)
        {
            Vector3 targetPos = player.position;

            if (isCeilingGhost)
            {
                // โหมดผีเพดาน: ล็อกความสูงไว้บนฝ้า และห้อยหัวมอง
                targetPos.y = ghost.transform.position.y;
                ghost.transform.position = Vector3.MoveTowards(ghost.transform.position, targetPos, chaseSpeed * Time.deltaTime);

                Vector3 direction = targetPos - ghost.transform.position;
                if (direction != Vector3.zero)
                {
                    Quaternion targetRot = Quaternion.LookRotation(direction, Vector3.down);
                    ghost.transform.rotation = Quaternion.Slerp(ghost.transform.rotation, targetRot, Time.deltaTime * 5f);
                }
            }
            else
            {
                // โหมดผีเดินพื้น: เดินพุ่งไปหาผู้เล่นตรงๆ 
                ghost.transform.position = Vector3.MoveTowards(ghost.transform.position, targetPos, chaseSpeed * Time.deltaTime);

                Vector3 direction = targetPos - ghost.transform.position;
                direction.y = 0; // ล็อกแกน Y ไม่ให้ผีก้มหน้ามุดดินเวลาลงบันได
                if (direction != Vector3.zero)
                {
                    Quaternion targetRot = Quaternion.LookRotation(direction);
                    ghost.transform.rotation = Quaternion.Slerp(ghost.transform.rotation, targetRot, Time.deltaTime * 5f);
                }
            }
        }
    }

    // ฉากจบเรียกใช้: หยุดไล่และซ่อนผีทันที (เสียงปล่อยให้ GameManager ค่อยๆ ลดเสียงลงเอง)
    public void EndForEnding()
    {
        isChasing = false;
        hasTriggered = true; // กันไม่ให้เริ่มไล่ใหม่
        if (ghost != null) ghost.SetActive(false);
    }

    // ฟังก์ชันนี้เปิดไว้ให้ Point_End เรียกใช้เพื่อปิดผี
    public void StopChase()
    {
        isChasing = false;
        if (quietPushed) { ScareFX.PopQuiet(); quietPushed = false; } // ให้เสียงบรรยากาศกลับมา
        if (ghost != null)
        {
            ghost.SetActive(false); // ปิดการแสดงผลผี
        }
        
        // สิ่งที่เพิ่มเข้ามา: สั่งให้ลำโพงเสียงคลานหยุดทำงาน
        if (chaseSound != null)
        {
            chaseSound.Stop(); 
        }
    }
}