using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour {

	// Object to follow
	public Transform target;

	// Camera movement
	public float smoothing;

	// Camera bounds
	public float lowerBoundary = 10f;

	Vector3 offset;
	float lowY;

	// Use this for initialization
	void Start () {
		//transform.position =  new Vector3(target.position.x, target.position.y, transform.position.z);
		offset = transform.position - target.position;
		lowY = transform.position.y - lowerBoundary;
	}
	
	// Update is called once per frame
	void FixedUpdate () {
		Vector3 targetCamPos = target.position + offset;
		//transform.position = Vector3.Lerp (transform.position, targetCamPos, smoothing * Time.deltaTime);
		if (transform.position.y < lowY) {
			//transform.position = new Vector3 (transform.position.x, lowY, transform.position.z);
		}
	}
}
