using System.Collections.Generic;
using UnityEngine;

public class MeshGenerator : MonoBehaviour {

    public static MeshData FromHeightMap(int seed, MeshSettings settings, float[,] heightMap, int levelOfDetail) {

        Random.State initialState = Random.state;
        Random.InitState(seed);

        int meshSimplificationIncrement = (levelOfDetail == 0) ? 1 : levelOfDetail * 2;
        int borderedSize = heightMap.GetLength(0);

        #region Border Array

        #region Variables

        int[,] vertexIndices = new int[borderedSize, borderedSize];
        int borderVertexIndex = -1;
        int meshVertexIndex = 0;

        #endregion

        for (int z = 0; z < borderedSize; z += meshSimplificationIncrement) {
            for (int x = 0; x < borderedSize; x += meshSimplificationIncrement) {
                bool isBorderVertex = z == 0 || z == borderedSize - 1 || x == 0 || x == borderedSize - 1;

                if (isBorderVertex) {
                    vertexIndices[x, z] = borderVertexIndex;
                    borderVertexIndex--;
                } else {
                    vertexIndices[x, z] = meshVertexIndex;
                    meshVertexIndex++;
                }
            }
        }

        #endregion

        #region Mesh Data

        #region Variables

        int meshSize = borderedSize - 2 * meshSimplificationIncrement;
        int meshSizeUnsimplified = borderedSize - 2;
        float topLeftX = (meshSizeUnsimplified - 1) / -2f;
        float topLeftZ = (meshSizeUnsimplified - 1) / 2f;
        int verticesPerLine = (meshSize - 1) / meshSimplificationIncrement + 1;

        MeshData meshData = new MeshData(verticesPerLine, settings.flatShade);

        int vertexIndex;
        Vector2 uvPercent;
        Vector3 vertexPosition;

        #endregion

        for (int z = 0; z < borderedSize; z += meshSimplificationIncrement) {
            for (int x = 0; x < borderedSize; x += meshSimplificationIncrement) {

                vertexIndex = vertexIndices[x, z];

                uvPercent = new Vector2(
                        (x - meshSimplificationIncrement) / (float)meshSize,
                        (z - meshSimplificationIncrement) / (float)meshSize
                    );

                vertexPosition = new Vector3(
                        (topLeftX + uvPercent.x * meshSizeUnsimplified) * settings.scale,
                        heightMap[x, z],
                        (topLeftZ - uvPercent.y * meshSizeUnsimplified) * settings.scale
                    );

                meshData.AddVertex(vertexPosition, uvPercent, vertexIndex);

                if (x < borderedSize - 1 && z < borderedSize - 1) {
                    int a = vertexIndices[x, z];
                    int b = vertexIndices[x + meshSimplificationIncrement, z];
                    int c = vertexIndices[x, z + meshSimplificationIncrement];
                    int d = vertexIndices[x + meshSimplificationIncrement, z + meshSimplificationIncrement];

                    meshData.AddTriangle(a, d, c);
                    meshData.AddTriangle(d, a, b);
                }

                vertexIndex++;
            }
        }

        #endregion

        meshData.BakeNormals();
        Random.state = initialState;
        return meshData;
    }

    public static MeshData PointCloudAtHeight(int resolution, int seed, Vector2 sampleCenter, float height) {

        #region Variables

        Random.State initialState = Random.state;
        Random.InitState(seed);

        int verticesPerLine = resolution;
        float spaceBetweenPoints = (float)Statics.terrainDataResolution / verticesPerLine;
        float noise = spaceBetweenPoints * 0.25f;

        float realX;
        float realZ;
        Vector2 noiseOffset;
        Vector2 sampleOffset;
        Vector3 origin;

        Vector3 vertexPosition;
        Vector2 uvPercent;
        Vector3 normalDirection;
        Color color;

        int index = 0;

        MeshData meshData = new MeshData(true);

        #endregion

        for (int z = 0; z < verticesPerLine; z++) {
            for (int x = 0; x < verticesPerLine; x++) {

                realX = x * spaceBetweenPoints;
                realZ = z * spaceBetweenPoints;

                noiseOffset = new Vector2(Random.Range(-noise, noise), Random.Range(-noise, noise));
                sampleOffset = new Vector2(realX - (Statics.terrainDataResolution * 0.5f), realZ - (Statics.terrainDataResolution * 0.5f));
                origin = new Vector3(sampleCenter.x + sampleOffset.x + noiseOffset.x, height, sampleCenter.y + sampleOffset.y + noiseOffset.y);

                vertexPosition = new Vector3(origin.x - sampleCenter.x, origin.y, origin.z - sampleCenter.y);
                uvPercent = new Vector2((realX + noiseOffset.x) / 1024, (realZ + noiseOffset.y) / 1024);

                normalDirection = -Vector3.up;
                color = Color.white;

                meshData.AddPoint(index, vertexPosition, uvPercent, normalDirection, color);
                index++;

            }
        }

        Random.state = initialState;
        return meshData;

    }

    public static MeshData PointCloudFromRaycasts(int resolution, int seed, Vector2 sampleCenter, WorldData worldData, PlantProfile profile) {

        #region Variables

        Random.State initialState = Random.state;
        Random.InitState(seed);

        int verticesPerLine = resolution;
        float spaceBetweenGrass = (float)Statics.terrainDataDetailResolution / verticesPerLine;
        float noise = spaceBetweenGrass * 0.25f;

        float maxAltitude = Statics.maximumGlobalHeight + 1;
        float minAltitude = Statics.minimumGlobalHeight - 1;

        float elevation;

        Ray ray;
        RaycastHit hit;
        float distance = maxAltitude - minAltitude;
        LayerMask collisionMask = Statics.groundLayer;
        LayerMask obstacleMask = Statics.noGrowZone;

        Vector3 vertexPosition;
        Vector2 uvPercent;
        Vector3 normalDirection;
        Color color;
        int index = 0;

        MeshData meshData = new MeshData(true);

        #endregion

        for (int z = 0; z < verticesPerLine; z++) {
            for (int x = 0; x < verticesPerLine; x++) {

                float realX = x * spaceBetweenGrass;
                float realZ = z * spaceBetweenGrass;

                Vector2 noiseOffset = new Vector2(Random.Range(-noise, noise), Random.Range(-noise, noise));
                Vector2 sampleOffset = new Vector2(realX - (verticesPerLine * 0.5f), realZ - (verticesPerLine * 0.5f));
                Vector3 origin = new Vector3(sampleCenter.x + sampleOffset.x + noiseOffset.x, maxAltitude, sampleCenter.y + sampleOffset.y + noiseOffset.y);

                if (profile.density < Statics.hash.SampleHashAbs(origin)) continue;

                elevation = Logics.SampleDataSet(origin.z, origin.x, worldData.elevation);

                if (profile.habitat == PlantHabitat.Terrestrial) {

                    if (elevation < (Statics.minimumGrowthHeight + 1f) / Statics.maximumGlobalHeight) continue;
                    if (elevation > (Statics.maximumGrowthHeight - 1f) / Statics.maximumGlobalHeight) continue;

                } else if (profile.habitat == PlantHabitat.Marine) {

                    if (elevation > (Statics.minimumGrowthHeight + 1f) / Statics.maximumGlobalHeight) continue;
                    if (elevation < Statics.seaLevel - 0.002f) continue;

                } else if (profile.habitat == PlantHabitat.Aquatic) {

                    if (elevation > Statics.seaLevel - 0.002f) continue;

                }

                ray = new Ray(origin, -Vector3.up);
                if (Physics.Raycast(origin + (Vector3.up * (1000 - maxAltitude)), -Vector3.up, out hit, distance + 1001, obstacleMask, QueryTriggerInteraction.Collide)) continue;

                if (Physics.Raycast(ray, out hit, distance, collisionMask, QueryTriggerInteraction.Collide)) {

                    vertexPosition = new Vector3(hit.point.x - sampleCenter.x, hit.point.y - 0.01f, hit.point.z - sampleCenter.y);
                    uvPercent = new Vector2((realX + noiseOffset.x) / verticesPerLine, (realZ + noiseOffset.y) / verticesPerLine);

                    float perlin = Statics.hash.SamplePerlin(vertexPosition);

                    normalDirection = hit.normal * Maths.Map(perlin, 0.25f, 0.75f, 0.5f, 1.25f);
                    if (perlin <= profile.diseasePrevalence) color = Color.Lerp(profile.unhealthyDark, profile.unhealthyBright, Statics.hash.SampleHashAbs(vertexPosition));
                    else color = Color.Lerp(profile.healthyDark, profile.healthyBright, Statics.hash.SampleHashAbs(vertexPosition));

                    meshData.AddPoint(index, vertexPosition, uvPercent, normalDirection, color);
                    index++;

                }

            }
        }

        Random.state = initialState;
        return meshData;
    }

    public static List<TreeData> TreeCloudFromRaycasts(int seed, Vector2 sampleCenter, WorldData worldData, PlantProfile profile) {

        #region Variables

        Random.State initialState = Random.state;
        Random.InitState(seed);

        List<TreeData> treeCloud = new List<TreeData>();
        Vector3 position;
        Quaternion rotation;
        Vector3 scale;

        int density = Mathf.RoundToInt((Statics.terrainDataResolution * 0.5f) * profile.density);
        float maxAltitude = Statics.maximumGlobalHeight + 1;
        float minAltitude = Statics.minimumGlobalHeight - 1;
        float noise = 0.5f;

        Ray ray;
        RaycastHit hit;
        float distance = maxAltitude - minAltitude;
        LayerMask collisionMask = Statics.groundLayer;
        LayerMask obstacleMask = Statics.obstacleLayer;

        float elevation;

        #endregion

        for (int i = 0; i < density; i++) {
            Vector3 origin = new Vector3(sampleCenter.x, 0, sampleCenter.y);
            origin.y = maxAltitude;
            origin.x += Statics.terrainDataResolution * Random.Range(-0.5f, 0.5f);
            origin.z += Statics.terrainDataResolution * Random.Range(-0.5f, 0.5f);

            elevation = Logics.SampleDataSet(origin.z, origin.x, worldData.elevation);

            if (profile.habitat == PlantHabitat.Terrestrial) {

                if (elevation < (Statics.minimumGrowthHeight + 2.5f) / Statics.maximumGlobalHeight) continue;
                if (elevation > (Statics.maximumGrowthHeight - 2.5f) / Statics.maximumGlobalHeight) continue;

            } else if (profile.habitat == PlantHabitat.Marine) {

                if (elevation > (Statics.minimumGrowthHeight + 2.5f) / Statics.maximumGlobalHeight) continue;
                if (elevation < Statics.seaLevel - 0.002f) continue;

            } else if (profile.habitat == PlantHabitat.Aquatic) {

                if (elevation > Statics.seaLevel - 0.002f) continue;

            }

            ray = new Ray(origin, -Vector3.up);

            if (Physics.Raycast(origin + (Vector3.up * (1000 - maxAltitude)), -Vector3.up, out hit, distance + 1001, obstacleMask, QueryTriggerInteraction.Collide)) continue;

            if (Physics.Raycast(ray, out hit, distance, collisionMask, QueryTriggerInteraction.Collide)) {

                position = hit.point - Vector3.up * Random.Range(1f, 2f);
                rotation = Quaternion.Euler(
                        (Vector3.right * Random.Range(-noise, noise)) +
                        (Vector3.up * Random.Range(0, 360)) +
                        (Vector3.forward * Random.Range(-noise, noise))
                    );
                scale = Vector3.one * Random.Range(1 - noise, 1 + noise);

                GameObject newTree = Instantiate(profile.model, Statics.treesParent, true);
                newTree.name = profile.name + " (" + i + ")";

                newTree.transform.position = position;
                newTree.transform.rotation = rotation;
                newTree.transform.localScale = scale;

                treeCloud.Add(new TreeData(position, rotation, scale));
            }
        }

        Random.state = initialState;
        return treeCloud;
    }

}

public class MeshData {

    #region Standard Mesh

    private bool flatShading;

    private int triangleIndex;
    private Vector3[] vertices;
    private int[] triangles;
    private Vector2[] uv;
    private Vector3[] normals;
    private Color[] colors;

    private int borderTriangleIndex;
    private Vector3[] borderVertices;
    private int[] borderTriangles;

    public MeshData(int verticesPerLine, bool flatShading) {
        this.flatShading = flatShading;

        vertices = new Vector3[verticesPerLine * verticesPerLine];
        triangles = new int[(verticesPerLine - 1) * (verticesPerLine - 1) * 6];
        uv = new Vector2[verticesPerLine * verticesPerLine];

        borderVertices = new Vector3[verticesPerLine * 4 + 4];
        borderTriangles = new int[24 * verticesPerLine];
    }

    public void AddVertex(Vector3 vertexPosition, Vector2 uvPercent, int vertexIndex) {
        if (vertexIndex < 0) {
            borderVertices[-vertexIndex - 1] = vertexPosition;
        } else {
            vertices[vertexIndex] = vertexPosition;
            uv[vertexIndex] = uvPercent;
        }
    }

    public void AddTriangle(int a, int b, int c) {
        if (a < 0 || b < 0 || c < 0) {
            borderTriangles[borderTriangleIndex] = a;
            borderTriangles[borderTriangleIndex + 1] = b;
            borderTriangles[borderTriangleIndex + 2] = c;
            borderTriangleIndex += 3;
        } else {
            triangles[triangleIndex] = a;
            triangles[triangleIndex + 1] = b;
            triangles[triangleIndex + 2] = c;
            triangleIndex += 3;
        }
    }

    private Vector3 SurfaceNormalFromIndices(int indexA, int indexB, int indexC) {
        Vector3 pointA = (indexA < 0) ? borderVertices[-indexA - 1] : vertices[indexA];
        Vector3 pointB = (indexB < 0) ? borderVertices[-indexB - 1] : vertices[indexB];
        Vector3 pointC = (indexC < 0) ? borderVertices[-indexC - 1] : vertices[indexC];

        Vector3 sideAB = pointB - pointA;
        Vector3 sideAC = pointC - pointA;
        return Vector3.Cross(sideAB, sideAC).normalized;
    }

    public Mesh CreateMesh() {
        Mesh mesh = new Mesh();
        mesh.name = "Terrain Mesh";

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uv;

        if (flatShading) mesh.RecalculateNormals();
        else mesh.normals = normals;
        mesh.RecalculateBounds();

        return mesh;
    }

    public void BakeNormals() {
        if (flatShading) FlatShade();
        else SmoothShade();
    }

    private void FlatShade() {
        Vector3[] flatVertices = new Vector3[triangles.Length];
        Vector2[] flatUV = new Vector2[triangles.Length];

        for (int i = 0; i < triangles.Length; i++) {
            flatVertices[i] = vertices[triangles[i]];
            flatUV[i] = uv[triangles[i]];

            triangles[i] = i;
        }

        vertices = flatVertices;
        uv = flatUV;
    }

    private void SmoothShade() {
        normals = SmoothNormals();
    }

    private Vector3[] SmoothNormals() {
        Vector3[] vertexNormals = new Vector3[vertices.Length];

        int triangleCount = triangles.Length / 3;
        for (int i = 0; i < triangleCount; i++) {
            int normalTriangleIndex = i * 3;
            int vertexIndexA = triangles[normalTriangleIndex];
            int vertexIndexB = triangles[normalTriangleIndex + 1];
            int vertexIndexC = triangles[normalTriangleIndex + 2];

            Vector3 triangleNormal = SurfaceNormalFromIndices(vertexIndexA, vertexIndexB, vertexIndexC);
            vertexNormals[vertexIndexA] += triangleNormal;
            vertexNormals[vertexIndexB] += triangleNormal;
            vertexNormals[vertexIndexC] += triangleNormal;
        }

        int borderTriangleCount = borderTriangles.Length / 3;
        for (int i = 0; i < borderTriangleCount; i++) {
            int normalTriangleIndex = i * 3;
            int vertexIndexA = borderTriangles[normalTriangleIndex];
            int vertexIndexB = borderTriangles[normalTriangleIndex + 1];
            int vertexIndexC = borderTriangles[normalTriangleIndex + 2];

            Vector3 triangleNormal = SurfaceNormalFromIndices(vertexIndexA, vertexIndexB, vertexIndexC);
            if (vertexIndexA >= 0) vertexNormals[vertexIndexA] += triangleNormal;
            if (vertexIndexB >= 0) vertexNormals[vertexIndexB] += triangleNormal;
            if (vertexIndexC >= 0) vertexNormals[vertexIndexC] += triangleNormal;
        }

        for (int i = 0; i < vertexNormals.Length; i++) {
            vertexNormals[i].Normalize();
        }

        return vertexNormals;
    }

    #endregion

    #region Point Cloud

    private List<int> pointIndices;
    private List<Vector3> pointVertices;
    private List<Vector2> pointUV;
    private List<Vector3> pointNormals;
    private List<Color> pointColors;

    public MeshData(bool pointCloud) {
        if (pointCloud == true) {
            pointIndices = new List<int>();
            pointVertices = new List<Vector3>();
            pointUV = new List<Vector2>();
            pointNormals = new List<Vector3>();
            pointColors = new List<Color>();
        }
    }

    public void AddPoint(int vertexIndex, Vector3 vertexPosition, Vector2 uvPercent, Vector3 normalDirection, Color color) {
        pointIndices.Add(vertexIndex);
        pointVertices.Add(vertexPosition);
        pointUV.Add(uvPercent);
        pointNormals.Add(normalDirection);
        pointColors.Add(color);
    }

    public Mesh CreatePointCloud() {
        Mesh mesh = new Mesh();
        mesh.name = "Point Cloud";

        mesh.vertices = pointVertices.ToArray();
        mesh.SetIndices(pointIndices.ToArray(), MeshTopology.Points, 0);
        mesh.uv = pointUV.ToArray();
        mesh.normals = pointNormals.ToArray();
        mesh.colors = pointColors.ToArray();
        mesh.RecalculateBounds();

        return mesh;
    }

    #endregion

}

public struct TreeData {
    public readonly Vector3 position;
    public readonly Quaternion rotation;
    public readonly Vector3 scale;

    public TreeData(Vector3 worldSpacePosition, Quaternion worldSpaceRotation, Vector3 localSpaceScale) {
        position = worldSpacePosition;
        rotation = worldSpaceRotation;
        scale = localSpaceScale;
    }
}
