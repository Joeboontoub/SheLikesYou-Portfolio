using UnityEngine;

// ตัวกลางรับการกดปุ่มบนจอมือถือ ให้สคริปต์อื่น (PlayerInteractor, Flashlight, GameManager) อ่านได้
// เหมือนการกดคีย์บอร์ด/เมาส์ บนคอมจะไม่มีอะไรมาเรียก ค่าเป็น false ตลอด จึงไม่มีผลกับการเล่นบนคอม
public static class MobileInput
{
    const float Window = 0.25f; // กดแล้วต้องถูกอ่านภายในกี่วินาที (กันการกดค้างไว้แล้วไปทำงานทีหลัง)

    static float interactDown = -99f, interactUp = -99f, collect = -99f, flashlight = -99f, pause = -99f;

    public static bool InteractHeld { get; private set; }

    // ---- ปุ่มบนจอเรียก ----
    public static void PressInteract()   { InteractHeld = true;  interactDown = Time.unscaledTime; }
    public static void ReleaseInteract() { InteractHeld = false; interactUp = Time.unscaledTime; }
    public static void PressCollect()    { collect = Time.unscaledTime; }
    public static void PressFlashlight() { flashlight = Time.unscaledTime; }
    public static void PressPause()      { pause = Time.unscaledTime; }

    // ---- สคริปต์เกมอ่าน (อ่านแล้วถือว่าใช้ไปแล้ว) ----
    public static bool ConsumeInteractDown() => Consume(ref interactDown);
    public static bool ConsumeInteractUp()   => Consume(ref interactUp);
    public static bool ConsumeCollect()      => Consume(ref collect);
    public static bool ConsumeFlashlight()   => Consume(ref flashlight);
    public static bool ConsumePause()        => Consume(ref pause);

    static bool Consume(ref float t)
    {
        if (Time.unscaledTime - t <= Window) { t = -99f; return true; }
        return false;
    }

    public static void ResetAll()
    {
        InteractHeld = false;
        interactDown = interactUp = collect = flashlight = pause = -99f;
    }
}
