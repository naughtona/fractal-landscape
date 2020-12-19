using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class flyingcamera : MonoBehaviour
{
    public GameObject terrain;
    public GameObject water;

    public float movementSpeed;
    public float mouseSensitivity;
    private float waterHeight;
    private float terrainLength;
    private float gridSize;
    private int terrainPartitions;
    public float minInitHeight;
	
	[Range(0.5f, 5f)] public float heightBuffer;
    Vector3[] vertices;

    public float rotationSpeed = 3f;
    private float pitch = 0f;
    private float yaw = 0f;
    
    // limit pitch movement
    private float maxPitch = -65; 
    private float minPitch = 50;

	[Header("Boundary Value")]
	public DiamondSquare scpMap;
	[Range(5f, 100f)] public float gapDistanceY;
	[Range(0f, 5f)] public float boundX, boundZ;
	Vector3 startPos;
	Vector3 posLeftBottom, posRightTop;


	void Start()
	{
		// lock cursor
		Cursor.visible = false;

		GetVerticesAndResetCamera();
		posLeftBottom = new Vector3(boundX, -scpMap.maxHeight - gapDistanceY, boundZ);
		posRightTop	= new Vector3(scpMap.terrainLength - boundX, scpMap.maxHeight + gapDistanceY, scpMap.terrainLength - boundZ);
	}

	void Update() {
        
        // move the flying camera around
        this.transform.Translate(new Vector3(movementSpeed * Input.GetAxis("Horizontal"), 0f,movementSpeed * Input.GetAxis("Vertical")));
        
        // rotate the pitch and yaw of the camera
        yaw	+= rotationSpeed * Input.GetAxis("Mouse X");
        pitch -= rotationSpeed * Input.GetAxis("Mouse Y");
        pitch = Mathf.Clamp(pitch, maxPitch, minPitch); 
        transform.eulerAngles = new Vector3(pitch, yaw, 0.0f);

		CheckPositionBoundary();
	}

	public void GetVerticesAndResetCamera() {
        // get the vertices of generated terrain
        vertices = terrain.GetComponent<MeshFilter>().mesh.vertices;
        terrainLength = terrain.GetComponent<DiamondSquare>().terrainLength;
        terrainPartitions = terrain.GetComponent<DiamondSquare>().getTerrainPartitions();
        waterHeight	= water.GetComponent<WaterPlane>().height;
        gridSize = terrainLength / terrainPartitions;

        // reset camera, makes sure it is above highest point in terrain 
    	// and find angle so you are looking at the mid point of map
        float y = Mathf.Max(terrain.GetComponent<DiamondSquare>().getMaxHeight() + minInitHeight,waterHeight + minInitHeight);
        this.transform.position = new Vector3(0f, y, 0f);

        // store the mouse x and y coords
        float angle = 180 * Mathf.Atan(y / (0.5f * terrainLength)) / Mathf.PI;
        pitch = angle;
        yaw = 45;
    }

	void CheckPositionBoundary()
	{
		// prevent moving outside terrain in x,z plane
		Vector3 _pos = transform.position;
		if (_pos.x < posLeftBottom.x)
			_pos.x = posLeftBottom.x;
		else if (_pos.x > posRightTop.x)
			_pos.x = posRightTop.x;

		if (_pos.z < posLeftBottom.z)
			_pos.z = posLeftBottom.z;
		else if (_pos.z > posRightTop.z)
			_pos.z = posRightTop.z;
		
		if (_pos.y < posLeftBottom.y)
			_pos.y = posLeftBottom.y;
		else if (_pos.y > posRightTop.y)
			_pos.y = posRightTop.y;


		// prevent moving under terrain or water
		float _meshHeight = GetHeight(_pos.x, _pos.z);

		if (_pos.y < _meshHeight + heightBuffer)
			_pos.y = _meshHeight + heightBuffer;

		transform.position = _pos;
	}


	// to check height of terrain at current x,z coordinates
	// we need to find correct vertex
	private float GetHeight(float _x, float _z) {
        float _ROUND_HELPER = 0.5f;
        int _xColumn = (int)(_x / gridSize + _ROUND_HELPER), zRow = (int) (_z / gridSize + _ROUND_HELPER);
        
        return Mathf.Max(vertices[zRow*(terrainPartitions+1)+_xColumn].y,waterHeight);
    }
}
