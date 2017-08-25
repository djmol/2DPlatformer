using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyHitbox : MonoBehaviour {

	public EnemyAttackController eac { get; private set; }
	public EnemyMovementController emc { get; private set; }
	public EnemyHurtbox hurtbox { get; private set; }

	EnemyHealth health;
	PlayerAttack playerAttack;

	public void Hit(GameObject attackGO) {
		playerAttack = attackGO.GetComponent<PlayerAttack>();
	}

	// Use this for initialization
	void Start () {
		health = GetComponentInParent<EnemyHealth>();
		eac = GetComponentInParent<EnemyAttackController>();
		emc = GetComponentInParent<EnemyMovementController>();
		hurtbox = GetComponent<EnemyHurtbox>();
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
}
