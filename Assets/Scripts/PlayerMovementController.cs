﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovementController : MonoBehaviour {

	// Adapted from Travis Martin's platformer physics
	// Physics properties
	public float accel = 4f;
	public float maxSpeed = 150f;
	public float gravity = 6f;
	public float maxFall = 200f;
	public float jumpSpeed = 200f;

	// For collisions and convenience
	Collider2D cd;
	Rect box;
	int layerMask;

	// Player movement speed
	Vector2 velocity;

	// Inputs
	bool lastInput;

	// Jumping
	bool jumpPressedLastFrame = false;
	float prevJumpDownTime = 0f;
	float jumpPressLeeway = 0.1f;
	
	// Checks
	bool grounded = false;
	bool falling = false;

	// Raycasting
	int hRays = 6;
	int vRays = 4;
	float margin = .02f;

	// Use this for initialization
	void Start () {
		cd = GetComponent<BoxCollider2D>();
		layerMask = 1 << LayerMask.NameToLayer("NormalCollisions");
	}
	
	// Update is called once per frame
	void Update () {
	}

	/// <summary>
	/// This function is called every fixed framerate frame, if the MonoBehaviour is enabled.
	/// </summary>
	void FixedUpdate() {
		// For convenience
		box = new Rect(
			cd.bounds.min.x,
			cd.bounds.min.y,
			cd.bounds.size.x,
			cd.bounds.size.y
		);
		
		// --- Gravity & Ground Check ---
		// If player is not grounded, apply gravity
		if (!grounded) {
			velocity = new Vector2(velocity.x, Mathf.Max(velocity.y - gravity, -maxFall));
		}

		// Check if player is currently falling
		if (velocity.y < 0) {
			falling = true;
		}

		// Check for collisions below
		// (No need to check if we're in mid-air but not falling)
		if (grounded || falling) {
			// Determine first and last rays
			Vector2 minRay = new Vector2(box.xMin + margin, box.center.y);
			Vector2 maxRay = new Vector2(box.xMax - margin, box.center.y);	

			// Calculate ray distance (if not grounded, set to current fall speed)
			float rayDistance = box.height / 2 + ((grounded) ? margin : Mathf.Abs(velocity.y * Time.deltaTime));

			// Check below for ground
			RaycastHit2D[] hitInfo = new RaycastHit2D[vRays];
			bool hit = false;
			float closestHit = float.MaxValue;
			int closestHitIndex = 0;
			for (int i = 0; i < vRays; i++) {
				// Create and cast ray
				float lerpDistance = (float)i / (float)(vRays - 1);
				Vector2 rayOrigin = Vector2.Lerp(minRay, maxRay, lerpDistance);
				Ray2D ray = new Ray2D(rayOrigin, Vector2.down);
				hitInfo[i] = Physics2D.Raycast(rayOrigin, Vector2.down, rayDistance, RayLayers.downRay);
				
				// Check raycast results and keep track of closest ground hit
				if (hitInfo[i].fraction > 0) {
					hit = true;
					if (hitInfo[i].fraction < closestHit) {
						closestHit = hitInfo[i].fraction;
						closestHitIndex = i;
					}
				}
			}

			// If we hit ground, snap to the closest ground
			if (hit) {
				grounded = true;
				falling = false;
				transform.Translate(Vector2.down * (hitInfo[closestHitIndex].distance - box.height / 2));
				SendMessage("OnLand", SendMessageOptions.DontRequireReceiver);
				velocity = new Vector2(velocity.x, 0);
			}
			else {
				grounded = false;
			}
		}

		// --- Lateral Movement & Collisions ---
		// Get input
		float hAxis = Input.GetAxisRaw("Horizontal");
		
		float newVelocityX = velocity.x;

		// Move if input exists
		if (hAxis != 0) {
			newVelocityX += accel * hAxis;
			newVelocityX = Mathf.Clamp(newVelocityX, -maxSpeed, maxSpeed);
		}
		// Decelerate if moving without input
		else if (velocity.x != 0) {
			int decelDir = (velocity.x > 0) ? -1 : 1;
			// Ensure player doesn't decelerate past zero
			newVelocityX += (velocity.x > 0) ?
				((newVelocityX + accel * decelDir) < 0) ?
			 		-newVelocityX : accel * decelDir
				: ((newVelocityX + accel * decelDir) > 0) ?
				    -newVelocityX : accel * decelDir;
		}

		velocity = new Vector2(newVelocityX, velocity.y);

		// Check for lateral collisions
		if (velocity.x != 0) {
			// Determine first and last rays
			Vector2 minRay = new Vector2(box.center.x, box.yMin + margin);
			Vector2 maxRay = new Vector2(box.center.x, box.yMax - margin);
			
			// Calculate ray distance and determine direction of movement
			float rayDistance = box.width / 2 + Mathf.Abs(newVelocityX * Time.deltaTime);
			Vector2 rayDirection = (newVelocityX > 0) ? Vector2.right : Vector2.left;

			RaycastHit2D[] hitInfo = new RaycastHit2D[hRays];
			bool hit = false;
			float closestHit = float.MaxValue;
			int closestHitIndex = 0;
			for (int i = 0; i < hRays; i++) {
				// Create and cast ray
				float lerpDistance = (float)i / (float)(hRays - 1);
				Vector2 rayOrigin = Vector2.Lerp(minRay, maxRay, lerpDistance);
				Ray2D ray = new Ray2D(rayOrigin, rayDirection);
				hitInfo[i] = Physics2D.Raycast(rayOrigin, rayDirection, rayDistance, RayLayers.onlyCollisions);
			
				// Check raycast results
				if (hitInfo[i].fraction > 0) {
					hit = true;
					if (hitInfo[i].fraction < closestHit) {
						closestHit = hitInfo[i].fraction;
						closestHitIndex = i;
					}
				}
			}

			// If we hit something, snap to it
			if (hit) {
				transform.Translate(rayDirection * (hitInfo[closestHitIndex].distance - box.width/2));
				SendMessage("OnLateralCollision", SendMessageOptions.DontRequireReceiver);
				velocity = new Vector2(0, velocity.y);
			}
		}

		// --- Ceiling Check ---
		// Only check if we're grounded or jumping
		if (grounded || velocity.y > 0) {
			// Determine first and last rays
			Vector2 minRay = new Vector2(box.xMin + margin, box.center.y);
			Vector2 maxRay = new Vector2(box.xMax - margin, box.center.y);

			// Calculate ray distance (if not grounded, set to current jump speed)			
			float rayDistance = box.height / 2 + ((grounded) ? margin : velocity.y * Time.deltaTime);
			
			// Check above for ceiling
			RaycastHit2D[] hitInfo = new RaycastHit2D[vRays];
			bool hit = false;
			float closestHit = float.MaxValue;
			int closestHitIndex = 0;
			for (int i = 0; i < vRays; i++) {
				// Create and cast ray
				float lerpDistance = (float)i / (float)(vRays - 1);
				Vector2 rayOrigin = Vector2.Lerp(minRay, maxRay, lerpDistance);
				Ray2D ray = new Ray2D(rayOrigin, Vector2.up);
				hitInfo[i] = Physics2D.Raycast(rayOrigin, Vector2.up, rayDistance, RayLayers.upRay);

				// Check raycast results and keep track of closest ceiling hit
				if (hitInfo[i].fraction > 0) {
					hit = true;
					if (hitInfo[i].fraction < closestHit) {
						closestHit = hitInfo[i].fraction;
						closestHitIndex = i;
					}
				}
			}

			// If we hit ceiling, snap to the closest ceiling
			// TODO: Maybe give rebound instead of snapping?
			if (hit) {
				transform.Translate(Vector3.up * (hitInfo[closestHitIndex].distance - box.height / 2));
				SendMessage("OnCeilingCollision", SendMessageOptions.DontRequireReceiver);
				velocity = new Vector2(velocity.x, 0);
			}
		}

		// --- Jumping ---
		bool input = Input.GetButton("Jump");
		if (input && !jumpPressedLastFrame) {
			prevJumpDownTime = Time.time;
		}
		else if (!input) {
			prevJumpDownTime = 0f;
		}

		if (grounded && Time.time - prevJumpDownTime < jumpPressLeeway) {
			velocity = new Vector2(velocity.x, jumpSpeed);
			prevJumpDownTime = 0f;
		}

		jumpPressedLastFrame = input;
		
		// Apply jump
		transform.Translate(velocity * Time.deltaTime);
	}

	void OnLand() {
		Debug.Log("Landed!");
	}

	void OnLateralCollision() {
		Debug.Log("Lateral collision!");
	}

	void OnCeilingCollision() {
		Debug.Log("Ceiling collision!");
	}

	/// <summary>
	/// LateUpdate is called every frame, if the Behaviour is enabled.
	/// It is called after all Update functions have been called.
	/// </summary>
	void LateUpdate() {
		transform.Translate(velocity * Time.deltaTime);
	}

	/// <summary>
	/// OnGUI is called for rendering and handling GUI events.
	/// This function can be called multiple times per frame (one call per event).
	/// </summary>
	void OnGUI()
	{
		GUI.Box(new Rect(5,5,80,40), "vel.X: " + velocity.x + "\nvel.Y: " + velocity.y);
	}
}
