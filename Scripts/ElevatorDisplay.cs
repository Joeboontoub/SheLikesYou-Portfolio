using System;
using UnityEngine;
using TMPro;

public class ElevatorDisplay : MonoBehaviour
{
    public ElevatorController elevator;
    public TMP_Text floorLabel;   // ป้ายเลขชั้น
    public TMP_Text arrowLabel;   // ป้ายลูกศร

    public string upArrow = "^";
    public string downArrow = "v";
    public bool keepLastArrowWhenStopped = true;   // หยุดแล้วยังโชว์ทิศล่าสุด
    public bool logOnStart = true;                 // พิมพ์สถานะลง Console ตอนเริ่มเกม

    int lastFloor = -1;
    string lastArrow = null;
    string currentArrow = "^";

    void Start()
    {
        // ถ้าช่องว่าง หาป้ายใต้แผงนี้จากคำในชื่อ เช่น Display_Floor_1, Lbl_Floor
        if (floorLabel == null) floorLabel = FindLabel("Floor");
        if (arrowLabel == null) arrowLabel = FindLabel("Arrow");

        if (logOnStart)
        {
            Debug.Log("[ElevatorDisplay] " + name
                + " | elevator=" + (elevator != null ? elevator.name : "ว่าง")
                + " | floorLabel=" + (floorLabel != null ? floorLabel.name : "ว่าง")
                + " | arrowLabel=" + (arrowLabel != null ? arrowLabel.name : "ว่าง"), this);
        }
    }

    TMP_Text FindLabel(string keyword)
    {
        foreach (TMP_Text t in GetComponentsInChildren<TMP_Text>(true))
        {
            if (t.gameObject.name.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0)
                return t;
        }
        return null;
    }

    void Update()
    {
        if (elevator == null) return;

        int floor = elevator.DisplayFloor;
        int dir = elevator.Direction;

        if (floorLabel != null && floor != lastFloor)
        {
            floorLabel.text = (floor + 1).ToString();
            lastFloor = floor;
        }

        if (dir > 0) currentArrow = upArrow;
        else if (dir < 0) currentArrow = downArrow;
        else if (!keepLastArrowWhenStopped) currentArrow = "";

        if (arrowLabel != null && currentArrow != lastArrow)
        {
            arrowLabel.text = currentArrow;
            lastArrow = currentArrow;
        }
    }
}