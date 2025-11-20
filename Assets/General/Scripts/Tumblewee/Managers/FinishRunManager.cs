using UnityEngine;
using UnityEngine.EventSystems;

public class FinishRunManager : MonoBehaviour
{
    public GameObject playerParent;
    public Camera mainCamera;
    public GameObject finishRunCanvas;
    public GameObject pauseMenuCanvas;

    public void DoFinishRun()
    {
        Time.timeScale = 1f;

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        if (finishRunCanvas != null)
            finishRunCanvas.SetActive(true);

        if (pauseMenuCanvas != null)
            pauseMenuCanvas.SetActive(false);

        if (playerParent != null)
            playerParent.SetActive(false);

        var es = EventSystem.current;
        if (es != null && finishRunCanvas != null)
        {
            var firstButton = finishRunCanvas.GetComponentInChildren<UnityEngine.UI.Button>();
            if (firstButton != null)
                es.SetSelectedGameObject(firstButton.gameObject);
        }

        if (mainCamera != null)
        {
            var camGO = mainCamera.gameObject;
            camGO.SetActive(false);
            camGO.SetActive(true);
        }
    }
}