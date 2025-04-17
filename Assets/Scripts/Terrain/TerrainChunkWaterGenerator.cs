using UnityEngine;
using System.Collections.Generic;

public class TerrainChunkWaterGenerator : MonoBehaviour
{
    public float waterThreshold = 0f;
    public Material waterMaterial;

    private GameObject waterParent;
    private List<GameObject> waterPlanes = new List<GameObject>();

    public void Initialize(float threshold, Material material)
    {
        waterThreshold = threshold;
        waterMaterial = material;
        // Crear un parent para los planos de agua en este chunk
        waterParent = new GameObject("Water");
        waterParent.transform.SetParent(transform, false);
    }

    // Genera agua usando directamente la malla del terreno
    public void GenerateWaterForMesh()
    {
        ClearWaterPlanes();

        MeshFilter meshFilter = transform.GetComponent<MeshFilter>();
        if (meshFilter == null || meshFilter.sharedMesh == null)
            return;

        Mesh terrainMesh = meshFilter.sharedMesh;
        Vector3[] vertices = terrainMesh.vertices;
        int[] triangles = terrainMesh.triangles;

        for (int i = 0; i < triangles.Length; i += 3)
        {
            Vector3 a = vertices[triangles[i]];
            Vector3 b = vertices[triangles[i + 1]];
            Vector3 c = vertices[triangles[i + 2]];

            // Si alguno de los vértices está bajo el umbral, añadimos un triángulo de agua
            if (a.y < waterThreshold || b.y < waterThreshold || c.y < waterThreshold)
            {
                CreateWaterTriangle(a, b, c);
            }
        }
    }

    private void CreateWaterTriangle(Vector3 a, Vector3 b, Vector3 c)
    {
        GameObject waterPlane = new GameObject("WaterTriangle");
        // Colocar bajo el mismo parent y sin alterar posición/rotación
        waterPlane.transform.SetParent(waterParent.transform, false);

        MeshFilter meshFilter = waterPlane.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = waterPlane.AddComponent<MeshRenderer>();
        meshRenderer.sharedMaterial = waterMaterial;

        Mesh mesh = new Mesh();
        // Crear triángulo plano al nivel del agua
        mesh.vertices = new Vector3[]
        {
        new Vector3(a.x, waterThreshold, a.z),
        new Vector3(b.x, waterThreshold, b.z),
        new Vector3(c.x, waterThreshold, c.z)
        };
        mesh.triangles = new int[] { 0, 1, 2 };
        mesh.RecalculateNormals();

        meshFilter.sharedMesh = mesh;
        waterPlanes.Add(waterPlane);
    }

    private void ClearWaterPlanes()
    {
        for (int i = waterPlanes.Count - 1; i >= 0; i--)
        {
            if (waterPlanes[i] != null)
                DestroyImmediate(waterPlanes[i]);
        }
        waterPlanes.Clear();
    }

    public void SetVisible(bool visible)
    {
        if (waterParent != null)
            waterParent.SetActive(visible);
    }
}