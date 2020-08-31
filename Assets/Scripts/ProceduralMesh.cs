using Assets.Scripts;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;


struct EdgePointWithGridPoint
{
    public Vector3 edgePoint;
    public Vector3 gridPoint;

    public EdgePointWithGridPoint(Vector3 e, Vector3 g)
    {
        edgePoint = e;
        gridPoint = g;
    }
}


[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class ProceduralMesh : MonoBehaviour
{
    private Mesh mesh;
    public Vector3 meshSize = new Vector3(5f, 5f, 5f);
    [Min(0.01f)]
    public float cubeSize = 1f;

    private List<Vector3> cubePositions = new List<Vector3>();
    private List<Vector3> vertices;
    private List<int> triangles;

    void Awake()
    {
        mesh = GetComponent<MeshFilter>().mesh;
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
    }

    // Start is called before the first frame update
    void Start()
    {
        InitializeCubePositions();
        CreateMesh();
    }

    private void InitializeCubePositions()
    {
        cubePositions = new List<Vector3>();
        Vector3 boundsOffset = meshSize / 2;


        for (float x = -boundsOffset.x; x <= boundsOffset.x - cubeSize; x += cubeSize)
        {
            for (float y = -boundsOffset.y; y <= boundsOffset.y - cubeSize; y += cubeSize)
            {
                for (float z = -boundsOffset.z; z <= boundsOffset.z - cubeSize; z += cubeSize)
                {
                    cubePositions.Add(new Vector3(x, y, z));
                }
            }
        }
    }

    private void CreateMesh()
    {
        float surfaceLevel = 0;
        vertices = new List<Vector3>();
        triangles = new List<int>();

        Dictionary<Vector3, int> vertexToIndex = new Dictionary<Vector3, int>();
        Dictionary<Vector3, List<Vector3>> vertexToSatellites = new Dictionary<Vector3, List<Vector3>>();

        foreach (Vector3 p in cubePositions)
        {
            int cubeIndex = 0;

            Vector3[] cubePoints = GetCubePoints(p);
            float[] surfaceLevels = new float[8];

            for (int i = 0; i < 8; i++)
            {
                surfaceLevels[i] = Surface(cubePoints[i]);
            }

            if (surfaceLevels[0] < surfaceLevel) cubeIndex |= 1;
            if (surfaceLevels[1] < surfaceLevel) cubeIndex |= 2;
            if (surfaceLevels[2] < surfaceLevel) cubeIndex |= 4;
            if (surfaceLevels[3] < surfaceLevel) cubeIndex |= 8;
            if (surfaceLevels[4] < surfaceLevel) cubeIndex |= 16;
            if (surfaceLevels[5] < surfaceLevel) cubeIndex |= 32;
            if (surfaceLevels[6] < surfaceLevel) cubeIndex |= 64;
            if (surfaceLevels[7] < surfaceLevel) cubeIndex |= 128;

            int edgeInfo = MarchingCubeTables.edgeTable[cubeIndex];
            EdgePointWithGridPoint[] vertexPositions = new EdgePointWithGridPoint[12];

            if (edgeInfo == 0) continue;

            if ((edgeInfo & 1) != 0)
            {
                vertexPositions[0] = (GetEdgePoint(surfaceLevel, cubePoints[0], cubePoints[1], surfaceLevels[0], surfaceLevels[1]));
            }
            if ((edgeInfo & 2) != 0)
            {
                vertexPositions[1] = (GetEdgePoint(surfaceLevel, cubePoints[1], cubePoints[2], surfaceLevels[1], surfaceLevels[2]));
            }
            if ((edgeInfo & 4) != 0)
            {
                vertexPositions[2] = (GetEdgePoint(surfaceLevel, cubePoints[2], cubePoints[3], surfaceLevels[2], surfaceLevels[3]));
            }
            if ((edgeInfo & 8) != 0)
            {
                vertexPositions[3] = (GetEdgePoint(surfaceLevel, cubePoints[3], cubePoints[0], surfaceLevels[3], surfaceLevels[0]));
            }
            if ((edgeInfo & 16) != 0)
            {
                vertexPositions[4] = (GetEdgePoint(surfaceLevel, cubePoints[4], cubePoints[5], surfaceLevels[4], surfaceLevels[5]));
            }
            if ((edgeInfo & 32) != 0)
            {
                vertexPositions[5] = (GetEdgePoint(surfaceLevel, cubePoints[5], cubePoints[6], surfaceLevels[5], surfaceLevels[6]));

            }
            if ((edgeInfo & 64) != 0)
            {
                vertexPositions[6] = (GetEdgePoint(surfaceLevel, cubePoints[6], cubePoints[7], surfaceLevels[6], surfaceLevels[7]));
            }
            if ((edgeInfo & 128) != 0)
            {
                vertexPositions[7] = (GetEdgePoint(surfaceLevel, cubePoints[7], cubePoints[4], surfaceLevels[7], surfaceLevels[4]));
            }
            if ((edgeInfo & 256) != 0)
            {
                vertexPositions[8] = (GetEdgePoint(surfaceLevel, cubePoints[0], cubePoints[4], surfaceLevels[0], surfaceLevels[4]));

            }
            if ((edgeInfo & 512) != 0)
            {
                vertexPositions[9] = (GetEdgePoint(surfaceLevel, cubePoints[1], cubePoints[5], surfaceLevels[1], surfaceLevels[5]));

            }
            if ((edgeInfo & 1024) != 0)
            {
                vertexPositions[10] = (GetEdgePoint(surfaceLevel, cubePoints[2], cubePoints[6], surfaceLevels[2], surfaceLevels[6]));

            }
            if ((edgeInfo & 2048) != 0)
            {
                vertexPositions[11] = (GetEdgePoint(surfaceLevel, cubePoints[3], cubePoints[7], surfaceLevels[3], surfaceLevels[7]));
            }



            for (int i = 0; MarchingCubeTables.triTable[cubeIndex, i] != -1; i += 3)
            {
                EdgePointWithGridPoint v1 = vertexPositions[MarchingCubeTables.triTable[cubeIndex, i]];
                EdgePointWithGridPoint v2 = vertexPositions[MarchingCubeTables.triTable[cubeIndex, i + 1]];
                EdgePointWithGridPoint v3 = vertexPositions[MarchingCubeTables.triTable[cubeIndex, i + 2]];

                if (!v1.gridPoint.Equals(v2.gridPoint) && !v1.gridPoint.Equals(v3.gridPoint) && !v2.gridPoint.Equals(v3.gridPoint))
                {
                    if (vertexToIndex.ContainsKey(v1.gridPoint))
                    {
                        triangles.Add(vertexToIndex[v1.gridPoint]);
                        vertexToSatellites[v1.gridPoint].Add(v1.edgePoint);
                    }
                    else
                    {
                        vertices.Add(v1.gridPoint);
                        triangles.Add(vertices.Count - 1);
                        vertexToIndex.Add(v1.gridPoint, vertices.Count - 1);
                        vertexToSatellites.Add(v1.gridPoint, new List<Vector3> { v1.edgePoint });
                    }

                    if (vertexToIndex.ContainsKey(v2.gridPoint))
                    {
                        triangles.Add(vertexToIndex[v2.gridPoint]);
                        vertexToSatellites[v2.gridPoint].Add(v2.edgePoint);
                    }
                    else
                    {
                        vertices.Add(v2.gridPoint);
                        triangles.Add(vertices.Count - 1);
                        vertexToIndex.Add(v2.gridPoint, vertices.Count - 1);
                        vertexToSatellites.Add(v2.gridPoint, new List<Vector3> { v2.edgePoint });
                    }

                    if (vertexToIndex.ContainsKey(v3.gridPoint))
                    {
                        triangles.Add(vertexToIndex[v3.gridPoint]);
                        vertexToSatellites[v3.gridPoint].Add(v3.edgePoint);
                    }
                    else
                    {
                        vertices.Add(v3.gridPoint);
                        triangles.Add(vertices.Count - 1);
                        vertexToIndex.Add(v3.gridPoint, vertices.Count - 1);
                        vertexToSatellites.Add(v3.gridPoint, new List<Vector3> { v3.edgePoint });
                    }
                }
            }

        }

        for (int i = 0; i < vertices.Count; i++)
        {
            Vector3 v = vertices[i];
            List<Vector3> edgePoints = vertexToSatellites[v];
            Vector3 newV = new Vector3();

            foreach (Vector3 s in edgePoints)
            {
                newV += s;
            }

            vertices[i] = newV / edgePoints.Count;
        }


        mesh.Clear();
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals();
        mesh.Optimize();
    }

    private float Surface(Vector3 p)
    {
        return Mathf.Sin(p.x * p.z) - p.y;
    }

    private Vector3[] GetCubePoints(Vector3 center)
    {
        Vector3[] result = new Vector3[8];

        Vector3 initialPoint = center;
        result[0] = initialPoint;

        result[1] = initialPoint + cubeSize * Vector3.right;

        result[2] = initialPoint + cubeSize * (Vector3.forward + Vector3.right);

        result[3] = initialPoint + cubeSize * (Vector3.forward);

        result[4] = initialPoint + cubeSize * Vector3.up;

        result[5] = initialPoint + cubeSize * (Vector3.right + Vector3.up);

        result[6] = initialPoint + cubeSize * (Vector3.right + Vector3.up + Vector3.forward);

        result[7] = initialPoint + cubeSize * (Vector3.up + Vector3.forward);

        return result;
    }

    private EdgePointWithGridPoint GetEdgePoint(float level, Vector3 A, Vector3 B, float A_val, float B_val)
    {
        
        if (Mathf.Abs(level - A_val) < 0.00001)
        {
            return new EdgePointWithGridPoint ( A, A );
        }

        if (Mathf.Abs(level - B_val) < 0.00001)
        {
            return new EdgePointWithGridPoint(B, B);
        }

        if (Mathf.Abs(A_val - B_val) < 0.00001)
        {
            return new EdgePointWithGridPoint(A, A);
        }

        float mu = (level - A_val) / (B_val - A_val);

        float x = A.x + mu * (B.x - A.x);
        float y = A.y + mu * (B.y - A.y);
        float z = A.z + mu * (B.z - A.z);

        Vector3 edgePoint = new Vector3(x, y, z);

        if (Vector3.Distance(edgePoint, A) < Vector3.Distance(edgePoint, B))
        {
            return new EdgePointWithGridPoint(edgePoint, A);
        }
        else
        {
            return new EdgePointWithGridPoint(edgePoint, B);
        }

    }
}
