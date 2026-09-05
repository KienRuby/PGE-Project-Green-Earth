using System.IO;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Tool tạo texture Noise 512x512 Seamless Tileable đa tầng cho UI Dissolve Shader.
/// Kết hợp Perlin đa tần số (FBM) và Worley (Cellular) để tạo các mảnh vụn phân rã tự nhiên.
/// </summary>
public static class UIDissolveNoiseGenerator
{
    public const string NoiseTexturePath = "Assets/Textures/Noise/dissolve_noise.png";

#if UNITY_EDITOR
    [MenuItem("Tools/PGE/Generate Dissolve Noise Texture", priority = 200)]
    public static void GenerateNoiseTextureMenuItem()
    {
        GenerateAndSaveTexture(512, 512, NoiseTexturePath);
        EditorUtility.DisplayDialog("UI Dissolve", "Đã tạo thành công texture noise tại: " + NoiseTexturePath, "OK");
    }
#endif

    public static Texture2D GenerateNoiseTexture(int width = 512, int height = 512)
    {
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false, true);
        Color[] pixels = new Color[width * height];

        int seedX = 137;
        int seedY = 929;

        for (int y = 0; y < height; y++)
        {
            float ty = (float)y / height;
            for (int x = 0; x < width; x++)
            {
                float tx = (float)x / width;

                // Seamless Tileable Perlin
                float baseNoise = SampleTileableFBM(tx, ty, 4.0f, 4, 0.5f, 2.0f, seedX, seedY);

                // High-frequency detail noise (hạt nhỏ stardust)
                float fineNoise = SampleTileableFBM(tx, ty, 16.0f, 3, 0.6f, 2.0f, seedX + 50, seedY + 50);

                // Worley cellular noise (tạo lỗ thủng phân rã)
                float worley = SampleTileableWorley(tx, ty, 8, seedX, seedY);

                // R: Main FBM Noise
                // G: Fine Dust Noise
                // B: Worley Cellular Noise
                // A: Composite (0..1)
                float r = Mathf.Clamp01(baseNoise);
                float g = Mathf.Clamp01(fineNoise);
                float b = Mathf.Clamp01(worley);
                float a = Mathf.Clamp01(baseNoise * 0.65f + fineNoise * 0.20f + (1f - worley) * 0.15f);

                pixels[y * width + x] = new Color(r, g, b, a);
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }

    public static void GenerateAndSaveTexture(int width, int height, string fullRelativePath)
    {
        Texture2D tex = GenerateNoiseTexture(width, height);
        byte[] pngData = tex.EncodeToPNG();

        string absolutePath = Path.Combine(Application.dataPath, fullRelativePath.Replace("Assets/", ""));
        string dir = Path.GetDirectoryName(absolutePath);
        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        File.WriteAllBytes(absolutePath, pngData);
        Debug.Log($"[UIDissolve] Đã lưu Noise Texture vào: {absolutePath} ({pngData.Length} bytes)");

#if UNITY_EDITOR
        AssetDatabase.ImportAsset(fullRelativePath, ImportAssetOptions.ForceUpdate);
        TextureImporter importer = AssetImporter.GetAtPath(fullRelativePath) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Default;
            importer.sRGBTexture = false;
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.alphaIsTransparency = false;
            importer.SaveAndReimport();
        }
#endif
    }

    private static float SampleTileableFBM(float x, float y, float frequency, int octaves, float persistence, float lacunarity, int sx, int sy)
    {
        float total = 0f;
        float amplitude = 1f;
        float maxAmp = 0f;
        float freq = frequency;

        for (int i = 0; i < octaves; i++)
        {
            float nx = x * freq;
            float ny = y * freq;

            float v = Mathf.PerlinNoise(nx + sx, ny + sy) * (1f - x) * (1f - y) +
                      Mathf.PerlinNoise(nx - freq + sx, ny + sy) * x * (1f - y) +
                      Mathf.PerlinNoise(nx + sx, ny - freq + sy) * (1f - x) * y +
                      Mathf.PerlinNoise(nx - freq + sx, ny - freq + sy) * x * y;

            total += v * amplitude;
            maxAmp += amplitude;
            amplitude *= persistence;
            freq *= lacunarity;
        }

        return maxAmp > 0f ? total / maxAmp : 0f;
    }

    private static float SampleTileableWorley(float x, float y, int cells, int sx, int sy)
    {
        float minDist = 1f;
        float cellX = x * cells;
        float cellY = y * cells;

        int ix = Mathf.FloorToInt(cellX);
        int iy = Mathf.FloorToInt(cellY);

        for (int dy = -1; dy <= 1; dy++)
        {
            for (int dx = -1; dx <= 1; dx++)
            {
                int neighborX = (ix + dx + cells) % cells;
                int neighborY = (iy + dy + cells) % cells;

                float hash = Mathf.Sin((neighborX + sx) * 12.9898f + (neighborY + sy) * 78.233f) * 43758.5453f;
                float px = (hash - Mathf.Floor(hash));
                float py = ((hash * 1.5f) - Mathf.Floor(hash * 1.5f));

                float distX = (ix + dx + px) - cellX;
                float distY = (iy + dy + py) - cellY;
                float d = Mathf.Sqrt(distX * distX + distY * distY);

                if (d < minDist)
                {
                    minDist = d;
                }
            }
        }

        return minDist;
    }
}
