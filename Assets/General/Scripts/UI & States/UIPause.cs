using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIPause : MonoBehaviour, IGameUI
{
    [Header("UI Elements")]
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button optionsButton;
    [SerializeField] private Button quitButton;

    public void Init()
    {
        if (resumeButton != null)
            resumeButton.onClick.AddListener(OnResumeClicked);

        if (optionsButton != null)
            optionsButton.onClick.AddListener(OnOptionsClicked);

        if (quitButton != null)
            quitButton.onClick.AddListener(OnQuitClicked);
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

    private void Update()
    {
        if (pausePanel != null && pausePanel.activeSelf)
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                OnResumeClicked();
            }
        }
    }
}