using UnityEngine;
using UnityEngine.UI;

public class UIPause : MonoBehaviour, IGameUI
{
    [Header("UI Elements")]
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button optionsButton;
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private Button quitButton;

    // RIMOSSO: InputActionReference pauseAction — gestito da PlayerInput.
    // UIPause non deve mai chiamare pauseAction.action.Disable() perché
    // disabilita l'action globalmente nell'asset, rompendo PlayerInput.

    public void Init()
    {
        if (resumeButton   != null) resumeButton.onClick.AddListener(OnResumeClicked);
        if (optionsButton  != null) optionsButton.onClick.AddListener(OnOptionsClicked);
        if (mainMenuButton != null) mainMenuButton.onClick.AddListener(OnMainMenuClicked);
        if (quitButton     != null) quitButton.onClick.AddListener(OnQuitClicked);
    }

    public void SetActive(bool active) => gameObject.SetActive(active);

    public UIManager.UIType GetUIType() => UIManager.UIType.Pause;

    private void OnResumeClicked()   => GameManager.Instance.ResumeGame();
    private void OnOptionsClicked()  => GameStateManager.instance.SetCurrentGameState(GameStateManager.GameStates.Options);
    private void OnMainMenuClicked() => GameManager.Instance.ReturnToMainMenu();
    private void OnQuitClicked()     => GameManager.Instance.QuitGame();
}
