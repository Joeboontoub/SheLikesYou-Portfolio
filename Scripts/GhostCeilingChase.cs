using UnityEngine;
using System.Collections;

public class GhostCeilingChase : MonoBehaviour
{
    [Header("ตั้งค่าเหตุการณ์คลานบนเพดาน")]
    public GameObject itemToWatch;   // ของในตู้ที่ต้องหยิบ
    public GameObject crawlerGhost;  // ผีคลานบนเพดาน
    public Transform player;         // ตัวละครผู้เล่น
    public AudioSource crawlSound;   // ลำโพงเสียงคลานกุกกัก (ถ้ามี)

    [Header("ตั้งค่าการเคลื่อนที่")]
    public float crawlSpeed = 1.5f;  // ความเร็วในการคลาน (ปรับให้น้อยๆ จะค่อยๆ คลานมาหลอนๆ)
    public float turnSpeed = 3f;     // ความเร็วในการหันหน้ามองผู้เล่น

    [Header("ก่อนผีโผล่ / เอฟเฟกต์ (ScareFX)")]
    public float delayBeforeAppear = 1.2f;          // หยิบของแล้ว เงียบ+ไฟกะพริบกี่วินาที ก่อนผีโผล่
    [Range(0f, 2f)] public float scareStrength = 0.6f; // ความแรงจอสั่น/ขอบจอแดงตอนโผล่

    private bool isTriggered = false; // หยิบของแล้ว
    private bool isActive = false;    // ผีโผล่และกำลังคลานอยู่
    private bool quietPushed = false;

    void Start()
    {
        // หาตัวผู้เล่นอัตโนมัติถ้าลืมลากมาใส่
        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }
    }

    void Update()
    {
        // 1. ตัวจุดชนวน: เช็คว่าของโดนหยิบไปหรือยัง
        if (!isTriggered && (itemToWatch == null || !itemToWatch.activeInHierarchy))
        {
            isTriggered = true;
            if (crawlerGhost != null) StartCoroutine(AppearRoutine());
        }

        // ผีถูกปิดไปแล้ว (เช่น จับผู้เล่นได้) -> คืนเสียงบรรยากาศ
        if (quietPushed && (crawlerGhost == null || !crawlerGhost.activeInHierarchy))
        {
            ScareFX.PopQuiet();
            quietPushed = false;
        }

        // 2. ระบบคลานล่าเหยื่อ
        if (isActive && crawlerGhost != null && player != null)
        {
            // ล็อกความสูง (แกน Y) ให้อยู่ติดเพดานเท่าเดิมเสมอ
            Vector3 targetPosition = player.position;
            targetPosition.y = crawlerGhost.transform.position.y;

            // สั่งให้เคลื่อนที่ไปหาผู้เล่นอย่างช้าๆ
            crawlerGhost.transform.position = Vector3.MoveTowards(crawlerGhost.transform.position, targetPosition, crawlSpeed * Time.deltaTime);

            // สั่งให้หันหน้าไปหาเป้าหมาย (แบบห้อยหัว)
            Vector3 direction = targetPosition - crawlerGhost.transform.position;
            if (direction != Vector3.zero)
            {
                // ใช้ Vector3.down เป็นตัวบอกว่าด้านบนของผีคือพื้นด้านล่าง
                Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.down);
                crawlerGhost.transform.rotation = Quaternion.Slerp(crawlerGhost.transform.rotation, targetRotation, Time.deltaTime * turnSpeed);
            }
        }
    }

    // ฉากจบเรียกใช้: หยุดคลานและซ่อนผีทันที
    public void EndForEnding()
    {
        StopAllCoroutines();
        isTriggered = true;
        isActive = false;
        if (crawlerGhost != null) crawlerGhost.SetActive(false);
    }

    IEnumerator AppearRoutine()
    {
        // ความเงียบก่อนผีโผล่: เสียงบรรยากาศหายไป ไฟตรงจุดที่ผีจะโผล่กะพริบ
        ScareFX.Quiet(delayBeforeAppear + 1f);
        ScareFX.FlickerNear(crawlerGhost.transform.position, delayBeforeAppear, 6f); // กะพริบเฉพาะช่วงก่อนโผล่
        yield return new WaitForSeconds(delayBeforeAppear);

        crawlerGhost.SetActive(true); // เปิดตัวผี
        if (crawlSound != null) crawlSound.Play(); // เล่นเสียง
        if (scareStrength > 0f) ScareFX.Hit(scareStrength);

        ScareFX.PushQuiet(); // ปิดเสียงบรรยากาศตลอดเวลาที่ผีคลานอยู่
        quietPushed = true;
        isActive = true;
    }
}
