using UnityEngine;

public static class PerlinHeightGenerator
{
    public static void Generate(Terrain terrain, float heightScale, float noiseScale, int seed)
    {
        TerrainData data = terrain.terrainData;
        int width = data.heightmapResolution;
        int height = data.heightmapResolution;

        float[,] heights = new float[width, height];
        System.Random rand = new System.Random(seed);
        float offsetX = rand.Next(0, 10000);
        float offsetY = rand.Next(0, 10000);

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                float xCoord = offsetX + (float)x / width * noiseScale;
                float yCoord = offsetY + (float)y / height * noiseScale;
                float sample = Mathf.PerlinNoise(xCoord, yCoord);
                heights[x, y] = sample * heightScale;
            }
        }

        data.SetHeights(0, 0, heights);
    }
}