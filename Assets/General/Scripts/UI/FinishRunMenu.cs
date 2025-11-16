using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class FinishRunMenu : MonoBehaviour
{
    public Button leftButton;
    public Button rightButton;
    public Button exitButton;

    private void Awake()
    {
        if (leftButton != null) leftButton.onClick.RemoveAllListeners();
        if (rightButton != null) rightButton.onClick.RemoveAllListeners();

        if (leftButton != null)
            leftButton.onClick.AddListener(() => PostRunManager.Instance?.SwitchAimPoint(false));

        if (rightButton != null)
            rightButton.onClick.AddListener(() => PostRunManager.Instance?.SwitchAimPoint(true));
    }

    public void LoadScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }
}