using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Shot : PlayerAttack {

	public float speed;
	public float lifetime;
	public Vector2 direction;
	float time = 0;
	Vector2 velocity;

	// Use this for initialization
	void Start () {
		velocity = direction * speed;
	}
	
	// Update is called once per frame
	void Update () {
		time += Time.deltaTime;

		if (time >= lifetime) {
			Destroy(gameObject, 0f);
		}

		// TODO: Add collider, collisions, damage
	}

	/// <summary>
	/// This function is called every fixed framerate frame, if the MonoBehaviour is enabled.
	/// </summary>
	void FixedUpdate() {

	}

	/// <summary>
	/// LateUpdate is called every frame, if the Behaviour is enabled.
	/// It is called after all Update functions have been called.
	/// </summary>
	void LateUpdate() {
		transform.Translate(velocity * Time.deltaTime);
	}
}
