using UnityEngine;

// ─────────────────────────────────────────────────────────────────────────────
//  MAIN MENU
// ─────────────────────────────────────────────────────────────────────────────
public class GSMainMenu : IGameState
{
    public void OnStateEnter()
    {
        UIManager.Instance.ShowUI(UIManager.UIType.MainMenu);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;
        Time.timeScale   = 1f;

        GameManager.Instance.SetGameActive(false);
        GameManager.Instance.SetGamePaused(false);
        GameManager.Instance.SetGameplayCameraActive(false);
    }

    public void OnStateUpdate() { }
    public void OnStateExit()   { }
}

// ─────────────────────────────────────────────────────────────────────────────
//  OPTIONS
// ─────────────────────────────────────────────────────────────────────────────
public class GSOptions : IGameState
{
    private bool _wasInGameplay;

    public void OnStateEnter()
    {
        _wasInGameplay = GameManager.Instance.isGameActive;

        UIManager.Instance.ShowUI(UIManager.UIType.Options);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;

        if (_wasInGameplay)
        {
            Time.timeScale = 0f;
            GameManager.Instance.SetGamePaused(true);
            SetPlayerInput(false);
        }
    }

    public void OnStateUpdate()
    {
        // Mantieni timescale a 0 se eravamo in gameplay
        if (_wasInGameplay)
            Time.timeScale = 0f;
    }

    public void OnStateExit()
    {
        if (_wasInGameplay && GameManager.Instance.isGameActive)
            SetPlayerInput(true);
    }

    private void SetPlayerInput(bool enabled)
    {
        if (GameManager.Instance.playerInstance == null) return;

        var uiInput = GameManager.Instance.playerInstance
            .GetComponent<UnityEngine.InputSystem.PlayerInput>();

        if (uiInput != null)
            uiInput.SwitchCurrentActionMap(enabled ? "Player" : "UI");

        foreach (var mb in GameManager.Instance.playerInstance.GetComponents<MonoBehaviour>())
        {
            if (mb != null && mb.GetType().Name != "HealthSystem")
                mb.enabled = enabled;
        }

        var rb = GameManager.Instance.playerInstance.GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = !enabled;

        var cc = GameManager.Instance.playerInstance.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = enabled;
    }
}

// ─────────────────────────────────────────────────────────────────────────────
//  CREDITS
// ─────────────────────────────────────────────────────────────────────────────
public class GSCredits : IGameState
{
    public void OnStateEnter()
    {
        UIManager.Instance.ShowUI(UIManager.UIType.Credits);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;
        Time.timeScale   = 1f;
        GameManager.Instance.SetGameplayCameraActive(false);
    }

    public void OnStateUpdate() { }
    public void OnStateExit()   { }
}

// ─────────────────────────────────────────────────────────────────────────────
//  GAMEPLAY
// ─────────────────────────────────────────────────────────────────────────────
public class GSGameplay : IGameState
{
    public void OnStateEnter()
    {
        UIManager.Instance.ShowUI(UIManager.UIType.Gameplay);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible   = false;
        Time.timeScale   = 1f;

        GameManager.Instance.SetGameplayCameraActive(true);

        // Avvia il gioco solo se non era già attivo (primo avvio, non resume da pausa)
        if (!GameManager.Instance.isGameActive)
            GameManager.Instance.StartGame();

        // Assicura che l'input del player sia sempre attivo
        EnsurePlayerInputEnabled();
    }

    public void OnStateUpdate()
    {
        // Corregge timescale se qualcuno lo ha modificato esternamente
        if (!GameManager.Instance.isGamePaused && Time.timeScale != 1f)
            Time.timeScale = 1f;
    }

    public void OnStateExit()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;
    }

    private void EnsurePlayerInputEnabled()
    {
        if (GameManager.Instance.playerInstance == null) return;

        var uiInput = GameManager.Instance.playerInstance
            .GetComponent<UnityEngine.InputSystem.PlayerInput>();

        if (uiInput != null)
        {
            uiInput.ActivateInput();
            uiInput.SwitchCurrentActionMap("Player");
        }

        foreach (var mb in GameManager.Instance.playerInstance.GetComponents<MonoBehaviour>())
        {
            if (mb != null && !mb.enabled)
                mb.enabled = true;
        }

        var rb = GameManager.Instance.playerInstance.GetComponent<Rigidbody>();
        if (rb != null && rb.isKinematic)
            rb.isKinematic = false;

        var cc = GameManager.Instance.playerInstance.GetComponent<CharacterController>();
        if (cc != null && !cc.enabled)
            cc.enabled = true;
    }
}

// ─────────────────────────────────────────────────────────────────────────────
//  PAUSE
// ─────────────────────────────────────────────────────────────────────────────
public class GSPause : IGameState
{
    public void OnStateEnter()
    {
        UIManager.Instance.ShowUI(UIManager.UIType.Pause);

        Time.timeScale = 0f;
        GameManager.Instance.SetGamePaused(true);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;

        SetPlayerInput(false);
    }

    public void OnStateUpdate()
    {
        if (Time.timeScale != 0f)
            Time.timeScale = 0f;
    }

    public void OnStateExit()
    {
        if (GameManager.Instance.isGameActive)
            SetPlayerInput(true);
    }

    private void SetPlayerInput(bool enabled)
    {
        if (GameManager.Instance.playerInstance == null) return;

        var uiInput = GameManager.Instance.playerInstance
            .GetComponent<UnityEngine.InputSystem.PlayerInput>();

        if (uiInput != null)
            uiInput.SwitchCurrentActionMap(enabled ? "Player" : "UI");

        foreach (var mb in GameManager.Instance.playerInstance.GetComponents<MonoBehaviour>())
        {
            if (mb != null && mb.GetType().Name != "HealthSystem")
                mb.enabled = enabled;
        }

        var rb = GameManager.Instance.playerInstance.GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = !enabled;

        var cc = GameManager.Instance.playerInstance.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = enabled;
    }
}

// ─────────────────────────────────────────────────────────────────────────────
//  GAME OVER
// ─────────────────────────────────────────────────────────────────────────────
public class GSGameOver : IGameState
{
    public void OnStateEnter()
    {
        UIManager.Instance.ShowUI(UIManager.UIType.GameOver);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;
        Time.timeScale   = 1f;

        GameManager.Instance.SetGameActive(false);
        GameManager.Instance.SetGamePaused(false);
        GameManager.Instance.SetGameplayCameraActive(false);

        if (GameManager.Instance.playerInstance != null)
        {
            var uiInput = GameManager.Instance.playerInstance
                .GetComponent<UnityEngine.InputSystem.PlayerInput>();
            uiInput?.SwitchCurrentActionMap("UI");
        }
    }

    public void OnStateUpdate() { }
    public void OnStateExit()   { }
}

// ─────────────────────────────────────────────────────────────────────────────
//  WIN
// ─────────────────────────────────────────────────────────────────────────────
public class GSWin : IGameState
{
    public void OnStateEnter()
    {
        UIManager.Instance.ShowUI(UIManager.UIType.Win);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;
        Time.timeScale   = 1f;

        GameManager.Instance.SetGameActive(false);
        GameManager.Instance.SetGamePaused(false);
        GameManager.Instance.SetGameplayCameraActive(false);

        if (GameManager.Instance.playerInstance != null)
        {
            var uiInput = GameManager.Instance.playerInstance
                .GetComponent<UnityEngine.InputSystem.PlayerInput>();
            uiInput?.SwitchCurrentActionMap("UI");
        }
    }

    public void OnStateUpdate() { }
    public void OnStateExit()   { }
}
