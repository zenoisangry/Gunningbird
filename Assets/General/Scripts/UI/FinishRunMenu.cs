using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class FinishRunMenu : MonoBehaviour
{
    [Header("UI Buttons")]
    public Button leftButton;
    public Button rightButton;
    public Button exitButton;

    private PauseMenu pauseMenu;

    private void Awake()
    {
        pauseMenu = FindAnyObjectByType<PauseMenu>();

        if (leftButton != null)
            leftButton.onClick.AddListener(PreviousProxy);

        if (rightButton != null)
            rightButton.onClick.AddListener(NextProxy);
    }

    private void OnEnable()
    {
        if (pauseMenu != null)
            pauseMenu.FinishRun();
    }

    public void NextProxy()
    {
        if (PostRunManager.Instance != null)
            PostRunManager.Instance.SwitchAimPoint(true);
    }

    public void PreviousProxy()
    {
        if (PostRunManager.Instance != null)
            PostRunManager.Instance.SwitchAimPoint(false);
    }

    public void LoadScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }
}