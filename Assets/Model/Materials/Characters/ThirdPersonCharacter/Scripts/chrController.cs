using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Astar;
using System.Threading;
using UnityEditor;
using System;
using System.Diagnostics;
using System.IO;
using UnityEngine.UI;

public class chrController : MonoBehaviour
{
    GameObject plane { get; set; }
    public static List<Vector3> path;
    public static int index;
    public Gradient gradient;
    public LineRenderer lr;
    public static List<Vector3> pathTaken;
    public float recalcCost { get; set; }
    public float recalcNodesExpanded { get; set; }
    public enum state { waiting, planning, avoidance, moving, recalculating };
    public state agentState;
    public int callno = 0;
    public Ray lookahead;
    public RaycastHit lookaheadinfo;
    public string primaryObstacle;
    public Vector3 endVector, startVector;
    public List<Vector3> obstacles, dynamicObstacles;
    public Vector3 startPosThisRun, origDir, avoidanceMoveDirection;
    public bool isPathObstructed;
    public bool avoidance;
    Stopwatch sw;

    void Start()
    {
        pathTaken = new List<Vector3>();
        plane = GameObject.Find("Plane(Clone)");
        gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] { new GradientColorKey(Color.red, 0.0f), new GradientColorKey(Color.blue, 1.0f) },
            new GradientAlphaKey[] { new GradientAlphaKey(1.0f, 0.0f), new GradientAlphaKey(1.0f, 0.0f) }
            );
        lr = plane.AddComponent<LineRenderer>();
        lr.material = (Material)AssetDatabase.LoadAssetAtPath("Assets/Material/Destination.mat", typeof(Material));
        lr.colorGradient = gradient;
        recalcCost = 0;
        recalcNodesExpanded = 0;
        agentState = state.waiting;
        isPathObstructed = false;
        avoidance = false;
        
    }

    void Update()
    //void FixedUpdate()
    {
        // Feeler
        UnityEngine.Debug.DrawRay(this.transform.position + new Vector3(0.0f, 5.0f,0.0f), this.transform.forward * 3, Color.magenta);

        if (this.agentState == state.planning)
        {
            lr.positionCount = 0;

            path = getPath(endVector);
            if (path != null)
            {
                index = path.Count - 1;
                lr.positionCount = index;
                lr.SetPositions(path.ToArray());


            }
            //Control from UI
            this.agentState = state.moving;
        }

        if (this.agentState == state.avoidance)
        {
            // Add move Direction
            lookahead = new Ray(transform.position, origDir);
            Physics.Raycast(lookahead, out lookaheadinfo, 3f);
            if (lookaheadinfo.collider != null)
            {
                if (!(lookaheadinfo.collider.name.Contains("dynObj")))
                {
                    isPathObstructed = false;
                    this.agentState = state.planning;
                }

                if (avoidanceMoveDirection == Vector3.zero)
                {
                    Vector3 dir = (lookaheadinfo.point - transform.position).normalized;
                    dir.x = (dir.x == 0) ? 0f : (dir.x > 0) ? 1f : -1f;
                    dir.z = (dir.z == 0) ? 0f : (dir.z > 0) ? 1f : -1f;
                    dir += lookaheadinfo.normal;
                    // For 90 degree approach
                    if (dir == Vector3.zero)
                    {
                        avoidanceMoveDirection = Vector3.zero;
                        avoidanceMoveDirection.x = origDir.z;
                        avoidanceMoveDirection.z = origDir.x;
                    }
                    else
                    {
                        avoidanceMoveDirection = dir;
                    }

                }
                obstacles.Add(lookaheadinfo.transform.localPosition);

                RaycastHit raymoveDirn;
                Physics.Raycast(new Ray(transform.position, avoidanceMoveDirection), out raymoveDirn, 1f);
                if (raymoveDirn.collider != null)
                {
                    avoidanceMoveDirection = raymoveDirn.normal;

                }
                else {
                    float angle = Vector3.SignedAngle(avoidanceMoveDirection, Vector3.forward, Vector3.down);
                    transform.localEulerAngles = new Vector3(0.0f, angle, 0.0f);
                    
                    transform.position += avoidanceMoveDirection;
                    pathTaken.Add(transform.localPosition);
                }

            }
            else
            {
                avoidanceMoveDirection = Vector3.zero;
                isPathObstructed = false;
                this.agentState = state.planning;
            }
        }

        if (this.agentState == state.recalculating)
        {
            lr.positionCount = 0;
            Vector3 lastStart = path[path.Count - 1];
            foreach (Vector3 v in dynamicObstacles)
            {
                obstacles.Add(v);
            }
            isPathObstructed = false;
            this.agentState = state.planning;
        }

        if (path != null && index >= 0 && this.agentState == state.moving)
        {
            // 3f will change into parameter provided by the user

            // To change direction
            Vector3 dir = (path[index] - transform.position);
            float angle = Vector3.SignedAngle(dir, Vector3.forward, Vector3.down);
            transform.localEulerAngles = new Vector3(0.0f, angle, 0.0f);

            lookahead = new Ray(transform.position, transform.forward);

            Physics.Raycast(lookahead, out lookaheadinfo, 3f);
            if (lookaheadinfo.collider == null)
            {
                // To move
                transform.Translate(dir, Space.World);
                // Add to path taken

                pathTaken.Add(path[index]);
                index -= 1;

                // Recalculate total cost on change in path after obstacle detection
                //recalcCost += Vector3.Angle(dir, Vector3.forward) % 90f == 0 ? 1.0f : 1.4f;

            }
            else
            {
                //Re compute A*
                // Check if path goes going through obstacle is a straight line changed to raycasting
                for (int l = 0; l <= 3; l++)
                {
                    if (index - l >= 0)
                    {
                        RaycastHit rayDown;
                        Vector3 newVec = transform.position + (dir * (l + 1)) + Vector3.up * 50;
                        Physics.Raycast(newVec, -Vector3.up, out rayDown);
                        if (rayDown.collider != null && !(rayDown.collider.name.Contains("Plane") || rayDown.collider.name.Contains("ThirdPersonController")))
                        {
                            // If obstructed path from raycast belongs to path to be traversed
                            if (path[index - l] == (transform.position + (dir * (l + 1))))
                            {
                                isPathObstructed = true;
                                break;
                            }
                        }

                    }
                    else
                    {
                        isPathObstructed = false;
                        break;
                    }

                }

                if (isPathObstructed)
                {
                    origDir = dir;
                    this.agentState = avoidance ? state.avoidance : state.recalculating;

                }
                // Repetition here
                else
                {
                    // To move
                    transform.Translate(dir, Space.World);
                    // Add to path taken
                    pathTaken.Add(path[index]);
                    index -= 1;
                    // Recalculate total cost on change in path after obstacle detection
                    //recalcCost += Vector3.Angle(dir, Vector3.forward) % 90f == 0 ? 1.0f : 1.4f;
                }
            }
        }
        else if (index == -1)
        {
            Vector3 _end = pathTaken[pathTaken.Count - 1];
            Vector3 _start = pathTaken[0];
            lr.positionCount = 0;
            lr.positionCount = pathTaken.Count - 1;
            lr.SetPositions(pathTaken.ToArray());
            sw.Stop();
            print(sw.ElapsedMilliseconds);
            index -= 1;
            for (int i = 0; i < pathTaken.Count - 1; i++)
            {
                recalcCost += Vector3.Angle((pathTaken[i] - pathTaken[i + 1]), Vector3.forward) % 90f == 0 ? 1.0f : 1.4f;
            }
            //float opCost = astar.optimalCost;
            //int optimalNodesExpanded = astar.totalNodesExpanded;

            this.agentState = state.waiting;
            print(" \n Cost of path is " + recalcCost.ToString());
            print(" \n Nodes expanded are " + recalcNodesExpanded.ToString());
            string exppath = GameObject.Find("ExperimentOutField").GetComponent<InputField>().text;
            if (exppath.Length != 0) {
                string res = "";
                string mappath = GameObject.Find("InputField").GetComponent<InputField>().text;
                Toggle isoptimalcheck = GameObject.Find("AstarToggle").GetComponent<Toggle>();
                string typeused = isoptimalcheck.isOn? "Optimal Check" : avoidance ? "Obstacle Avoidance" : "Only recalculation";
                res += mappath + " | " + startVector.ToString() + " | " + endVector.ToString() + " | " + typeused + " - " + "  " + sw.ElapsedMilliseconds + " | " + recalcCost.ToString() + " | " + recalcNodesExpanded.ToString();
                StreamWriter writer = new StreamWriter(exppath, true);
                writer.WriteLine(res);
                writer.Close();
            } else {
                print("Exp path not set");
            }
            
            resetAll();
        }
    }

    public void resetAll()
    {
        
        recalcCost = 0;
        recalcNodesExpanded = 0;
        this.agentState = state.waiting;
        path = null;
        index = -2;
        pathTaken.Clear();
        this.transform.localPosition = startVector;
    }

    

    public int checkInPath(Vector3 dir, int len = 30)
    {
        // Todo: Find someplace to store map size we hardcode it for now
        for (int i = 1; i <= len; i++)
        {
            Vector3 newVec = transform.position + (dir * i);
            if (Math.Abs(newVec.x) > 30 || Math.Abs(newVec.z) > 30)
            {
                return -100;
            }
            if (path.Contains(newVec))
            {
                return path.IndexOf(newVec);
            }
        }
        return -100;
    }

  
    public List<Vector3> getPath(Vector3 dest)
    {
        RaycastHit rayDown;
        Vector3 newVec = dest + Vector3.up * 50;
        Physics.Raycast(newVec, -Vector3.up, out rayDown);
        if (rayDown.collider != null && (rayDown.collider.name.Contains("Plane") || rayDown.collider.name.Contains("ThirdPersonController")))
        {
            Vector3 _end = dest;
            Vector3 _start = transform.position;
            Vector3 mid_start = midTile(_start);
            Vector3 mid_end = midTile(_end);
            a_star recomp = new a_star(mid_start, mid_end, obstacles);
            List<Vector3> path = recomp.runAStar();
            recalcNodesExpanded += recomp.totalNodesExpanded;
            return path;
        }
        return null;
    }

    // Converts clicked location to the grid center
    public Vector3 midTile(Vector3 _vec)
    {
        return new Vector3(Mathf.FloorToInt(_vec.x) + 0.5f, _vec.y, Mathf.FloorToInt(_vec.z) + 0.5f);
    }

    public void runAll() {
        lr.positionCount = 0;
        this.agentState = state.planning;
        sw = Stopwatch.StartNew();
    }

}
