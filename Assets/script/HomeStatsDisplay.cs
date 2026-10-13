using TMPro;
using UnityEngine;

public class HomeStatsDisplay : MonoBehaviour
{
    [SerializeField] private TMP_Text coinText;
    [SerializeField] private TMP_Text wisdomText;

    private bool subscribed;

    private void OnEnable()
    {
        Subscribe();
        Refresh();
    }

    private void Start()
    {
        // Agar GameManager OnEnable ke waqt tak ready nahi tha (pehli baar game start par)
        Subscribe();
        Refresh();
    }

    private void OnDisable()
    {
        if (subscribed && GameManager.Instance != null)
        {
            GameManager.Instance.OnCoinsChanged -= OnCoinsChanged;
            GameManager.Instance.OnWisdomChanged -= OnWisdomChanged;
        }
        subscribed = false;
    }

    private void Subscribe()
    {
        if (subscribed || GameManager.Instance == null) return;
        GameManager.Instance.OnCoinsChanged += OnCoinsChanged;
        GameManager.Instance.OnWisdomChanged += OnWisdomChanged;
        subscribed = true;
    }

    private void Refresh()
    {
        if (GameManager.Instance == null) return;
        if (coinText != null) coinText.text = GameManager.Instance.Coins.ToString();
        if (wisdomText != null) wisdomText.text = GameManager.Instance.Wisdom.ToString();
    }

    private void OnCoinsChanged(int coins)
    {
        if (coinText != null) coinText.text = coins.ToString();
    }

    private void OnWisdomChanged(int wisdom)
    {
        if (wisdomText != null) wisdomText.text = wisdom.ToString();
    }
}