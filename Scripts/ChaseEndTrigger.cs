using UnityEngine;

public class ChaseEndTrigger : MonoBehaviour
{
    [Header("ลากสคริปต์จุดเริ่มต้น (Start Trigger) มาเชื่อมตรงนี้")]
    public ChaseStartTrigger startTriggerManager;

    private void OnTriggerEnter(Collider other)
    {
        // เมื่อผู้เล่นหนีมาถึงเส้นชัย
        if (other.CompareTag("Player"))
        {
            if (startTriggerManager != null)
            {
                startTriggerManager.StopChase(); // สั่งระงับการไล่ล่าและผีหายตัวไป
            }
        }
    }
}