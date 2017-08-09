using Extensions;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyMovementController : MonoBehaviour {

	// Physics properties
	public float accel = .5f;
	public float maxSpeed = 10f;
	public float gravity = 6f;
	public float maxFall = 110f;
	float finalAccel;
	Vector2 velocity;

	// Collisions
	Collider2D headCd;
	Collider2D bodyCd;
	Collider2D[] cds;
	Rect bodyBox;

	// Raycasting
	int hRays = 4;
	int vRays = 4;
	RaycastHit2D closestHitInfo;

	// Checks
	bool grounded;

	// Enemy movement state
	[System.Flags]
	enum MovementState {
		Idle = 0x01,
		Moving = 0x02,
		Falling = 0x04,
		Landing = 0x08,
		Jumping = 0x10
	};
	MovementState state;

	// Enemy behavior
	float behaviorTime = 0f;
	float hAxis = 1f;
	float idleBehaviorTime = 0f;
	float idleTime = 3f;
	float idleTimeVar = 2f;

	// Enemy behavior state
	[System.Flags]
	enum BehaviorState {
		Idle = 0x01,
	};
	BehaviorState behaviorState;

	// Appearance
	SpriteRenderer rend;

	// Use this for initialization
	void Start () {
		headCd = transform.Find("Head").GetComponent<Collider2D>();
		bodyCd = transform.Find("Body").GetComponent<Collider2D>();
		rend = GetComponent<SpriteRenderer>();
		behaviorState = BehaviorState.Idle;
		idleBehaviorTime = GetNewIdleBehaviorTime();
	}
	
	// Update is called once per frame
	void Update () {
		
	}

	/// <summary>
	/// This function is called every fixed framerate frame, if the MonoBehaviour is enabled.
	/// </summary>
	void FixedUpdate() {
		// For convenience
		bodyBox = new Rect(
			bodyCd.bounds.min.x,
			bodyCd.bounds.min.y,
			bodyCd.bounds.size.x,
			bodyCd.bounds.size.y
		);

		// Set default values
		finalAccel = accel;

		// --- Gravity ---
		// If enemy is not grounded, apply gravity
		if (!grounded) {
			velocity = new Vector2(velocity.x, Mathf.Max(velocity.y - gravity, -maxFall));
		}

		// Check if enemy is falling
		if (velocity.y < 0) {
			state = state.Include(MovementState.Falling);
		}

		// Determine first and last rays
			Vector2 minRay = new Vector2(bodyBox.xMin, bodyBox.center.y);
			Vector2 maxRay = new Vector2(bodyBox.xMax, bodyBox.center.y);	

			// Calculate ray distance (if not grounded, set to current fall speed)
			float rayDistance = bodyBox.height / 2 + ((grounded) ? 0 : Mathf.Abs(velocity.y * Time.deltaTime));

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

			// If enemy hits ground, snap to the closest ground
			if (hit) {
				// Check if enemy is landing this frame
				if (state.Has(MovementState.Falling)) {
					SendMessage("OnLand", SendMessageOptions.DontRequireReceiver);
					state = state.Include(MovementState.Landing);
					state = state.Remove(MovementState.Jumping);
				}
				grounded = true;
				state = state.Remove(MovementState.Falling);		
				velocity = new Vector2(velocity.x, 0);
			} else {
				grounded = false;
			}

			// --- Behavior ---
			// Idle behavior
			if (behaviorState.Has(BehaviorState.Idle)) {
				behaviorTime += Time.deltaTime;
				
				// Set new behavior
				if (behaviorTime > idleBehaviorTime) {
					float newHAxis = Random.Range(-1,1);
					// Flip sprite if changing direction of movement
					if (newHAxis != 0 && newHAxis != ((rend.flipX) ? -1 : 1))
						rend.flipX = !rend.flipX;
					hAxis = newHAxis;
					behaviorTime = 0f;
					idleBehaviorTime = GetNewIdleBehaviorTime();
					Debug.Log("Rolled: " + hAxis + " for " + idleBehaviorTime);
				}
			}

			// --- Lateral Collisions & Movement ---
			ApplyGroundEffects();

			// Move horizontally
			float newVelocityX = velocity.x;
			if (hAxis != 0) {
				newVelocityX += finalAccel * hAxis;
				newVelocityX = Mathf.Clamp(newVelocityX, -maxSpeed, maxSpeed);
			}
			// Decelerate if moving without input
			else if (velocity.x != 0) {
				int decelDir = (velocity.x > 0) ? -1 : 1;
				// Ensure enemy doesn't decelerate past zero
				newVelocityX += (velocity.x > 0) ?
					((newVelocityX + finalAccel * decelDir) < 0) ?
						-newVelocityX : finalAccel * decelDir
					: ((newVelocityX + finalAccel * decelDir) > 0) ?
						-newVelocityX : finalAccel * decelDir;
			}
			// Account for slope
			if (Mathf.Abs(closestHitInfo.normal.x) > 0.1f) {
				float friction = 0.7f;
				newVelocityX = Mathf.Clamp((newVelocityX - (closestHitInfo.normal.x * friction)), -maxSpeed, maxSpeed);
				Vector2 newPosition = transform.position;
				newPosition.y += -closestHitInfo.normal.x * Mathf.Abs(newVelocityX) * Time.deltaTime * ((newVelocityX - closestHitInfo.normal.x > 0) ? 1 : -1);
				transform.position = newPosition;
				state = state.Remove(MovementState.Landing);			
			} 
			Debug.Log(newVelocityX);
			velocity = new Vector2(newVelocityX, velocity.y);

			// TODO: Horizontal collisions
			// TODO: Check if accel/decel is making enemy sink into slopes
	}

	/// <summary>
	/// LateUpdate is called every frame, if the Behaviour is enabled.
	/// It is called after all Update functions have been called.
	/// </summary>
	void LateUpdate() {
		transform.Translate(velocity * Time.deltaTime);
		if (velocity != Vector2.zero) {
			state = state.Include(MovementState.Moving);
		} else {
			state = state.Remove(MovementState.Moving);
		}
	}

	void OnLand() {}

	void ApplyGroundEffects() {
		// TODO: Implement me!
	} 

	float GetNewIdleBehaviorTime() {
		return idleTime + Random.Range(-idleTimeVar, idleTimeVar);
	}
}
