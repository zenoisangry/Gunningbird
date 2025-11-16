using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class FinishRunManager : MonoBehaviour
{
    [Header("References")]
    public GameObject playerParent;
    public Camera mainCamera;
    public GameObject finishRunCanvas;
    public GameObject pauseMenuCanvas;

    private WindPush playerMovement;

    private void Awake()
    {
        playerMovement = FindAnyObjectByType<WindPush>();
    }

    public void DoFinishRun()
    {
        if (playerMovement != null)
            playerMovement.PauseInput(true);

        if (playerParent != null)
            playerParent.SetActive(false);

        if (mainCamera != null)
        {
            mainCamera.gameObject.SetActive(false);
            mainCamera.gameObject.SetActive(true);
        }

        if (finishRunCanvas != null)
            finishRunCanvas.SetActive(true);

        if (pauseMenuCanvas != null)
            pauseMenuCanvas.SetActive(false);

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        EventSystem es = EventSystem.current;
        if (es != null && finishRunCanvas != null)
        {
            Button firstButton = finishRunCanvas.GetComponentInChildren<Button>();
            if (firstButton != null)
                es.SetSelectedGameObject(firstButton.gameObject);
        }

        Time.timeScale = 1f;
    }
}