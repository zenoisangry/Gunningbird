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

        // Usa i metodi dedicati che lasciano sempre attiva la pauseAction
        if (enabled)
            player.EnableGameplayInput();
        else
            player.DisableGameplayInput();

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
        // NON settiamo timeScale = 0 al Main Menu — in Unity 6 causa
        // mancata inizializzazione del renderer al primo frame.
        // Il player è immobile perché l'input è disabilitato.
        Time.timeScale = 1f;

        var player = GameManager.Instance.playerInstance;
        if (player != null)
            player.DisableGameplayInput();

        var wm = GameManager.Instance.playerInstance?.GetComponent<WeaponManager>();
        if (wm != null) wm.enabled = false;

        UIManager.Instance.ShowUI(UIManager.UIType.MainMenu);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;

        GameManager.Instance.SetGameActive(false);
        GameManager.Instance.SetGamePaused(false);
    }

    public void OnStateUpdate() { }
    public void OnStateExit()   { }
}

// ─────────────────────────────────────────────────────────────────────────────
//  OPTIONS
// ─────────────────────────────────────────────────────────────────────────────
public class GSOptions : IGameState
{
    public void OnStateEnter()
    {
        // timeScale 0 solo se eravamo in gioco (pausa) — non dal menu principale
        if (GameManager.Instance.isGameActive)
            Time.timeScale = 0f;
        else
            Time.timeScale = 1f;

        var player = GameManager.Instance.playerInstance;
        if (player != null) player.DisableGameplayInput();

        var wm = GameManager.Instance.playerInstance?.GetComponent<WeaponManager>();
        if (wm != null) wm.enabled = false;

        UIManager.Instance.ShowUI(UIManager.UIType.Options);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;
    }

    public void OnStateUpdate()
    {
        if (GameManager.Instance.isGameActive && Time.timeScale != 0f)
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
        Time.timeScale = 1f;

        var player = GameManager.Instance.playerInstance;
        if (player != null) player.DisableGameplayInput();

        var wm = GameManager.Instance.playerInstance?.GetComponent<WeaponManager>();
        if (wm != null) wm.enabled = false;

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
