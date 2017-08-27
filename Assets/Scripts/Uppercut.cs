using Extensions;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Uppercut : PlayerAttack {

	public float lifetime;
	public float upwardAttackSpeed;
	public float upwardHitSpeed;
	float time;

	EnemyHitbox hitbox;
	bool hit = false;

	PlayerStateManager state;
	PlayerMovementController pmc;
	bool startedAttack = false;

	override public void HitTaken() {
		Debug.Log("hit taken");
	}

	// Use this for initialization
	void Start () {
		state = GetComponentInParent<PlayerStateManager>();
		pmc = GetComponentInParent<PlayerMovementController>();
		// Player is invincible during attack
		pmc.hitbox.vulnerable = false;
		// Subscribe
		pmc.OnFall += DestroySelf;
	}
	
	// Update is called once per frame
	void Update () {
		time += Time.deltaTime;

		if (!startedAttack) {
			startedAttack = true;
			pmc.ForceMovement(null, upwardAttackSpeed);
			// TODO: Gravity mod? Increase speed and gravity? (So faster jump but slows down quickly?)
		}

		if (hitbox != null && !hit) {
			hitbox.Hit(gameObject);
			hitbox.emc.ForceMovement(null, upwardHitSpeed);
			hitbox = null;
			hit = true;
		}

		// Does this attack need a lifetime?
		/*if (time >= lifetime) {
			Destroy(gameObject, 0f);
		}*/
	}

	void DestroySelf(object sender, System.EventArgs e) {
		Destroy(gameObject, 0f);
	}

	/// <summary>
	/// This function is called when the MonoBehaviour will be destroyed.
	/// </summary>
	void OnDestroy() {
		pmc.hitbox.vulnerable = true;
		state.condState = state.condState.Remove(PlayerState.Condition.RestrictedAttacking);
		// Unsubscribe
		pmc.OnFall -= DestroySelf;
	}

	/// <summary>
	/// Sent when another object enters a trigger collider attached to this
	/// object (2D physics only).
	/// </summary>
	/// <param name="other">The other Collider2D involved in this collision.</param>
	void OnTriggerEnter2D(Collider2D other) {
		 OnTriggerStay2D(other);
	}

	/// <summary>
	/// Sent each frame where another object is within a trigger collider
	/// attached to this object (2D physics only).
	/// </summary>
	/// <param name="other">The other Collider2D involved in this collision.</param>
	void OnTriggerStay2D(Collider2D other) {
		if (other.gameObject.GetComponent<EnemyHitbox>()) {
			// Get enemy hitbox hit
			hitbox = other.gameObject.GetComponent<EnemyHitbox>();
		} else if (other.gameObject.layer == LayerMask.NameToLayer("NormalCollisions")) {
			// Destroy if colliding with wall
			Destroy(gameObject, 0f);
		}
	}
}
