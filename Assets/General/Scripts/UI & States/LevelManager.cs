using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

public class LevelManager : MonoBehaviour
{
    [Header("Singleton")]
    private static LevelManager _instance;
    public static LevelManager Instance
    {
        get
        {
            if (_instance == null)
                _instance = FindAnyObjectByType<LevelManager>();
            if (_instance == null)
                Debug.LogWarning("[LevelManager] Error can't instantiate singleton");
            return _instance;
        }
    }

    [Header("Level Prefab")]
    [SerializeField] private GameObject levelPrefab;
    [SerializeField] private bool instantiateLevelOnStart = false;

    [Header("Player Setup")]
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private Transform playerSpawnPoint;

    [Header("Enemy Spawning")]
    [SerializeField] private List<EnemySpawnData> enemySpawnPoints = new List<EnemySpawnData>();

    [Header("Events")]
    public UnityEvent onLevelInstantiated;
    public UnityEvent onLevelStart;
    public UnityEvent onLevelReset;
    public UnityEvent onPlayerSpawned;

    private GameObject currentLevelInstance;
    private GameObject currentPlayerInstance;
    private List<GameObject> spawnedEnemies = new List<GameObject>();

    private bool isLevelActive = false;

    [System.Serializable]
    public class EnemySpawnData
    {
        public GameObject enemyPrefab;
        public Transform spawnPoint;
        [HideInInspector] public GameObject spawnedInstance;
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

    private void Start()
    {
        if (instantiateLevelOnStart)
        {
            InstantiateLevel();
        }
    }

    public void InstantiateLevel()
    {
        if (levelPrefab == null)
        {
            return;
        }

        if (currentLevelInstance != null)
        {
            Destroy(currentLevelInstance);
        }

        currentLevelInstance = Instantiate(levelPrefab, Vector3.zero, Quaternion.identity);
        currentLevelInstance.name = "Level_Instance";

        onLevelInstantiated?.Invoke();

        if (playerSpawnPoint == null)
        {
            GameObject spawnObj = GameObject.Find("PlayerSpawnPoint");
            if (spawnObj != null)
                playerSpawnPoint = spawnObj.transform;
        }

        isLevelActive = true;
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

    }

    public void ResetLevel()
    {
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
        SpawnAllEnemies();
        SpawnPlayer();

        isLevelActive = true;
        onLevelReset?.Invoke();
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

        isLevelActive = false;

    }

    public void SpawnPlayer()
    {
        if (playerPrefab == null)
        {
            return;
        }

        if (playerSpawnPoint == null)
        {
            return;
        }

        if (currentPlayerInstance != null)
        {
            Destroy(currentPlayerInstance);
            currentPlayerInstance = null;
        }

        currentPlayerInstance = Instantiate(
            playerPrefab,
            playerSpawnPoint.position,
            playerSpawnPoint.rotation
        );

        currentPlayerInstance.name = "Player";

        SetupPlayer();
        onPlayerSpawned?.Invoke();
    }

    private void SetupPlayer()
    {
        if (currentPlayerInstance == null) return;

        PlayerInput playerInput = currentPlayerInstance.GetComponent<PlayerInput>();
        if (playerInput == null)
        {
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
    }

    public GameObject GetCurrentPlayer()
    {
        return currentPlayerInstance;
    }

    private void SpawnAllEnemies()
    {
        if (enemySpawnPoints == null || enemySpawnPoints.Count == 0)
        {
            return;
        }

        foreach (var spawnData in enemySpawnPoints)
        {
            SpawnEnemy(spawnData);
        }
    }

    private void SpawnEnemy(EnemySpawnData spawnData)
    {
        if (spawnData.enemyPrefab == null)
        {
            return;
        }

        if (spawnData.spawnPoint == null)
        {
            return;
        }

        GameObject enemy = Instantiate(
            spawnData.enemyPrefab,
            spawnData.spawnPoint.position,
            spawnData.spawnPoint.rotation
        );

        enemy.name = $"{spawnData.enemyPrefab.name}_Spawned";

        spawnData.spawnedInstance = enemy;
        spawnedEnemies.Add(enemy);

        Debug.Log($"[LevelManager] Enemy {enemy.name} spawned at {spawnData.spawnPoint.position}");
    }

    private void DestroyAllEnemies()
    {
        Debug.Log($"[LevelManager] Destroying {spawnedEnemies.Count} enemies");

        foreach (GameObject enemy in spawnedEnemies)
        {
            if (enemy != null)
            {
                Destroy(enemy);
            }
        }

        spawnedEnemies.Clear();

        foreach (var spawnData in enemySpawnPoints)
        {
            spawnData.spawnedInstance = null;
        }

        Debug.Log("[LevelManager] All enemies destroyed");
    }

    public int GetAliveEnemyCount()
    {
        spawnedEnemies.RemoveAll(enemy => enemy == null);
        return spawnedEnemies.Count;
    }

    public bool IsLevelActive()
    {
        return isLevelActive;
    }

    public GameObject GetCurrentLevelInstance()
    {
        return currentLevelInstance;
    }

    public void SetPlayerSpawnPoint(Transform newSpawnPoint)
    {
        if (newSpawnPoint != null)
        {
            playerSpawnPoint = newSpawnPoint;
            Debug.Log($"[LevelManager] Player spawn point changed to {newSpawnPoint.position}");
        }
    }

    public Transform GetPlayerSpawnPoint()
    {
        return playerSpawnPoint;
    }

    private void OnDrawGizmosSelected()
    {
        if (playerSpawnPoint != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(playerSpawnPoint.position, 1f);
            Gizmos.DrawLine(
                playerSpawnPoint.position,
                playerSpawnPoint.position + playerSpawnPoint.forward * 2f
            );
        }

        if (enemySpawnPoints != null)
        {
            foreach (var spawnData in enemySpawnPoints)
            {
                if (spawnData.spawnPoint != null)
                {
                    Gizmos.color = Color.red;
                    Gizmos.DrawWireSphere(spawnData.spawnPoint.position, 0.5f);
                    Gizmos.DrawLine(
                        spawnData.spawnPoint.position,
                        spawnData.spawnPoint.position + spawnData.spawnPoint.forward * 1f
                    );
                }
            }
        }
    }
}