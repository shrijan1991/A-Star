using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Astar
{
	public class a_star
	{
		public static Dictionary<Vector3, node> openList = new Dictionary<Vector3, node>();
		public static Dictionary<Vector3, node> closedList = new Dictionary<Vector3, node>();
		public static node src;
		public static Vector3 endVector;
		public float optimalCost{ get; set;}

        public static List<Vector3> obstacles;
        public static RaycastHit rayDown;
        public static int nodesExpanded;
        public int totalNodesExpanded { get; set; }

        public class node
		{
			public bool open { get; set;}
			public Vector3 coordinates{ get; set;}
			public float f { get; set;}
			public float g { get; set;}
			public float h { get; set;}
			public Vector3 goal { get; set;}
			public node parent { get; set;}
			public List<node> successors;

			public node(node _parent, Vector3 _coordinates, float _g, float _h, Vector3 _goal){
				parent = _parent;
				open = true;
				coordinates = _coordinates;
				g = _g;
				h = _h;
				f = g + h;
				goal = _goal;
				successors = new List<node>();
			}

			// Expansion operator LGamma as per the paper
			public void expand() {	 
				float[] movements = new float[] {-1f, 0f, 1f};
                nodesExpanded++;
				foreach (float vertical in movements){
					foreach (float horizontal in movements) {
                        if (!(vertical == 0f && horizontal == 0f))
                        {
                            Vector3 newVec = new Vector3(coordinates.x + horizontal, coordinates.y, coordinates.z + vertical);
                            if (validCoordinate(newVec))
                            {
                                float dx = Mathf.Abs(newVec.x - goal.x);
                                float dz = Mathf.Abs(newVec.z - goal.z);
                                float heuristic = Mathf.Sqrt((dx * dx) + (dz * dz));
                                float g_val = this.g;
                                g_val += (vertical == 0 || horizontal == 0) ? 1f : 1.4f;
                                // Change parent if new evaluation cost is  less than existing cost i.e. change in path taken to reach this node if old f is greater than new f.
                                if (openList.ContainsKey(newVec))
                                {
                                    if (openList[newVec].f > g_val + heuristic)
                                    {
                                        node existing = openList[newVec];
                                        existing.g = g_val;
                                        existing.h = heuristic;
                                        existing.parent = this;
                                        this.successors.Add(existing);
                                    }
                                    // Avoid reopening old node, also the authors mentioned that once closed the algorithm wouldn't open a closed node again
                                }
                                else if (closedList.ContainsKey(newVec))
                                {
                                    if (closedList[newVec].f > g_val + heuristic)
                                    {
                                        node existing = closedList[newVec];
                                        existing.g = g_val;
                                        existing.h = heuristic;
                                        //?????????????????????
                                        if (this.coordinates != existing.coordinates) {
                                            existing.parent = this;
                                        }
                                        
                                        existing.open = true;
                                        closedList.Remove(newVec);
                                        openList.Add(newVec, existing);
                                        this.successors.Add(existing);
                                    }
                                }
                                else
                                {
                                    node freshNode = new node(this, newVec, g_val, heuristic, this.goal);
                                    this.successors.Add(freshNode);
                                    // Add to open list
                                    openList.Add(newVec, freshNode);
                                }
                            }
						}
					}
				}
			}
				
			// Close a node - Kind of irrelevant
			public void close () {
				this.open = false;
			}
			
		}

		public static bool validCoordinate (Vector3 c) {
            foreach (Vector3 v in obstacles) {
                if (v.x == c.x && v.z == c.z) {
                    return false;
                }
            }
            return true;
        }


		// Constructor
		public a_star(Vector3 _startVec, Vector3 _endVec, List<Vector3> obs) {
			openList = new Dictionary<Vector3, node>();
			closedList = new Dictionary<Vector3, node>();
			this.optimalCost = 0;
			src = null;
			Vector3 startVector = _startVec;
			endVector = _endVec;
			float dx = Mathf.Abs (startVector.x - endVector.x);
			float dz = Mathf.Abs (startVector.z - endVector.z);
			// Euclidean distance as h
			float heuristic = Mathf.Sqrt ((dx * dx) + (dz * dz));
			src = new node (null, startVector, 0f, heuristic, endVector);
			openList.Add (src.coordinates, src);
            obstacles = obs;
            nodesExpanded = 0;
            totalNodesExpanded = 0;
        }

		public List<Vector3> runAStar () {
			List<Vector3> path = new List<Vector3> ();
			node  endNode = computePath (src, endVector);
			this.optimalCost = endNode.f;
			path.Add (endNode.coordinates);
			// Trace back parent from end node to get to parent node
			while(endNode.parent != null) {
				endNode = endNode.parent;
                if (endNode.parent!= null && endNode.parent.coordinates == endNode.coordinates) {
                    break;
                }
				path.Add (endNode.coordinates);
			}
            totalNodesExpanded = nodesExpanded;
            return path;
		}
			
         
		public static node computePath(node sourceNode, Vector3 _endVec){
            // Close current node, remove from open list and add to close list
            if (sourceNode == null) {
                return null;
            }
            openList.Remove(sourceNode.coordinates);
            sourceNode.close();
			closedList.Add (sourceNode.coordinates, sourceNode);
			if (sourceNode.coordinates.x == _endVec.x && sourceNode.coordinates.z == _endVec.z) {
				return sourceNode;
			}
			sourceNode.expand();
			float smallest = Mathf.Infinity;
			node smallestNode = null;
			// Check for smallest f in open list
			foreach (node child in openList.Values) {
				if (child.f < smallest) {
					smallest = child.f;
					smallestNode = child;
				}
			}
			// Call compute path from src node to end vector again
			return computePath (smallestNode, _endVec);
		}

	}
}

