using UnityEngine;
using UnityEngine.InputSystem;

// ─────────────────────────────────────────────────────────────────────────────
//  HELPER CONDIVISO
// ─────────────────────────────────────────────────────────────────────────────
internal static class PlayerInputHelper
{
    internal static void SetEnabled(bool enabled)
    {
        var player = GameManager.Instance.playerInstance;
        if (player == null) return;

        // Abilita/disabilita il MonoBehaviour PlayerInput — OnEnable/OnDisable
        // gestiscono internamente tutte le InputAction e i loro listener.
        player.enabled = enabled;

        // Rigidbody
        var rb = player.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = !enabled;
            if (!enabled)
            {
                rb.linearVelocity  = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }

        // WeaponManager
        var wm = player.GetComponent<WeaponManager>();
        if (wm != null) wm.enabled = enabled;
    }
}

// ─────────────────────────────────────────────────────────────────────────────
//  MAIN MENU
// ─────────────────────────────────────────────────────────────────────────────
public class GSMainMenu : IGameState
{
    public void OnStateEnter()
    {
        // Ferma il gioco se stavamo giocando (es. return to menu da pausa)
        Time.timeScale = 0f;
        PlayerInputHelper.SetEnabled(false);

        UIManager.Instance.ShowUI(UIManager.UIType.MainMenu);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;

        GameManager.Instance.SetGameActive(false);
        GameManager.Instance.SetGamePaused(false);
    }

    public void OnStateUpdate() { }

    public void OnStateExit()
    {
        // Il timeScale viene ripristinato a 1 da GSGameplay
    }
}

// ─────────────────────────────────────────────────────────────────────────────
//  OPTIONS
// ─────────────────────────────────────────────────────────────────────────────
public class GSOptions : IGameState
{
    public void OnStateEnter()
    {
        // Mantieni timeScale a 0 (eravamo in pausa o al menu)
        Time.timeScale = 0f;
        PlayerInputHelper.SetEnabled(false);

        UIManager.Instance.ShowUI(UIManager.UIType.Options);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;
    }

    public void OnStateUpdate()
    {
        if (Time.timeScale != 0f)
            Time.timeScale = 0f;
    }

    public void OnStateExit() { }
}

// ─────────────────────────────────────────────────────────────────────────────
//  CREDITS
// ─────────────────────────────────────────────────────────────────────────────
public class GSCredits : IGameState
{
    public void OnStateEnter()
    {
        Time.timeScale = 0f;
        PlayerInputHelper.SetEnabled(false);

        UIManager.Instance.ShowUI(UIManager.UIType.Credits);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;
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
        // Ripristina sempre il tempo e l'input — sia per primo avvio che per resume da pausa
        Time.timeScale = 1f;
        PlayerInputHelper.SetEnabled(true);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible   = false;

        UIManager.Instance.ShowUI(UIManager.UIType.Gameplay);

        // Avvia StartGame solo al primo avvio o dopo un restart (non al resume da pausa)
        if (!GameManager.Instance.isGameActive)
            GameManager.Instance.StartGame();
        else
            GameManager.Instance.SetGamePaused(false);
    }

    public void OnStateUpdate()
    {
        // Guardrail: corregge timeScale se qualcosa lo ha modificato esternamente
        if (!GameManager.Instance.isGamePaused && Time.timeScale != 1f)
            Time.timeScale = 1f;
    }

    public void OnStateExit()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;
    }
}

// ─────────────────────────────────────────────────────────────────────────────
//  PAUSE
// ─────────────────────────────────────────────────────────────────────────────
public class GSPause : IGameState
{
    public void OnStateEnter()
    {
        // Ferma tutto subito, poi aggiorna lo stato
        Time.timeScale = 0f;
        PlayerInputHelper.SetEnabled(false);

        GameManager.Instance.SetGamePaused(true);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;

        UIManager.Instance.ShowUI(UIManager.UIType.Pause);
    }

    public void OnStateUpdate()
    {
        if (Time.timeScale != 0f)
            Time.timeScale = 0f;
    }

    public void OnStateExit()
    {
        // Non riattiviamo qui l'input: lo fa GSGameplay.OnStateEnter()
        // Questo evita il doppio-enable che rompeva ESC al secondo uso
    }
}

// ─────────────────────────────────────────────────────────────────────────────
//  GAME OVER
// ─────────────────────────────────────────────────────────────────────────────
public class GSGameOver : IGameState
{
    public void OnStateEnter()
    {
        Time.timeScale = 0f;
        PlayerInputHelper.SetEnabled(false);

        GameManager.Instance.SetGameActive(false);
        GameManager.Instance.SetGamePaused(false);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;

        UIManager.Instance.ShowUI(UIManager.UIType.GameOver);
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
        Time.timeScale = 0f;
        PlayerInputHelper.SetEnabled(false);

        GameManager.Instance.SetGameActive(false);
        GameManager.Instance.SetGamePaused(false);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;

        UIManager.Instance.ShowUI(UIManager.UIType.Win);
    }

    public void OnStateUpdate() { }
    public void OnStateExit()   { }
}
