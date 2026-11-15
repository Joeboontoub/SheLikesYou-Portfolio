using UnityEngine;

public class FlickerLight : MonoBehaviour
{
    public Light targetLight;
    public float normalIntensity = 1.5f;
    public bool flickering = false;        // ติ๊กเฉพาะดวงที่ต้องการให้กะพริบ
    public float minInterval = 0.05f;
    public float maxInterval = 0.4f;
    [Range(0f, 1f)] public float offChance = 0.3f;   // โอกาสที่ไฟดับวูบ

    float timer;

    void Reset()
    {
        targetLight = GetComponent<Light>();
    }

    void Update()
    {
        if (targetLight == null) return;

        if (!flickering)
        {
            targetLight.intensity = normalIntensity;
            return;
        }

        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            timer = Random.Range(minInterval, maxInterval);
            targetLight.intensity = Random.value < offChance
                ? 0f
                : normalIntensity * Random.Range(0.4f, 1f);
        }
    }

    public void SetFlickering(bool value)
    {
        flickering = value;
    }
}