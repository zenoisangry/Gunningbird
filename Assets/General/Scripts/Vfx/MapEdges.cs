using UnityEngine;

public class MapEdges : MonoBehaviour
{
    [Header("Player")]
    public Transform player;

    [Header("Muro")]
    public Renderer wallRenderer;

    [Header("Distanza di comparsa")]
    public float revealDistance = 5f;

    private void Start()
    {
        if (wallRenderer != null)
        {
            wallRenderer.enabled = false;
        }
    }

    private void Update()
    {
        if (player == null || wallRenderer == null)
            return;

        float distance = Vector3.Distance(
            player.position,
            transform.position
        );

        wallRenderer.enabled = distance <= revealDistance;
    }
}
