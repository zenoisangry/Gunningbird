using System;
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Traccia i nemici e notifica quando tutti sono morti.
/// Lista manuale: trascina i HealthSystem nell'Inspector.
/// Lista vuota: auto-find escludendo il player.
/// </summary>
public class WinConditionTracker : MonoBehaviour
{
    public static WinConditionTracker Instance { get; private set; }

    [Header("Nemici")]
    [Tooltip("Popola manualmente per escludere oggetti indesiderati. Se vuoto, auto-find.")]
    [SerializeField] private HealthSystem[] enemiesManual;

    public event Action OnAllEnemiesDead;
    public event Action EnemyDied;

    private int aliveEnemyCount = 0;
    private readonly List<HealthSystem> trackedEnemies = new List<HealthSystem>();

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        TrackEnemies();
    }

    private void TrackEnemies()
    {
        // Unsub da eventuali riferimenti precedenti
        foreach (var hs in trackedEnemies)
            if (hs != null) hs.Died -= OnEnemyDied;
        trackedEnemies.Clear();
        aliveEnemyCount = 0;

        HealthSystem[] toTrack;

        if (enemiesManual != null && enemiesManual.Length > 0)
        {
            toTrack = enemiesManual;
            Debug.Log($"[WinConditionTracker] Lista manuale: {toTrack.Length} nemici.");
        }
        else
        {
            PlayerInput player = FindAnyObjectByType<PlayerInput>();
            var allHS = FindObjectsByType<HealthSystem>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            var list  = new List<HealthSystem>();
            foreach (var hs in allHS)
            {
                if (player != null && hs.transform.root == player.transform.root) continue;
                list.Add(hs);
            }
            toTrack = list.ToArray();
            Debug.Log($"[WinConditionTracker] Auto-found {toTrack.Length} nemici.");
        }

        foreach (var hs in toTrack)
        {
            if (hs == null) continue;
            trackedEnemies.Add(hs);
            aliveEnemyCount++;
            hs.Died += OnEnemyDied;
        }
    }

    private void OnEnemyDied()
    {
        aliveEnemyCount = Mathf.Max(0, aliveEnemyCount - 1);
        EnemyDied?.Invoke();

        if (aliveEnemyCount <= 0)
            OnAllEnemiesDead?.Invoke();
    }

    private void OnDestroy()
    {
        foreach (var hs in trackedEnemies)
            if (hs != null) hs.Died -= OnEnemyDied;
    }

    public int GetAliveEnemyCount() => aliveEnemyCount;
}
