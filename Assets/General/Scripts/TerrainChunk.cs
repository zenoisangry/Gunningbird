using UnityEngine;

[System.Serializable]
public class TerrainObjectSettings
{
    [Tooltip("Prefab che possono essere spawnati sopra il terreno")]
    public GameObject[] prefabs;

    [Tooltip("Quanti oggetti spawnare per ogni chunk")]
    public int countPerChunk = 20;

    [Tooltip("Intervallo (in percentuale dell’altezza totale del terreno) dove gli oggetti possono apparire")]
    public Vector2 heightRange = new Vector2(0.2f, 0.8f);
}

public class TerrainChunk : MonoBehaviour
{
    public Terrain terrain;
    public TerrainData terrainData;
    public Vector2Int coord;

    public void Initialize(Vector2Int coord, float chunkSize, int worldSeed, Vector2 noiseOffset, TerrainShapeSettings shape, TerrainObjectSettings objSettings)
    {
        this.coord = coord;

        terrainData = new TerrainData();
        terrainData.heightmapResolution = shape.resolution;
        terrainData.size = new Vector3(chunkSize, shape.heightMultiplier, chunkSize);

        terrain = Terrain.CreateTerrainGameObject(terrainData).GetComponent<Terrain>();
        terrain.transform.parent = transform;
        terrain.transform.localPosition = Vector3.zero;

        GenerateHeightmap(coord, chunkSize, worldSeed, noiseOffset, shape);
        SpawnObjects(objSettings);
    }

    void GenerateHeightmap(Vector2Int coord, float chunkSize, int seed, Vector2 offset, TerrainShapeSettings s)
    {
        float[,] heights = new float[s.resolution, s.resolution];

        float frequency = 1f / s.scale;

        float worldStartX = coord.x * (s.resolution - 1);
        float worldStartY = coord.y * (s.resolution - 1);

        for (int x = 0; x < s.resolution; x++)
        {
            for (int y = 0; y < s.resolution; y++)
            {
                float worldX = (worldStartX + x) * frequency + offset.x;
                float worldY = (worldStartY + y) * frequency + offset.y;

                float baseNoise = Mathf.PerlinNoise(worldX + seed, worldY + seed) * s.baseRoughness;

                float mountainNoise = Mathf.PerlinNoise((worldX + seed) * s.mountainFrequency, (worldY + seed) * s.mountainFrequency);

                if (mountainNoise > s.mountainThreshold)
                    baseNoise += (mountainNoise - s.mountainThreshold) * s.mountainStrength;

                heights[y, x] = Mathf.Clamp01(baseNoise);
            }
        }

        terrainData.SetHeights(0, 0, heights);
    }

    void SpawnObjects(TerrainObjectSettings objSettings)
    {
        if (objSettings.prefabs == null || objSettings.prefabs.Length == 0)
            return;

        GameObject container = new GameObject("SpawnedObjects");
        container.transform.SetParent(transform);

        for (int i = 0; i < objSettings.countPerChunk; i++)
        {
            Vector3 localPos = new Vector3(
                Random.Range(0f, terrainData.size.x),
                0f,
                Random.Range(0f, terrainData.size.z)
            );

            Vector3 worldPos = terrain.transform.position + localPos;
            float height = terrain.SampleHeight(worldPos) + terrain.GetPosition().y;
            worldPos.y = height;

            float normalizedHeight = height / terrainData.size.y;
            if (normalizedHeight < objSettings.heightRange.x || normalizedHeight > objSettings.heightRange.y)
                continue;

            GameObject prefab = objSettings.prefabs[Random.Range(0, objSettings.prefabs.Length)];
            Instantiate(prefab, worldPos, Quaternion.identity, container.transform);
        }
    }
}