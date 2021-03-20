using UnityEngine;

public abstract class Maths {

    /// <summary>
    /// An arbitrarily small positive quantity.
    /// </summary>
    public const float epsilon = 1E-4f;

    /// <summary>
    /// Generates a 2D hash grid of random values between 0 and 1. Non-destructive to Random.state
    /// </summary>
    /// <param name="seed">Random seed to use. Defaults to 6754</param>
    /// <param name="resolution">Resolution of the hash per line. Defaults to 256</param>
    /// <returns></returns>
    public static bool[,] GenerateHashBoolean(int seed = 6754, int resolution = Statics.hashResolution) {
        Random.State initialState = Random.state;
        Random.InitState(seed);

        bool[,] hashGrid = new bool[resolution, resolution];
        for (int x = 0; x < resolution; x++) {
            for (int y = 0; y < resolution; y++) {
                hashGrid[x, y] = (Random.value >= 0.5f) ? true : false;
            }
        }

        Random.state = initialState;
        return hashGrid;
    }

    /// <summary>
    /// Generates a 2D hash grid of random values between 0 and 1. Non-destructive to Random.state
    /// </summary>
    /// <param name="seed">Random seed to use. Defaults to 6754</param>
    /// <param name="resolution">Resolution of the hash per line. Defaults to 256</param>
    /// <returns></returns>
    public static float[,] GenerateHashAbs(int seed = 6754, int resolution = Statics.hashResolution) {
        Random.State initialState = Random.state;
        Random.InitState(seed);

        float[,] hashGrid = new float[resolution, resolution];
        for (int x = 0; x < resolution; x++) {
            for (int y = 0; y < resolution; y++) {
                hashGrid[x, y] = Random.value;
            }
        }

        Random.state = initialState;
        return hashGrid;
    }

    /// <summary>
    /// Generates a 2D hash grid of random values between 0 and 1. Non-destructive to Random.state
    /// </summary>
    /// <param name="seed">Random seed to use. Defaults to 6754</param>
    /// <param name="resolution">Resolution of the hash per line. Defaults to 256</param>
    /// <returns></returns>
    public static float[,] GenerateHashNormal(int seed = 6754, int resolution = Statics.hashResolution) {
        Random.State initialState = Random.state;
        Random.InitState(seed);

        float[,] hashGrid = new float[resolution, resolution];
        for (int x = 0; x < resolution; x++) {
            for (int y = 0; y < resolution; y++) {
                hashGrid[x, y] = Random.Range(-1, 1);
            }
        }

        Random.state = initialState;
        return hashGrid;
    }

    /// <summary>
    /// Returns true if the given value is positive.
    /// </summary>
    /// <param name="value">Number that may or may not be positive.</param>
    /// <returns></returns>
    public static bool IsEven(float value) {
        return (value % 2f == 0f);
    }

    /// <summary>
    /// Inverses a number within a set range. Always reterns positive numbers.
    /// Example: 0.75 in range 1.0 returns 0.25
    /// </summary>
    /// <param name="value">Number within range.</param>
    /// <param name="max">Maximum number of the range. Minimum is assumed to be zero.</param>
    /// <returns></returns>
    public static float Inverse(float value, float max = 1.0f) {
        return Mathf.Abs(value - max);
    }

    /// <summary>
    /// Halves a value the number of times indicated by factor.
    /// </summary>
    /// <param name="value">Number to be reduced.</param>
    /// <param name="factor">Number of times to reduce value.</param>
    /// <returns></returns>
    public static float Downscale(float value, float factor) {
        for (int i = 0; i < factor; i++) {
            value *= 0.5f;
        }
        return value;
    }

    /// <summary>
    /// Maps a value from one range to another.
    /// </summary>
    /// <param name="value">Number to be reranged.</param>
    /// <param name="originalMin">Minimum number in the original range.</param>
    /// <param name="originalMax">Maximum number in the original range.</param>
    /// <param name="targetMin">Minimum number in the target range.</param>
    /// <param name="targetMax">Maximum number in the target range.</param>
    /// <returns></returns>
    public static float Map(float value, float originalMin, float originalMax, float targetMin, float targetMax) {
        return (value - originalMin) * (targetMax - targetMin) / (originalMax - originalMin) + targetMin;
    }

    /// <summary>
    /// Maps a value from one range to another.
    /// Skews the results to relative to a center point.
    /// </summary>
    /// <param name="value">Number to be reranged.</param>
    /// <param name="center">Number to be considered the middle.</param>
    /// <param name="originalMin">Minimum number in the original range.</param>
    /// <param name="originalMax">Maximum number in the original range.</param>
    /// <param name="targetMin">Minimum number in the target range.</param>
    /// <param name="targetMax">Maximum number in the target range.</param>
    /// <returns></returns>
    public static float MapToCenter(float value, float center, float originalMin, float originalMax, float targetMin, float targetMax) {
        return (value >= center) ? Map(value, center, originalMax, targetMax, targetMin) : Map(value, originalMin, center, targetMin, targetMax);
    }

    /// <summary>
    /// Returns the length of a 2D vector that is always positive.
    /// </summary>
    /// <param name="v">Vector to be measured.</param>
    /// <returns></returns>
    public static float AbsLength(Vector2 v) {
        return (Mathf.Abs(v.x) + Mathf.Abs(v.y)) / 2.0f;
    }

    /// <summary>
    /// Normalizes an array of floats between zero and one to an array of floats equal to one.
    /// </summary>
    /// <param name="values">Array of floats to be normalized. Each float should be between zero and one.</param>
    /// <returns></returns>
    public static float[] Normalize(float[] values) {
        float total = 0;
        for (int i = 0; i < values.Length; i++) {
            total += values[i];
        }

        for (int i = 0; i < values.Length; i++) {
            values[i] /= total;
        }
        return values;
    }

    /// <summary>
    /// I can't remember what I thought I was up to here.
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <param name="c"></param>
    /// <param name="t"></param>
    /// <returns></returns>
    public static Vector3 Bezier(Vector3 a, Vector3 b, Vector3 c, float t) {
        float r = 1f - t;
        return r * r * a + 2f * r * t * b + t * t * c;
    }

    /// <summary>
    /// I can't remember what I thought I was up to here.
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <param name="c"></param>
    /// <param name="t"></param>
    /// <returns></returns>
    public static Vector3 BezierDerivative(Vector3 a, Vector3 b, Vector3 c, float t) {
        return 2f * ((1f - t) * (b - a) + t * (c - b));
    }

}

public struct HashSet {
    public readonly bool[,] boolean;
    public readonly float[,] absolute;
    public readonly float[,] normal;
    public readonly float[,] perlin;

    public HashSet(bool[,] booleanSet, float[,] absoluteSet, float[,] normalSet, float[,] perlinSet) {
        boolean = booleanSet;
        absolute = absoluteSet;
        normal = normalSet;
        perlin = perlinSet;
    }

    /// <summary>
    /// Returns true or false.
    /// </summary>
    /// <param name="position">Position in the hash grid to sample.</param>
    /// <returns></returns>
    public bool SampleHashBoolean(Vector3 position) {
        int x = (int)(position.x * Statics.hashScale) % Statics.hashResolution;
        if (x < 0) x += Statics.hashResolution;
        int z = (int)(position.z * Statics.hashScale) % Statics.hashResolution;
        if (z < 0) z += Statics.hashResolution;
        return boolean[x, z];
    }

    /// <summary>
    /// Returns a value from 0 to 1.
    /// </summary>
    /// <param name="position">Position in the hash grid to sample.</param>
    /// <returns></returns>
    public float SampleHashAbs(Vector3 position) {
        int x = (int)(position.x * Statics.hashScale) % Statics.hashResolution;
        if (x < 0) x += Statics.hashResolution;
        int z = (int)(position.z * Statics.hashScale) % Statics.hashResolution;
        if (z < 0) z += Statics.hashResolution;
        return absolute[x, z];
    }

    /// <summary>
    /// Returns a value from -1 to 1.
    /// </summary>
    /// <param name="position">Position in the hash grid to sample.</param>
    /// <returns></returns>
    public float SampleHash(Vector3 position) {
        int x = (int)(position.x * Statics.hashScale) % Statics.hashResolution;
        if (x < 0) x += Statics.hashResolution;
        int z = (int)(position.z * Statics.hashScale) % Statics.hashResolution;
        if (z < 0) z += Statics.hashResolution;
        return normal[x, z];
    }

    /// <summary>
    /// Returns a coherent value from 0 to 1.
    /// </summary>
    /// <param name="position">Position in the hash grid to sample.</param>
    /// <returns></returns>
    public float SamplePerlin(Vector3 position) {
        int x = (int)(position.x * Statics.hashScale) % Statics.terrainDataResolution;
        if (x < 0) x += Statics.terrainDataResolution;
        int z = (int)(position.z * Statics.hashScale) % Statics.terrainDataResolution;
        if (z < 0) z += Statics.terrainDataResolution;
        return perlin[x, z];
    }
	
	/// <summary>
    /// Failsafe against going out of range inside 2D for loop.
    /// </summary>
    /// <param name="x">Current position inside the width of the for loop.</param>
    /// <param name="width">Maximum width of the for loop.</param>
    /// <param name="y">Current position inside the length of the for loop.</param>
    /// <param name="length">Maximum length of the for loop.</param>
    /// <returns></returns>
    public static bool OnLoopEdge(int x, int width, int y, int length) {

        if (x == 0) return true;
        if (x == width - 1) return true;
        if (y == 0) return true;
        if (y == length - 1) return true;

        return false;

    }

    /// <summary>
    /// Smooths out a 2D array of floats using a blurring kernel.
    /// </summary>
    /// <param name="dataSet">2D Array of floats. For example a height map.</param>
    /// <param name="sizeOFKernel">Range of the kernel. Data points in range factor into final smoothing.</param>
    /// <returns></returns>
    public static float[,] Smooth(float[,] dataSet, int sizeOfKernel = 1) {

        #region Variables

        int width = dataSet.GetLength(0);
        int length = dataSet.GetLength(1);

        int kernelSize = sizeOfKernel * 2 + 1;
        int kernelExtents = (kernelSize - 1) / 2;

        int sample, removeIndex, addIndex;

        float[,] smoothMap = new float[width, length];

        #endregion

        #region Horizontal Pass

        float[,] horizontalPass = new float[width, length];

        for (int y = 0; y < length; y++) {

            for (int x = -kernelExtents; x <= kernelExtents; x++) {

                sample = Mathf.Clamp(x, 0, kernelExtents);
                horizontalPass[0, y] += dataSet[sample, y];

            }

            for (int x = 1; x < width; x++) {

                removeIndex = Mathf.Clamp(x - kernelExtents - 1, 0, width);
                addIndex = Mathf.Clamp(x + kernelExtents, 0, width - 1);

                horizontalPass[x, y] = horizontalPass[x - 1, y] - dataSet[removeIndex, y] + dataSet[addIndex, y];

            }

        }

        #endregion

        #region Vertical Pass

        float[,] verticalPass = new float[width, length];

        for (int x = 0; x < width; x++) {

            for (int y = -kernelExtents; y <= kernelExtents; y++) {

                sample = Mathf.Clamp(0, y, kernelExtents);
                verticalPass[x, 0] += horizontalPass[x, sample];

            }

            smoothMap[x, 0] = verticalPass[x, 0] / ((float)kernelSize * kernelSize);

            for (int y = 1; y < length; y++) {

                removeIndex = Mathf.Clamp(y - kernelExtents - 1, 0, length);
                addIndex = Mathf.Clamp(y + kernelExtents, 0, length - 1);

                verticalPass[x, y] = verticalPass[x, y - 1] - horizontalPass[x, removeIndex] + horizontalPass[x, addIndex];
                smoothMap[x, y] = verticalPass[x, y] / ((float)kernelSize * kernelSize);

            }

        }

        #endregion

        return smoothMap;
    }

    /// <summary>
    /// Smooths out a 3D array of floats using a blurring kernel.
    /// </summary>
    /// <param name="dataSet">3D Array of floats. For example a height map.</param>
    /// <param name="sizeOFKernel">Range of the kernel. Data points in range factor into final smoothing.</param>
    /// <returns></returns>
    public static float[,,] Smooth(float[,,] dataSet, int sizeOfKernel = 1) {

        #region Variables

        int width = dataSet.GetLength(0);
        int length = dataSet.GetLength(1);
        int depth = dataSet.GetLength(2);

        int kernelSize = sizeOfKernel * 2 + 1;
        int kernelExtents = (kernelSize - 1) / 2;

        int sample, removeIndex, addIndex;

        float[,,] smoothMap = new float[width, length, depth];

        #endregion

        #region Horizontal Pass

        float[,,] horizontalPass = new float[width, length, depth];

        for (int y = 0; y < length; y++) {

            for (int x = -kernelExtents; x <= kernelExtents; x++) {

                sample = Mathf.Clamp(x, 0, kernelExtents);
                for (int i = 0; i < depth; i++) {
                    horizontalPass[0, y, i] += dataSet[sample, y, i];
                }

            }

            for (int x = 1; x < width; x++) {

                removeIndex = Mathf.Clamp(x - kernelExtents - 1, 0, width);
                addIndex = Mathf.Clamp(x + kernelExtents, 0, width - 1);

                for (int i = 0; i < depth; i++) {
                    horizontalPass[x, y, i] = horizontalPass[x - 1, y, i] - dataSet[removeIndex, y, i] + dataSet[addIndex, y, i];
                }

            }

        }

        #endregion

        #region Vertical Pass

        float[,,] verticalPass = new float[width, length, depth];

        for (int x = 0; x < width; x++) {

            for (int y = -kernelExtents; y <= kernelExtents; y++) {

                sample = Mathf.Clamp(0, y, kernelExtents);
                for (int i = 0; i < depth; i++) {
                    verticalPass[x, 0, i] += horizontalPass[x, sample, i];
                }

            }

            for (int i = 0; i < depth; i++) {
                smoothMap[x, 0, i] = verticalPass[x, 0, i] / ((float)kernelSize * kernelSize);
            }

            for (int y = 1; y < length; y++) {

                removeIndex = Mathf.Clamp(y - kernelExtents - 1, 0, length);
                addIndex = Mathf.Clamp(y + kernelExtents, 0, length - 1);

                for (int i = 0; i < depth; i++) {
                    verticalPass[x, y, i] = verticalPass[x, y - 1, i] - horizontalPass[x, removeIndex, i] + horizontalPass[x, addIndex, i];
                    smoothMap[x, y, i] = verticalPass[x, y, i] / ((float)kernelSize * kernelSize);
                }

            }

        }

        #endregion

        return smoothMap;
    }
}
