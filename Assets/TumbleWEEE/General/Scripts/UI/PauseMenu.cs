using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PauseMenu : MonoBehaviour
{
    public static bool GameIsPaused = false;

    [Header("UI References")]
    public GameObject pauseDisplay;
    public GameObject settingsDisplay;
    public Button returnButton;

    private WindPush playerMovement;

    private void Awake()
    {
        pauseDisplay.SetActive(false);
        settingsDisplay.SetActive(false);

        playerMovement = FindAnyObjectByType<WindPush>();

        if (returnButton != null)
            returnButton.onClick.AddListener(ResumeGame);

        HideCursorGameplay();
    }

    private void Update()
    {
        if (Keyboard.current == null) return;

        if (Keyboard.current.escapeKey.wasPressedThisFrame ||
            Keyboard.current.pKey.wasPressedThisFrame)
        {
            if (GameIsPaused)
                ResumeGame();
            else
                OpenPauseMenu();
        }
    }

    public void OpenPauseMenu()
    {
        GameIsPaused = true;

        pauseDisplay.SetActive(true);
        settingsDisplay.SetActive(false);

        Time.timeScale = 0f;
        ShowCursorMenu(true);
    }

    public void ResumeGame()
    {
        GameIsPaused = false;

        pauseDisplay.SetActive(false);
        settingsDisplay.SetActive(false);

        Time.timeScale = 1f;

        HideCursorGameplay();
    }

    public void FinishRun()
    {
        GameIsPaused = false;
        Time.timeScale = 1f;

        ShowCursorMenu(false);
    }

    void ShowCursorMenu(bool blockPlayer)
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        if (playerMovement != null)
            playerMovement.PauseInput(blockPlayer, false, false);
    }

    void HideCursorGameplay()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        if (playerMovement != null)
            playerMovement.PauseInput(false, false, false);
    }
}