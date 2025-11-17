using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class TerrainShapeSettings
{
    public int resolution = 129;
    public float scale = 60f;
    public float heightMultiplier = 2f;
    public float baseRoughness = 0.3f;
    public float mountainFrequency = 1.5f;
    public float mountainStrength = 0.6f;
    public float mountainThreshold = 0.55f;

    public int GetValidResolution()
    {
        return Mathf.ClosestPowerOfTwo(resolution - 1) + 1;
    }
}

[System.Serializable]
public class ObjectSpawnSettings
{
    public GameObject[] prefabs;
    public int countPerChunk = 10;
    public Vector3 positionOffset = new Vector3(0.5f, 0f, 0.5f);
}

public class ChunkManager : MonoBehaviour
{
    private static ChunkManager _instance;
    public static ChunkManager instance
    {
        get
        {
            if (_instance == null)
                _instance = FindFirstObjectByType<ChunkManager>();
            return _instance;
        }
    }

    [Header("References")]
    public GameObject playerParent;
    private Transform playerSphere;

    [Header("World Settings")]
    public float chunkSize = 200f;
    public int viewDistance = 2;
    public int worldSeed;

    [Header("Terrain Settings")]
    public TerrainShapeSettings shapeSettings;

    public ObjectSpawnSettings gameObjectAmbient;
    public ObjectSpawnSettings gameObjectCute;
    public ObjectSpawnSettings gameObjectDecorations;

    [Header("Terrain Prefab")]
    public GameObject terrainPrefab;

    [Header("Terrain Layers")]
    public TerrainLayer[] terrainLayers;

    [Header("Prestazioni")]
    public float unloadDistance = 800f;

    private Dictionary<Vector2Int, TerrainChunk> activeChunks = new Dictionary<Vector2Int, TerrainChunk>();

    void Start()
    {
        worldSeed = GenerateValidSeed();
        Debug.Log("Generated World Seed: " + worldSeed);

        FindPlayer();
        UpdateChunks(true);
        PositionPlayerSafely();
    }

    void Update()
    {
        if (playerSphere != null)
            UpdateChunks();
    }

    void FindPlayer()
    {
        playerSphere = null;
        foreach (Transform t in playerParent.GetComponentsInChildren<Transform>(true))
        {
            if (t.gameObject.layer == LayerMask.NameToLayer("Player"))
            {
                playerSphere = t;
                break;
            }
        }
    }

    void PositionPlayerSafely()
    {
        if (playerSphere == null || activeChunks.Count == 0) return;

        Terrain closest = null;
        float bestDist = float.MaxValue;
        foreach (var c in activeChunks.Values)
        {
            float d = Vector3.Distance(Vector3.zero, c.terrain.transform.position);
            if (d < bestDist)
            {
                bestDist = d;
                closest = c.terrain;
            }
        }

        if (closest != null)
        {
            Vector3 pos = new Vector3(0, 500, 0);
            float y = closest.SampleHeight(pos) + closest.GetPosition().y;
            playerParent.transform.position = new Vector3(0, y + 3f, 0);
        }
    }

    int GenerateValidSeed()
    {
        int attempts = 0;
        while (attempts < 50)
        {
            int seed = Random.Range(0, int.MaxValue);
            float test1 = Mathf.PerlinNoise(seed * 0.001f, seed * 0.001f);
            float test2 = Mathf.PerlinNoise(seed * 0.002f, seed * 0.002f);
            if (Mathf.Abs(test1 - test2) > 0.1f)
                return seed;
            attempts++;
        }
        return Random.Range(0, int.MaxValue);
    }

    void UpdateChunks(bool force = false)
    {
        if (playerSphere == null) return;

        Vector2Int playerCoord = new Vector2Int(
            Mathf.FloorToInt(playerSphere.position.x / chunkSize),
            Mathf.FloorToInt(playerSphere.position.z / chunkSize)
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
            float dist = Vector3.Distance(playerSphere.position, kvp.Value.transform.position);
            if (dist > unloadDistance)
                toRemove.Add(kvp.Key);
        }

        foreach (var c in toRemove)
        {
            Destroy(activeChunks[c].gameObject);
            activeChunks.Remove(c);
        }

        UpdateNeighbors();
    }

    void CreateChunk(Vector2Int coord)
    {
        if (terrainPrefab == null)
        {
            Debug.LogError("Prefab del terrain non assegnato!");
            return;
        }

        GameObject terrainGO = Instantiate(terrainPrefab, transform);
        terrainGO.name = $"Chunk_{coord.x}_{coord.y}";
        terrainGO.transform.position = new Vector3(coord.x * chunkSize, 0, coord.y * chunkSize);

        Terrain terrain = terrainGO.GetComponent<Terrain>();
        if (terrain == null)
        {
            Debug.LogError("Il prefab deve contenere un componente Terrain!");
            return;
        }

        TerrainData terrainData = terrain.terrainData;

        int resolution = shapeSettings.GetValidResolution();
        terrainData.heightmapResolution = resolution;
        terrainData.size = new Vector3(chunkSize, shapeSettings.heightMultiplier, chunkSize);

        if (terrainLayers != null && terrainLayers.Length > 0)
            terrain.terrainData.terrainLayers = terrainLayers;

        GenerateHeights(terrainData, coord, shapeSettings, worldSeed);

        TerrainChunk chunk = terrainGO.GetComponent<TerrainChunk>();
        if (chunk == null)
            chunk = terrainGO.AddComponent<TerrainChunk>();

        chunk.terrain = terrain;
        chunk.terrainData = terrainData;
        chunk.coord = coord;

        chunk.SpawnCategory(gameObjectAmbient);
        chunk.SpawnCategory(gameObjectCute);
        chunk.SpawnCategory(gameObjectDecorations);

        activeChunks.Add(coord, chunk);
    }

    void GenerateHeights(TerrainData terrainData, Vector2Int coord, TerrainShapeSettings s, int seed)
    {
        int res = terrainData.heightmapResolution;
        float[,] heights = new float[res, res];

        for (int x = 0; x < res; x++)
        {
            for (int y = 0; y < res; y++)
            {
                heights[y, x] = 0f;
            }
        }

        terrainData.SetHeights(0, 0, heights);
    }

    void UpdateNeighbors()
    {
        foreach (var kvp in activeChunks)
        {
            Vector2Int c = kvp.Key;
            TerrainChunk chunk = kvp.Value;

            Terrain left = activeChunks.ContainsKey(c + Vector2Int.left) ? activeChunks[c + Vector2Int.left].terrain : null;
            Terrain right = activeChunks.ContainsKey(c + Vector2Int.right) ? activeChunks[c + Vector2Int.right].terrain : null;
            Terrain up = activeChunks.ContainsKey(c + Vector2Int.up) ? activeChunks[c + Vector2Int.up].terrain : null;
            Terrain down = activeChunks.ContainsKey(c + Vector2Int.down) ? activeChunks[c + Vector2Int.down].terrain : null;

            chunk.terrain.SetNeighbors(left, up, right, down);
        }
    }
}