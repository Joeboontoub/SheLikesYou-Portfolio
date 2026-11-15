using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class GhostKillPlayer : MonoBehaviour
{
    [Header("ตั้งค่าการตาย (Jumpscare)")]
    public Transform playerCamera;
    public float stareTime = 2f;
    public float shakeIntensity = 0.1f;
    public bool isCeilingGhost = false;
    public GameObject jumpscareBackground; // แผ่นฉากหลังที่จะโผล่มา
    public AudioSource jumpscareSound; // ลำโพงเสียงตอนโดนจับ
    public AudioSource crawlSoundToStop; // เพิ่มบรรทัดนี้: ลำโพงเสียงคลานที่จะสั่งให้หยุด

    [Header("ตั้งค่าอนิเมชันจ้องหน้า")]
    public Animator ghostAnimator;       // ลากผีตัวนี้มาใส่ เพื่อเข้าถึง Animator
    public string standAnimStateName = "Stand"; // ชื่อท่าใน Animator ที่ต้องการให้เล่นตอนจ้องหน้า

    private bool isDead = false;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !isDead)
        {
            StartCoroutine(JumpscareRoutine(other.gameObject));
        }
    }

    IEnumerator JumpscareRoutine(GameObject player)
    {
        isDead = true;

        // สิ่งที่เพิ่มเข้ามา: หยุดเสียงคลานทันทีที่โดนจับ
        if (crawlSoundToStop != null)
        {
            crawlSoundToStop.Stop();
        }

        // เล่นเสียงตุ้งแช่ทันทีที่โดนจับ
        if (jumpscareSound != null)
        {
            jumpscareSound.Play();
        }
        ScareFX.Hit(1.6f); // ขอบจอแดงมืดเต็มที่ + จอสั่นแรง

        // หยุดผู้เล่น
        var controller = player.GetComponent<CharacterController>();
        if (controller != null) controller.enabled = false;

        // **จุดที่เพิ่มเข้ามา:** สั่งให้อนิเมชันตัดจบท่าคลาน แล้วเล่น "ท่ายืน" ทันที
        if (ghostAnimator != null)
        {
            ghostAnimator.Play(standAnimStateName);
        }
        // เปิดแผ่นฉากหลังสีๆ ทันทีที่โดนจับ
        if (jumpscareBackground != null)
        {
            jumpscareBackground.SetActive(true);
        }

        Vector3 originalCamPos = playerCamera.localPosition;
        float timer = 0;

        while (timer < stareTime)
        {
            // ดึงผีมาหน้ากล้อง (ผมปรับให้ต่ำลงนิดนึง หน้าผีจะได้ตรงกับกล้องพอดีตอนยืน)
            transform.position = playerCamera.position + (playerCamera.forward * 0.8f) - (playerCamera.up * 0.4f);
            
            transform.LookAt(playerCamera);
            
            if (isCeilingGhost) 
            {
                transform.rotation *= Quaternion.Euler(0, 0, 180); 
            }

            // กล้องสั่น
            playerCamera.localPosition = originalCamPos + Random.insideUnitSphere * shakeIntensity;

            timer += Time.deltaTime;
            yield return null;
        }

        playerCamera.localPosition = originalCamPos;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}