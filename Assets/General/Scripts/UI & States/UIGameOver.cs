using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIGameOver : MonoBehaviour, IGameUI
{
    [Header("UI Elements")]
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private GameObject menuBackground;
    [SerializeField] private Button     retryButton;
    [SerializeField] private Button     mainMenuButton;
    [SerializeField] private Button     quitButton;
    [SerializeField] private TMP_Text   titleText;
    [SerializeField] private TMP_Text   subtitleText;

    public void Init()
    {
        if (retryButton    != null) retryButton.onClick.AddListener(OnRetryClicked);
        if (mainMenuButton != null) mainMenuButton.onClick.AddListener(OnMainMenuClicked);
        if (quitButton     != null) quitButton.onClick.AddListener(OnQuitClicked);
    }

    public void SetActive(bool active)
    {
        if (gameOverPanel  != null) gameOverPanel.SetActive(active);
        if (menuBackground != null) menuBackground.SetActive(active);
    }

    public UIManager.UIType GetUIType() => UIManager.UIType.GameOver;

    private void OnRetryClicked()    => GameManager.Instance.RestartGame();
    private void OnMainMenuClicked() => GameManager.Instance.ReturnToMainMenu();
    private void OnQuitClicked()     => GameManager.Instance.QuitGame();
}
