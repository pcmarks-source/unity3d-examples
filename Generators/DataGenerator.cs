using UnityEngine;

public abstract class DataGenerator {

    public static float[,] FalloffData(int resolution) {
        float[,] falloffMap = new float[resolution, resolution];
        float xValue, yValue, value;

        for (int y = 0; y < resolution; y++) {
            for (int x = 0; x < resolution; x++) {

                xValue = x / (float)resolution * 2 - 1;
                yValue = y / (float)resolution * 2 - 1;
                value = Mathf.Max(Mathf.Abs(xValue), Mathf.Abs(yValue));

                falloffMap[y, x] = Falloff.Evaluate(value);

            }
        }

        return falloffMap;
    }

    public static float[,] FlatMapData(int width, int length, float level) {
        float[,] flatMap = new float[width, length];
        for (int y = 0; y < length; y++) {
            for (int x = 0; x < width; x++) {
                flatMap[y, x] = level;
            }
        }
        return flatMap;
    }

    public static float[,] NoiseMapData(NoiseSettings settings, Vector2 center, int width, int length, bool preview) {

        #region Variables

        Random.State initialState = Random.state;
        Random.InitState(settings.seed);

        NormalizeMode normalizeMode_previewsafe = (preview) ? NormalizeMode.Local : settings.normalizeMode;
        float scale_previewsafe = settings.scale;

        float[,] noiseMap = new float[width, length];
        float halfWidth = width / 2f;
        float halfLength = length / 2f;

        float noiseValue = 0;
        float noiseEstimate = 0;

        float noiseMin = (normalizeMode_previewsafe == NormalizeMode.Global) ? 0 : float.MaxValue;
        float noiseMax = (normalizeMode_previewsafe == NormalizeMode.Global) ? 0 : float.MinValue;

        float amplitude = 1;
        float frequency = 1;

        #endregion

        #region Random Number Generator

        System.Random prng = new System.Random(settings.seed);
        Vector2[] octaveOffsets = new Vector2[settings.octaves];
        for (int o = 0; o < settings.octaves; o++) {
            float xOffset = prng.Next(-100000, 100000) + settings.offset.x + center.x;
            float yOffset = prng.Next(-100000, 100000) - settings.offset.y - center.y;
            octaveOffsets[o] = new Vector2(xOffset, yOffset);

            if (normalizeMode_previewsafe == NormalizeMode.Global) noiseMax += amplitude * Mathf.Pow(settings.persistance, o);
            else noiseEstimate += amplitude;
            amplitude *= settings.persistance;
        }
        if (normalizeMode_previewsafe == NormalizeMode.Global) noiseMin = -noiseMax;

        #endregion

        #region Main

        for (int y = 0; y < length; y++) {
            for (int x = 0; x < width; x++) {

                amplitude = 1;
                frequency = 1;
                noiseValue = 0;

                for (int o = 0; o < settings.octaves; o++) {

                    float xSample = (x - halfWidth + octaveOffsets[o].x) / scale_previewsafe * frequency;
                    float ySample = (y - halfLength + octaveOffsets[o].y) / scale_previewsafe * frequency;

                    float perlinValue = Mathf.PerlinNoise(xSample, ySample) * 2f - 1f;
                    noiseValue += perlinValue * amplitude;

                    amplitude *= settings.persistance;
                    frequency *= settings.lacunarity;

                }

                #region Normalization

                if (normalizeMode_previewsafe != NormalizeMode.Global) {
                    if (noiseValue > noiseMax) noiseMax = noiseValue;
                    if (noiseValue < noiseMin) noiseMin = noiseValue;
                }

                #endregion

                if (normalizeMode_previewsafe != NormalizeMode.Global) noiseMap[x, y] = noiseValue;
                else noiseMap[x, y] = Mathf.InverseLerp(noiseMin, noiseMax, noiseValue);

            }
        }

        #endregion

        #region Normalization

        if (normalizeMode_previewsafe != NormalizeMode.Global) {
            for (int y = 0; y < length; y++) {
                for (int x = 0; x < width; x++) {
                    if (normalizeMode_previewsafe == NormalizeMode.Local) noiseMap[x, y] = Mathf.InverseLerp(noiseMin, noiseMax, noiseMap[x, y]);
                    else noiseMap[x, y] = Mathf.Clamp((noiseMap[x, y] + 1f) / (2f * noiseEstimate / 1.70f), 0, int.MaxValue);
                }
            }
        }

        #endregion

        Random.state = initialState;
        return noiseMap;
    }

    public static HeightMap HeightMapData(HeightMapSettings settings, Vector2 center, int resolution, bool preview = false) {
        float heightMultiplier_previewsafe = settings.heightMultiplier;
        AnimationCurve heightCurve_threadsafe = new AnimationCurve(settings.heightCurve.keys);

        float[,] values = Logics.Smooth(NoiseMapData(settings.noiseSettings, center, resolution, resolution, preview), 3);
        float minimumValue = float.MaxValue;
        float maximumValue = float.MinValue;

        FalloffMode falloff = FalloffMode.Crater;
        float[,] falloffSample = (falloff != FalloffMode.None) ? Logics.Smooth(FalloffData(resolution), 3) : null;

        for (int y = 0; y < resolution; y++) {
            for (int x = 0; x < resolution; x++) {
                if (falloff == FalloffMode.Island) values[x, y] = Maths.Map(values[x, y] - falloffSample[x, y], -1, 1, 0, 1);
                else if (falloff == FalloffMode.Crater) values[x, y] = Maths.Map(values[x, y] + falloffSample[x, y], 0, 2, 0, 1);
                values[x, y] = heightCurve_threadsafe.Evaluate(values[x, y]) * heightMultiplier_previewsafe;

                if (values[x, y] > maximumValue) maximumValue = values[x, y];
                if (values[x, y] < minimumValue) minimumValue = values[x, y];
            }
        }

        return new HeightMap(values, minimumValue, maximumValue);
    }

    public static WorldData GenerateWorldData(TerrainData terrainData, TerrainSurfaceData[] surfaceData, bool climateData = false) {

        #region Variables

        Statics.maximumGlobalHeight = terrainData.bounds.max.y;
        Statics.minimumGlobalHeight = 0;

        Statics.maximumGrowthHeight = Statics.maximumGlobalHeight * surfaceData[3].startingHeight;
        Statics.minimumGrowthHeight = Statics.maximumGlobalHeight * surfaceData[2].startingHeight;

        Statics.seaLevel = Mathf.Lerp(surfaceData[1].startingHeight, surfaceData[2].startingHeight, 0.9f);
        Statics.cloudLevel = Statics.maximumGlobalHeight * 1.1f;

        int width = Statics.terrainDataResolution;
        int length = Statics.terrainDataResolution;
        int depth = surfaceData.Length;

        float[,] elevation = new float[width, length];
        float[,] temperature = new float[width, length];
        float[,] moisture = new float[width, length];
        int[,] surface = new int[width, length];
        float[,,] splatMap = new float[width, length, depth];

        float[] surfaceValues;

        float minimumGrowthHeight = Statics.minimumGrowthHeight / Statics.maximumGlobalHeight;
        float maximumGrowthHeight = Statics.maximumGrowthHeight / Statics.maximumGlobalHeight;

        #endregion

        for (int y = 0; y < length; y++) {
            for (int x = 0; x < width; x++) {

                #region Elevation

                elevation[x, y] = terrainData.GetHeight(y, x) / Statics.maximumGlobalHeight;

                #endregion

                #region Temperature

                if (elevation[x, y] >= maximumGrowthHeight)
                    temperature[x, y] = Maths.Map(elevation[x, y], maximumGrowthHeight, 1.0f, 0.25f, 0.0f);
                else if (elevation[x, y] <= minimumGrowthHeight)
                    temperature[x, y] = Maths.Map(elevation[x, y], 0.0f, minimumGrowthHeight, 0.0f, 0.25f);
                else temperature[x, y] = Maths.MapToCenter(elevation[x, y], Statics.seaLevel, minimumGrowthHeight, maximumGrowthHeight, 0.25f, 0.75f);

                #endregion

                #region Moisture

                if (elevation[x, y] >= maximumGrowthHeight)
                    moisture[x, y] = 0;
                else if (elevation[x, y] <= Statics.seaLevel)
                    moisture[x, y] = 1;
                else if (elevation[x, y] > Statics.seaLevel && elevation[x, y] < minimumGrowthHeight)
                    moisture[x, y] = Maths.Map(elevation[x, y], Statics.seaLevel, minimumGrowthHeight, 1.0f, 0.75f);
                else moisture[x, y] = Maths.Map(elevation[x, y], minimumGrowthHeight, maximumGrowthHeight, 0.75f, 0.0f);

                #endregion

                #region Surface

                surfaceValues = new float[depth];

                for (int i = 0; i < depth; i++) {
                    if (i == depth - 1 && elevation[x, y] >= surfaceData[i].startingHeight) {
                        surfaceValues[i] = 1;
                        surface[x, y] = i;
                    } else if (elevation[x, y] >= surfaceData[i].startingHeight && elevation[x, y] < surfaceData[i + 1].startingHeight) {
                        surfaceValues[i] = 1;
                        surface[x, y] = i;
                    }
                }

                for (int i = 0; i < depth; i++) {
                    splatMap[x, y, i] = surfaceValues[i];
                }

                #endregion

            }
        }

        #region Smoothing

        for (int i = 0; i < 3; i++) {
            splatMap = Logics.Smooth(splatMap, i);
        }

        #endregion

        return new WorldData(elevation, temperature, moisture, surface, splatMap);
    }

    public static Texture2D SurfaceImage(int[,] values, TerrainSurfaceData[] surfaceData) {

        int width = values.GetLength(0);
        int length = values.GetLength(1);

        Color[] colorMap = new Color[width * length];

        for (int y = 0; y < length; y++) {
            for (int x = 0; x < width; x++) {

                colorMap[(y * width) + x] = surfaceData[values[x, y]].color;

            }
        }

        return TextureFromColors(colorMap, width, length);
    }

    public static Texture2D GradientImage(float[,] values, Color minimumColor, Color maximumColor, float minimumValue = 0.0f, float maximumValue = 1.0f) {

        int width = values.GetLength(0);
        int length = values.GetLength(1);

        Color[] colorMap = new Color[width * length];

        for (int y = 0; y < length; y++) {
            for (int x = 0; x < width; x++) {
                colorMap[(y * width) + x] = Color.Lerp(minimumColor, maximumColor, Mathf.InverseLerp(minimumValue, maximumValue, values[x, y]));
            }
        }

        return TextureFromColors(colorMap, width, length);
    }

    public static Texture2D DeepGradientImage(float[,] values, bool inverse = false, float minimumValue = 0.0f, float maximumValue = 1.0f) {

        int width = values.GetLength(0);
        int length = values.GetLength(1);

        Color[] colorMap = new Color[width * length];
        float value;

        Color colorZero = Color.magenta;
        Color colorTwentieth = Color.blue;
        Color colorFortieth = Color.green;
        Color colorSixtieth = Color.yellow;
        Color colorEightieth = new Color(1.0f, 0.5f, 0.0f, 1.0f);
        Color colorOne = Color.red;

        for (int y = 0; y < length; y++) {
            for (int x = 0; x < width; x++) {

                value = Maths.Map(values[x, y], minimumValue, maximumValue, 0.0f, 1.0f);
                if (inverse == true) value = Maths.Inverse(value);

                if (value <= 0.2f) colorMap[(y * width) + x] = Color.Lerp(colorZero, colorTwentieth, value);
                else if (value <= 0.4f) colorMap[(y * width) + x] = Color.Lerp(colorTwentieth, colorFortieth, value);
                else if (value <= 0.6f) colorMap[(y * width) + x] = Color.Lerp(colorFortieth, colorSixtieth, value);
                else if (value <= 0.8f) colorMap[(y * width) + x] = Color.Lerp(colorSixtieth, colorEightieth, value);
                else colorMap[(y * width) + x] = Color.Lerp(colorEightieth, colorOne, value);

            }
        }

        return TextureFromColors(colorMap, width, length);
    }

    public static Texture2D TextureFromColors(Color[] colorMap, int width, int length) {
        Texture2D texture = new Texture2D(width, length);
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.SetPixels(colorMap);
        texture.Apply();

        return texture;
    }

    public static Texture2D DataSetToImage(WorldData dataSet) {
        int width = dataSet.elevation.GetLength(0);
        int length = dataSet.elevation.GetLength(1);

        Color[] colorMap = new Color[width * length];

        for (int y = 0; y < length; y++) {
            for (int x = 0; x < width; x++) {
                colorMap[(y * width) + x].r = dataSet.elevation[x, y];
                colorMap[(y * width) + x].g = dataSet.temperature[x, y];
                colorMap[(y * width) + x].b = dataSet.moisture[x, y];
                colorMap[(y * width) + x].a = 1;
            }
        }

        return TextureFromColors(colorMap, width, length);
    }

    public static WorldData ImageToDataSet(Texture2D texture) {

        int width = texture.width;
        int length = texture.height;

        Color[] pixels = texture.GetPixels();

        float[,] elevation = new float[width, length];
        float[,] temperature = new float[width, length];
        float[,] moisture = new float[width, length];

        for (int y = 0; y < length; y++) {
            for (int x = 0; x < width; x++) {
                elevation[x, y] = pixels[(y * width) + x].r;
                temperature[x, y] = pixels[(y * width) + x].g;
                moisture[x, y] = pixels[(y * width) + x].b;
            }
        }

        return new WorldData(elevation, temperature, moisture, null, null);
    }

}

public enum NormalizeMode { Local, Global, Estimate }
public enum FalloffMode { None, Island, Crater }

[System.Serializable]
public class NoiseSettings {
    public NormalizeMode normalizeMode = NormalizeMode.Global;
    public int seed = 6754;
    public Vector2 offset = new Vector2(0, 0);
    [Space(12)]
    public int octaves = 10;
    [Range(0, 1)] public float persistance = 0.5f;
    public float lacunarity = 2;
    [Space(12)]
    public float relativeScale = 1;
    public float scale {
        get {
            return relativeScale * Statics.terrainDataResolution;
        }
    }
}

[System.Serializable]
public class HeightMapSettings {
    public NoiseSettings noiseSettings;
    [Space(12)]
    public float heightMultiplier = 150;
    public AnimationCurve heightCurve;

    public float minHeight {
        get {
            return heightCurve.Evaluate(0) * heightMultiplier;
        }
    }
    public float maxHeight {
        get {
            return heightCurve.Evaluate(1) * heightMultiplier;
        }
    }
}

[System.Serializable]
public class MeshSettings {
    public const int supportedLODCount = 5;
    public const int supportedFlatSizes = 3;
    public const int supportedSmoothSizes = 9;
    public static readonly int[] supportedChunkSizes = { 48, 72, 96, 120, 144, 168, 192, 216, 240 };

    public float scale = 4f;
    public bool flatShade = false;
    [Space(12)]
    [Range(0, supportedFlatSizes - 1)]
    public int flatChunkSizeIndex;
    [Range(0, supportedSmoothSizes - 1)] public int smoothChunkSizeIndex;

    /// <summary>
    /// Number of vertices per line of mesh rendered at LOD0.
    /// Includes 2 extra vertices for calculating normals (needed to support smooth shading) that are excluded from the final mesh.
    /// </summary>
    public int chunkResolution {
        get {
            return supportedChunkSizes[(flatShade) ? flatChunkSizeIndex : smoothChunkSizeIndex] + 1;
        }
    }

    /// <summary>
    /// Edge size of the mesh in world units.
    /// </summary>
    public float meshWorldSize {
        get {
            return (chunkResolution - 3) * scale;
        }
    }
}

public struct HeightMap {
    public readonly float[,] values;
    public readonly float minimumValue;
    public readonly float maximumValue;

    public HeightMap(float[,] values, float minimumValue, float maximumValue) {
        this.values = values;
        this.minimumValue = minimumValue;
        this.maximumValue = maximumValue;
    }
}

public struct WorldData {
    public readonly float[,] elevation;
    public readonly float[,] temperature;
    public readonly float[,] moisture;
    public readonly int[,] surface;
    public readonly float[,,] splatMap;

    public WorldData(float[,] elevation, float[,] temperature, float[,] moisture, int[,] surface, float[,,] splatMap) {
        this.elevation = elevation;
        this.temperature = temperature;
        this.moisture = moisture;
        this.surface = surface;
        this.splatMap = splatMap;
    }
}
