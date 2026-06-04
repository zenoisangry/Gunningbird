using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIMainMenu : MonoBehaviour, IGameUI
{
    [Header("UI Elements")]
    [SerializeField] private Button   startButton;
    [SerializeField] private Button   optionsButton;
    [SerializeField] private Button   creditsButton;
    [SerializeField] private Button   quitButton;
    [SerializeField] private TMP_Text titleText;

    public void Init()
    {
        if (startButton   != null) startButton.onClick.AddListener(OnStartClicked);
        if (optionsButton != null) optionsButton.onClick.AddListener(OnOptionsClicked);
        if (creditsButton != null) creditsButton.onClick.AddListener(OnCreditsClicked);
        if (quitButton    != null) quitButton.onClick.AddListener(OnQuitClicked);
    }

    // UIManager attiva/disattiva il root GameObject di questo script.
    // Tutti i figli (background, bottoni, testi) seguono automaticamente.
    public void SetActive(bool active) => gameObject.SetActive(active);

    public UIManager.UIType GetUIType() => UIManager.UIType.MainMenu;

    private void OnStartClicked()   => GameStateManager.instance.SetCurrentGameState(GameStateManager.GameStates.Gameplay);
    private void OnOptionsClicked() => GameStateManager.instance.SetCurrentGameState(GameStateManager.GameStates.Options);
    private void OnCreditsClicked() => GameStateManager.instance.SetCurrentGameState(GameStateManager.GameStates.Credits);
    private void OnQuitClicked()    => GameManager.Instance.QuitGame();
}
