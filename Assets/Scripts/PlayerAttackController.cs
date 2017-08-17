using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAttackController : MonoBehaviour {

	public GameObject shotPrefab;

	bool attackInput;
	enum AttackType {
		Shot = 0,
	}
	AttackType currentAttack;

	PlayerMovementController pmc;

	// Use this for initialization
	void Start () {
		pmc = gameObject.GetComponent<PlayerMovementController>();
	}
	
	// Update is called once per frame
	void Update () {
		if (Input.GetButtonUp("Fire2"))
			attackInput = true;

		if (attackInput) {
			switch (currentAttack) {
				case AttackType.Shot:
					Shot();
					break;
			}
		}
		
		attackInput = false;
	}

	void Shot() {
		GameObject shotGO = Instantiate(shotPrefab, transform.position, Quaternion.identity);
		Shot shot = shotGO.GetComponent<Shot>();
		shot.direction = pmc.facing;
	}
}
