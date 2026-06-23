using System;
using UnityEngine;

/// <summary>
/// Traccia i nemici e notifica quando tutti sono morti.
/// Se la lista manuale è popolata, usa quella.
/// Altrimenti trova automaticamente tutti i HealthSystem in scena (escluso il player).
/// </summary>
public class WinConditionTracker : MonoBehaviour
{
    public static WinConditionTracker Instance { get; private set; }

    [Header("Nemici")]
    [Tooltip("Popola manualmente per escludere oggetti indesiderati. Se vuoto, trova tutto automaticamente.")]
    [SerializeField] private HealthSystem[] enemiesManual;

    public event Action OnAllEnemiesDead;
    public event Action EnemyDied;

    private int aliveEnemyCount = 0;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        HealthSystem[] toTrack;

        if (enemiesManual != null && enemiesManual.Length > 0)
        {
            // Lista manuale
            toTrack = enemiesManual;
            Debug.Log($"[WinConditionTracker] Usando lista manuale: {toTrack.Length} nemici.");
        }
        else
        {
            // Auto-find — esclude il player
            PlayerInput player = FindAnyObjectByType<PlayerInput>();
            var allHS = FindObjectsByType<HealthSystem>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            var list  = new System.Collections.Generic.List<HealthSystem>();
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

    public int GetAliveEnemyCount() => aliveEnemyCount;
}
