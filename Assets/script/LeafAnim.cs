using UnityEngine;

public class LeafAnim : MonoBehaviour
{
    [Header("Fall")]
    [SerializeField] private float fallSpeed = 200f;
    [SerializeField] private float resetY = -1050f;
    [SerializeField] private float startY = 1050f;

    [Header("Sway")]
    [SerializeField] private float swayAmount = 40f;
    [SerializeField] private float swaySpeed = 2f;

    [Header("Rotation")]
    [SerializeField] private bool continuousSpin = false;   // true = poora ghoomega
    [SerializeField] private float spinSpeed = 90f;         // continuousSpin ke liye (degrees/sec)
    [SerializeField] private float tiltAmount = 35f;        // swing wali rotation (degrees)
    [SerializeField] private float tiltSpeed = 2f;

    private RectTransform rect;
    private float startX;
    private float timeOffset;

    private void Awake()
    {
        rect = GetComponent<RectTransform>();
        startX = rect.anchoredPosition.x;
        timeOffset = Random.value * 10f;
    }

    private void Update()
    {
        float t = Time.time + timeOffset;
        Vector2 pos = rect.anchoredPosition;

        // Fall + sway
        pos.y -= fallSpeed * Time.deltaTime;
        pos.x = startX + Mathf.Sin(t * swaySpeed) * swayAmount;

        if (pos.y < resetY)
            pos.y = startY;

        rect.anchoredPosition = pos;

        // Rotation
        if (continuousSpin)
        {
            rect.Rotate(0f, 0f, spinSpeed * Time.deltaTime);
        }
        else
        {
            float angle = Mathf.Sin(t * tiltSpeed) * tiltAmount;
            rect.localRotation = Quaternion.Euler(0f, 0f, angle);
        }
    }
}