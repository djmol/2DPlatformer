using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SelfDestructPS : MonoBehaviour {

	public ParticleSystem ps;

	// Use this for initialization
	void Start () {
		// Only works for multiplier since I'm not using curves
		Destroy(gameObject,ps.main.startLifetimeMultiplier);
	}
	
	// Update is called once per frame
	void Update () {
		
		
	}

}
