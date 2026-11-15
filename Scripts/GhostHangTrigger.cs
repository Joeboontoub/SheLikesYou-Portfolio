using UnityEngine;
using System.Collections; // เพิ่มบรรทัดนี้เพื่อใช้งาน Coroutine (ระบบหน่วงเวลา)

public class GhostHangTrigger : MonoBehaviour
{
    [Header("ตั้งค่าเหตุการณ์ห้อยหัว")]
    public GameObject itemInCabinet;
    public GameObject ceilingGhost;
    public AudioSource jumpscareSound;

    [Header("ตั้งค่าการเคลื่อนที่ (Jumpscare)")]
    public float dropDistance = 2.5f; // ระยะที่พุ่งทะลุฝ้าลงมา (หน่วยเป็นเมตร)
    public float dropSpeed = 15f;     // ความเร็วตอนพุ่งลงและดึงกลับ (ยิ่งเยอะยิ่งกระชากแรง)
    public float waitTime = 2f;       // เวลาที่ค้างไว้ให้ตกใจ (วินาที)

    [Header("ก่อนผีโผล่ / เอฟเฟกต์ (ScareFX)")]
    public float delayBeforeDrop = 1.0f; // หยิบของแล้ว เงียบ+ไฟกะพริบกี่วินาที ก่อนผีพุ่งลงมา
    [Range(0f, 2f)] public float scareStrength = 1f; // ความแรงจอสั่น/ขอบจอแดง

    private bool isTriggered = false;
    private Vector3 originalPosition;
    private Vector3 targetPosition;

    void Start()
    {
        // จดจำตำแหน่งที่ซ่อนอยู่บนฝ้า และคำนวณจุดที่ต้องพุ่งลงมา
        if (ceilingGhost != null)
        {
            originalPosition = ceilingGhost.transform.position;
            targetPosition = originalPosition - new Vector3(0, dropDistance, 0); 
        }
    }

    void Update()
    {
        if (!isTriggered && (itemInCabinet == null || !itemInCabinet.activeInHierarchy))
        {
            isTriggered = true;
            if (ceilingGhost != null)
            {
                StartCoroutine(DropAndRetractRoutine());
            }
        }
    }

    IEnumerator DropAndRetractRoutine()
    {
        // 0. ความเงียบก่อนผีโผล่: เสียงบรรยากาศหายไป ไฟแถวนั้นกะพริบ
        ScareFX.Quiet(delayBeforeDrop + waitTime + 4f);
        ScareFX.FlickerNear(originalPosition, delayBeforeDrop, 6f); // กะพริบเฉพาะช่วงก่อนโผล่ ตอนผีพุ่งลงมาภาพต้องนิ่ง
        yield return new WaitForSeconds(delayBeforeDrop);

        // 1. เปิดตัวผี และกระแทกเสียง Jumpscare
        ceilingGhost.SetActive(true);
        if (jumpscareSound != null) jumpscareSound.Play();
        if (scareStrength > 0f) ScareFX.Hit(scareStrength);

        // 2. พุ่งทะลุเพดานลงมาอย่างรวดเร็ว
        while (Vector3.Distance(ceilingGhost.transform.position, targetPosition) > 0.01f)
        {
            ceilingGhost.transform.position = Vector3.MoveTowards(ceilingGhost.transform.position, targetPosition, dropSpeed * Time.deltaTime);
            yield return null;
        }

        // 3. ห้อยหัวค้างไว้จ้องหน้าผู้เล่นตามเวลาที่กำหนด (2 วินาที)
        yield return new WaitForSeconds(waitTime);

        // 4. กระชากกลับขึ้นฝ้าเพดานอย่างรวดเร็ว
        while (Vector3.Distance(ceilingGhost.transform.position, originalPosition) > 0.01f)
        {
            ceilingGhost.transform.position = Vector3.MoveTowards(ceilingGhost.transform.position, originalPosition, dropSpeed * Time.deltaTime);
            yield return null;
        }

        // 5. ปิดการทำงานผี
        ceilingGhost.SetActive(false);
    }
}