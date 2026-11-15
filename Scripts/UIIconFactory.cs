using UnityEngine;

// สร้างรูปไอคอน (ปุ่มคีย์บอร์ด / เมาส์ / ลูกศร) ด้วยโค้ด ไม่ต้องมีไฟล์รูป
public static class UIIconFactory
{
    public enum MouseButton { None, Left, Right }

    static Sprite roundedRect, leftFade;
    static Sprite mouseLeft, mouseLook;

    static readonly Color White = Color.white;
    static readonly Color Outline = new Color(0.55f, 0.55f, 0.58f, 1f);
    static readonly Color Highlight = new Color(0.62f, 0.62f, 0.66f, 1f);

    // สี่เหลี่ยมมุมมน (ใช้แบบ Sliced ยืดได้ทุกขนาด) สำหรับปุ่มคีย์บอร์ด
    public static Sprite RoundedRect()
    {
        if (roundedRect != null) return roundedRect;
        const int size = 64; const float r = 14f;
        var tex = NewTex(size, size);
        var px = new Color[size * size];
        Vector2 half = new Vector2(size / 2f, size / 2f);
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                Vector2 p = new Vector2(x + 0.5f, y + 0.5f) - half;
                float d = SdRoundBox(p, half - Vector2.one, r);
                px[y * size + x] = new Color(1, 1, 1, Cov(d));
            }
        tex.SetPixels(px); tex.Apply();
        roundedRect = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0,
            SpriteMeshType.FullRect, new Vector4(r + 2, r + 2, r + 2, r + 2));
        return roundedRect;
    }

    // แถบมืดจากซ้ายไปขวา ให้ตัวหนังสือเมนูอ่านง่ายบนฉากจริง
    public static Sprite LeftFade()
    {
        if (leftFade != null) return leftFade;
        const int w = 256;
        var tex = NewTex(w, 1);
        for (int x = 0; x < w; x++)
        {
            float t = x / (float)(w - 1);
            float a = Mathf.SmoothStep(0.85f, 0f, t);
            tex.SetPixel(x, 0, new Color(0, 0, 0, a));
        }
        tex.Apply();
        leftFade = Sprite.Create(tex, new Rect(0, 0, w, 1), new Vector2(0.5f, 0.5f));
        return leftFade;
    }

    // วงกลมขอบนุ่ม (ปุ่ม/จอยบนมือถือ)
    static Sprite circle, ring;
    public static Sprite Circle()
    {
        if (circle == null) circle = MakeCircle(false);
        return circle;
    }
    public static Sprite Ring()
    {
        if (ring == null) ring = MakeCircle(true);
        return ring;
    }
    static Sprite MakeCircle(bool ringOnly)
    {
        const int size = 128;
        var tex = NewTex(size, size);
        var px = new Color[size * size];
        float r = size / 2f - 1f;
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = new Vector2(x + 0.5f - size / 2f, y + 0.5f - size / 2f).magnitude - r; // <0 = ข้างใน
                float a = ringOnly ? Ring(d, 5f) : Cov(d);
                px[y * size + x] = new Color(1, 1, 1, a);
            }
        tex.SetPixels(px); tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }

    // ขอบจอมืด (โปร่งตรงกลาง ทึบที่ขอบ) ใช้ตอนผีโผล่
    static Sprite vignette;
    public static Sprite Vignette()
    {
        if (vignette != null) return vignette;
        const int size = 256;
        var tex = NewTex(size, size);
        var px = new Color[size * size];
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = (x + 0.5f) / size * 2f - 1f;
                float dy = (y + 0.5f) / size * 2f - 1f;
                float d = Mathf.Sqrt(dx * dx + dy * dy) / 1.414f;
                px[y * size + x] = new Color(1, 1, 1, Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.25f, 0.95f, d)));
            }
        tex.SetPixels(px); tex.Apply();
        vignette = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        return vignette;
    }

    // เมาส์ที่ไฮไลต์ปุ่มซ้าย (คลิก / กดค้าง)
    public static Sprite MouseLeftClick()
    {
        if (mouseLeft == null) mouseLeft = MakeMouse(MouseButton.Left, false);
        return mouseLeft;
    }

    // เมาส์ที่มีลูกศรซ้าย-ขวา (มองรอบตัว)
    public static Sprite MouseLook()
    {
        if (mouseLook == null) mouseLook = MakeMouse(MouseButton.None, true);
        return mouseLook;
    }

    static Sprite MakeMouse(MouseButton highlight, bool arrows)
    {
        const int s = 2; // วาดใหญ่ 2 เท่าเพื่อความคมชัด
        int h = 128 * s;
        int w = (arrows ? 256 : 128) * s;
        var tex = NewTex(w, h);
        var px = new Color[w * h];

        Vector2 c = new Vector2(w / 2f, h / 2f);
        Vector2 bodyHalf = new Vector2(30, 46) * s;
        float bodyR = 30 * s;
        float ow = 4.5f * s;               // ความหนาเส้นขอบ
        float splitY = c.y + 6 * s;        // เส้นแบ่งปุ่มบน/ล่าง
        Vector2 wheelC = new Vector2(c.x, c.y + 22 * s);
        Vector2 wheelHalf = new Vector2(5, 10) * s;

        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                Vector2 p = new Vector2(x + 0.5f, y + 0.5f);
                Color col = Color.clear;

                // ตัวเมาส์
                float d = SdRoundBox(p - c, bodyHalf, bodyR);
                float inside = Cov(d);
                if (inside > 0f)
                {
                    Blend(ref col, White, inside);

                    // ไฮไลต์ปุ่มที่ใช้
                    bool inLeft = p.x < c.x && p.y > splitY;
                    bool inRight = p.x > c.x && p.y > splitY;
                    if ((highlight == MouseButton.Left && inLeft) || (highlight == MouseButton.Right && inRight))
                        Blend(ref col, Highlight, inside);

                    // เส้นแบ่งปุ่มซ้าย/ขวา และเส้นแบ่งบน/ล่าง
                    if (p.y > splitY) Blend(ref col, Outline, inside * Line(p.x - c.x, 1.6f * s));
                    Blend(ref col, Outline, inside * Line(p.y - splitY, 1.6f * s));

                    // ลูกกลิ้ง
                    float wd = SdRoundBox(p - wheelC, wheelHalf, wheelHalf.x);
                    Blend(ref col, White, Cov(wd));
                    Blend(ref col, Outline, Ring(wd, 1.8f * s));
                }
                // เส้นขอบตัวเมาส์
                Blend(ref col, Outline, Ring(d, ow));

                // ลูกศรซ้าย/ขวา
                if (arrows)
                {
                    float a = Mathf.Max(Arrow(p, c, -1, s), Arrow(p, c, 1, s));
                    Blend(ref col, White, a);
                }

                px[y * w + x] = col;
            }

        tex.SetPixels(px); tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f * s);
    }

    // ลูกศรสามเหลี่ยม dir = -1 ซ้าย, 1 ขวา
    static float Arrow(Vector2 p, Vector2 c, int dir, int s)
    {
        float baseX = c.x + dir * 42 * s;   // ฐานลูกศร (ชิดเมาส์)
        float tipX = c.x + dir * 72 * s;    // ปลายลูกศร
        float headLen = 16 * s, headHalf = 13 * s, shaftHalf = 3.2f * s;

        float t = (p.x - baseX) * dir;      // ระยะจากฐานไปทางปลาย
        float len = Mathf.Abs(tipX - baseX);
        float dy = Mathf.Abs(p.y - c.y);
        if (t < 0f || t > len) return 0f;

        float headStart = len - headLen;
        if (t >= headStart)
        {
            float allowed = headHalf * (len - t) / headLen;
            return Mathf.Clamp01(allowed - dy + 0.5f);
        }
        return Mathf.Clamp01(shaftHalf - dy + 0.5f);
    }

    // ---------- ตัวช่วย ----------
    static Texture2D NewTex(int w, int h)
    {
        var t = new Texture2D(w, h, TextureFormat.RGBA32, false);
        t.wrapMode = TextureWrapMode.Clamp;
        t.filterMode = FilterMode.Bilinear;
        return t;
    }

    static float SdRoundBox(Vector2 p, Vector2 half, float r)
    {
        Vector2 q = new Vector2(Mathf.Abs(p.x), Mathf.Abs(p.y)) - half + new Vector2(r, r);
        return Mathf.Min(Mathf.Max(q.x, q.y), 0f) + new Vector2(Mathf.Max(q.x, 0f), Mathf.Max(q.y, 0f)).magnitude - r;
    }

    static float Cov(float d) => Mathf.Clamp01(0.5f - d);
    static float Ring(float d, float width) => Mathf.Clamp01(width * 0.5f + 0.5f - Mathf.Abs(d + width * 0.5f));
    static float Line(float dist, float width) => Mathf.Clamp01(width * 0.5f + 0.5f - Mathf.Abs(dist));

    static void Blend(ref Color dst, Color src, float a)
    {
        if (a <= 0f) return;
        float outA = a + dst.a * (1f - a);
        if (outA <= 0f) { dst = Color.clear; return; }
        Color rgb = (src * a + dst * dst.a * (1f - a)) / outA;
        dst = new Color(rgb.r, rgb.g, rgb.b, outA);
    }
}
