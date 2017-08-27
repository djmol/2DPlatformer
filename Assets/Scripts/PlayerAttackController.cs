using Extensions;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAttackController : MonoBehaviour {

	public GameObject shotPrefab;
	public GameObject uppercutPrefab;

	bool attackInput;
	enum AttackType {
		Shot = 0,
		Uppercut = 1,
	}
	AttackType currentAttack;

	PlayerMovementController pmc;

	// Use this for initialization
	void Start () {
		pmc = gameObject.GetComponent<PlayerMovementController>();
	}
	
	// Update is called once per frame
	void Update () {
		if (pmc.conditionState.Missing(PlayerMovementController.ConditionState.Hit) && 
			pmc.conditionState.Missing(PlayerMovementController.ConditionState.RestrictedAttacking))
		if (Input.GetButtonUp("Fire2")) {
			attackInput = true;
			currentAttack = AttackType.Shot;
		} else if (Input.GetButtonUp("Fire3")) {
			attackInput = true;
			currentAttack = AttackType.Uppercut;
		}

		if (attackInput) {
			switch (currentAttack) {
				case AttackType.Shot:
					Shot();
					break;
				case AttackType.Uppercut:
					Uppercut();
					break;
			}
		}
		
		attackInput = false;
	}

	void Shot() {
		GameObject shotGO = Instantiate(shotPrefab, transform.position, Quaternion.identity);
		Shot shot = shotGO.GetComponent<Shot>();
		shot.direction = pmc.facing;
		// TODO: How to unset FreeAttacking on last shot disappearing?
		//pmc.conditionState = pmc.conditionState.Include(PlayerMovementController.ConditionState.FreeAttacking);
	}

	void Uppercut() {
		GameObject emptyGO = new GameObject();
		emptyGO.transform.parent = transform;
		GameObject uppercutGO = Instantiate(uppercutPrefab, transform.position, Quaternion.identity, emptyGO.transform);
		Collider2D cd = uppercutGO.GetComponent<Collider2D>();
		Collider2D charCD = pmc.gameObject.GetComponent<Collider2D>();
		uppercutGO.transform.position = new Vector3(transform.position.x + (pmc.facing.x * ((cd.bounds.max.x - cd.bounds.min.x) / 2 + (charCD.bounds.max.x - charCD.bounds.min.x) / 2)), transform.position.y, transform.position.z); 
		pmc.conditionState = pmc.conditionState.Include(PlayerMovementController.ConditionState.RestrictedAttacking);
	}
}
