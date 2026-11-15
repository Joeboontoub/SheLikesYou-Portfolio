using UnityEngine;
using System.Collections;

public class GhostWalkBy : MonoBehaviour
{
    [Header("ตั้งค่าผีเดินผ่าน")]
    public GameObject walkGhost; // ตัวผีแสดงแทน
    public Transform startPoint; // จุดเกิด
    public Transform endPoint;   // จุดเป้าหมายที่ให้เดินไป
    public float speed = 2.5f;   // ความเร็วในการเดิน (ปรับให้เดินช้าๆ จะดูหลอน)

    [Header("ระบบเสียง")]
    public AudioSource ghostAudioSource; // ตัวปล่อยเสียง (ลำโพง)
    public AudioClip scareSound;         // ไฟล์เสียงหลอน

    [Header("เอฟเฟกต์ตอนโผล่ (ScareFX)")]
    [Range(0f, 2f)] public float scareStrength = 0.4f; // ความแรงจอสั่น/ขอบจอแดง (0 = ไม่มี)
    public float lightFlickerTime = 1.2f;               // ไฟแถวนั้นกะพริบกี่วินาที

    private bool hasTriggered = false; // ป้องกันไม่ให้ทำงานซ้ำเวลาปิด-เปิดประตูใหม่

    public void TriggerWalk()
    {
        if (!hasTriggered && walkGhost != null)
        {
            hasTriggered = true;
            StartCoroutine(WalkRoutine());
        }
    }

    IEnumerator WalkRoutine()
    {
        // เอฟเฟกต์: เสียงบรรยากาศเงียบลง ไฟแถวนั้นกะพริบ จอสั่นเบาๆ
        ScareFX.Quiet(6f);
        ScareFX.FlickerNear(startPoint.position, lightFlickerTime);
        if (scareStrength > 0f) ScareFX.Hit(scareStrength);

        // 1. โผล่มาที่จุดเริ่มต้น
        walkGhost.SetActive(true);
        // เล่นเสียงทันทีที่ผีโผล่มา
        if (ghostAudioSource != null && scareSound != null)
        {
            ghostAudioSource.PlayOneShot(scareSound);
        }
        walkGhost.transform.position = startPoint.position;
        
        // 2. หันหน้าไปหาเป้าหมาย
        walkGhost.transform.LookAt(endPoint);

        // 3. สั่งให้เคลื่อนที่ไปหาจุดเป้าหมายอย่างช้าๆ
        while (Vector3.Distance(walkGhost.transform.position, endPoint.position) > 0.1f)
        {
            walkGhost.transform.position = Vector3.MoveTowards(walkGhost.transform.position, endPoint.position, speed * Time.deltaTime);
            yield return null;
        }

        // 4. เมื่อเดินถึงจุดเป้าหมาย (พ้นสายตา) ให้หายตัวไป
        walkGhost.SetActive(false);
    }
}