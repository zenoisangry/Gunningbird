using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

public class LevelManager : MonoBehaviour
{
    private static LevelManager _instance;
    public static LevelManager Instance
    {
        get
        {
            if (_instance == null)
                _instance = FindAnyObjectByType<LevelManager>();
            if (_instance == null)
                Debug.LogWarning("[LevelManager] Instance not found in scene!");
            return _instance;
        }
    }

    [Header("Level Configuration")]
    [SerializeField] private LevelData[] availableLevels;
    [SerializeField] private int currentLevelIndex = 0;

    [Header("Player Setup")]
    [SerializeField] private GameObject playerPrefab;

    [Header("Events")]
    public UnityEvent onLevelInstantiated;
    public UnityEvent onLevelStart;
    public UnityEvent onLevelReset;
    public UnityEvent onPlayerSpawned;

    private GameObject currentLevelInstance;
    private GameObject currentPlayerInstance;
    private Transform playerSpawnPoint;
    private List<Transform> enemySpawnPoints = new List<Transform>();
    private List<GameObject> spawnedEnemies = new List<GameObject>();
    private bool isLevelActive = false;

    [System.Serializable]
    public class LevelData
    {
        public string levelName;
        public GameObject levelPrefab;
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
    }

    public void InstantiateLevel()
    {
        if (availableLevels == null || availableLevels.Length == 0)
        {
            Debug.LogError("[LevelManager] No levels configured!");
            return;
        }

        if (currentLevelIndex < 0 || currentLevelIndex >= availableLevels.Length)
        {
            Debug.LogError($"[LevelManager] Invalid level index: {currentLevelIndex}");
            return;
        }

        LevelData levelData = availableLevels[currentLevelIndex];
        if (levelData.levelPrefab == null)
        {
            Debug.LogError($"[LevelManager] Level prefab is null for {levelData.levelName}");
            return;
        }

        if (currentLevelInstance != null)
        {
            Destroy(currentLevelInstance);
        }

        currentLevelInstance = Instantiate(levelData.levelPrefab, Vector3.zero, Quaternion.identity);
        currentLevelInstance.name = $"{levelData.levelName}_Instance";

        FindSpawnPoints();

        onLevelInstantiated?.Invoke();
        isLevelActive = true;

        Debug.Log($"[LevelManager] Level '{levelData.levelName}' instantiated successfully");
    }

    private void FindSpawnPoints()
    {
        playerSpawnPoint = null;
        enemySpawnPoints.Clear();

        if (currentLevelInstance == null) return;

        Transform[] allTransforms = currentLevelInstance.GetComponentsInChildren<Transform>(true);

        foreach (Transform t in allTransforms)
        {
            if (t.name.Equals("PlayerSpawnPoint", System.StringComparison.OrdinalIgnoreCase))
            {
                playerSpawnPoint = t;
                Debug.Log($"[LevelManager] Found PlayerSpawnPoint at {t.position}");
            }
            else if (t.name.StartsWith("EnemySpawn", System.StringComparison.OrdinalIgnoreCase))
            {
                enemySpawnPoints.Add(t);
                Debug.Log($"[LevelManager] Found {t.name} at {t.position}");
            }
        }

        if (playerSpawnPoint == null)
        {
            Debug.LogWarning("[LevelManager] PlayerSpawnPoint not found in level! Player spawn may fail.");
        }

        Debug.Log($"[LevelManager] Found {enemySpawnPoints.Count} enemy spawn points");
    }

    public void StartLevel()
    {
        if (currentLevelInstance == null)
        {
            InstantiateLevel();
        }

        SpawnPlayer();
        SpawnAllEnemies();

        isLevelActive = true;
        onLevelStart?.Invoke();

        Debug.Log("[LevelManager] Level started");
    }

    public void ResetLevel()
    {
        Debug.Log("[LevelManager] Resetting level...");

        if (currentPlayerInstance != null)
        {
            Destroy(currentPlayerInstance);
            currentPlayerInstance = null;

            if (GameManager.Instance != null)
            {
                GameManager.Instance.playerInstance = null;
            }
        }

        DestroyAllEnemies();

        if (currentLevelInstance != null)
        {
            Destroy(currentLevelInstance);
            currentLevelInstance = null;
        }

        InstantiateLevel();
        SpawnPlayer();
        SpawnAllEnemies();

        isLevelActive = true;
        onLevelReset?.Invoke();

        Debug.Log("[LevelManager] Level reset complete");
    }

    public void CleanupLevel()
    {
        if (currentPlayerInstance != null)
        {
            Destroy(currentPlayerInstance);
            currentPlayerInstance = null;
        }

        DestroyAllEnemies();

        if (currentLevelInstance != null)
        {
            Destroy(currentLevelInstance);
            currentLevelInstance = null;
        }

        playerSpawnPoint = null;
        enemySpawnPoints.Clear();
        isLevelActive = false;

        Debug.Log("[LevelManager] Level cleaned up");
    }

    private void SpawnPlayer()
    {
        if (playerPrefab == null)
        {
            Debug.LogError("[LevelManager] Player prefab not assigned!");
            return;
        }

        if (playerSpawnPoint == null)
        {
            Debug.LogError("[LevelManager] Player spawn point not found!");
            return;
        }

        if (currentPlayerInstance != null)
        {
            Destroy(currentPlayerInstance);
            currentPlayerInstance = null;
        }

        currentPlayerInstance = Instantiate(playerPrefab, playerSpawnPoint.position, playerSpawnPoint.rotation);
        currentPlayerInstance.name = "Player";

        SetupPlayer();
        onPlayerSpawned?.Invoke();

        Debug.Log($"[LevelManager] Player spawned at {playerSpawnPoint.position}");
    }

    private void SetupPlayer()
    {
        if (currentPlayerInstance == null) return;

        PlayerInput playerInput = currentPlayerInstance.GetComponent<PlayerInput>();
        if (playerInput == null)
        {
            Debug.LogError("[LevelManager] PlayerInput component not found on player prefab!");
            return;
        }

        GameManager.Instance.playerInstance = playerInput;

        HealthSystem health = playerInput.GetHealthSystem();
        if (health != null)
        {
            health.SetHealth(health.GetMaxHealth());
        }

        var playerInputComponent = currentPlayerInstance.GetComponent<UnityEngine.InputSystem.PlayerInput>();
        if (playerInputComponent != null)
        {
            playerInputComponent.ActivateInput();
            playerInputComponent.SwitchCurrentActionMap("Player");
        }

        currentPlayerInstance.SetActive(true);

        Debug.Log("[LevelManager] Player setup complete");
    }

    private void SpawnAllEnemies()
    {
        if (enemySpawnPoints.Count == 0)
        {
            Debug.LogWarning("[LevelManager] No enemy spawn points found");
            return;
        }

        foreach (Transform spawnPoint in enemySpawnPoints)
        {
            SpawnEnemyAtPoint(spawnPoint);
        }

        Debug.Log($"[LevelManager] Spawned {spawnedEnemies.Count} enemies");
    }

    private void SpawnEnemyAtPoint(Transform spawnPoint)
    {
        if (spawnPoint == null) return;

        EnemySpawnPoint spawnConfig = spawnPoint.GetComponent<EnemySpawnPoint>();
        if (spawnConfig == null || spawnConfig.enemyPrefab == null)
        {
            Debug.LogWarning($"[LevelManager] {spawnPoint.name} missing EnemySpawnPoint component or prefab!");
            return;
        }

        GameObject enemy = Instantiate(spawnConfig.enemyPrefab, spawnPoint.position, spawnPoint.rotation);
        enemy.name = $"{spawnConfig.enemyPrefab.name}_Spawned";

        spawnedEnemies.Add(enemy);

        Debug.Log($"[LevelManager] Spawned {enemy.name} at {spawnPoint.position}");
    }

    private void DestroyAllEnemies()
    {
        foreach (GameObject enemy in spawnedEnemies)
        {
            if (enemy != null)
            {
                Destroy(enemy);
            }
        }

        spawnedEnemies.Clear();

        Debug.Log("[LevelManager] All enemies destroyed");
    }

    public void LoadLevel(int levelIndex)
    {
        if (levelIndex < 0 || levelIndex >= availableLevels.Length)
        {
            Debug.LogError($"[LevelManager] Invalid level index: {levelIndex}");
            return;
        }

        CleanupLevel();
        currentLevelIndex = levelIndex;
        InstantiateLevel();
        StartLevel();
    }

    public void LoadNextLevel()
    {
        int nextIndex = currentLevelIndex + 1;
        if (nextIndex >= availableLevels.Length)
        {
            Debug.Log("[LevelManager] No more levels available");
            return;
        }

        LoadLevel(nextIndex);
    }

    public int GetAliveEnemyCount()
    {
        spawnedEnemies.RemoveAll(enemy => enemy == null);
        return spawnedEnemies.Count;
    }

    public bool IsLevelActive() => isLevelActive;
    public GameObject GetCurrentPlayer() => currentPlayerInstance;
    public GameObject GetCurrentLevelInstance() => currentLevelInstance;
    public Transform GetPlayerSpawnPoint() => playerSpawnPoint;
    public int GetCurrentLevelIndex() => currentLevelIndex;
    public int GetTotalLevelCount() => availableLevels != null ? availableLevels.Length : 0;

    private void OnDrawGizmosSelected()
    {
        if (playerSpawnPoint != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(playerSpawnPoint.position, 1f);
            Gizmos.DrawLine(playerSpawnPoint.position, playerSpawnPoint.position + playerSpawnPoint.forward * 2f);
        }

        foreach (Transform enemySpawn in enemySpawnPoints)
        {
            if (enemySpawn != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(enemySpawn.position, 0.5f);
                Gizmos.DrawLine(enemySpawn.position, enemySpawn.position + enemySpawn.forward * 1f);
            }
        }
    }
}