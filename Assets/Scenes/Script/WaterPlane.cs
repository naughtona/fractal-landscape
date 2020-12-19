using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaterPlane : MonoBehaviour
{
    Mesh mesh;
    Vector3[] vertices;
    Vector2[] uvs;
    int[] triangles;
    
    public GameObject terrain;
    public GameObject sun;

    public int waterResolution;
    private int waterPartitions;
    public float height;
    public Shader shader; 
    MeshRenderer mRenderer;
    private float waterLength;

    void Start()
    {
        mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;
        mRenderer = GetComponent<MeshRenderer>();
        waterLength = terrain.GetComponent<DiamondSquare>().terrainLength;
        mRenderer.material.shader = shader;
        GenerateMesh();
    }

    void Update()
    {
        // user can raise or lower water level
        this.gameObject.transform.position = new Vector3(0f,height,0f);

        // pass position of point light (sun)
        mRenderer.material.SetColor("_PointLightColor", sun.GetComponent<sunRotation>().color);
        mRenderer.material.SetVector("_PointLightPosition", sun.GetComponent<sunRotation>().getWorldPosition());
    }

    private void GenerateMesh() {
        waterPartitions = (int) Mathf.Pow(2, waterResolution);

        float gridSize = waterLength / waterPartitions;
        int nVertices = (waterPartitions + 1) * (waterPartitions + 1);

        // generate vertices
        vertices = new Vector3[nVertices];
        uvs = new Vector2[nVertices];

        for (int i = 0, z = 0; z <= waterPartitions; z++) {
            for (int x = 0; x <= waterPartitions; x++) {
                vertices[i] = new Vector3(x * gridSize, 0f, z * gridSize);
                uvs[i++] = new Vector2((float) z / waterPartitions, (float) x / waterPartitions);
            }
        }

        // group vertices to make triangles
        triangles = new int[waterPartitions * waterPartitions * 6];
        
        int vertexIndex = 0, triangleIndex = 0;
        for (int z = 0; z < waterPartitions; z++) {
            // build row by row
            for (int x = 0; x < waterPartitions; x++) {
                // build a square with two triangles
                triangles[triangleIndex + 0] = vertexIndex + 0;
                triangles[triangleIndex + 1] = vertexIndex + waterPartitions + 1;
                triangles[triangleIndex + 2] = vertexIndex + 1;
                triangles[triangleIndex + 3] = vertexIndex + waterPartitions + 1;
                triangles[triangleIndex + 4] = vertexIndex + waterPartitions + 2;
                triangles[triangleIndex + 5] = vertexIndex + 1;

                vertexIndex++;
                triangleIndex += 6;
            }
            vertexIndex++;
        }

        mesh.vertices = vertices;
        mesh.uv = uvs;
        mesh.triangles=triangles;

        mesh.RecalculateBounds();
        mesh.RecalculateNormals();
    }

    public void setHeight(float height) {
        this.height = height;
    }
    
}
