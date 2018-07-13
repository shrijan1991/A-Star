using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class cameraController : MonoBehaviour {
	public Vector3 panLimit;
	public float speed = 15f;
	float widthMid = Screen.width/2;
	float heightMid = Screen.height/2;

	// Use this for initialization
	void Start () {
	}

	// Update is called once per frame
	void Update () {
		float dx = (Input.mousePosition.x - widthMid) / widthMid;
		float dz = (Input.mousePosition.y - heightMid) / heightMid;

		Vector3 pos = transform.position;
		// Move camera if mouse is over the 10% threshold on either axis relative to how far camera is from the center of that axis
		// Camera speed is dependent on how far it is from the center of that particualar axis. 
		if (Mathf.Abs (dx) > 0.9 || Mathf.Abs (dz) > 0.9) {
			pos.x += dx * speed * Time.deltaTime;
			pos.z += dz * speed * Time.deltaTime;
		}
		// Limitation of camera movement
		pos.x = Mathf.Clamp (pos.x, -panLimit.x, panLimit.x);
		pos.z = Mathf.Clamp (pos.z, -panLimit.z, panLimit.z - 10f);

		transform.position = pos;
	}
		
}
