using UnityEngine;
using System.Collections.Generic;

public class TerrainChunkWaterGenerator : MonoBehaviour
{
    public float waterThreshold = 0f;
    public Material waterMaterial;
    private GameObject waterObject;

    public void Initialize(float threshold, Material material)
    {
        waterThreshold = threshold;
        waterMaterial = material;

        // Create a single water object for the entire chunk
        waterObject = new GameObject("Water");
        waterObject.transform.SetParent(transform, false);
        waterObject.AddComponent<MeshFilter>();
        waterObject.AddComponent<MeshRenderer>().sharedMaterial = waterMaterial;
    }

    // Generate water using the terrain mesh
    public void GenerateWaterForMesh()
    {
        MeshFilter meshFilter = transform.GetComponent<MeshFilter>();
        if (meshFilter == null || meshFilter.sharedMesh == null)
            return;

        Mesh terrainMesh = meshFilter.sharedMesh;
        Vector3[] terrainVertices = terrainMesh.vertices;
        int[] terrainTriangles = terrainMesh.triangles;

        // Lists to build our water mesh
        List<Vector3> waterVertices = new List<Vector3>();
        List<int> waterTriangles = new List<int>();
        List<Vector2> waterUVs = new List<Vector2>();

        // Check each terrain triangle
        for (int i = 0; i < terrainTriangles.Length; i += 3)
        {
            Vector3 a = terrainVertices[terrainTriangles[i]];
            Vector3 b = terrainVertices[terrainTriangles[i + 1]];
            Vector3 c = terrainVertices[terrainTriangles[i + 2]];

            // If any vertex is below the water threshold, add a water triangle
            if (a.y < waterThreshold || b.y < waterThreshold || c.y < waterThreshold)
            {
                // Set all vertices to water level
                a.y = waterThreshold;
                b.y = waterThreshold;
                c.y = waterThreshold;

                // Add vertices
                int vertexIndex = waterVertices.Count;
                waterVertices.Add(a);
                waterVertices.Add(b);
                waterVertices.Add(c);

                // Add triangle
                waterTriangles.Add(vertexIndex);
                waterTriangles.Add(vertexIndex + 1);
                waterTriangles.Add(vertexIndex + 2);

                // Generate simple UVs
                waterUVs.Add(new Vector2(a.x, a.z) * 0.1f); // Scale UVs for texture tiling
                waterUVs.Add(new Vector2(b.x, b.z) * 0.1f);
                waterUVs.Add(new Vector2(c.x, c.z) * 0.1f);
            }
        }

        // Create the water mesh if we have triangles
        if (waterTriangles.Count > 0)
        {
            Mesh waterMesh = new Mesh();
            waterMesh.vertices = waterVertices.ToArray();
            waterMesh.triangles = waterTriangles.ToArray();
            waterMesh.uv = waterUVs.ToArray();
            waterMesh.RecalculateNormals();

            // Apply the mesh
            MeshFilter waterMeshFilter = waterObject.GetComponent<MeshFilter>();
            waterMeshFilter.sharedMesh = waterMesh;

            // Enable the water object
            waterObject.SetActive(true);
        }
        else
        {
            // No water in this chunk
            waterObject.SetActive(false);
        }
    }

    public void SetVisible(bool visible)
    {
        if (waterObject != null)
            waterObject.SetActive(visible);
    }
}