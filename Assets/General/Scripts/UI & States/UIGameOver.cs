using UnityEngine;
using UnityEngine.UI;

public class UIGameOver : MonoBehaviour, IGameUI
{
    [Header("UI Elements")]
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private Button quitButton;

    public void Init()
    {
        if (restartButton != null)
            restartButton.onClick.AddListener(OnRestartClicked);

        if (mainMenuButton != null)
            mainMenuButton.onClick.AddListener(OnMainMenuClicked);

        if (quitButton != null)
            quitButton.onClick.AddListener(OnQuitClicked);
    }

    public void SetActive(bool active)
    {
        if (gameOverPanel != null)
            gameOverPanel.SetActive(active);
    }

    public UIManager.UIType GetUIType()
    {
        return UIManager.UIType.GameOver;
    }

    private void OnRestartClicked()
    {
        GameManager.Instance.RestartLevel();
    }

    private void OnMainMenuClicked()
    {
        GameManager.Instance.ReturnToMainMenu();
    }

    private void OnQuitClicked()
    {
        GameManager.Instance.QuitGame();
    }
}