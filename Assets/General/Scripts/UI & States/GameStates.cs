using UnityEngine;

public class GSMainMenu : IGameState
{
    public void OnStateEnter()
    {
        UIManager.Instance.ShowUI(UIManager.UIType.MainMenu);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        Time.timeScale = 1f;

        GameManager.Instance.isGameActive = false;
        GameManager.Instance.isGamePaused = false;
    }

    public void OnStateUpdate(){}

    public void OnStateExit(){}
}

public class GSOptions : IGameState
{
    private bool wasInGameplay = false;

    public void OnStateEnter()
    {
        wasInGameplay = GameManager.Instance.isGameActive;

        UIManager.Instance.ShowUI(UIManager.UIType.Options);

        if (wasInGameplay)
        {
            GameManager.Instance.PauseGame();
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void OnStateUpdate(){}

    public void OnStateExit(){}
}

public class GSGameplay : IGameState
{
    public void OnStateEnter()
    {
        UIManager.Instance.ShowUI(UIManager.UIType.Gameplay);

        GameManager.Instance.StartGame();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        Time.timeScale = 1f;
    }

    public void OnStateUpdate(){}

    public void OnStateExit()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}

public class GSGameOver : IGameState
{
    public void OnStateEnter()
    {
        UIManager.Instance.ShowUI(UIManager.UIType.GameOver);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        Time.timeScale = 1f;

        GameManager.Instance.isGameActive = false;
        GameManager.Instance.isGamePaused = false;
    }

    public void OnStateUpdate(){}

    public void OnStateExit(){}
}
