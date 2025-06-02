using UnityEngine;

public class GrassPlane : MonoBehaviour
{
    [Range(10, 200)]
    public int resolution = 50;

    void Start()
    {
        GenerateGrassPlane();
    }

    void GenerateGrassPlane()
    {
        Mesh mesh = new Mesh();

        int vertCount = (resolution + 1) * (resolution + 1);
        Vector3[] vertices = new Vector3[vertCount];
        Vector2[] uvs = new Vector2[vertCount];
        int[] triangles = new int[resolution * resolution * 6];

        // Generar vértices
        for (int y = 0; y <= resolution; y++)
        {
            for (int x = 0; x <= resolution; x++)
            {
                int i = y * (resolution + 1) + x;
                vertices[i] = new Vector3(x, 0, y);
                uvs[i] = new Vector2((float)x / resolution, (float)y / resolution);
            }
        }

        // Generar triángulos
        int t = 0;
        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                int i = y * (resolution + 1) + x;

                triangles[t] = i;
                triangles[t + 1] = i + resolution + 1;
                triangles[t + 2] = i + 1;

                triangles[t + 3] = i + 1;
                triangles[t + 4] = i + resolution + 1;
                triangles[t + 5] = i + resolution + 2;

                t += 6;
            }
        }

        mesh.vertices = vertices;
        mesh.uv = uvs;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();

        GetComponent<MeshFilter>().mesh = mesh;
    }
}