using UnityEngine;

public class GhostCeilingLook : MonoBehaviour
{
    public Transform player;
    public float turnSpeed = 2f;

    void Start()
    {
        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }
    }

    void Update()
    {
        if (player != null)
        {
            Vector3 direction = player.position - transform.position;
            
            if (direction != Vector3.zero)
            {
                // จุดสำคัญ: สั่งให้ LookRotation รู้ว่า "ทิศด้านบน" ของผีตัวนี้ คือการชี้ลงพื้น (Vector3.down)
                Quaternion targetRot = Quaternion.LookRotation(direction, Vector3.down);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * turnSpeed);
            }
        }
    }
}