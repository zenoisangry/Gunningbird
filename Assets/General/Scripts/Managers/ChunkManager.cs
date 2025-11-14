using System.Collections.Generic;
using UnityEngine;

public class ChunkManager : MonoBehaviour
{
    [Header("Player")]
    public Transform playerParent;
    public string playerChildLayerName = "Player";

    private Transform player;

    [Header("Chunk Settings")]
    public float chunkSize = 200f;
    public int viewDistance = 2;
    public float unloadDistance = 800f;

    [Header("Generation Settings")]
    public int worldSeed = 1234;
    public TerrainShapeSettings shapeSettings;
    public TerrainObjectSettings objectSettings;

    private Dictionary<Vector2Int, TerrainChunk> activeChunks = new Dictionary<Vector2Int, TerrainChunk>();

    void Start()
    {
        if (playerParent == null)
        {
            Debug.LogError("Assegna il Parent del Player al ChunkManager");
            return;
        }

        player = FindPlayerChild(playerParent);

        if (player == null)
        {
            Debug.LogError("Nessun child del Player ha il layer 'Player'");
            return;
        }

        UpdateChunks(force: true);
        PositionPlayerOnTerrain();
    }

    void Update()
    {
        if (player == null) return;
        UpdateChunks();
    }

    Transform FindPlayerChild(Transform parent)
    {
        foreach (Transform child in parent.GetComponentsInChildren<Transform>())
        {
            if (child.gameObject.layer == LayerMask.NameToLayer(playerChildLayerName))
                return child;
        }
        return null;
    }

    void PositionPlayerOnTerrain()
    {
        if (player == null || activeChunks.Count == 0) return;

        Terrain firstTerrain = null;
        foreach (var c in activeChunks.Values)
        {
            firstTerrain = c.terrain;
            break;
        }

        if (firstTerrain != null)
        {
            float y = firstTerrain.SampleHeight(player.position) + firstTerrain.GetPosition().y + 1f;
            playerParent.position = new Vector3(playerParent.position.x, y, playerParent.position.z);
        }
    }

    void UpdateChunks(bool force = false)
    {
        Vector2Int playerCoord = new Vector2Int(
            Mathf.FloorToInt(player.position.x / chunkSize),
            Mathf.FloorToInt(player.position.z / chunkSize)
        );

        HashSet<Vector2Int> needed = new HashSet<Vector2Int>();

        for (int x = -viewDistance; x <= viewDistance; x++)
        {
            for (int y = -viewDistance; y <= viewDistance; y++)
            {
                Vector2Int coord = playerCoord + new Vector2Int(x, y);
                needed.Add(coord);

                if (!activeChunks.ContainsKey(coord))
                    CreateChunk(coord);
            }
        }

        List<Vector2Int> toRemove = new List<Vector2Int>();
        foreach (var kvp in activeChunks)
        {
            float dist = Vector3.Distance(player.position, kvp.Value.transform.position);
            if (dist > unloadDistance)
                toRemove.Add(kvp.Key);
        }

        foreach (var coord in toRemove)
        {
            Destroy(activeChunks[coord].gameObject);
            activeChunks.Remove(coord);
        }

        UpdateNeighbors();
    }

    void CreateChunk(Vector2Int coord)
    {
        GameObject obj = new GameObject($"Chunk_{coord.x}_{coord.y}");
        obj.transform.parent = transform;
        obj.transform.position = new Vector3(coord.x * chunkSize, 0, coord.y * chunkSize);

        TerrainChunk chunk = obj.AddComponent<TerrainChunk>();
        chunk.Initialize(coord, chunkSize, worldSeed, Vector2.zero, shapeSettings, objectSettings);

        activeChunks.Add(coord, chunk);
    }

    void UpdateNeighbors()
    {
        foreach (var kvp in activeChunks)
        {
            Vector2Int c = kvp.Key;

            Terrain left = activeChunks.ContainsKey(c + Vector2Int.left) ? activeChunks[c + Vector2Int.left].terrain : null;
            Terrain right = activeChunks.ContainsKey(c + Vector2Int.right) ? activeChunks[c + Vector2Int.right].terrain : null;
            Terrain top = activeChunks.ContainsKey(c + Vector2Int.up) ? activeChunks[c + Vector2Int.up].terrain : null;
            Terrain bottom = activeChunks.ContainsKey(c + Vector2Int.down) ? activeChunks[c + Vector2Int.down].terrain : null;

            kvp.Value.terrain.SetNeighbors(left, top, right, bottom);
        }
    }
}