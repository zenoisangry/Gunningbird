using UnityEngine;
using UnityEngine.InputSystem;

public class GameManager : MonoBehaviour
{
    [Header("Singleton")]
    private static GameManager _instance;
    public static GameManager Instance
    {
        get
        {
            if (_instance == null)
                _instance = FindAnyObjectByType<GameManager>();
            if (_instance == null)
                Debug.LogError("[GameManager] Error can't instantiate singleton");
            return _instance;
        }
    }

    [Header("Game Configuration")]
    public GameStateManager.GameStates startingGameState = GameStateManager.GameStates.MainMenu;

    [Header("Game State")]
    public bool isGameActive = false;
    public bool isGamePaused = false;

    [Header("Player Reference")]
    public PlayerInput playerInstance;

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

    private void RegisterGameStates()
    {
        GameStateManager.instance.RegisterState(GameStateManager.GameStates.MainMenu, new GSMainMenu());
        GameStateManager.instance.RegisterState(GameStateManager.GameStates.Options, new GSOptions());
        GameStateManager.instance.RegisterState(GameStateManager.GameStates.Pause, new GSPause());
        GameStateManager.instance.RegisterState(GameStateManager.GameStates.Gameplay, new GSGameplay());
        GameStateManager.instance.RegisterState(GameStateManager.GameStates.GameOver, new GSGameOver());
    }

    public void StartGame()
    {

        isGameActive = true;
        isGamePaused = false;
        Time.timeScale = 1f;

        if (LevelManager.Instance == null)
        {
            return;
        }

        LevelManager.Instance.InstantiateLevel();
        LevelManager.Instance.StartLevel();

        if (playerInstance != null)
        {
            HealthSystem playerHealth = playerInstance.GetHealthSystem();
            if (playerHealth != null)
            {
                playerHealth.Died -= OnPlayerDeath;
                playerHealth.Died += OnPlayerDeath;
            }
        }
    }
    public void EndGame()
    {

        isGameActive = false;
        isGamePaused = false;

        GameStateManager.instance.SetCurrentGameState(GameStateManager.GameStates.GameOver);
    }

    public void PauseGame()
    {
        if (!isGameActive)
        {
            return;
        }

        isGamePaused = true;
        Time.timeScale = 0f;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void OnPause(InputAction.CallbackContext context)
    {
        if (!context.performed) return;

        if (isGameActive && !isGamePaused)
        {
            GameStateManager.instance.SetCurrentGameState(GameStateManager.GameStates.Pause);
        }
        else if (isGamePaused)
        {
            var currentState = GameStateManager.instance.currentGameState;

            if (currentState is GSOptions)
            {
                GameStateManager.instance.SetCurrentGameState(GameStateManager.GameStates.Pause);
            }
            else if (currentState is GSPause)
            {
                ResumeGame();
            }
        }
    }

    public void ResumeGame()
    {
        if (!isGameActive) return;

        isGamePaused = false;
        Time.timeScale = 1f;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        GameStateManager.instance.SetCurrentGameState(GameStateManager.GameStates.Gameplay);
    }

    public void RestartLevel()
    {
        isGameActive = false;
        isGamePaused = false;
        Time.timeScale = 1f;

        if (playerInstance != null)
        {
            HealthSystem playerHealth = playerInstance.GetHealthSystem();
            if (playerHealth != null)
            {
                playerHealth.Died -= OnPlayerDeath;
            }
        }

        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.ResetLevel();
        }

        GameStateManager.instance.SetCurrentGameState(GameStateManager.GameStates.Gameplay);
    }

    public void ReturnToMainMenu()
    {
        isGameActive = false;
        isGamePaused = false;
        Time.timeScale = 1f;

        if (playerInstance != null)
        {
            HealthSystem playerHealth = playerInstance.GetHealthSystem();
            if (playerHealth != null)
            {
                playerHealth.Died -= OnPlayerDeath;
            }
            playerInstance = null;
        }

        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.CleanupLevel();
        }

        GameStateManager.instance.SetCurrentGameState(GameStateManager.GameStates.MainMenu);
    }

    public void QuitGame()
    {
         Application.Quit();
    }

    private void OnPlayerDeath()
    {
        EndGame();
    }

    private void OnDestroy()
    {
        if (playerInstance != null)
        {
            HealthSystem playerHealth = playerInstance.GetHealthSystem();
            if (playerHealth != null)
            {
                playerHealth.Died -= OnPlayerDeath;
            }
        }
    }

    private void Update()
    {
        if (playerInstance != null)
        {
            var input = playerInstance.GetComponent<UnityEngine.InputSystem.PlayerInput>();
        }
    }
}