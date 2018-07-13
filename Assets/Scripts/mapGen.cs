using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class mapGen : MonoBehaviour {
    private float col;
    private float row;
    public Vector3 markedpoint, startPoint, endPoint;
    public enum elementTypes {StartPoint, EndPoint, Obstacle };
    List<Vector3> obstacleList, dobsList;
    UnityEngine.Object agentPrefab;
    GameObject agent;
    UnityEngine.Object selpointPrefab;
    GameObject selPointProjection;
    UnityEngine.Object endPointPrefab;
    GameObject endPointProjection;
    UnityEngine.Object dynamicObstaclesPrefab;
    GameObject[] dynamicObstacles;
    public GameObject unitSize;
    public int dynObslen;                                                   // Dynamic objects length
    float startcol;
    float startrow;
    chrController charC;

    // Use this for initialization
    void Start()
    {
        obstacleList = new List<Vector3>();
        dobsList = new List<Vector3>();
        agentPrefab = AssetDatabase.LoadAssetAtPath("Assets/Prefabs/ThirdPersonController.prefab", typeof(GameObject));
        selpointPrefab = AssetDatabase.LoadAssetAtPath("Assets/Prefabs/project.prefab", typeof(GameObject));
        selPointProjection = Instantiate(selpointPrefab, Vector3.zero, Quaternion.identity) as GameObject;
        selPointProjection.transform.localEulerAngles = new Vector3(-90f, 0.0f, 0.0f);
        endPointPrefab = AssetDatabase.LoadAssetAtPath("Assets/Prefabs/endProj.prefab", typeof(GameObject));
        dynamicObstaclesPrefab = AssetDatabase.LoadAssetAtPath("Assets/Prefabs/dynObj.prefab", typeof(GameObject));
        dynamicObstacles = new GameObject[10000];
        unitSize = GameObject.Find("unitSize");
        unitSize.SetActive(false);
        dynObslen = 0;
    }
	
	// Update is called once per frame
	void Update () {
        Vector3 pos = transform.position;
        if (Input.GetKey("w")) {
            pos.z += 50 * Time.deltaTime;
        }
        if (Input.GetKey("d"))
        {
            pos.x += 50 * Time.deltaTime;
        }
        if (Input.GetKey("s"))
        {
            pos.z -= 50 * Time.deltaTime;
        }
        if (Input.GetKey("a"))
        {
            pos.x -= 50 * Time.deltaTime;
        }
        if (Input.GetKey("up"))
        {
            pos.y -= 40 * Time.deltaTime;
        }
        if (Input.GetKey("down"))
        {
            pos.y += 40 * Time.deltaTime;
        }
        transform.position = pos;
        if (Input.GetMouseButtonDown(1)) {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit movePoint;
            if (Physics.Raycast(ray, out movePoint, 1000f, LayerMask.NameToLayer("Plane")))
            {
                // Raycast to get details on collision and coordinates of the right click
                if (movePoint.collider.name.Contains("Plane"))
                {
                    Vector3 selectedTile = midTile(movePoint.point);
                    
                    markedpoint = selectedTile;
                    selPointProjection.transform.localPosition = markedpoint + Vector3.down;
                }
            }
        }
        //print(dynamicObstacles.Length);
    }

    public void loadMap() {
        string path = GameObject.Find("InputField").GetComponent<InputField>().text;
        if (path.Length == 0) {
            EditorUtility.DisplayDialog("Map path", "Map to load not selected.", "ok");
            return;
        }
        // Needs more error handling on path
       
        string[] lines = System.IO.File.ReadAllLines(@path);

        UnityEngine.Object planeprefab = AssetDatabase.LoadAssetAtPath("Assets/Prefabs/Plane.prefab", typeof(GameObject));
        UnityEngine.Object outofbounds = AssetDatabase.LoadAssetAtPath("Assets/Prefabs/outofbounds.prefab", typeof(GameObject));
        UnityEngine.Object block1 = AssetDatabase.LoadAssetAtPath("Assets/Prefabs/block1.prefab", typeof(GameObject));
        
        // Add a new plane
        GameObject plane = Instantiate(planeprefab, Vector3.zero, Quaternion.identity) as GameObject;
        int firstLine = lines[0].Contains("type") ? 4 : 0; 
        col = lines[firstLine].Length;
        row = lines.Length - firstLine;
        startcol = -(col * 0.5f);
        startcol += ((col % 2) == 0) ? .5f : .0f;
        startrow = (row * .5f);
        startrow -= ((row % 2) == 0) ? .5f : .0f;
        //scale the plane
        plane.transform.localScale = new Vector3(col/10, 0.01f, row/10);
        Dictionary<char, int> map = new Dictionary<char, int>();

        // Out of bounds, passable and unpassable tree/block
        map.Add('@', 0);
        map.Add('.', 0);
        map.Add('T', 0);

        // Count of each type of element in the map
        int counter = 0;
        foreach (string line in lines)
        {
            if (counter >= firstLine) {
                foreach (char c in line)
                {
                    if (c == '@' || c == 'O')
                    {
                        map['@']++;
                    }
                    else if (c == 'T' || c == 'W')
                    {
                        map['T']++;
                    }
                    else
                    {
                        map['.']++;
                    }
                }
            }
            counter++;
        }

        try
        {
        
            Vector3 curPos = new Vector3(startcol, 0.01f, startrow);
            GameObject[] oob = new GameObject[map['@']];
            GameObject[] unpass = new GameObject[map['T']];
            int oobIndex = 0, unpassIndex = 0,dynIndex = 0;
            counter = 0;
            foreach (string line in lines)
            {
                if (counter >= firstLine)
                {
                    foreach (char c in line)
                    {
                        if (c == '@' || c == 'O')
                        {
                            oob[oobIndex] = Instantiate(outofbounds, curPos, Quaternion.identity) as GameObject;
                            oob[oobIndex].transform.localPosition = curPos;
                            obstacleList.Add(curPos);
                            oobIndex++;
                        }
                        else if ((c == 'T' || c == 'W'))
                        {
                            unpass[unpassIndex] = Instantiate(block1, curPos, Quaternion.identity) as GameObject;
                            obstacleList.Add(curPos);
                            unpassIndex++;
                        }
                        else if (c == '$')
                        {
                            dynamicObstacles[dynIndex] = Instantiate(dynamicObstaclesPrefab, curPos, Quaternion.identity) as GameObject;
                            dobsList.Add(curPos);
                            dynIndex++;
                        }
                        curPos += new Vector3(1f, 0.0f, 0.0f);
                    }
                    curPos += new Vector3(0.0f, 0.0f, -1.0f);
                    curPos.x = startcol;
                }
                counter++;
            }
            dynObslen = dynIndex;
        }
        catch (System.Exception e)
        {
            print(e.ToString());
        }
    }

    public void saveMap() {
        string[] lines = new string[(int)row];
        string path = GameObject.Find("OutputField").GetComponent<InputField>().text;
        if (path.Length == 0)
        {
            EditorUtility.DisplayDialog("Map path", "Blank path selected.", "ok");
            return;
        }
        if (GameObject.Find("Plane(Clone)") == null) {
            EditorUtility.DisplayDialog("Map path", "Map doesn't exist", "ok");
            return;
        }
        RaycastHit rayDown;
        Vector3 curPos = new Vector3(startcol, 0.01f, startrow);
        for (int i = 0; i < row; i++) {
            for (int j = 0; j < col; j++) {
                Vector3 newVec = curPos + Vector3.up * 50;
                Physics.Raycast(newVec, -Vector3.up, out rayDown);
                if (rayDown.collider != null && (rayDown.collider.name.Contains("Plane") || rayDown.collider.name.Contains("ThirdPersonController")))
                {
                    lines[i] += ".";
                }
                else if (rayDown.collider != null && rayDown.collider.name.Contains("outofbounds"))
                {
                    lines[i] += "@";
                }
                else if (rayDown.collider != null && rayDown.collider.name.Contains("block1"))
                {
                    lines[i] += "T";
                }
                else if (rayDown.collider != null && rayDown.collider.name.Contains("dynObj")) {
                    lines[i] += "$";
                }
                curPos += new Vector3(1f, 0.0f, 0.0f);
            }
            curPos += new Vector3(0.0f, 0.0f, -1.0f);
            curPos.x = startcol;
        }
        StreamWriter writer = new StreamWriter(path);
        foreach (string line in lines) {
            writer.WriteLine(line);
        }
        writer.Close();
        EditorUtility.DisplayDialog("Map path", "Map successfully saved", "ok");
    }

    // Converts clicked location to the grid center
    private Vector3 midTile(Vector3 _vec)
    {
        return new Vector3(Mathf.FloorToInt(_vec.x) + 0.5f, _vec.y, Mathf.FloorToInt(_vec.z) + 0.5f);
    }

    public void addElement()
    {
        if (markedpoint != null) {
            Dropdown elementType = GameObject.Find("AddToMap").GetComponent<Dropdown>();
            switch (elementType.value) {
                case 0:
                    startPoint = markedpoint;
                    if (agent == null)
                    {
                        agent = Instantiate(agentPrefab, startPoint, Quaternion.identity) as GameObject;

                    }
                    else
                    {
                        agent.transform.localPosition = startPoint;
                    }
                    break;
                case 1:
                    endPoint = markedpoint;
                    if (endPointProjection == null)
                    {
                        endPointProjection = Instantiate(endPointPrefab, endPoint, Quaternion.identity) as GameObject;
                        endPointProjection.transform.localEulerAngles = new Vector3(-90f, 0.0f, 0.0f);
                    }
                    else {
                        endPointProjection.transform.localPosition = endPoint + Vector3.down;
                    }
                    break;
                // Add case for obstacles
                case 2:
                    addDynamicObstacles();
                    break;
            }
        }
    }

    public void charRunAStar() {
        // Get the class object in chrController
        charC = agent.GetComponent<chrController>();
        charC.endVector = endPoint;
        // Create a clone so that references aren't copied when mixed
        charC.obstacles = new List<Vector3>(obstacleList);
        charC.dynamicObstacles = new List<Vector3>(dobsList);

        
        //charC.agentState = chrController.state.moving;
    }

    public void runAllExperiments() {
        if (agent == null || endPoint == Vector3.zero) {
            EditorUtility.DisplayDialog("Error", "Missing either the start point or end point", "ok");
            return;
        } 
        charC = agent.GetComponent<chrController>();
        charC.endVector = endPoint;
        charC.startVector = charC.transform.localPosition;
        charC.obstacles = new List<Vector3>(obstacleList);
        charC.dynamicObstacles = new List<Vector3>(dobsList);
        Toggle useAvoidance = GameObject.Find("Toggle").GetComponent<Toggle>();
        Toggle isoptimalcheck = GameObject.Find("AstarToggle").GetComponent<Toggle>();
        if (isoptimalcheck.isOn) {
            foreach (Vector3 v in dobsList) {
                charC.obstacles.Add(v);
            }
        }
        charC.avoidance = useAvoidance.isOn;
        charC.runAll();
    }

    public void addDynamicObstacles()
    {
        float obsMultiplier = unitSize.GetComponent<Slider>().value;
        for (float i = -obsMultiplier + 1; i < obsMultiplier; i++) {
            for (float j = -obsMultiplier + 1; j < obsMultiplier; j++)
            {
                Vector3 newpoint = markedpoint + new Vector3(i, 0.0f, j);
                dynamicObstacles[dynObslen] = Instantiate(dynamicObstaclesPrefab, newpoint, Quaternion.identity) as GameObject;
                dobsList.Add(newpoint);
                dynObslen++;
            }
        }
    }

    public void onDropDownChange()
    {
        Dropdown cur = GameObject.Find("AddToMap").GetComponent<Dropdown>();
        if (cur.value == 2)
        {
            unitSize.SetActive(true);
        }
        else
        {
            unitSize.SetActive(false);
        }
    }

}
