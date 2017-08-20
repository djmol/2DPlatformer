using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Shot : PlayerAttack {

	public float speed;
	public float lifetime;
	public Vector2 direction;
	float time = 0;
	Vector2 velocity;

	EnemyHitbox hitbox;
	bool hit = false;

	// Use this for initialization
	void Start () {
		velocity = direction * speed;
	}
	
	// Update is called once per frame
	void Update () {
		time += Time.deltaTime;

		if (hitbox != null && !hit) {
			hitbox.Hit(gameObject);
			hitbox = null;
			hit = true;
		}

		if (time >= lifetime) {
			Destroy(gameObject, 0f);
		}
	}

	/// <summary>
	/// LateUpdate is called every frame, if the Behaviour is enabled.
	/// It is called after all Update functions have been called.
	/// </summary>
	void LateUpdate() {
		transform.Translate(velocity * Time.deltaTime);
	}

	override public void HitTaken() {
		Destroy(gameObject, 0f);
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
