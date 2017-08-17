using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerHealth : Health {

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		if (hp <= 0)
			Die();
	}

	void Die() {
		// TODO: Some sort of death animation.
		SceneManager.LoadScene(SceneManager.GetActiveScene().name);
	}
}
