using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour {

	// Object to follow
	public Transform target;

	// Camera movement
	public float smoothRate = 6f;

	// Camera bounds
	public float trackingDistance = 2f;

	Vector3 followVector;

	// Use this for initialization
	void Start () {

	}
	
	// Update is called once per frame
	void Update () {
		// Follow target
		if (transform.position.y < target.position.y - trackingDistance) {
			followVector = new Vector3(target.position.x, target.position.y - trackingDistance, transform.position.z);
		} else if (transform.position.y > target.position.y + trackingDistance) {
			followVector = new Vector3(target.position.x, target.position.y + trackingDistance, transform.position.z);
		} else {
			followVector = new Vector3(target.position.x, transform.position.y, transform.position.z);
		}

		transform.position = Vector3.Lerp(transform.position, followVector, smoothRate * Time.deltaTime);

	}
}
