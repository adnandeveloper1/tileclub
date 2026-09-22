using UnityEngine;

/// <summary>
/// Sirf isi kaam ke liye: jab GameBoardManager confirm kare ke level win ho gaya
/// (onLevelComplete event), tabhi ye assigned panel ko active karta hai.
/// Kisi aur jagah se (start pe, Inspector checkbox se) panel accidentally active
/// ho jaye to Awake() mein khud usse wapas hide kar deta hai.
/// </summary>
public class WinPanelActivator : MonoBehaviour
{
    [Header("Reference")]
    [Tooltip("GameBoardManager jiske onLevelComplete event ko sunna hai. Khali chhodo to khud auto-find kar lega.")]
    [SerializeField] private GameBoardManager gameBoardManager;

    [Header("Panel")]
    [Tooltip("Ye panel sirf tab active hoga jab GameBoardManager confirm kare ke level win ho gaya.")]
    [SerializeField] private GameObject winPanel;

    private void Awake()
    {
        if (gameBoardManager == null)
            gameBoardManager = FindObjectOfType<GameBoardManager>(true);

        // Safety: game start pe (ya Inspector mein galti se checked reh jaye to)
        // panel hamesha shuru mein chhupa rahe.
        if (winPanel != null)
            winPanel.SetActive(false);
    }

    private void OnEnable()
    {
        if (gameBoardManager != null)
            gameBoardManager.onLevelComplete.AddListener(ShowWinPanel);
    }

    private void OnDisable()
    {
        if (gameBoardManager != null)
            gameBoardManager.onLevelComplete.RemoveListener(ShowWinPanel);
    }

    public void ShowWinPanel()
    {
        if (winPanel != null)
            winPanel.SetActive(true);
    }
}