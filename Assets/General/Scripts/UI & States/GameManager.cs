using UnityEngine;

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
                Debug.LogError("Error can't instantiate singleton");
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

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isGameActive && !isGamePaused)
            {
                GameStateManager.instance.SetCurrentGameState(GameStateManager.GameStates.Options);
            }
            else if (isGamePaused)
            {
                ResumeGame();
            }
        }
    }

    private void RegisterGameStates()
    {
        GameStateManager.instance.RegisterState(GameStateManager.GameStates.MainMenu, new GSMainMenu());
        GameStateManager.instance.RegisterState(GameStateManager.GameStates.Options, new GSOptions());
        GameStateManager.instance.RegisterState(GameStateManager.GameStates.Gameplay, new GSGameplay());
        GameStateManager.instance.RegisterState(GameStateManager.GameStates.GameOver, new GSGameOver());
    }

    public void StartGame()
    {
        isGameActive = true;
        isGamePaused = false;

        if (playerInstance == null)
        {
            playerInstance = FindAnyObjectByType<PlayerInput>();
        }

        if (playerInstance != null)
        {
            HealthSystem playerHealth = playerInstance.GetHealthSystem();
            if (playerHealth != null)
            {
                playerHealth.Died -= OnPlayerDeath;
                playerHealth.Died += OnPlayerDeath;
            }
        }

        Time.timeScale = 1f;
    }

    public void EndGame()
    {
        
        isGameActive = false;
        isGamePaused = false;

        GameStateManager.instance.SetCurrentGameState(GameStateManager.GameStates.GameOver);
    }

    public void PauseGame()
    {
        if (!isGameActive) return;

        isGamePaused = true;
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
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
}