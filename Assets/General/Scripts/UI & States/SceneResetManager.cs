using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// SceneResetManager – Mono-scena.
/// Non carica/scarica scene né istanzia prefab.
/// Memorizza lo stato iniziale di player e nemici e li ripristina al reset.
/// Traccia anche la condizione di vittoria (tutti i nemici morti).
/// </summary>
public class SceneResetManager : MonoBehaviour
{
    // ─── Singleton ───────────────────────────────────────────────────────────
    private static SceneResetManager _instance;
    public static SceneResetManager Instance
    {
        get
        {
            if (_instance == null)
                _instance = FindAnyObjectByType<SceneResetManager>();
            if (_instance == null)
                Debug.LogWarning("[SceneResetManager] Instance not found in scene!");
            return _instance;
        }
    }

    // ─── Inspector ───────────────────────────────────────────────────────────
    [Header("Player")]
    [Tooltip("Il Transform root del player già in scena.")]
    [SerializeField] private Transform playerTransform;

    [Header("Enemy Tracking")]
    [Tooltip("Lista degli enemy root già in scena. Popolata automaticamente se vuota.")]
    [SerializeField] private List<GameObject> enemyObjects = new List<GameObject>();

    [Header("Events")]
    public UnityEvent onSceneReset;

    // ─── Win condition ────────────────────────────────────────────────────────
    /// <summary>Fired quando tutti i nemici risultano morti.</summary>
    public event Action OnAllEnemiesDead;

    // ─── Snapshot interni ────────────────────────────────────────────────────
    private struct TransformSnapshot
    {
        public Vector3    position;
        public Quaternion rotation;
    }

    private TransformSnapshot              playerSnapshot;
    private List<TransformSnapshot>        enemySnapshots  = new List<TransformSnapshot>();
    private List<HealthSystem>             enemyHealthSystems = new List<HealthSystem>();
    private int                            aliveEnemyCount = 0;

    // ─── Lifecycle ───────────────────────────────────────────────────────────
    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;

        // Auto-find player se non assegnato
        if (playerTransform == null)
        {
            var pi = FindAnyObjectByType<PlayerInput>();
            if (pi != null) playerTransform = pi.transform;
        }

        // Auto-find nemici tramite HealthSystem se la lista è vuota
        if (enemyObjects.Count == 0)
            AutoFindEnemies();

        TakeSnapshots();
        SubscribeEnemyDeathEvents();
    }

    // ─── Snapshot ────────────────────────────────────────────────────────────
    private void TakeSnapshots()
    {
        // Player
        if (playerTransform != null)
        {
            playerSnapshot = new TransformSnapshot
            {
                position = playerTransform.position,
                rotation = playerTransform.rotation
            };
        }

        // Nemici
        enemySnapshots.Clear();
        foreach (var enemy in enemyObjects)
        {
            if (enemy == null) continue;
            enemySnapshots.Add(new TransformSnapshot
            {
                position = enemy.transform.position,
                rotation = enemy.transform.rotation
            });
        }
    }

    // ─── Auto-find ───────────────────────────────────────────────────────────
    private void AutoFindEnemies()
    {
        // Cerca tutti i HealthSystem in scena che NON appartengono al player
        HealthSystem[] allHS = FindObjectsByType<HealthSystem>(FindObjectsSortMode.None);
        foreach (var hs in allHS)
        {
            if (playerTransform != null && hs.transform.root == playerTransform.root) continue;
            if (!enemyObjects.Contains(hs.gameObject))
                enemyObjects.Add(hs.gameObject);
        }

        Debug.Log($"[SceneResetManager] Auto-found {enemyObjects.Count} enemy objects.");
    }

    // ─── Enemy death tracking ────────────────────────────────────────────────
    private void SubscribeEnemyDeathEvents()
    {
        enemyHealthSystems.Clear();
        aliveEnemyCount = 0;

        foreach (var enemy in enemyObjects)
        {
            if (enemy == null) continue;
            HealthSystem hs = enemy.GetComponentInChildren<HealthSystem>();
            if (hs == null) continue;

            enemyHealthSystems.Add(hs);
            aliveEnemyCount++;
            hs.Died += OnEnemyDied;
        }

        Debug.Log($"[SceneResetManager] Tracking {aliveEnemyCount} enemies for win condition.");
    }

    private void UnsubscribeEnemyDeathEvents()
    {
        foreach (var hs in enemyHealthSystems)
        {
            if (hs != null)
                hs.Died -= OnEnemyDied;
        }
    }

    private void OnEnemyDied()
    {
        aliveEnemyCount = Mathf.Max(0, aliveEnemyCount - 1);
        Debug.Log($"[SceneResetManager] Enemy died. Alive: {aliveEnemyCount}");

        if (aliveEnemyCount <= 0)
            OnAllEnemiesDead?.Invoke();
    }

    // ─── Reset ───────────────────────────────────────────────────────────────
    /// <summary>
    /// Ripristina posizione e salute di player e nemici ai valori iniziali.
    /// Non distrugge né istanzia nulla.
    /// </summary>
    public void ResetScene()
    {
        ResetPlayer();
        ResetEnemies();
        RehookEnemyDeathEvents();

        onSceneReset?.Invoke();
        Debug.Log("[SceneResetManager] Scene reset complete.");
    }

    private void ResetPlayer()
    {
        if (playerTransform == null) return;

        // Disabilita CharacterController temporaneamente per teletrasporto sicuro
        CharacterController cc = playerTransform.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;

        playerTransform.SetPositionAndRotation(playerSnapshot.position, playerSnapshot.rotation);

        if (cc != null) cc.enabled = true;

        // Riporta la salute al massimo
        HealthSystem playerHealth = playerTransform.GetComponentInChildren<HealthSystem>();
        if (playerHealth != null)
            playerHealth.Revive(1f);

        // Riattiva tutti i MonoBehaviour del player
        foreach (var mb in playerTransform.GetComponentsInChildren<MonoBehaviour>(true))
        {
            if (mb != null) mb.enabled = true;
        }

        // Riattiva Rigidbody
        Rigidbody rb = playerTransform.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.linearVelocity    = Vector3.zero;
            rb.angularVelocity   = Vector3.zero;
        }

        Debug.Log("[SceneResetManager] Player reset.");
    }

    private void ResetEnemies()
    {
        for (int i = 0; i < enemyObjects.Count; i++)
        {
            GameObject enemy = enemyObjects[i];
            if (enemy == null) continue;

            // Ripristina transform
            if (i < enemySnapshots.Count)
            {
                enemy.transform.SetPositionAndRotation(
                    enemySnapshots[i].position,
                    enemySnapshots[i].rotation
                );
            }

            // Riattiva il GameObject se era stato disattivato alla morte
            enemy.SetActive(true);

            // Ripristina salute
            HealthSystem hs = enemy.GetComponentInChildren<HealthSystem>();
            if (hs != null)
                hs.Revive(1f);

            // Riattiva MonoBehaviour
            foreach (var mb in enemy.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (mb != null) mb.enabled = true;
            }

            // Riattiva Rigidbody
            Rigidbody rb = enemy.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = false;
                rb.linearVelocity    = Vector3.zero;
                rb.angularVelocity   = Vector3.zero;
            }
        }

        Debug.Log($"[SceneResetManager] {enemyObjects.Count} enemies reset.");
    }

    private void RehookEnemyDeathEvents()
    {
        UnsubscribeEnemyDeathEvents();
        SubscribeEnemyDeathEvents();
    }

    // ─── Queries ─────────────────────────────────────────────────────────────
    public int  GetAliveEnemyCount()  => aliveEnemyCount;
    public bool AllEnemiesDead()      => aliveEnemyCount <= 0;

    private void OnDestroy()
    {
        UnsubscribeEnemyDeathEvents();
    }
}
