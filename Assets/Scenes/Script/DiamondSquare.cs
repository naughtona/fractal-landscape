using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DiamondSquare : MonoBehaviour
{
    Mesh mesh;
    Vector3[] vertices;
    Vector2[] uvs;
    int[] triangles;

    public GameObject water;
    public GameObject sun;
    public GameObject flyingCamera;

    public int terrainResolution;
    private int terrainPartitions;
    public float terrainLength;
    public float maxHeight;
    public float heightDecayRate;

    private float waterHeight;
    private MeshFilter mFilter;
    private MeshRenderer mRenderer;

    private float minMeshHeight;
    private float maxMeshHeight;

    void Start()
    {
        Application.targetFrameRate = 60; // constant stable frame rate
        int seed = 50;
        Random.InitState(seed);

        mesh = new Mesh();
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mFilter = GetComponent<MeshFilter>();
        mRenderer = GetComponent<MeshRenderer>();
        waterHeight = water.GetComponent<WaterPlane>().height;
        GenerateMesh();
        flyingCamera.GetComponent<flyingcamera>().GetVerticesAndResetCamera();
    }

    void Update() {
        // regenerate mesh when user presses the space bar
        if (Input.GetKey(KeyCode.Space)) {
            GenerateMesh();

            // send the terrain to the camera
            flyingCamera.GetComponent<flyingcamera>().GetVerticesAndResetCamera();
        }

        // send sun's position and color to the shader
        mRenderer.material.SetColor("_PointLightColor", sun.GetComponent<sunRotation>().color);
        mRenderer.material.SetVector("_PointLightPosition", sun.GetComponent<sunRotation>().getWorldPosition());
    }

    void GenerateMesh() {
        terrainPartitions = (int) Mathf.Pow(2, terrainResolution);

        float gridSize = terrainLength / terrainPartitions;
        int nVertices = (terrainPartitions + 1) * (terrainPartitions + 1);

        // generate vertices
        vertices = new Vector3[nVertices];
        uvs = new Vector2[nVertices];

        for (int i = 0, z = 0; z <= terrainPartitions; z++) {
            for (int x = 0; x <= terrainPartitions; x++) {
                vertices[i] = new Vector3(x * gridSize, 0f, z * gridSize);
                uvs[i++] = new Vector2( (float) z / terrainPartitions, (float) x / terrainPartitions);
            }
        }

        // group vertices to make triangles
        triangles = new int[terrainPartitions * terrainPartitions * 6];
        
        int vertexIndex = 0, triangleIndex = 0;
        for (int z = 0; z < terrainPartitions; z++) {
            // build row by row
            for (int x = 0; x < terrainPartitions; x++) {
                // build a square with two triangles
                triangles[triangleIndex + 0] = vertexIndex + 0;
                triangles[triangleIndex + 1] = vertexIndex + terrainPartitions + 1;
                triangles[triangleIndex + 2] = vertexIndex + 1;
                triangles[triangleIndex + 3] = vertexIndex + terrainPartitions + 1;
                triangles[triangleIndex + 4] = vertexIndex + terrainPartitions + 2;
                triangles[triangleIndex + 5] = vertexIndex + 1;

                vertexIndex++;
                triangleIndex += 6;
            }
            vertexIndex++;
        }

        // initialise the four corner heights
        vertices[0].y = Random.Range(-maxHeight, maxHeight);
        vertices[terrainPartitions].y = Random.Range(-maxHeight, maxHeight);
        vertices[nVertices - (terrainPartitions + 1)].y = Random.Range(-maxHeight, maxHeight);
        vertices[nVertices - 1].y = Random.Range(-maxHeight, maxHeight);

        int steps = (int) Mathf.Log(terrainPartitions, 2);
        int numSquares = 1;
        int squareSize = terrainPartitions;
        float height = maxHeight;

        // loop through n iterations of diamond square
        for (int row, i = 0; i < steps; i++) {
            // diamonds must preceed squares, calculation is dependent

            // diamond steps
            row = 0;
            for (int j = 0; j < numSquares; j++) {
                int column = 0;
                for (int k = 0; k < numSquares; k++) {
                    diamond(row, column, squareSize, height);
                    column += squareSize;
                }
                row += squareSize;
            }

            // square steps
            row = 0;
            for (int j = 0; j < numSquares; j++) {
                int column = 0;
                for (int k = 0; k < numSquares; k++) {
                    square(row, column, squareSize, height);
                    column += squareSize;
                }
                row += squareSize;
            }

            numSquares *= 2;
            squareSize /= 2;
            height *= heightDecayRate;
        }
        
        minMeshHeight = 100f;
        maxMeshHeight = -100f;
        for (int i = 0; i < nVertices; i++) {
            if (vertices[i].y < minMeshHeight) minMeshHeight = vertices[i].y;
            if (vertices[i].y > maxMeshHeight) maxMeshHeight = vertices[i].y;
        }

        water.GetComponent<WaterPlane>().setHeight(minMeshHeight + (maxMeshHeight-minMeshHeight) / 3);

        mRenderer.material.SetFloat("_maxHeight", maxMeshHeight);
        mRenderer.material.SetFloat("_minHeight", minMeshHeight);

        mesh.vertices = vertices;
        mesh.uv = uvs;
        mesh.triangles = triangles;

        mesh.RecalculateBounds();
        mesh.RecalculateNormals();

        mFilter.mesh = mesh;
    }

    private int getRow(int vertexPosition) {
        return vertexPosition / (terrainPartitions + 1);
    }

    private int getColumn(int vertexPosition) {
        return vertexPosition % (terrainPartitions + 1);
    }

    private int getVertexPosition(int row, int column) {
        return row * (terrainPartitions + 1) + column;
    }

    void diamond(int row, int column, int size, float height) {
        int bottomLeft = getVertexPosition(row, column);
        int bottomRight = bottomLeft + size;
        int topRight = getVertexPosition(row + size, column + size);
        int topLeft = topRight - size;
        int middle = (int) (0.5f * (bottomLeft + topRight));

        // diamond step
        int[] squarePoints = new int[] {bottomLeft, bottomRight, topLeft, topRight};
        vertices[middle].y = averageHeight(squarePoints) + Random.Range(-height, height);
    }

    void square(int row, int column, int size, float height) {
        int bottomLeft = getVertexPosition(row, column);
        int bottomRight = bottomLeft + size;
        int topRight = getVertexPosition(row + size, column + size);
        int topLeft = topRight - size;
       
        int middle = (int) (0.5f * (bottomLeft + topRight));
        int halfway = (int) (0.5f * size);

        int outsideVertexPosition;
        int[] squarePoints;
        
        // We check the top and bottom against the limits of the vertices array
        // However we check the left and right against the limits of the row

        // Bottom
        if (row == 0) {
            squarePoints = new int[] {middle, bottomLeft, bottomRight};
            outsideVertexPosition = bottomLeft + halfway - halfway * (terrainPartitions + 1);
            vertices[bottomLeft + halfway].y = averageHeight(squarePoints, row - halfway, outsideVertexPosition)
                + Random.Range(-height, height);
        }

        // Left
        if (column == 0) {
            squarePoints = new int[] {middle, bottomLeft, topLeft};
            outsideVertexPosition = middle - size;
            vertices[middle - halfway].y = averageHeight(squarePoints, column - halfway, outsideVertexPosition)
                + Random.Range(-height, height);
        }

        // Top
        squarePoints = new int[] {middle, topRight, topLeft};
        outsideVertexPosition = topRight - halfway + halfway * (terrainPartitions + 1);
        vertices[topRight - halfway].y = averageHeight(squarePoints, row + size + halfway, outsideVertexPosition)
            + Random.Range(-height, height);
        
        // Right
        squarePoints = new int[] {middle, bottomRight, topRight};
        outsideVertexPosition = middle + size;
        vertices[middle + halfway].y = averageHeight(squarePoints, column + size + halfway, outsideVertexPosition)
            + Random.Range(-height, height);
    }

    private float averageHeight(int[] squarePoints, int outsideVertexCoordinate = -1, int outsideVertexPosition = 0) {
        float sum = 0.0f;
        int divisor = squarePoints.Length;
        
        if (checkOuterPoint(outsideVertexCoordinate)) {
            sum = vertices[outsideVertexPosition].y;
            divisor++;
        }

        for (int i = 0; i < squarePoints.Length; i++) 
            sum += vertices[squarePoints[i]].y;

        return sum / divisor;
    }

    private bool checkOuterPoint(int position) {
        if (position >= 0 && position <= terrainPartitions)
            return true;
        else
            return false;
    }

    public float getMaxHeight() {
        return maxMeshHeight;
    }

    public int getTerrainPartitions() {
        return this.terrainPartitions;
    }
}


