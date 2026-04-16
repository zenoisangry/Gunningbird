using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIMainMenu : MonoBehaviour, IGameUI
{
    [Header("UI Elements")]
    [SerializeField] private GameObject menuPanel;
    [SerializeField] private Button startButton;
    [SerializeField] private Button optionsButton;
    [SerializeField] private Button creditsButton; //Cat was here!=^..^=
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

        //Cat was here!
        if (creditsButton != null)
            quitButton.onClick.AddListener(OnCreditsClicked);
        //=^..^=
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

    //Cat was here too!
    private void OnCreditsClicked()
    {
        GameStateManager.instance.SetCurrentGameState(GameStateManager.GameStates.Credits);
    }
    //=^..^=
    private void OnQuitClicked()
    {
        GameManager.Instance.QuitGame();
    }
}