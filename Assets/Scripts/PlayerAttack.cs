using System.Collections;
using System.Collections.Generic;
using UnityEngine;

abstract public class PlayerAttack : MonoBehaviour {

	public float damage;
	public float cooldown;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}

	abstract public void HitTaken();
}
