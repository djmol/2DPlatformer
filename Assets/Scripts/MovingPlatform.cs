using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovingPlatform : MonoBehaviour {

	public float speed;
	public float acceleration;
	public Transform platform;
	public Transform[] nodes;

	int currentNode;
	bool isBeingRidden;
	GameObject rider;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}

	/// <summary>
	/// This function is called every fixed framerate frame, if the MonoBehaviour is enabled.
	/// </summary>
	void FixedUpdate() {
		Vector2 velocity = (nodes[currentNode].position - platform.position).normalized * speed * Time.deltaTime;

		if ((platform.position - nodes[currentNode].position).magnitude <= speed * Time.deltaTime) {
			velocity = Vector2.ClampMagnitude(velocity, (nodes[currentNode].position - platform.position).magnitude);
			currentNode = GetNextNode();
		}

		platform.Translate(velocity);

		if (isBeingRidden) {
			rider.transform.Translate(velocity);
			return;
		}
	}

	int GetNextNode() {
		return (currentNode + 1 < nodes.Length) ? currentNode + 1 : 0;
	}

	public void GetOnPlatform(GameObject gettingOn) {
		rider = gettingOn;
		isBeingRidden = true;
	}

	public void GetOffPlatform(GameObject gettingOff) {
		if (rider == gettingOff)
			rider = null;
		isBeingRidden = false;
	}
}
