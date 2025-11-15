using UnityEngine;

[System.Serializable]
public class ObjectSpawnSettings
{
    public GameObject[] prefabs;
    public int countPerChunk = 10;
    [Tooltip("Offset per evitare che spawnino sotto terra")]
    public Vector3 positionOffset = new Vector3(0.5f, 0f, 0.5f);
}

public class TerrainChunk : MonoBehaviour
{
    public Terrain terrain;
    public TerrainData terrainData;
    public Vector2Int coord;

    [Header("Spawn Settings")]
    public ObjectSpawnSettings gameObjectAmbient;
    public ObjectSpawnSettings gameObjectCute;
    public ObjectSpawnSettings gameObjectDecorations;

    [Header("Terrain Layers")]
    public TerrainLayer[] terrainLayers;

    public void Initialize(
        Vector2Int coord,
        float chunkSize,
        int worldSeed,
        Vector2 noiseOffset,
        TerrainShapeSettings shape,
        ObjectSpawnSettings ambient,
        ObjectSpawnSettings cute,
        ObjectSpawnSettings decorations
    )
    {
        this.coord = coord;
        this.gameObjectAmbient = ambient;
        this.gameObjectCute = cute;
        this.gameObjectDecorations = decorations;

        terrainData = new TerrainData();
        terrainData.heightmapResolution = shape.resolution;
        terrainData.size = new Vector3(chunkSize, shape.heightMultiplier, chunkSize);

        terrain = Terrain.CreateTerrainGameObject(terrainData).GetComponent<Terrain>();
        terrain.transform.parent = transform;
        terrain.transform.localPosition = Vector3.zero;

        if (terrainLayers != null && terrainLayers.Length > 0)
            terrain.terrainData.terrainLayers = terrainLayers;

        GenerateHeightmap(coord, chunkSize, worldSeed, noiseOffset, shape);

        SpawnCategory(gameObjectAmbient);
        SpawnCategory(gameObjectCute);
        SpawnCategory(gameObjectDecorations);
    }

    void GenerateHeightmap(Vector2Int coord, float chunkSize, int seed, Vector2 offset, TerrainShapeSettings s)
    {
        float[,] heights = new float[s.resolution, s.resolution];
        float baseFreq = 1f / s.scale;
        int worldXOffset = coord.x * (s.resolution - 1);
        int worldYOffset = coord.y * (s.resolution - 1);

        for (int x = 0; x < s.resolution; x++)
        {
            for (int y = 0; y < s.resolution; y++)
            {
                float wx = (worldXOffset + x) * baseFreq + offset.x;
                float wy = (worldYOffset + y) * baseFreq + offset.y;

                float low = Mathf.PerlinNoise(wx + seed * 0.001f, wy + seed * 0.001f) * 0.5f;
                float mid = Mathf.PerlinNoise(wx * 2 + seed * 0.002f, wy * 2 + seed * 0.002f) * 0.3f;
                float high = Mathf.PerlinNoise(wx * 8 + seed * 0.005f, wy * 8 + seed * 0.005f) * 0.1f;

                float height = (low + mid + high) * s.baseRoughness;

                float mountainMask = Mathf.PerlinNoise((wx + seed) * s.mountainFrequency, (wy + seed) * s.mountainFrequency);
                if (mountainMask > s.mountainThreshold)
                    height += Mathf.Pow(mountainMask - s.mountainThreshold, 2f) * s.mountainStrength;

                heights[y, x] = Mathf.Clamp01(height);
            }
        }

        terrainData.SetHeights(0, 0, heights);
    }

    void SpawnCategory(ObjectSpawnSettings settings)
    {
        if (settings == null || settings.prefabs == null || settings.prefabs.Length == 0)
            return;

        GameObject container = new GameObject(settings.ToString());
        container.transform.SetParent(transform);

        for (int i = 0; i < settings.countPerChunk; i++)
        {
            Vector3 local = new Vector3(
                Random.Range(0f, terrainData.size.x),
                0f,
                Random.Range(0f, terrainData.size.z)
            );

            Vector3 world = terrain.transform.position + local;

            float groundY = terrain.SampleHeight(world) + terrain.GetPosition().y;
            world.y = groundY;
            world += settings.positionOffset;

            GameObject prefab = settings.prefabs[Random.Range(0, settings.prefabs.Length)];
            Instantiate(prefab, world, Quaternion.identity, container.transform);
        }
    }

    public float GetSafeHeightAtWorldPos(Vector3 worldPos)
    {
        if (terrain == null) return 0;
        return terrain.SampleHeight(worldPos) + terrain.GetPosition().y;
    }
}