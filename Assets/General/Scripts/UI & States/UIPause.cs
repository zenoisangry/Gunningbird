using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class UIPause : MonoBehaviour, IGameUI
{
    [Header("UI Elements")]
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button optionsButton;
    [SerializeField] private Button quitButton;

    [Header("Input")]
    [SerializeField] private InputActionReference pauseAction;

    public void Init()
    {
        if (resumeButton != null)
            resumeButton.onClick.AddListener(OnResumeClicked);

        if (optionsButton != null)
            optionsButton.onClick.AddListener(OnOptionsClicked);

        if (quitButton != null)
            quitButton.onClick.AddListener(OnQuitClicked);
    }

    private void OnEnable()
    {
        if (pauseAction != null)
        {
            pauseAction.action.performed += OnPausePerformed;
            pauseAction.action.Enable();
        }
    }

    private void OnDisable()
    {
        if (pauseAction != null)
        {
            pauseAction.action.performed -= OnPausePerformed;
            pauseAction.action.Disable();
        }
    }

    public void SetActive(bool active)
    {
        if (pausePanel != null)
            pausePanel.SetActive(active);
    }

    public UIManager.UIType GetUIType()
    {
        return UIManager.UIType.Pause;
    }

    private void OnPausePerformed(InputAction.CallbackContext context)
    {
        if (pausePanel != null && pausePanel.activeSelf)
        {
            OnResumeClicked();
        }
        else
        {
            GameManager.Instance.PauseGame();
        }
    }

    private void OnResumeClicked()
    {
        GameManager.Instance.ResumeGame();
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