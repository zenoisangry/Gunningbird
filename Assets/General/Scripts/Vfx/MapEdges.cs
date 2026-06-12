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

        float distance = Vector3.Distance(player.position, transform.position);
      
        if (player == null || wallRenderer == null) return;
        if (distance <= revealDistance) wallRenderer.enabled = true;
        else wallRenderer.enabled = false;
    }

}
