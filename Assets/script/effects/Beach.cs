using UnityEngine;

public class Beach : MonoBehaviour
{
    [Header("Sway (calm)")]
    public float angle = 2.5f;
    public float speed = 0.45f;
    public float secondaryAngle = 1.2f;
    public float secondarySpeed = 0.23f;

    [Header("Gust (optional)")]
    public bool useGust = true;
    public float gustAngle = 1.0f;
    public float gustSpeed = 0.12f;

    [Header("Variation")]
    public bool randomizeStart = true;

    float startZ;
    float offset;

    void Start()
    {
        startZ = transform.localEulerAngles.z;
        offset = randomizeStart ? Random.value * 20f : 0f;
    }

    void Update()
    {
        float t = Time.time + offset;

        float z = Mathf.Sin(t * speed) * angle
                + Mathf.Sin(t * secondarySpeed + 1.7f) * secondaryAngle;

        if (useGust)
        {
            float g = Mathf.SmoothStep(0f, 1f, (Mathf.Sin(t * gustSpeed) + 1f) * 0.5f);
            z += g * gustAngle * Mathf.Sin(t * speed * 1.3f);
        }

        transform.localRotation = Quaternion.Euler(0f, 0f, startZ + z);
    }
}