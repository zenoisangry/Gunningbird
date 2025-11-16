using UnityEngine;

[System.Serializable]
public class SpawnSettings
{
    public GameObject[] prefabs;
    public int countPerChunk = 10;
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

    public void SpawnCategory(ObjectSpawnSettings settings)
    {
        if (settings == null || settings.prefabs == null || settings.prefabs.Length == 0)
            return;

        GameObject container = new GameObject(settings.ToString());
        container.transform.SetParent(transform);

        for (int i = 0; i < settings.countPerChunk; i++)
        {
            Vector3 localPos = new Vector3(
                Random.Range(0f, terrain.terrainData.size.x),
                0f,
                Random.Range(0f, terrain.terrainData.size.z)
            );

            Vector3 worldPos = terrain.transform.position + localPos;

            float groundY = terrain.SampleHeight(worldPos) + terrain.transform.position.y;
            worldPos.y = groundY;
            worldPos += settings.positionOffset;

            GameObject prefab = settings.prefabs[Random.Range(0, settings.prefabs.Length)];
            Instantiate(prefab, worldPos, Quaternion.identity, container.transform);
        }
    }

    public float GetSafeHeightAtWorldPos(Vector3 worldPos)
    {
        if (terrain == null) return 0;
        return terrain.SampleHeight(worldPos) + terrain.transform.position.y;
    }
}