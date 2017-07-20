﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovementController : MonoBehaviour {

	/* Known bugs:
	/* - If bottom of character falls down through bottom of soft-bottom platform, character warps up to lands on it 
	 */

	// Resources: Travis Martin's platformer physics
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
	bool canJump = true;
	bool jumpPressedLastFrame = false;
	float prevJumpDownTime = 0f;
	float jumpPressLeeway = 0.1f;

	// Slopes
	float angleLeeway = 5f;
	RaycastHit2D closestHitInfo;
	
	// Checks
	bool grounded = false;
	bool falling = false;

	// Raycasting
	int hRays = 6;
	int vRays = 4;
	float margin = .02f;

	// Moving platforms
	MovingPlatform movingPlatform;

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
		// Set flag to prevent player from jumping before character lands
		bool landing = false;
		
		// If player is not grounded, apply gravity
		if (!grounded) {
			velocity = new Vector2(velocity.x, Mathf.Max(velocity.y - gravity, -maxFall));
		}

		// Check if player is currently falling
		if (velocity.y < 0) {
			falling = true;
		}

		// Check for collisions below
		// (No need to check if player is in mid-air but not falling)
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
						closestHitInfo = hitInfo[i];
					}
				}
			}

			// If player hits ground, snap to the closest ground
			if (hit) {
				// Check if player is landing this frame
				if (falling) {
					SendMessage("OnLand", SendMessageOptions.DontRequireReceiver);
					landing = true;
				}
				grounded = true;
				falling = false;
				Debug.DrawLine(box.center, hitInfo[closestHitIndex].point, Color.white, 1f);
				transform.Translate(Vector2.down * (hitInfo[closestHitIndex].distance - box.height / 2));
				velocity = new Vector2(velocity.x, 0);			
				
				// Check if player is on a moving platform
				MovingPlatform newMovingPlatform = hitInfo[closestHitIndex].transform.parent.gameObject.GetComponent<MovingPlatform>();
				if (newMovingPlatform != null) {
					movingPlatform = newMovingPlatform;
					movingPlatform.GetOnPlatform(gameObject);
				}
			}
			else {
				grounded = false;
				if (movingPlatform != null) {
					movingPlatform.GetOffPlatform(gameObject);
					movingPlatform = null;
				}
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

			// Account for slope
			if (Mathf.Abs(closestHitInfo.normal.x) > 0.1f) {
				velocity = new Vector2(velocity.x - (closestHitInfo.normal.x * 0.7f), velocity.y);
				Vector2 newPosition = transform.position;
				newPosition.y += -closestHitInfo.normal.x * Mathf.Abs(velocity.x) * Time.deltaTime * ((velocity.x - closestHitInfo.normal.x > 0) ? 1 : -1);
				transform.position = newPosition;
			} 
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
		// (This condition will always be true, of course. It's temporary, but allows for moving platforms to push you while not riding them.)
		if (velocity.x != 0 || velocity.x == 0) {
			// Determine first and last rays
			Vector2 minRay = new Vector2(box.center.x, box.yMin);
			Vector2 maxRay = new Vector2(box.center.x, box.yMax);
			
			// Calculate ray distance and determine direction of movement
			float rayDistance = box.width / 2 + Mathf.Abs(newVelocityX * Time.deltaTime);
			Vector2 rayDirection = (newVelocityX > 0) ? Vector2.right : Vector2.left;

			RaycastHit2D[] hitInfo = new RaycastHit2D[hRays];
			float closestHit = float.MaxValue;
			int closestHitIndex = 0;
			float lastFraction = 0;
			int numHits = 0; // for debugging
			for (int i = 0; i < hRays; i++) {
				// Create and cast ray
				float lerpDistance = (float)i / (float)(hRays - 1);
				Vector2 rayOrigin = Vector2.Lerp(minRay, maxRay, lerpDistance);
				Ray2D ray = new Ray2D(rayOrigin, rayDirection);
				hitInfo[i] = Physics2D.Raycast(rayOrigin, rayDirection, rayDistance, RayLayers.sideRay);
				Debug.DrawRay(rayOrigin, rayDirection * rayDistance, Color.cyan, Time.deltaTime);
				// Check raycast results
				/*if (hitInfo[i].fraction > 0) {
					numHits++; // for debugging
					if (hitInfo[i].fraction < closestHit) {
						closestHit = hitInfo[i].fraction;
						closestHitIndex = i;
					}
					// If more than one ray hits, check the slope of what player is colliding with
					if (lastFraction > 0) {
						float slopeAngle = Vector2.Angle(hitInfo[i].point - hitInfo[i - 1].point, Vector2.right);
						Debug.Log(Mathf.Abs(slopeAngle)); // for debugging
						// If we hit a wall, snap to it
						if (Mathf.Abs(slopeAngle - 90) < angleLeeway) {
							transform.Translate(rayDirection * (hitInfo[i].distance - box.width / 2));
							SendMessage("OnLateralCollision", SendMessageOptions.DontRequireReceiver);
							velocity = new Vector2(0, velocity.y);
							break;
						}
					}
					lastFraction = hitInfo[i].fraction;
				}*/
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
		if (canJump && !landing) {
			bool input = Input.GetButton("Jump");
			
			// Prevent player from holding down jump to autobounce
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
		}
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
