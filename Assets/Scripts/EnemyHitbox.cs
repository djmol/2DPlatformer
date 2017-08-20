using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyHitbox : MonoBehaviour {

	EnemyHealth health;
	PlayerAttack playerAttack;

	// Use this for initialization
	void Start () {
		health = GetComponentInParent<EnemyHealth>();
	}
	
	// Update is called once per frame
	void Update () {
		// If hit by player attack, apply damage
		if (playerAttack != null) {
			health.TakeDamage(playerAttack.damage);
			Debug.Log(playerAttack.damage);
			playerAttack.HitTaken();
			playerAttack = null;
		}
	}

	public void Hit(GameObject attackGO) {
		playerAttack = attackGO.GetComponent<PlayerAttack>();
	}
}
