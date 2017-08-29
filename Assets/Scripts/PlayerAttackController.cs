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

	public float nextShotTime { get; private set; }
	public float nextUppercutTime { get; private set; }

	PlayerStateManager state;
	PlayerMovementController pmc;

	// Use this for initialization
	void Start () {
		state = gameObject.GetComponent<PlayerStateManager>();
		pmc = gameObject.GetComponent<PlayerMovementController>();
		nextShotTime = 0;
		nextUppercutTime = 0;
	}
	
	// Update is called once per frame
	void Update () {
		if (state.condState.Missing(PlayerState.Condition.Hit) && 
			state.condState.Missing(PlayerState.Condition.RestrictedAttacking))
		if (Input.GetButtonUp("Fire2")) {
			if (Time.time >= nextShotTime) {
				attackInput = true;
				currentAttack = AttackType.Shot;
			}
		} else if (Input.GetButtonUp("Fire3")) {
			if (Time.time >= nextUppercutTime) {
				attackInput = true;
				currentAttack = AttackType.Uppercut;
			}
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
		// Create attack
		GameObject shotGO = Instantiate(shotPrefab, transform.position, Quaternion.identity);
		Shot shot = shotGO.GetComponent<Shot>();
		shot.direction = pmc.facing;

		// Set next time attack can be used
		nextShotTime = Time.time + shot.cooldown;
		
		// TODO: How to unset FreeAttacking on last shot disappearing?
		//pmc.conditionState = pmc.conditionState.Include(PlayerMovementController.ConditionState.FreeAttacking);
	}

	void Uppercut() {
		// Create attack
		GameObject emptyGO = new GameObject();
		emptyGO.transform.parent = transform;
		GameObject uppercutGO = Instantiate(uppercutPrefab, transform.position, Quaternion.identity, emptyGO.transform);
		Uppercut uppercut = uppercutGO.GetComponent<Uppercut>();
		Collider2D cd = uppercutGO.GetComponent<Collider2D>();
		Collider2D charCD = pmc.gameObject.GetComponent<Collider2D>();
		uppercutGO.transform.position = new Vector3(transform.position.x + (pmc.facing.x * ((cd.bounds.max.x - cd.bounds.min.x) / 2 + (charCD.bounds.max.x - charCD.bounds.min.x) / 2)), transform.position.y, transform.position.z); 
		
		// Set next time attack can be used
		nextUppercutTime = Time.time + uppercut.cooldown;

		// Set player state
		state.condState = state.condState.Include(PlayerState.Condition.RestrictedAttacking);
		state.condState = state.condState.Remove(PlayerState.Condition.Normal);
	}
}
