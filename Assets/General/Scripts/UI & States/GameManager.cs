using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// GameManager – Singleton DontDestroyOnLoad.
/// Mono-scena: restart ricarica la scena da zero.
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

    private bool _pendingRestart    = false;
    private bool _pendingMainMenu   = false;

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
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void Start()
    {
        StartCoroutine(InitStateNextFrame());
    }

    private System.Collections.IEnumerator InitStateNextFrame()
    {
        yield return null;
        yield return null;
        yield return null;
        GameStateManager.instance.SetCurrentGameState(startingGameState);
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        UnhookPlayerEvents();
    }

    // ─── Scene Reload Callback ───────────────────────────────────────────────
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Trova il nuovo PlayerInput nella scena appena caricata
        playerInstance = FindAnyObjectByType<PlayerInput>();

        // Re-registra le UI della nuova scena
        UIManager.Instance.ReRegisterUIs();

        isGameActive  = false;
        isGamePaused  = false;
        Time.timeScale = 1f;

        if (_pendingRestart)
        {
            _pendingRestart = false;
            StartCoroutine(StartGameNextFrame());
        }
        else if (_pendingMainMenu)
        {
            _pendingMainMenu = false;
            StartCoroutine(GoToMainMenuNextFrame());
        }
    }

    private System.Collections.IEnumerator StartGameNextFrame()
    {
        yield return null;
        yield return null;
        GameStateManager.instance.SetCurrentGameState(GameStateManager.GameStates.Gameplay);
    }

    private System.Collections.IEnumerator GoToMainMenuNextFrame()
    {
        yield return null;
        yield return null;
        GameStateManager.instance.SetCurrentGameState(GameStateManager.GameStates.MainMenu);
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
    public void StartGame()
    {
        isGameActive  = true;
        isGamePaused  = false;
        Time.timeScale = 1f;

        HookPlayerEvents();
        HookWinCondition();

        if (playerInstance != null)
            UIManager.Instance.RegisterPlayer(playerInstance);
    }

    public void RestartGame()
    {
        isGameActive  = false;
        isGamePaused  = false;
        Time.timeScale = 1f;

        UnhookPlayerEvents();

        _pendingRestart = true;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
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
            PauseGame();
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

        _pendingMainMenu = true;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
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
        // Con reload scena non serve SceneResetManager —
        // Win condition gestita da WinConditionTracker in scena
        var winTracker = FindAnyObjectByType<WinConditionTracker>();
        if (winTracker != null)
        {
            winTracker.OnAllEnemiesDead -= OnAllEnemiesDead;
            winTracker.OnAllEnemiesDead += OnAllEnemiesDead;
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

    internal void SetGameActive(bool value)  => isGameActive  = value;
    internal void SetGamePaused(bool value)  => isGamePaused  = value;
}
