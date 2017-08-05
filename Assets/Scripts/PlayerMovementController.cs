using Extensions;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayerMovementController : MonoBehaviour {

	/* Known bugs:
	/* - If bottom of character falls down through bottom of soft-bottom platform, character warps up to lands on it. Same in vice-versa on soft-top.
	 */

	// Resources: Travis Martin's platformer physics
	// Physics properties
	public float accel = 8f;
	public float maxSpeed = 50f;
	public float gravity = 6f;
	public float maxFall = 110f;
	public float jumpSpeed = 110f;
	public float dashSpeed = 80f;
	public float dashTime = .15f;
	float finalAccel;
	float finalJumpSpeed;

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
	bool canDoubleJump = true;
	bool jumpPressedLastFrame = false;
	float prevJumpDownTime = 0f;
	float jumpPressLeeway = 0.1f;

	// Dashing
	float currentDashTime = 0f;
	bool canDash = true;
	bool dashReady = true;
	bool wallDashing = false;
	bool exitingDash = false;

	// Wall sticking/jumping
	public float wallSlideSpeed = 40f;
	public float wallSlideDelay = .05f;
	public float jumpAwayDistance = 50f;
	bool lateralCollision = false;
	bool canStickWall = true;
	Vector2 wallDirection;
	float wallSlideTime = 0f;
	
	// Slopes
	float angleLeeway = 5f;
	RaycastHit2D closestHitInfo;
	
	// Checks
	bool grounded = false;

	// Raycasting
	int hRays = 8;
	int vRays = 8;
	float margin = .02f;

	// Moving platforms
	MovingPlatform movingPlatform;

	// Ground effects
	IEnumerable<SpecialGround> groundTypes;

	// Player movement state
	[System.Flags]
	enum MovementState {
		Idle = 0x01,
		Moving = 0x02,
		Falling = 0x04,
		Landing = 0x08,
		Jumping = 0x10,
		Dashing = 0x20,
		WallSticking = 0x40,
		WallSliding = 0x80,
	};
	MovementState state;

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

		// Set default values
		finalJumpSpeed = jumpSpeed;
		finalAccel = accel;
		
		// Get input and set idle state if applicable
		float hAxis = Input.GetAxisRaw("Horizontal");
		bool dashInput = Input.GetButtonDown("Fire1");
		bool jumpInput = Input.GetButton("Jump");
		if (hAxis == 0f && !dashInput && !jumpInput) {
			state = state.Include(MovementState.Idle);
		} else {
			state = state.Remove(MovementState.Idle);
		}

		// --- Gravity & Ground Check ---
		// Set flag to prevent player from jumping before character lands
		state = state.Remove(MovementState.Landing);
		
		// If player is not grounded or sticking to wall, apply gravity
		if (!grounded && state.Missing(MovementState.WallSticking) && state.Missing(MovementState.WallSliding)) {
			velocity = new Vector2(velocity.x, Mathf.Max(velocity.y - gravity, -maxFall));
		}

		// Check if player is currently falling
		if (velocity.y < 0) {
			state = state.Include(MovementState.Falling);
		}

		// Check for collisions below
		// (No need to check if player is in mid-air but not falling)
		if (grounded || state.Has(MovementState.Falling)) {
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
				if (state.Has(MovementState.Falling)) {
					SendMessage("OnLand", SendMessageOptions.DontRequireReceiver);
					state = state.Include(MovementState.Landing);
					state = state.Remove(MovementState.Jumping);
				}
				grounded = true;
				state = state.Remove(MovementState.Falling);		
				state = state.Remove(MovementState.WallSticking | MovementState.WallSliding);
				exitingDash = false;
				Debug.DrawLine(box.center, hitInfo[closestHitIndex].point, Color.white, 1f);
				transform.Translate(Vector2.down * (hitInfo[closestHitIndex].distance - box.height / 2));
				velocity = new Vector2(velocity.x, 0);			
				
				// Check if player is on a moving platform
				MovingPlatform newMovingPlatform = hitInfo[closestHitIndex].transform.parent.gameObject.GetComponent<MovingPlatform>();
				if (newMovingPlatform != null) {
					movingPlatform = newMovingPlatform;
					movingPlatform.GetOnPlatform(gameObject);
				}

				// Check ground for special attributes
				groundTypes = hitInfo[closestHitIndex].collider.gameObject.GetComponents<SpecialGround>();
			}
			else {
				grounded = false;
				// Clear ground properties
				groundTypes = Enumerable.Empty<SpecialGround>();
				if (movingPlatform != null) {
					movingPlatform.GetOffPlatform(gameObject);
					movingPlatform = null;
				}
			}
		}

		if (state.Has(MovementState.Landing)) {
			state = state.Remove(MovementState.Dashing);
			exitingDash = false;
		}

		// --- Lateral Movement & Collisions ---
		// Get input
		float newVelocityX = velocity.x;
		ApplyGroundEffects();

		// Move if input exists
		if (hAxis != 0) {
			newVelocityX += finalAccel * hAxis;
			// Clamp speed to max if not exiting a dash (in order to keep air momentum)
			newVelocityX = Mathf.Clamp(newVelocityX, -maxSpeed, maxSpeed);

			// Dash
			if (canDash) {
				if (dashInput) {
					if (grounded && dashReady) {
						StartCoroutine(Dash());
						newVelocityX = dashSpeed * hAxis;
					} else if ((state.Has(MovementState.WallSticking) || state.Has(MovementState.WallSliding)) & dashReady) {
						StartCoroutine(Dash());
						//newVelocityX = dashSpeed * hAxis;
					}
				}
			}

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
				((newVelocityX + finalAccel * decelDir) < 0) ?
			 		-newVelocityX : finalAccel * decelDir
				: ((newVelocityX + finalAccel * decelDir) > 0) ?
				    -newVelocityX : finalAccel * decelDir;
		}

		velocity = new Vector2(newVelocityX, velocity.y);

		// Check for lateral collisions
		lateralCollision = false;

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
				hitInfo[i] = Physics2D.Raycast(rayOrigin, rayDirection, rayDistance, RayLayers.sideRay);
				Debug.DrawRay(rayOrigin, rayDirection * rayDistance, Color.cyan, Time.deltaTime);
				// Check raycast results
				if (hitInfo[i].fraction > 0) {
					lateralCollision = true;
					numHits++; // for debugging
					if (hitInfo[i].fraction < closestHit) {
						closestHit = hitInfo[i].fraction;
						closestHitIndex = i;
					}
					// If more than one ray hits, check the slope of what player is colliding with
					if (lastFraction > 0) {
						float slopeAngle = Vector2.Angle(hitInfo[i].point - hitInfo[i - 1].point, Vector2.right);
						//Debug.Log(Mathf.Abs(slopeAngle)); // for debugging
						// If we hit a wall, snap to it
						if (Mathf.Abs(slopeAngle - 90) < angleLeeway) {
							transform.Translate(rayDirection * (hitInfo[i].distance - box.width / 2));
							SendMessage("OnLateralCollision", SendMessageOptions.DontRequireReceiver);
							velocity = new Vector2(0, velocity.y);

							// Wall sticking
							if (canStickWall && !grounded && (state.Missing(MovementState.WallSticking) && state.Missing(MovementState.WallSliding))) {
								// Only stick if moving towards wall
								if (hAxis != 0 && ((hAxis < 0) == (rayDirection.x < 0))) {
								state = state.Include(MovementState.WallSticking);
								state = state.Remove(MovementState.Jumping);
								wallDirection = rayDirection;
								velocity = new Vector2(0,0);
								wallSlideTime = Time.time + wallSlideDelay;
								}
							}

							break;
						}
					}
					lastFraction = hitInfo[i].fraction;
				}
			}

			// Wall sticking
			if (state.Has(MovementState.WallSticking) || state.Has(MovementState.WallSliding)) {
				velocity = new Vector2(0, velocity.y);
				bool onWall = false;

				// Check for wall regardless of horizontal velocity (allows player to hold direction away from wall)
				for (int i = 0; i < hRays; i++) {
					// Create and cast ray
					float lerpDistance = (float)i / (float)(hRays - 1);
					Vector2 rayOrigin = Vector2.Lerp(minRay, maxRay, lerpDistance);
					hitInfo[i] = Physics2D.Raycast(rayOrigin, wallDirection, (box.width / 2) + .001f, RayLayers.sideRay);
					if (hitInfo[i].fraction > 0) {
						onWall = true;		
					}
				}
				
				// If no wall hit, end wallstick/slide
				if (!onWall) {
					state = state.Remove(MovementState.WallSticking | MovementState.WallSliding);
				}
			}

			// Wall sliding
			if (state.Has(MovementState.WallSticking) && Time.time >= wallSlideTime) {
				velocity = Vector2.down * wallSlideSpeed;
				state = state.Remove(MovementState.WallSticking);
				state = state.Include(MovementState.WallSliding);
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
		if (canJump && state.Missing(MovementState.Landing)) {
			// Prevent player from holding down jump to autobounce
			if (jumpInput && !jumpPressedLastFrame) {
				prevJumpDownTime = Time.time;
			}
			else if (!jumpInput) {
				prevJumpDownTime = 0f;
			}

			if (Time.time - prevJumpDownTime < jumpPressLeeway) {
				// Normal jump
				if (grounded) {
					velocity = new Vector2(velocity.x, finalJumpSpeed);
					prevJumpDownTime = 0f;
					canDoubleJump = true;
				} 
				// Wall jump
				else if (state.Has(MovementState.WallSticking) || state.Has(MovementState.WallSliding)) {
					velocity = new Vector2(-wallDirection.x * jumpAwayDistance, finalJumpSpeed * .8f);
					state = state.Remove(MovementState.WallSticking | MovementState.WallSliding);
				} 
				// Double jump
				else if (!grounded && dashReady && canDoubleJump) {
					velocity = new Vector2(velocity.x, finalJumpSpeed * .75f);
					prevJumpDownTime = 0f;
					canDoubleJump = false;
				}
				state = state.Remove(MovementState.Falling);
				state = state.Include(MovementState.Jumping);
			}
			jumpPressedLastFrame = jumpInput;
		}

		//Debug.Log(state);
	}

	IEnumerator Dash() {
		dashReady = false;
		float normalMaxSpeed = maxSpeed;
		yield return EnterDash();
		yield return ExitDash();
		FinishDash(normalMaxSpeed);
	}

	IEnumerator EnterDash () {
		// Dash for set amount of time
		currentDashTime = dashTime;
		while (currentDashTime > 0.0) {
			state = state.Include(MovementState.Dashing);
			maxSpeed = dashSpeed;
			currentDashTime -= Time.deltaTime;
			yield return null;
		}
	}

	IEnumerator ExitDash () {
		// Terminate dash, but maintain max speed from dash until dash is exited
		// This is to keep momentum in mid-air once the dash has ended
		state = state.Remove(MovementState.Dashing);
		exitingDash = true;
		while (exitingDash) {
			yield return null;
		}
	}

	void FinishDash (float normalMaxSpeed) {
		// Revert to normal max speed
		maxSpeed = normalMaxSpeed;
		dashReady = true;
	}


	void OnLand() {
		//Debug.Log("Landed!");
	}

	void OnLateralCollision() {
		Debug.Log("Lateral collision!");
	}

	void OnCeilingCollision() {
		Debug.Log("Ceiling collision!");
	}

	void ApplyGroundEffects() {
		foreach (var specialGround in groundTypes) {
			System.Type type = specialGround.GetType();
			// Icy ground
			if (type.Equals(typeof(IcyGround))) {
				finalAccel = accel * ((IcyGround)specialGround).accelerationRate;
			} 
			// Bouncy ground
			else if (type.Equals(typeof(BouncyGround))) {
				if (state.Missing(MovementState.Landing)) {
					finalJumpSpeed = jumpSpeed * ((BouncyGround)specialGround).bounceJumpRate;
					velocity = new Vector2(velocity.x, jumpSpeed * ((BouncyGround)specialGround).bounceRate);
					canDoubleJump = ((BouncyGround)specialGround).doubleJumpEnabled;
					//prevJumpDownTime = 0f;
				}
			}
		}
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

	/// <summary>
	/// OnGUI is called for rendering and handling GUI events.
	/// This function can be called multiple times per frame (one call per event).
	/// </summary>
	void OnGUI() {
		GUI.Box(new Rect(5,5,80,40), "vel.X: " + velocity.x + "\nvel.Y: " + velocity.y);
		GUI.Box(new Rect(5,50,120,40), state.ToString());
	}
}
