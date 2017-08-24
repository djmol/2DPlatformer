﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Uppercut : PlayerAttack {

	public float lifetime;
	public float upwardSpeed;
	float time;

	EnemyHitbox hitbox;
	bool hit = false;

	PlayerMovementController pmc;
	bool startedAttack = false;

	// Use this for initialization
	void Start () {
		pmc = GetComponentInParent<PlayerMovementController>();
	}
	
	// Update is called once per frame
	void Update () {
		time += Time.deltaTime;

		if (!startedAttack) {
			startedAttack = true;
			pmc.ForceMovement(null, upwardSpeed);
			// Gravity mod? Increase speed and gravity? (So faster jump but slows down quickly?)
		}

		if (hitbox != null && !hit) {
			hitbox.Hit(gameObject);
			hitbox = null;
			hit = true;
		}

		if (time >= lifetime) {
			Destroy(gameObject, 0f);
		}
	}

	override public void HitTaken() {
		Debug.Log("hit taken");
		//Destroy(gameObject, 0f);
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
