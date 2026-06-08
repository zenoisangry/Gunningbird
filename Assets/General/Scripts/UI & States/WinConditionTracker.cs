using System;
using UnityEngine;

/// <summary>
/// Traccia i nemici in scena e notifica quando tutti sono morti.
/// Sostituisce SceneResetManager per la win condition.
/// Metti questo script su un GameObject vuoto in scena.
/// </summary>
public class WinConditionTracker : MonoBehaviour
{
    public static WinConditionTracker Instance { get; private set; }

    public event Action OnAllEnemiesDead;
    public event Action EnemyDied;

    private int aliveEnemyCount = 0;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        // Trova tutti i HealthSystem in scena che non appartengono al player
        PlayerInput player = FindAnyObjectByType<PlayerInput>();
        HealthSystem[] allHS = FindObjectsByType<HealthSystem>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

        foreach (var hs in allHS)
        {
            if (player != null && hs.transform.root == player.transform.root) continue;
            aliveEnemyCount++;
            hs.Died += OnEnemyDied;
        }

        Debug.Log($"[WinConditionTracker] Tracking {aliveEnemyCount} enemies.");
    }

    private void OnEnemyDied()
    {
        aliveEnemyCount = Mathf.Max(0, aliveEnemyCount - 1);
        Debug.Log($"[WinConditionTracker] Enemy died. Alive: {aliveEnemyCount}");

        EnemyDied?.Invoke();

        if (aliveEnemyCount <= 0)
            OnAllEnemiesDead?.Invoke();
    }

    public int GetAliveEnemyCount() => aliveEnemyCount;
}
