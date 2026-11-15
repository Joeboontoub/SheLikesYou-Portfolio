using UnityEngine;

// ให้วัตถุนี้ขึ้นลงตามความสูงของ target (ห้องลิฟ) โดยไม่ขยับแกน X/Z
public class FollowY : MonoBehaviour
{
    public Transform target;     // ห้องลิฟที่จะตาม (Elevator_Cabin)
    float offsetY;

    void Start()
    {
        if (target != null)
            offsetY = transform.position.y - target.position.y;
    }

    void LateUpdate()
    {
        if (target == null) return;
        Vector3 p = transform.position;
        p.y = target.position.y + offsetY;
        transform.position = p;
    }
}