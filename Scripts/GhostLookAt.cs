using UnityEngine;

public class GhostLookAt : MonoBehaviour
{
    [Header("ตั้งค่าการมอง")]
    public Transform player; // ลากตัวผู้เล่นมาใส่ช่องนี้
    public float turnSpeed = 2f; // ความเร็วในการหันหน้า (ยิ่งค่าน้อย ยิ่งหันช้าๆ หลอนๆ)

    void Start()
    {
        // ถ้าลืมลากผู้เล่นมาใส่ ให้ระบบพยายามหาอัตโนมัติจาก Tag "Player"
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
            // หาว่าผู้เล่นอยู่ทิศไหนเมื่อเทียบกับตัวผี
            Vector3 direction = player.position - transform.position;
            
            // บังคับให้แกน Y เป็น 0 ผีจะได้ไม่ก้มหน้าหรือเงยหน้าแปลกๆ (ให้หันแค่ซ้าย-ขวา)
            direction.y = 0; 

            if (direction != Vector3.zero)
            {
                // คำนวณองศาเป้าหมายที่ต้องหันไป
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                
                // ค่อยๆ หมุนตัวตามไปหาผู้เล่นอย่างช้าๆ
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * turnSpeed);
            }
        }
    }
}