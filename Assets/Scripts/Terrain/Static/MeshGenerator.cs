﻿using UnityEngine;
using System.Collections;

public static class MeshGenerator
{


    public static MeshData GenerateTerrainMesh(float[,] heightMap, MeshSettings meshSettings, int levelOfDetail, BiomeMap biomeMap)
    {

        int skipIncrement = (levelOfDetail == 0) ? 1 : levelOfDetail * 2;
        int numVertsPerLine = meshSettings.numVertsPerLine;

        Vector2 topLeft = new Vector2(-1, 1) * meshSettings.meshWorldSize / 2f;

        MeshData meshData = new MeshData(numVertsPerLine, skipIncrement, meshSettings.useFlatShading);

        int[,] vertexIndicesMap = new int[numVertsPerLine, numVertsPerLine];
        int meshVertexIndex = 0;
        int outOfMeshVertexIndex = -1;

        for (int y = 0; y < numVertsPerLine; y++)
        {
            for (int x = 0; x < numVertsPerLine; x++)
            {
                bool isOutOfMeshVertex = y == 0 || y == numVertsPerLine - 1 || x == 0 || x == numVertsPerLine - 1;
                bool isSkippedVertex = x > 2 && x < numVertsPerLine - 3 && y > 2 && y < numVertsPerLine - 3 && ((x - 2) % skipIncrement != 0 || (y - 2) % skipIncrement != 0);
                if (isOutOfMeshVertex)
                {
                    vertexIndicesMap[x, y] = outOfMeshVertexIndex;
                    outOfMeshVertexIndex--;
                }
                else if (!isSkippedVertex)
                {
                    vertexIndicesMap[x, y] = meshVertexIndex;
                    meshVertexIndex++;
                }
            }
        }

        for (int y = 0; y < numVertsPerLine; y++)
        {
            for (int x = 0; x < numVertsPerLine; x++)
            {
                bool isSkippedVertex = x > 2 && x < numVertsPerLine - 3 && y > 2 && y < numVertsPerLine - 3 && ((x - 2) % skipIncrement != 0 || (y - 2) % skipIncrement != 0);

                if (!isSkippedVertex)
                {
                    bool isOutOfMeshVertex = y == 0 || y == numVertsPerLine - 1 || x == 0 || x == numVertsPerLine - 1;
                    bool isMeshEdgeVertex = (y == 1 || y == numVertsPerLine - 2 || x == 1 || x == numVertsPerLine - 2) && !isOutOfMeshVertex;
                    bool isMainVertex = (x - 2) % skipIncrement == 0 && (y - 2) % skipIncrement == 0 && !isOutOfMeshVertex && !isMeshEdgeVertex;
                    bool isEdgeConnectionVertex = (y == 2 || y == numVertsPerLine - 3 || x == 2 || x == numVertsPerLine - 3) && !isOutOfMeshVertex && !isMeshEdgeVertex && !isMainVertex;

                    int vertexIndex = vertexIndicesMap[x, y];
                    Vector2 percent = new Vector2(x - 1, y - 1) / (numVertsPerLine - 3);
                    Vector2 vertexPosition2D = topLeft + new Vector2(percent.x, -percent.y) * meshSettings.meshWorldSize;
                    float height = heightMap[x, y];
                    
                    int biomeMapIndex = y * numVertsPerLine + x;
                    Vector4 biomeStrengths = biomeMap.biomeStrengths[biomeMapIndex];
                    Vector4 biomeIndexes = biomeMap.biomeIndexes[biomeMapIndex];


                    if (isEdgeConnectionVertex)
                    {
                        bool isVertical = x == 2 || x == numVertsPerLine - 3;
                        int dstToMainVertexA = ((isVertical) ? y - 2 : x - 2) % skipIncrement;
                        int dstToMainVertexB = skipIncrement - dstToMainVertexA;
                        float dstPercentFromAToB = dstToMainVertexA / (float)skipIncrement;

                        Coord coordA = new Coord((isVertical) ? x : x - dstToMainVertexA, (isVertical) ? y - dstToMainVertexA : y);
                        Coord coordB = new Coord((isVertical) ? x : x + dstToMainVertexB, (isVertical) ? y + dstToMainVertexB : y);

                        float heightMainVertexA = heightMap[coordA.x, coordA.y];
                        float heightMainVertexB = heightMap[coordB.x, coordB.y];

                        height = heightMainVertexA * (1 - dstPercentFromAToB) + heightMainVertexB * dstPercentFromAToB;

                        EdgeConnectionVertexData edgeConnectionVertexData = new EdgeConnectionVertexData(vertexIndex, vertexIndicesMap[coordA.x, coordA.y], vertexIndicesMap[coordB.x, coordB.y], dstPercentFromAToB);
                        meshData.DeclareEdgeConnectionVertex(edgeConnectionVertexData);
                    }

                    meshData.AddVertex(new Vector3(vertexPosition2D.x, height, vertexPosition2D.y), percent, vertexIndex, biomeStrengths, biomeIndexes);

                    bool createTriangle = x < numVertsPerLine - 1 && y < numVertsPerLine - 1 && (!isEdgeConnectionVertex || (x != 2 && y != 2));

                    if (createTriangle)
                    {
                        int currentIncrement = (isMainVertex && x != numVertsPerLine - 3 && y != numVertsPerLine - 3) ? skipIncrement : 1;

                        int a = vertexIndicesMap[x, y];
                        int b = vertexIndicesMap[x + currentIncrement, y];
                        int c = vertexIndicesMap[x, y + currentIncrement];
                        int d = vertexIndicesMap[x + currentIncrement, y + currentIncrement];
                        meshData.AddTriangle(a, d, c);
                        meshData.AddTriangle(d, a, b);
                    }
                }
            }
        }

        meshData.ProcessMesh();

        return meshData;
    }

    public struct Coord
    {
        public readonly int x;
        public readonly int y;

        public Coord(int x, int y)
        {
            this.x = x;
            this.y = y;
        }

    }
}
public class EdgeConnectionVertexData
{
    public int vertexIndex;
    public int mainVertexAIndex;
    public int mainVertexBIndex;
    public float dstPercentFromAToB;

    public EdgeConnectionVertexData(int vertexIndex, int mainVertexAIndex, int mainVertexBIndex, float dstPercentFromAToB)
    {
        this.vertexIndex = vertexIndex;
        this.mainVertexAIndex = mainVertexAIndex;
        this.mainVertexBIndex = mainVertexBIndex;
        this.dstPercentFromAToB = dstPercentFromAToB;
    }


}
public class MeshData
{
    Vector3[] vertices;
    int[] triangles;
    Vector2[] uvs;
    Vector4[] uvs2;
    Vector4[] uvs3;
    Vector3[] bakedNormals;

    Vector3[] outOfMeshVertices;
    int[] outOfMeshTriangles;

    int triangleIndex;
    int outOfMeshTriangleIndex;

    EdgeConnectionVertexData[] edgeConnectionVertices;
    int edgeConnectionVertexIndex;

    bool useFlatShading;

    public MeshData(int numVertsPerLine, int skipIncrement, bool useFlatShading)
    {
        this.useFlatShading = useFlatShading;

        int numMeshEdgeVertices = (numVertsPerLine - 2) * 4 - 4;
        int numEdgeConnectionVertices = (skipIncrement - 1) * (numVertsPerLine - 5) / skipIncrement * 4;
        int numMainVerticesPerLine = (numVertsPerLine - 5) / skipIncrement + 1;
        int numMainVertices = numMainVerticesPerLine * numMainVerticesPerLine;

        vertices = new Vector3[numMeshEdgeVertices + numEdgeConnectionVertices + numMainVertices];
        uvs = new Vector2[vertices.Length];
        uvs2 = new Vector4[vertices.Length];
        uvs3 = new Vector4[vertices.Length];
        edgeConnectionVertices = new EdgeConnectionVertexData[numEdgeConnectionVertices];

        int numMeshEdgeTriangles = 8 * (numVertsPerLine - 4);
        int numMainTriangles = (numMainVerticesPerLine - 1) * (numMainVerticesPerLine - 1) * 2;
        triangles = new int[(numMeshEdgeTriangles + numMainTriangles) * 3];

        outOfMeshVertices = new Vector3[numVertsPerLine * 4 - 4];
        outOfMeshTriangles = new int[24 * (numVertsPerLine - 2)];
    }

    public void AddVertex(Vector3 vertexPosition, Vector2 uv, int vertexIndex, Vector4 biomeStrengths, Vector4 biomeIndexes)
    {
        if (vertexIndex < 0)
        {
            outOfMeshVertices[-vertexIndex - 1] = vertexPosition;
        }
        else
        {
            vertices[vertexIndex] = vertexPosition;
            uvs[vertexIndex] = uv;
            uvs2[vertexIndex] = biomeStrengths;
            uvs3[vertexIndex] = biomeIndexes;
        }
    }

    public void AddTriangle(int a, int b, int c)
    {
        if (a < 0 || b < 0 || c < 0)
        {
            outOfMeshTriangles[outOfMeshTriangleIndex] = a;
            outOfMeshTriangles[outOfMeshTriangleIndex + 1] = b;
            outOfMeshTriangles[outOfMeshTriangleIndex + 2] = c;
            outOfMeshTriangleIndex += 3;
        }
        else
        {
            triangles[triangleIndex] = a;
            triangles[triangleIndex + 1] = b;
            triangles[triangleIndex + 2] = c;
            triangleIndex += 3;
        }
    }


    public void DeclareEdgeConnectionVertex(EdgeConnectionVertexData edgeConnectionVertexData)
    {
        edgeConnectionVertices[edgeConnectionVertexIndex] = edgeConnectionVertexData;
        edgeConnectionVertexIndex++;
    }

    Vector3[] CalculateNormals()
    {

        Vector3[] vertexNormals = new Vector3[vertices.Length];
        int triangleCount = triangles.Length / 3;
        for (int i = 0; i < triangleCount; i++)
        {
            int normalTriangleIndex = i * 3;
            int vertexIndexA = triangles[normalTriangleIndex];
            int vertexIndexB = triangles[normalTriangleIndex + 1];
            int vertexIndexC = triangles[normalTriangleIndex + 2];

            Vector3 triangleNormal = SurfaceNormalFromIndices(vertexIndexA, vertexIndexB, vertexIndexC);
            vertexNormals[vertexIndexA] += triangleNormal;
            vertexNormals[vertexIndexB] += triangleNormal;
            vertexNormals[vertexIndexC] += triangleNormal;
        }

        int borderTriangleCount = outOfMeshTriangles.Length / 3;
        for (int i = 0; i < borderTriangleCount; i++)
        {
            int normalTriangleIndex = i * 3;
            int vertexIndexA = outOfMeshTriangles[normalTriangleIndex];
            int vertexIndexB = outOfMeshTriangles[normalTriangleIndex + 1];
            int vertexIndexC = outOfMeshTriangles[normalTriangleIndex + 2];

            Vector3 triangleNormal = SurfaceNormalFromIndices(vertexIndexA, vertexIndexB, vertexIndexC);
            if (vertexIndexA >= 0)
            {
                vertexNormals[vertexIndexA] += triangleNormal;
            }
            if (vertexIndexB >= 0)
            {
                vertexNormals[vertexIndexB] += triangleNormal;
            }
            if (vertexIndexC >= 0)
            {
                vertexNormals[vertexIndexC] += triangleNormal;
            }
        }


        for (int i = 0; i < vertexNormals.Length; i++)
        {
            vertexNormals[i].Normalize();
        }

        return vertexNormals;

    }


    void ProcessEdgeConnectionVertices()
    {
        foreach (EdgeConnectionVertexData e in edgeConnectionVertices)
        {
            bakedNormals[e.vertexIndex] = bakedNormals[e.mainVertexAIndex] * (1 - e.dstPercentFromAToB) + bakedNormals[e.mainVertexBIndex] * e.dstPercentFromAToB;
        }
    }

    Vector3 SurfaceNormalFromIndices(int indexA, int indexB, int indexC)
    {
        Vector3 pointA = (indexA < 0) ? outOfMeshVertices[-indexA - 1] : vertices[indexA];
        Vector3 pointB = (indexB < 0) ? outOfMeshVertices[-indexB - 1] : vertices[indexB];
        Vector3 pointC = (indexC < 0) ? outOfMeshVertices[-indexC - 1] : vertices[indexC];

        Vector3 sideAB = pointB - pointA;
        Vector3 sideAC = pointC - pointA;
        return Vector3.Cross(sideAB, sideAC).normalized;
    }

    public void ProcessMesh()
    {
        if (useFlatShading)
        {
            FlatShading();
        }
        else
        {
            BakeNormals();
            ProcessEdgeConnectionVertices();
        }
    }

    void BakeNormals()
    {
        bakedNormals = CalculateNormals();
    }

    void FlatShading()
    {
        Vector3[] flatShadedVertices = new Vector3[triangles.Length];
        Vector2[] flatShadedUvs = new Vector2[triangles.Length];
        Vector4[] flatShadedUvs2 = new Vector4[triangles.Length];
        Vector4[] flatShadedUvs3 = new Vector4[triangles.Length];

        for (int i = 0; i < triangles.Length; i++)
        {
            flatShadedVertices[i] = vertices[triangles[i]];
            flatShadedUvs[i] = uvs[triangles[i]];
            flatShadedUvs2[i] = uvs2[triangles[i]];
            flatShadedUvs3[i] = uvs3[triangles[i]];
            triangles[i] = i;
        }

        vertices = flatShadedVertices;
        uvs = flatShadedUvs;
        uvs2 = flatShadedUvs2;
        uvs3 = flatShadedUvs3;
    }

    public Mesh CreateMesh()
    {
        Mesh mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;
        mesh.SetUVs(1, uvs2); // Use SetUVs for Vector4 arrays
        mesh.SetUVs(2, uvs3); // Use SetUVs for Vector4 arrays
        if (useFlatShading)
        {
            mesh.RecalculateNormals();
        }
        else
        {
            mesh.normals = bakedNormals;
        }
        return mesh;
    }

}