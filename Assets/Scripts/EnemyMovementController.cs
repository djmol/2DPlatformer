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

	// Events
	public event System.EventHandler OnFall;
	public event System.EventHandler OnLand;
	public event System.EventHandler OnLateralCollision;
	public event System.EventHandler OnCeilingCollision;

	// Collisions
	Collider2D headCd;
	Collider2D bodyCd;
	Collider2D[] cds;
	Rect bodyBox;
	float angleLeeway = 5f;

	// Raycasting
	int hRays = 2;
	int vRays = 2;
	RaycastHit2D closestDownHitInfo;
	RaycastHit2D closestLatHatInfo;

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

	public void ForceMovement(float? x, float? y) {
		float veloX = (x == null) ? velocity.x : (float)x;
		float veloY = (y == null) ? velocity.y : (float)y;

		velocity = new Vector2(veloX, veloY);		
	}

	// Use this for initialization
	void Start () {
		headCd = transform.Find("Head").GetComponent<Collider2D>();
		bodyCd = transform.Find("Body").GetComponent<Collider2D>();
		rend = GetComponent<SpriteRenderer>();
		behaviorState = BehaviorState.Idle;
		idleBehaviorTime = GetNewIdleBehaviorTime();

		// Subscribe
		OnFall += OnFallEvent;
		OnLand += OnLandEvent;
		OnLateralCollision += OnLateralCollisionEvent;
		OnCeilingCollision += OnCeilingCollisionEvent;
	}

	/// <summary>
	/// This function is called when the MonoBehaviour will be destroyed.
	/// </summary>
	void OnDestroy() {
		// Unsubscribe
		OnFall -= OnFallEvent;
		OnLand -= OnLandEvent;
		OnLateralCollision -= OnLateralCollisionEvent;
		OnCeilingCollision -= OnCeilingCollisionEvent;
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
			if (state.Missing(MovementState.Falling)) {
				state = state.Include(MovementState.Falling);
				if (OnFall != null)
					OnFall(this, System.EventArgs.Empty);
			}
		}

		// Determine first and last rays
			Vector2 minDownRay = new Vector2(bodyBox.xMin, bodyBox.center.y);
			Vector2 maxDownRay = new Vector2(bodyBox.xMax, bodyBox.center.y);	

			// Calculate ray distance (if not grounded, set to current fall speed)
			float rayDownDistance = bodyBox.height / 2 + ((grounded) ? 0 : Mathf.Abs(velocity.y * Time.deltaTime));

			// Check below for ground
			RaycastHit2D[] hitDownInfo = new RaycastHit2D[vRays];
			bool hit = false;
			float closestDownHit = float.MaxValue;
			int closestDownHitIndex = 0;
			for (int i = 0; i < vRays; i++) {
				// Create and cast ray
				float lerpDistance = (float)i / (float)(vRays - 1);
				Vector2 rayOrigin = Vector2.Lerp(minDownRay, maxDownRay, lerpDistance);
				Ray2D ray = new Ray2D(rayOrigin, Vector2.down);
				hitDownInfo[i] = Physics2D.Raycast(rayOrigin, Vector2.down, rayDownDistance, RayLayers.downRay);
				
				// Check raycast results and keep track of closest ground hit
				if (hitDownInfo[i].fraction > 0) {
					hit = true;
					if (hitDownInfo[i].fraction < closestDownHit) {
						closestDownHit = hitDownInfo[i].fraction;
						closestDownHitIndex = i;
						closestDownHitInfo = hitDownInfo[i];
					}
				}
			}

			// If enemy hits ground, snap to the closest ground
			if (hit) {
				// Check if enemy is landing this frame
				if (state.Has(MovementState.Falling) && state.Missing(MovementState.Landing)) {
					if (OnLand != null)
						OnLand(this, System.EventArgs.Empty);
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
			if (Mathf.Abs(closestDownHitInfo.normal.x) > 0.1f) {
				float friction = 0.7f;
				newVelocityX = Mathf.Clamp((newVelocityX - (closestDownHitInfo.normal.x * friction)), -maxSpeed, maxSpeed);
				Vector2 newPosition = transform.position;
				newPosition.y += -closestDownHitInfo.normal.x * Mathf.Abs(newVelocityX) * Time.deltaTime * ((newVelocityX - closestDownHitInfo.normal.x > 0) ? 1 : -1);
				transform.position = newPosition;
				state = state.Remove(MovementState.Landing);			
			} 

			velocity = new Vector2(newVelocityX, velocity.y);

			// Lateral collisions
			bool lateralCollision = false;

			// Determine first and last rays
			Vector2 minLatRay = new Vector2(bodyBox.center.x, bodyBox.yMin);
			Vector2 maxLatRay = new Vector2(bodyBox.center.x, bodyBox.yMax);
			
			// Calculate ray distance and determine direction of movement
			float rayDistance = bodyBox.width / 2 + Mathf.Abs(newVelocityX * Time.deltaTime);
			Vector2 rayDirection = (newVelocityX > 0) ? Vector2.right : Vector2.left;

			RaycastHit2D[] latHitInfo = new RaycastHit2D[hRays];
			float closestLatHit = float.MaxValue;
			int closestLatHitIndex = 0;
			float lastFraction = 0;
			int numHits = 0; // for debugging
			for (int i = 0; i < hRays; i++) {
				// Create and cast ray
				float lerpDistance = (float)i / (float)(hRays - 1);
				Vector2 rayOrigin = Vector2.Lerp(minLatRay, maxLatRay, lerpDistance);
				latHitInfo[i] = Physics2D.Raycast(rayOrigin, rayDirection, rayDistance, RayLayers.sideRay);
				Debug.DrawRay(rayOrigin, rayDirection * rayDistance, Color.cyan, Time.deltaTime);
				// Check raycast results
				if (latHitInfo[i].fraction > 0) {
					lateralCollision = true;
					numHits++; // for debugging
					if (latHitInfo[i].fraction < closestLatHit) {
						closestLatHit = latHitInfo[i].fraction;
						closestLatHitIndex = i;
					}
					// If more than one ray hits, check the slope of what player is colliding with
					if (lastFraction > 0) {
						float slopeAngle = Vector2.Angle(latHitInfo[i].point - latHitInfo[i - 1].point, Vector2.right);
						// If we hit a wall, snap to it
						if (Mathf.Abs(slopeAngle - 90) < angleLeeway) {
							transform.Translate(rayDirection * (latHitInfo[i].distance - bodyBox.width / 2));
							if (OnLateralCollision != null)
								OnLateralCollision(this, System.EventArgs.Empty);
							velocity = new Vector2(0, velocity.y);

							break;
						}
					}
					lastFraction = latHitInfo[i].fraction;
				}
			}

			// TODO: Ceiling collisions?
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

	void OnFallEvent(object sender, System.EventArgs e) {

	}

	void OnLandEvent(object sender, System.EventArgs e) {

	}

	void OnLateralCollisionEvent(object sender, System.EventArgs e) {

	}

	void OnCeilingCollisionEvent(object sender, System.EventArgs e) {

	}

	void ApplyGroundEffects() {
		// TODO: Implement me!
	} 

	float GetNewIdleBehaviorTime() {
		return idleTime + Random.Range(-idleTimeVar, idleTimeVar);
	}

}
