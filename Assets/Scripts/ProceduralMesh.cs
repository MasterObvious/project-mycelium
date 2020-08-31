using Assets.Scripts;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;


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
        Dictionary<Vector3, int> vertexMap = new Dictionary<Vector3, int>();

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
            Vector3[] vertexPositions = new Vector3[12];

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

            

            for (int i = 0; MarchingCubeTables.triTable[cubeIndex, i] != -1; i++)
            {
                Vector3 v = vertexPositions[MarchingCubeTables.triTable[cubeIndex, i]];
                
                if (false)
                {
                    triangles.Add(vertexMap[v]);
                }
                else
                {
                    vertices.Add(v);
                    triangles.Add(vertices.Count - 1);
                    //vertexMap.Add(v, vertices.Count - 1);
                }
            }

        }


        mesh.Clear();
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();

        AutoWeld(mesh, 0.0001f, .03f);
    }

    private void AutoWeld(Mesh mesh, float threshold, float bucketStep)
    {
        Vector3[] oldVertices = mesh.vertices;
        Vector3[] newVertices = new Vector3[oldVertices.Length];
        int[] old2new = new int[oldVertices.Length];
        int newSize = 0;

        // Find AABB
        Vector3 min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
        Vector3 max = new Vector3(float.MinValue, float.MinValue, float.MinValue);
        for (int i = 0; i < oldVertices.Length; i++)
        {
            if (oldVertices[i].x < min.x) min.x = oldVertices[i].x;
            if (oldVertices[i].y < min.y) min.y = oldVertices[i].y;
            if (oldVertices[i].z < min.z) min.z = oldVertices[i].z;
            if (oldVertices[i].x > max.x) max.x = oldVertices[i].x;
            if (oldVertices[i].y > max.y) max.y = oldVertices[i].y;
            if (oldVertices[i].z > max.z) max.z = oldVertices[i].z;
        }

        // Make cubic buckets, each with dimensions "bucketStep"
        int bucketSizeX = Mathf.FloorToInt((max.x - min.x) / bucketStep) + 1;
        int bucketSizeY = Mathf.FloorToInt((max.y - min.y) / bucketStep) + 1;
        int bucketSizeZ = Mathf.FloorToInt((max.z - min.z) / bucketStep) + 1;
        List<int>[,,] buckets = new List<int>[bucketSizeX, bucketSizeY, bucketSizeZ];

        // Make new vertices
        for (int i = 0; i < oldVertices.Length; i++)
        {
            // Determine which bucket it belongs to
            int x = Mathf.FloorToInt((oldVertices[i].x - min.x) / bucketStep);
            int y = Mathf.FloorToInt((oldVertices[i].y - min.y) / bucketStep);
            int z = Mathf.FloorToInt((oldVertices[i].z - min.z) / bucketStep);

            // Check to see if it's already been added
            if (buckets[x, y, z] == null)
                buckets[x, y, z] = new List<int>(); // Make buckets lazily

            for (int j = 0; j < buckets[x, y, z].Count; j++)
            {
                Vector3 to = newVertices[buckets[x, y, z][j]] - oldVertices[i];
                if (Vector3.SqrMagnitude(to) < threshold)
                {
                    old2new[i] = buckets[x, y, z][j];
                    goto skip; // Skip to next old vertex if this one is already there
                }
            }

            // Add new vertex
            newVertices[newSize] = oldVertices[i];
            buckets[x, y, z].Add(newSize);
            old2new[i] = newSize;
            newSize++;

        skip:;
        }

        // Make new triangles
        int[] oldTris = mesh.triangles;
        int[] newTris = new int[oldTris.Length];
        for (int i = 0; i < oldTris.Length; i++)
        {
            newTris[i] = old2new[oldTris[i]];
        }

        Vector3[] finalVertices = new Vector3[newSize];
        for (int i = 0; i < newSize; i++)
            finalVertices[i] = newVertices[i];

        mesh.Clear();
        mesh.vertices = finalVertices;
        mesh.triangles = newTris;
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

    private Vector3 GetEdgePoint(float level, Vector3 A, Vector3 B, float A_val, float B_val)
    {
        
        if (Mathf.Abs(level - A_val) < 0.00001)
        {
            return A;
        }

        if (Mathf.Abs(level - B_val) < 0.00001)
        {
            return B;
        }

        if (Mathf.Abs(A_val - B_val) < 0.00001)
        {
            return A;
        }

        float mu = (level - A_val) / (B_val - A_val);

        float x = A.x + mu * (B.x - A.x);
        float y = A.y + mu * (B.y - A.y);
        float z = A.z + mu * (B.z - A.z);

        return new Vector3(x, y, z);

    }
}
