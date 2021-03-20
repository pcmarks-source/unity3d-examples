using System.Collections.Generic;
using UnityEngine;

public class MeshGenerator {

    private Mesh mesh;
    private MeshCollider meshCollider;

    private bool isPointCloud;
    private bool useUVs;
    private bool useColors;
    private bool useCollider;

    private List<Vector3> vertices;
    private List<int> triangles;
    private List<Vector3> normals;
    private List<Vector2> uvs;
    private List<Vector3> textureProperties;
    private List<Color> colors;

    private static Vector3 vertexA, vertexB, vertexC, vertexD;
    private static Vector2 uvA, uvB, uvC, uvD;

    public MeshGenerator(string objectName, Transform parentTransform, int x, int z, bool useUVs, bool useColors, bool useCollider) {
        this.isPointCloud = false;
        this.useUVs = useUVs;
        this.useColors = useColors;
        this.useCollider = useCollider;

        GameObject newObject = new GameObject(objectName);
        newObject.transform.SetParent(parentTransform, false);

        mesh = newObject.AddComponent<MeshFilter>().mesh = new Mesh();
        mesh.name = objectName + " (" + x.ToString() + ", " + z.ToString() + ")";

        MeshRenderer newRenderer = newObject.AddComponent<MeshRenderer>();

        if (objectName == "Grass") {

            this.isPointCloud = true;
            newRenderer.material = Metrics.grassMaterial;
            newRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        } else if (objectName == "Fog") {

            newRenderer.material = Metrics.fogMaterial;
            newRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        } else if (objectName == "Liquid") {

            newRenderer.material = Metrics.liquidMaterial;
            newRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        } else if (objectName == "CeilingMask") {

            newRenderer.material = Metrics.megaSurface.material;
            newRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;

        } else {

            newRenderer.material = Metrics.megaSurface.material;
            newRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;

            if (objectName == "Foundation") {
                newObject.layer = 9;
            } else if (objectName == "Structure" || objectName == "Ceiling") {
                newObject.layer = 10;
            }

        }

        meshCollider = (useCollider) ? newObject.AddComponent<MeshCollider>() : null;
    }

    public void ClearMesh() {
        mesh.Clear();
        vertices = ListPool<Vector3>.Get();
        triangles = ListPool<int>.Get();
        if (isPointCloud) normals = ListPool<Vector3>.Get();
        if (useUVs) uvs = ListPool<Vector2>.Get();
        if (useUVs) textureProperties = ListPool<Vector3>.Get();
        if (useColors) colors = ListPool<Color>.Get();
    }

    public void ApplyMesh() {
        mesh.SetVertices(vertices);
        ListPool<Vector3>.Add(vertices);

        if (isPointCloud == true) {
            mesh.SetIndices(triangles.ToArray(), MeshTopology.Points, 0);
            ListPool<int>.Add(triangles);
        } else {
            mesh.SetTriangles(triangles, 0);
            ListPool<int>.Add(triangles);
        }

        if (useUVs) {
            mesh.SetUVs(0, uvs);
            ListPool<Vector2>.Add(uvs);
        }

        if (useUVs) {
            mesh.SetUVs(1, textureProperties);
            ListPool<Vector3>.Add(textureProperties);
        }

        if (useColors) {
            mesh.SetColors(colors);
            ListPool<Color>.Add(colors);
        }

        if (isPointCloud == true) {
            mesh.SetNormals(normals);
            ListPool<Vector3>.Add(normals);

            mesh.RecalculateBounds();
        } else {
            mesh.RecalculateNormals();
        }

        if (useCollider) meshCollider.sharedMesh = mesh;
    }

    public void AddPoint(Vector3 point, Color color) {
        int vertexIndex = vertices.Count;

        vertices.Add(Metrics.Perturb(point));
        triangles.Add(vertexIndex);
        normals.Add(Vector3.up);
        colors.Add(color);
    }

    public void AddTriangle(Vector3 v1, Vector3 v2, Vector3 v3) {
        int vertexIndex = vertices.Count;

        vertices.Add(Metrics.Perturb(v1));
        vertices.Add(Metrics.Perturb(v2));
        vertices.Add(Metrics.Perturb(v3));

        triangles.Add(vertexIndex + 0);
        triangles.Add(vertexIndex + 1);
        triangles.Add(vertexIndex + 2);
    }

    public void AddTriangleUnperturbed(Vector3 v1, Vector3 v2, Vector3 v3) {
        int vertexIndex = vertices.Count;

        vertices.Add(v1);
        vertices.Add(v2);
        vertices.Add(v3);

        triangles.Add(vertexIndex + 0);
        triangles.Add(vertexIndex + 1);
        triangles.Add(vertexIndex + 2);
    }

    public void AddTriangleUV(Vector2 uv1, Vector2 uv2, Vector2 uv3) {
        uvs.Add(uv1);
        uvs.Add(uv2);
        uvs.Add(uv3);
    }

    public void AddTriangleTextureProperties(Vector3 textureProperty) {
        textureProperties.Add(textureProperty);
        textureProperties.Add(textureProperty);
        textureProperties.Add(textureProperty);
    }

    public void AddTriangleColor(Color color) {
        colors.Add(color);
        colors.Add(color);
        colors.Add(color);
    }

    public static void TriangulateGrassPoints(MeshGenerator mesh, Vector3 workingZero, int colorIndex) {
        if (mesh.isPointCloud == true) {

            Vector3 vertex = workingZero;
            mesh.AddPoint(vertex, Metrics.megaSurface.colors[colorIndex]);

            for (int i = 0; i < 8; i++) {
                vertex = workingZero + (Metrics.CornerFromDirection[i] * 0.6667f);
                mesh.AddPoint(vertex, Metrics.megaSurface.colors[colorIndex]);
            }

        } else {
            vertexA = workingZero + (Metrics.CornerFromIndex[0] * 0.5f);
            vertexB = workingZero + (Metrics.CornerFromIndex[1] * 0.5f);
            vertexC = workingZero + (Metrics.CornerFromIndex[2] * 0.5f);

            mesh.AddTriangle(vertexA, vertexB, vertexC);
            mesh.AddTriangleColor(Metrics.megaSurface.colors[colorIndex]);
        }
    }

    public static void TriangulateHorizontalQuad(MeshGenerator mesh, Vector3 workingZero, int colorIndex) {
        vertexA = workingZero + Metrics.CornerFromIndex[0];
        vertexB = workingZero + Metrics.CornerFromIndex[1];
        vertexC = workingZero + Metrics.CornerFromIndex[2];
        vertexD = workingZero + Metrics.CornerFromIndex[3];

        uvA = new Vector2(1, 1);
        uvB = new Vector2(1, 0);
        uvC = new Vector2(0, 0);
        uvD = new Vector2(0, 1);

        mesh.AddTriangle(vertexA, vertexB, vertexC);
        mesh.AddTriangle(vertexC, vertexD, vertexA);

        mesh.AddTriangleUV(uvA, uvB, uvC);
        mesh.AddTriangleUV(uvC, uvD, uvA);

        mesh.AddTriangleColor(Metrics.megaSurface.colors[colorIndex]);
        mesh.AddTriangleColor(Metrics.megaSurface.colors[colorIndex]);
    }

    public static void TriangulateHorizontalQuadFlat(MeshGenerator mesh, Vector3 workingZero, int colorIndex) {
        vertexA = workingZero + Metrics.CornerFromIndex[0];
        vertexB = workingZero + Metrics.CornerFromIndex[1];
        vertexC = workingZero + Metrics.CornerFromIndex[2];
        vertexD = workingZero + Metrics.CornerFromIndex[3];

        uvA = new Vector2(1, 1);
        uvB = new Vector2(1, 0);
        uvC = new Vector2(0, 0);
        uvD = new Vector2(0, 1);

        mesh.AddTriangleUnperturbed(Metrics.PerturbFlat(vertexA), Metrics.PerturbFlat(vertexB), Metrics.PerturbFlat(vertexC));
        mesh.AddTriangleUnperturbed(Metrics.PerturbFlat(vertexC), Metrics.PerturbFlat(vertexD), Metrics.PerturbFlat(vertexA));

        mesh.AddTriangleUV(uvA, uvB, uvC);
        mesh.AddTriangleUV(uvC, uvD, uvA);

        mesh.AddTriangleColor(Metrics.megaSurface.colors[colorIndex]);
        mesh.AddTriangleColor(Metrics.megaSurface.colors[colorIndex]);
    }

    public static void TriangulateHorizontalMegaQuad(MeshGenerator mesh, Vector3 workingZero, int materialIndex, int colorIndex, bool flat = false) {
        vertexA = workingZero + Metrics.CornerFromIndex[0];
        vertexB = workingZero + Metrics.CornerFromIndex[1];
        vertexC = workingZero + Metrics.CornerFromIndex[2];
        vertexD = workingZero + Metrics.CornerFromIndex[3];

        uvA = new Vector2(1, 1);
        uvB = new Vector2(1, 0);
        uvC = new Vector2(0, 0);
        uvD = new Vector2(0, 1);

        if (flat == true) {
            mesh.AddTriangleUnperturbed(Metrics.PerturbFlat(vertexA), Metrics.PerturbFlat(vertexB), Metrics.PerturbFlat(vertexC));
            mesh.AddTriangleUnperturbed(Metrics.PerturbFlat(vertexC), Metrics.PerturbFlat(vertexD), Metrics.PerturbFlat(vertexA));
        } else {
            mesh.AddTriangle(vertexA, vertexB, vertexC);
            mesh.AddTriangle(vertexC, vertexD, vertexA);
        }
        

        mesh.AddTriangleUV(uvA, uvB, uvC);
        mesh.AddTriangleUV(uvC, uvD, uvA);

        mesh.AddTriangleTextureProperties(Metrics.megaSurface.properties[materialIndex]);
        mesh.AddTriangleTextureProperties(Metrics.megaSurface.properties[materialIndex]);

        mesh.AddTriangleColor(Metrics.megaSurface.colors[colorIndex]);
        mesh.AddTriangleColor(Metrics.megaSurface.colors[colorIndex]);
    }

    public static void TriangulateReverseHorizontalMegaQuad(MeshGenerator mesh, Vector3 workingZero, int materialIndex, int colorIndex) {
        vertexA = workingZero + Metrics.CornerFromIndex[0];
        vertexB = workingZero + Metrics.CornerFromIndex[1];
        vertexC = workingZero + Metrics.CornerFromIndex[2];
        vertexD = workingZero + Metrics.CornerFromIndex[3];

        uvA = new Vector2(1, 1);
        uvB = new Vector2(1, 0);
        uvC = new Vector2(0, 0);
        uvD = new Vector2(0, 1);

        mesh.AddTriangle(vertexA, vertexD, vertexC);
        mesh.AddTriangle(vertexC, vertexB, vertexA);

        mesh.AddTriangleUV(uvA, uvD, uvC);
        mesh.AddTriangleUV(uvC, uvB, uvA);

        mesh.AddTriangleTextureProperties(Metrics.megaSurface.properties[materialIndex]);
        mesh.AddTriangleTextureProperties(Metrics.megaSurface.properties[materialIndex]);

        mesh.AddTriangleColor(Metrics.megaSurface.colors[colorIndex]);
        mesh.AddTriangleColor(Metrics.megaSurface.colors[colorIndex]);
    }

    public static void TriangulateVerticalMegaQuad(MeshGenerator mesh, Vector3 workingZero, float height, float offset, int materialIndex, int colorIndex, int cornerIndexA, int cornerIndexB) {
        vertexA = (workingZero + Metrics.CornerFromIndex[cornerIndexA]) + (Vector3.up * offset);
        vertexB = (workingZero + Metrics.CornerFromIndex[cornerIndexB]) + (Vector3.up * offset);
        vertexC = new Vector3(vertexA.x, height, vertexA.z) + (Vector3.up * offset);
        vertexD = new Vector3(vertexB.x, height, vertexB.z) + (Vector3.up * offset);

        uvA = new Vector2(1, height);
        uvB = new Vector2(0, height);
        uvC = new Vector2(1, 0);
        uvD = new Vector2(0, 0);

        mesh.AddTriangle(vertexC, vertexB, vertexA);
        mesh.AddTriangle(vertexD, vertexB, vertexC);

        mesh.AddTriangleUV(uvC, uvB, uvA);
        mesh.AddTriangleUV(uvD, uvB, uvC);

        mesh.AddTriangleTextureProperties(Metrics.megaSurface.properties[materialIndex]);
        mesh.AddTriangleTextureProperties(Metrics.megaSurface.properties[materialIndex]);

        mesh.AddTriangleColor(Metrics.megaSurface.colors[colorIndex]);
        mesh.AddTriangleColor(Metrics.megaSurface.colors[colorIndex]);
    }

    public static void TriangulateReverseVerticalMegaQuad(MeshGenerator mesh, Vector3 workingZero, float height, float offset, int materialIndex, int colorIndex, int cornerIndexA, int cornerIndexB) {
        vertexA = (workingZero + Metrics.CornerFromIndex[cornerIndexA]) + (Vector3.up * offset);
        vertexB = (workingZero + Metrics.CornerFromIndex[cornerIndexB]) + (Vector3.up * offset);
        vertexC = new Vector3(vertexA.x, height, vertexA.z) + (Vector3.up * offset);
        vertexD = new Vector3(vertexB.x, height, vertexB.z) + (Vector3.up * offset);

        uvA = new Vector2(1, height - offset);
        uvB = new Vector2(0, height - offset);
        uvC = new Vector2(1, 0);
        uvD = new Vector2(0, 0);

        mesh.AddTriangle(vertexC, vertexB, vertexD);
        mesh.AddTriangle(vertexA, vertexB, vertexC);

        mesh.AddTriangleUV(uvC, uvB, uvD);
        mesh.AddTriangleUV(uvA, uvB, uvC);

        mesh.AddTriangleTextureProperties(Metrics.megaSurface.properties[materialIndex]);
        mesh.AddTriangleTextureProperties(Metrics.megaSurface.properties[materialIndex]);

        mesh.AddTriangleColor(Metrics.megaSurface.colors[colorIndex]);
        mesh.AddTriangleColor(Metrics.megaSurface.colors[colorIndex]);
    }

    public static void TriangulateGrateMegaQuad(MeshGenerator mesh, Vector3 workingZero, int materialIndex, int colorIndex, int cornerIndexA, int cornerIndexB) {
        float horizontalScale = 0.5f;
        float verticalScale = 0.5f;
        float height = Metrics.trimHeight * verticalScale;
        float offset = -height;

        #region TriangulateHorizontalMegaQuad

        vertexA = (workingZero + Metrics.CornerFromIndex[cornerIndexA]);
        vertexB = (workingZero + (Metrics.CornerFromIndex[cornerIndexA] * horizontalScale));
        vertexC = (workingZero + (Metrics.CornerFromIndex[cornerIndexB] * horizontalScale));
        vertexD = (workingZero + Metrics.CornerFromIndex[cornerIndexB]);

        uvA = new Vector2(1, 1);
        uvB = new Vector2(0.75f, 0.25f);
        uvC = new Vector2(0.25f, 0.25f);
        uvD = new Vector2(0, 1);

        mesh.AddTriangle(vertexA, vertexB, vertexC);
        mesh.AddTriangle(vertexC, vertexD, vertexA);

        mesh.AddTriangleUV(uvA, uvB, uvC);
        mesh.AddTriangleUV(uvC, uvD, uvA);

        mesh.AddTriangleTextureProperties(Metrics.megaSurface.properties[materialIndex]);
        mesh.AddTriangleTextureProperties(Metrics.megaSurface.properties[materialIndex]);

        mesh.AddTriangleColor(Metrics.megaSurface.colors[colorIndex]);
        mesh.AddTriangleColor(Metrics.megaSurface.colors[colorIndex]);

        #endregion

        #region TriangulateReverseVerticalMegaQuad

        vertexA = (workingZero + (Metrics.CornerFromIndex[cornerIndexA] * horizontalScale)) + (Vector3.up * offset);
        vertexB = (workingZero + (Metrics.CornerFromIndex[cornerIndexB] * horizontalScale)) + (Vector3.up * offset);
        vertexC = new Vector3(vertexA.x, height, vertexA.z) + (Vector3.up * offset);
        vertexD = new Vector3(vertexB.x, height, vertexB.z) + (Vector3.up * offset);

        uvA = new Vector2(1, height - offset);
        uvB = new Vector2(verticalScale, height - offset);
        uvC = new Vector2(1, verticalScale);
        uvD = new Vector2(verticalScale, verticalScale);

        mesh.AddTriangle(vertexC, vertexB, vertexD);
        mesh.AddTriangle(vertexA, vertexB, vertexC);

        mesh.AddTriangleUV(uvC, uvB, uvD);
        mesh.AddTriangleUV(uvA, uvB, uvC);

        mesh.AddTriangleTextureProperties(Metrics.megaSurface.properties[materialIndex]);
        mesh.AddTriangleTextureProperties(Metrics.megaSurface.properties[materialIndex]);

        mesh.AddTriangleColor(Metrics.megaSurface.colors[colorIndex]);
        mesh.AddTriangleColor(Metrics.megaSurface.colors[colorIndex]);

        #endregion
    }

    public static void TriangulateBarMegaQuad(MeshGenerator mesh, Vector3 workingZero, float height, float offset, int cornerIndexA, int cornerIndexB) {
        offset -= 0.1f;
        float scale = 0.25f;
        vertexA = (workingZero + (Metrics.CornerFromIndex[cornerIndexA] * scale)) + (Vector3.up * offset);
        vertexB = (workingZero + (Metrics.CornerFromIndex[cornerIndexB] * scale)) + (Vector3.up * offset);
        vertexC = new Vector3(vertexA.x, height, vertexA.z) + (Vector3.up * offset);
        vertexD = new Vector3(vertexB.x, height, vertexB.z) + (Vector3.up * offset);

        uvA = new Vector2(0.5f + (scale / 2f), height);
        uvB = new Vector2(0.5f - (scale / 2f), height);
        uvC = new Vector2(0.5f + (scale / 2f), 0.5f - (scale / 2f));
        uvD = new Vector2(0.5f - (scale / 2f), 0.5f - (scale / 2f));

        mesh.AddTriangle(vertexC, vertexB, vertexA);
        mesh.AddTriangle(vertexD, vertexB, vertexC);

        mesh.AddTriangleUV(uvC, uvB, uvA);
        mesh.AddTriangleUV(uvD, uvB, uvC);

        mesh.AddTriangleTextureProperties(Metrics.megaSurface.properties[(int)MatI.Boards]);
        mesh.AddTriangleTextureProperties(Metrics.megaSurface.properties[(int)MatI.Boards]);

        mesh.AddTriangleColor(Metrics.megaSurface.colors[(int)ColI.Black]);
        mesh.AddTriangleColor(Metrics.megaSurface.colors[(int)ColI.Black]);
    }
}
