using UnityEngine;

public class EnemySpawnPoint : MonoBehaviour
{
    [Header("Enemy Configuration")]
    [Tooltip("The enemy prefab to spawn at this point")]
    public GameObject enemyPrefab;

    [Header("Spawn Settings")]
    [Tooltip("Delay before spawning (seconds)")]
    public float spawnDelay = 0f;

    [Header("Debug")]
    [Tooltip("Show spawn point gizmo in scene view")]
    public bool showGizmo = true;
    public Color gizmoColor = Color.red;

    private void OnDrawGizmos()
    {
        if (!showGizmo) return;

        Gizmos.color = gizmoColor;
        Gizmos.DrawWireSphere(transform.position, 0.5f);
        Gizmos.DrawLine(transform.position, transform.position + transform.forward * 1.5f);

        Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 0.3f);
        Gizmos.DrawSphere(transform.position, 0.3f);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 0.7f);
        Gizmos.DrawLine(transform.position, transform.position + transform.forward * 2f);
    }
}