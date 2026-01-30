using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIMainMenu : MonoBehaviour, IGameUI
{
    [Header("UI Elements")]
    [SerializeField] private GameObject menuPanel;
    [SerializeField] private Button startButton;
    [SerializeField] private Button optionsButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private TMP_Text titleText;

    public void Init()
    {
        if (startButton != null)
            startButton.onClick.AddListener(OnStartClicked);

        if (optionsButton != null)
            optionsButton.onClick.AddListener(OnOptionsClicked);

        if (quitButton != null)
            quitButton.onClick.AddListener(OnQuitClicked);
    }

    public void SetActive(bool active)
    {
        if (menuPanel != null)
            menuPanel.SetActive(active);
    }

    public UIManager.UIType GetUIType()
    {
        return UIManager.UIType.MainMenu;
    }

    private void OnStartClicked()
    {
        GameStateManager.instance.SetCurrentGameState(GameStateManager.GameStates.Gameplay);
    }

    private void OnOptionsClicked()
    {
        GameStateManager.instance.SetCurrentGameState(GameStateManager.GameStates.Options);
    }

    private void OnQuitClicked()
    {
        GameManager.Instance.QuitGame();
    }
}