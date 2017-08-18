using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyHealth : Health {

	// Use this for initialization
	void Start () {

	}
	
	// Update is called once per frame
	void Update () {
		if (hp <= 0)
			Die();
	}

	void Die() {
		gameObject.SetActive(false);
	}
}
