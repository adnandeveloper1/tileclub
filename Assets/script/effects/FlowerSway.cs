using UnityEngine;

public class FlowerSway : MonoBehaviour
{
    [Header("Halka sa hamesha hilna")]
    public float idleAngle = 1.5f;
    public float idleSpeed = 0.8f;

    [Header("Hawa ka jhonka")]
    public float gustAngle = 5f;        // jhonke mein kitna jhukna (degrees)
    public float gustSpeed = 0.25f;     // chhota = jhonke kam aur dheere dheere
    public float gustSharpness = 0.4f;  // bada = sirf tez jhonke asar karein, chhota = zyada waqt hile
    public float windDirection = -1f;   // -1 = right ki taraf jhukna, 1 = left ki taraf

    [Header("Jhonke mein halka kaanpna")]
    public float flutterAngle = 1f;
    public float flutterSpeed = 4f;

    [Header("Har phool alag waqt par")]
    public float travel = 1.5f;         // x position ke hisaab se jhonka der se pohanche
    public bool randomizeStart = true;

    float startZ;
    float offset;
    RectTransform rt;

    void Start()
    {
        rt = transform as RectTransform;
        startZ = transform.localEulerAngles.z;
        offset = randomizeStart ? Random.value * 20f : 0f;
    }

    void Update()
    {
        float t = Time.time;
        float x = rt != null ? rt.anchoredPosition.x * 0.001f : transform.position.x * 0.1f;

        // Jhonka: Perlin noise se kabhi aata hai, kabhi nahi
        float gustT = t * gustSpeed - x * travel;
        float n = Mathf.PerlinNoise(gustT, 3.7f);
        float g = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.5f - gustSharpness * 0.5f,
                                                              0.5f + gustSharpness * 0.5f, n));

        float idle = Mathf.Sin((t + offset) * idleSpeed) * idleAngle;
        float flutter = Mathf.Sin((t + offset) * flutterSpeed) * flutterAngle * g;

        float z = idle + g * gustAngle * windDirection + flutter;

        transform.localRotation = Quaternion.Euler(0f, 0f, startZ + z);
    }
}