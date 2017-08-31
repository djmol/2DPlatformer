using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EndDemo : MonoBehaviour {

	public GameObject endDemo;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}

	void OnDeath() {
		endDemo.GetComponent<Text>().enabled = true;
	}
}
