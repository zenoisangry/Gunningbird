using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// GameManager – Singleton DontDestroyOnLoad.
/// Mono-scena: player e nemici sono già in scena.
/// Si occupa di avviare/resettare il gioco, gestire pausa e
/// agganciare gli eventi di morte del player e vittoria.
/// </summary>
public class GameManager : MonoBehaviour
{
    // ─── Singleton ───────────────────────────────────────────────────────────
    private static GameManager _instance;
    public static GameManager Instance
    {
        get
        {
            if (_instance == null)
                _instance = FindAnyObjectByType<GameManager>();
            if (_instance == null)
                Debug.LogError("[GameManager] Singleton not found in scene!");
            return _instance;
        }
    }

    // ─── Inspector ───────────────────────────────────────────────────────────
    [Header("Game Configuration")]
    public GameStateManager.GameStates startingGameState = GameStateManager.GameStates.MainMenu;

    [Header("Player Reference")]
    [Tooltip("Assegna il PlayerInput già presente in scena.")]
    public PlayerInput playerInstance;



    // ─── State ───────────────────────────────────────────────────────────────
    public bool isGameActive  { get; private set; } = false;
    public bool isGamePaused  { get; private set; } = false;

    // ─── Lifecycle ───────────────────────────────────────────────────────────
    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);

        RegisterGameStates();
    }

    private void Start()
    {
        GameStateManager.instance.SetCurrentGameState(startingGameState);
    }

    // ─── State Registration ──────────────────────────────────────────────────
    private void RegisterGameStates()
    {
        GameStateManager.instance.RegisterState(GameStateManager.GameStates.MainMenu,  new GSMainMenu());
        GameStateManager.instance.RegisterState(GameStateManager.GameStates.Options,   new GSOptions());
        GameStateManager.instance.RegisterState(GameStateManager.GameStates.Credits,   new GSCredits());
        GameStateManager.instance.RegisterState(GameStateManager.GameStates.Gameplay,  new GSGameplay());
        GameStateManager.instance.RegisterState(GameStateManager.GameStates.Pause,     new GSPause());
        GameStateManager.instance.RegisterState(GameStateManager.GameStates.GameOver,  new GSGameOver());
        GameStateManager.instance.RegisterState(GameStateManager.GameStates.Win,       new GSWin());
    }

    // ─── Public API ──────────────────────────────────────────────────────────

    /// <summary>Avvia la sessione di gioco. Aggancia gli eventi sul player già in scena.</summary>
    public void StartGame()
    {
        isGameActive  = true;
        isGamePaused  = false;
        Time.timeScale = 1f;

        HookPlayerEvents();
        HookWinCondition();

        // Rebind HUD al player
        if (playerInstance != null)
            UIManager.Instance.RegisterPlayer(playerInstance);
    }

    /// <summary>Resetta la scena in-place (posizioni, salute, nemici) e ricomincia.</summary>
    public void RestartGame()
    {
        isGameActive  = false;
        isGamePaused  = false;
        Time.timeScale = 1f;

        UnhookPlayerEvents();

        // Delega il reset fisico al SceneResetManager
        if (SceneResetManager.Instance != null)
            SceneResetManager.Instance.ResetScene();

        // Riavvia la sessione
        StartGame();

        GameStateManager.instance.SetCurrentGameState(GameStateManager.GameStates.Gameplay);
    }

    public void PauseGame()
    {
        if (!isGameActive || isGamePaused) return;

        isGamePaused   = true;
        Time.timeScale  = 0f;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;

        GameStateManager.instance.SetCurrentGameState(GameStateManager.GameStates.Pause);
    }

    public void ResumeGame()
    {
        if (!isGameActive) return;

        isGamePaused   = false;
        Time.timeScale  = 1f;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible   = false;

        GameStateManager.instance.SetCurrentGameState(GameStateManager.GameStates.Gameplay);
    }

    public void OnPause(InputAction.CallbackContext context)
    {
        if (!context.performed) return;

        if (isGameActive && !isGamePaused)
        {
            PauseGame();
        }
        else if (isGamePaused)
        {
            if (GameStateManager.instance.IsInState(GameStateManager.GameStates.Options))
                GameStateManager.instance.SetCurrentGameState(GameStateManager.GameStates.Pause);
            else if (GameStateManager.instance.IsInState(GameStateManager.GameStates.Pause))
                ResumeGame();
        }
    }

    public void ReturnToMainMenu()
    {
        isGameActive  = false;
        isGamePaused  = false;
        Time.timeScale = 1f;

        UnhookPlayerEvents();

        if (SceneResetManager.Instance != null)
            SceneResetManager.Instance.ResetScene();

        GameStateManager.instance.SetCurrentGameState(GameStateManager.GameStates.MainMenu);
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ─── Internal ────────────────────────────────────────────────────────────

    private void HookPlayerEvents()
    {
        if (playerInstance == null) return;

        HealthSystem health = playerInstance.GetHealthSystem();
        if (health == null) return;

        health.Died -= OnPlayerDeath;
        health.Died += OnPlayerDeath;
    }

    private void UnhookPlayerEvents()
    {
        if (playerInstance == null) return;

        HealthSystem health = playerInstance.GetHealthSystem();
        if (health != null)
            health.Died -= OnPlayerDeath;
    }

    private void HookWinCondition()
    {
        if (SceneResetManager.Instance != null)
        {
            SceneResetManager.Instance.OnAllEnemiesDead -= OnAllEnemiesDead;
            SceneResetManager.Instance.OnAllEnemiesDead += OnAllEnemiesDead;
        }
    }

    private void OnPlayerDeath()
    {
        if (!isGameActive) return;

        isGameActive  = false;
        isGamePaused  = false;

        GameStateManager.instance.SetCurrentGameState(GameStateManager.GameStates.GameOver);
    }

    private void OnAllEnemiesDead()
    {
        if (!isGameActive) return;

        isGameActive  = false;
        isGamePaused  = false;

        GameStateManager.instance.SetCurrentGameState(GameStateManager.GameStates.Win);
    }

    // Setter interni usati dagli stati
    internal void SetGameActive(bool value)  => isGameActive  = value;
    internal void SetGamePaused(bool value)  => isGamePaused  = value;

    private void OnDestroy()
    {
        UnhookPlayerEvents();
    }
}
