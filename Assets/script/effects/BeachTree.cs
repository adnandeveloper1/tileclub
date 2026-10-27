using UnityEngine;

public class BeachTree : MonoBehaviour
{
    [Header("Rotation point (jahan se patta tane se judta hai)")]
    [Tooltip("0,0 = neeche left   1,1 = upar right   0.5,0.5 = center")]
    public Vector2 pivotPoint = new Vector2(0f, 0.75f);

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

    RectTransform rt;
    Vector2 startPos;
    float startZ;
    Vector2 v;          // rect ke apne pivot se rotation point tak ka faasla
    float offset;

    void Start()
    {
        rt = transform as RectTransform;
        startPos = rt.anchoredPosition;
        startZ = rt.localEulerAngles.z;

        Vector2 size = rt.rect.size;
        Vector3 sc = rt.localScale;
        v = new Vector2(
            (pivotPoint.x - rt.pivot.x) * size.x * sc.x,
            (pivotPoint.y - rt.pivot.y) * size.y * sc.y);

        offset = randomizeStart ? Random.value * 20f : 0f;
    }

    static Vector2 Rot(Vector2 p, float deg)
    {
        float r = deg * Mathf.Deg2Rad;
        float c = Mathf.Cos(r), s = Mathf.Sin(r);
        return new Vector2(c * p.x - s * p.y, s * p.x + c * p.y);
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

        // Patta rotation point ke gird ghume, aur wo point apni jagah par hi rahe
        rt.localRotation = Quaternion.Euler(0f, 0f, startZ + z);
        rt.anchoredPosition = startPos + Rot(v, startZ) - Rot(v, startZ + z);
    }

    void OnDrawGizmosSelected()
    {
        var r = transform as RectTransform;
        if (r == null) return;
        Vector3 local = new Vector3(
            r.rect.x + pivotPoint.x * r.rect.width,
            r.rect.y + pivotPoint.y * r.rect.height, 0f);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(r.TransformPoint(local), 25f * Mathf.Abs(r.lossyScale.x));
    }
}