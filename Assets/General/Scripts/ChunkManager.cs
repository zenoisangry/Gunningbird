using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class TerrainShapeSettings
{
    [Header("Dimensione e risoluzione")]
    public int resolution = 129;
    public float scale = 60f;
    public float heightMultiplier = 50f;
    public float baseRoughness = 0.25f;

    [Header("Montagne")]
    [Tooltip("Frequenza delle montagne (più alto = più fitte)")]
    public float mountainFrequency = 1.5f;

    [Tooltip("Intensità della montagna rispetto al terreno base)")]
    public float mountainStrength = 0.6f;

    [Tooltip("Soglia di attivazione della montagna (tra 0 e 1)")]
    public float mountainThreshold = 0.55f;
}

public class ChunkManager : MonoBehaviour
{
    [Header("Riferimenti principali")]
    public Transform playerPrefab;
    public float chunkSize = 200f;
    public int viewDistance = 2;
    public int worldSeed = 1234;

    [Header("Impostazioni terreno")]
    public TerrainShapeSettings shapeSettings;

    [Header("Spawn oggetti")]
    public TerrainObjectSettings objectSettings;

    [Header("Prestazioni")]
    [Tooltip("Distanza oltre la quale i chunk vengono distrutti")]
    public float unloadDistance = 800f;

    private Dictionary<Vector2Int, TerrainChunk> activeChunks = new Dictionary<Vector2Int, TerrainChunk>();
    private Transform player;

    void Start()
    {
        if (playerPrefab != null)
            player = Instantiate(playerPrefab, Vector3.zero, Quaternion.identity);
        else
            Debug.LogWarning("⚠️ Nessun playerPrefab assegnato al ChunkManager!");

        UpdateChunks(force: true);
        PositionPlayerOnTerrain();
    }

    void Update()
    {
        UpdateChunks();
    }

    void PositionPlayerOnTerrain()
    {
        if (player == null || activeChunks.Count == 0) return;

        Terrain firstTerrain = null;
        foreach (var chunk in activeChunks.Values)
        {
            firstTerrain = chunk.terrain;
            break;
        }

        if (firstTerrain != null)
        {
            float y = firstTerrain.SampleHeight(Vector3.zero) + firstTerrain.GetPosition().y + 5f;
            player.position = new Vector3(0, y, 0);
        }
    }

    void UpdateChunks(bool force = false)
    {
        if (player == null) return;

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