using Extensions;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayerMovementController : MonoBehaviour {

	/* Known bugs:
	/* - If bottom of character falls down through bottom of soft-bottom platform, character warps up to lands on it. Same in vice-versa on soft-top.
	 * - OnFall event triggers while moving on slopes
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
	Vector2 velocity;

	// Events
	public event System.EventHandler OnFall;
	public event System.EventHandler OnLand;
	public event System.EventHandler OnLateralCollision;
	public event System.EventHandler OnCeilingCollision;

	// For collisions and convenience
	public PlayerHitbox hitbox { get; private set; }
	Collider2D cd;
	Rect box;
	int layerMask;
	float angleLeeway = 5f;
	RaycastHit2D closestHitInfo;
	public Vector2 facing { get; private set; }

	// Inputs
	bool lastInput;

	// Jumping
	public GameObject doubleJumpPS;
	bool canJump = true;
	bool canDoubleJump = false;
	bool jumpPressedLastFrame = false;
	float prevJumpDownTime = 0f;
	float jumpPressLeeway = 0.1f;

	// Dashing
	public GameObject trail;
	float currentDashTime = 0f;
	bool canDash = true;
	bool dashReady = true;
	bool wallDashing = false;
	bool exitingDash = false;

	// Wall sticking/jumping
	public GameObject wallSlidePS;
	public float wallSlideSpeed = 40f;
	public float wallSlideDelay = .05f;
	public float wallJumpAwayDistance = 50f;
	GameObject currentWallSlidePS = null;
	float lastWallSlidePSEmission = 0f;
	bool lateralCollision = false;
	bool canStickWall = true;
	Vector2 wallDirection;
	float wallSlideTime = 0f;
	
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

	// Health & damage
	PlayerHealth playerHealth;
	float kbRecoveryTime = 1.25f;
	float kbEndTime = 0.35f;
	float kbTime = 0;
	float recoveryBlinkSpeed = 0.1f;
	EnemyHurtbox enemyHurtbox = null;

	// Player condition state
	// TODO: Move this somewhere more appropriate
	[System.Flags]
	public enum ConditionState {
		Normal = 0x01,
		Hit = 0x02,
		Recovering = 0x04,
		RestrictedAttacking = 0x08,
		FreeAttacking = 0x10,
	}
	public ConditionState conditionState;

	// Rendering
	SpriteRenderer rend;

	public void ForceMovement(float? x, float? y) {
		float veloX = (x == null) ? velocity.x : (float)x;
		float veloY = (y == null) ? velocity.y : (float)y;

		velocity = new Vector2(veloX, veloY);		
	}

	// Use this for initialization
	void Start () {
		hitbox = GetComponent<PlayerHitbox>();
		cd = GetComponent<BoxCollider2D>();
		rend = GetComponent<SpriteRenderer>();
		playerHealth = GetComponent<PlayerHealth>();
		layerMask = 1 << LayerMask.NameToLayer("NormalCollisions");
		state = MovementState.Idle;
		conditionState = ConditionState.Normal;
		facing = Vector2.right;

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
		box = new Rect(
			cd.bounds.min.x,
			cd.bounds.min.y,
			cd.bounds.size.x,
			cd.bounds.size.y
		);

		// Set default values
		finalJumpSpeed = jumpSpeed;
		finalAccel = accel;
		
		// --- Input ---
		// TODO: Put this in update?
		// Get input and set idle state if applicable
		float hAxis = 0;
		bool dashInput = false;
		bool jumpInput = false;

		// Restrict input if player is hit or attacking with restricted movement
		if (conditionState.Missing(ConditionState.Hit) && conditionState.Missing(ConditionState.RestrictedAttacking)) {
			hAxis = Input.GetAxisRaw("Horizontal");
			dashInput = Input.GetButtonDown("Fire1");
			jumpInput = Input.GetButton("Jump");
		}

		// Set facing direction
		if (hAxis != 0)
			facing = new Vector2(hAxis, 0);

		// Set idle state according to input
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
			if (state.Missing(MovementState.Falling)) {
				state = state.Include(MovementState.Falling);
				if (OnFall != null)
					OnFall(this, System.EventArgs.Empty);
			}
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
				if (state.Has(MovementState.Falling) && state.Missing(MovementState.Landing)) {
					if (OnLand != null)
						OnLand(this, System.EventArgs.Empty);
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
				float friction = 0.7f;
				newVelocityX = Mathf.Clamp((newVelocityX - (closestHitInfo.normal.x * friction)), -maxSpeed, maxSpeed);
				Vector2 newPosition = transform.position;
				newPosition.y += -closestHitInfo.normal.x * Mathf.Abs(newVelocityX) * Time.deltaTime * ((newVelocityX - closestHitInfo.normal.x > 0) ? 1 : -1);
				transform.position = newPosition;
				state = state.Remove(MovementState.Landing);			
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
							if (OnLateralCollision != null)
								OnLateralCollision(this, System.EventArgs.Empty);
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
				float lowestY = float.MaxValue;
				RaycastHit2D lowestYHit = new RaycastHit2D();

				// Check for wall regardless of horizontal velocity (allows player to hold direction away from wall)
				for (int i = 0; i < hRays; i++) {
					// Create and cast ray
					float lerpDistance = (float)i / (float)(hRays - 1);
					Vector2 rayOrigin = Vector2.Lerp(minRay, maxRay, lerpDistance);
					hitInfo[i] = Physics2D.Raycast(rayOrigin, wallDirection, (box.width / 2) + .001f, RayLayers.sideRay);
					if (hitInfo[i].fraction > 0) {
						onWall = true;		
						if (hitInfo[i].point.y < lowestY) {
							lowestY = hitInfo[i].point.y;
							lowestYHit = hitInfo[i];
						}
					}
				}
				
				// If hitting wall while sliding, update wall slide PS position
				if (onWall && state.Has(MovementState.WallSliding)) {
					if (currentWallSlidePS == null) {
						// TODO: Create a new PS if the old one hasn't disappeared yet
						currentWallSlidePS = Instantiate(wallSlidePS, new Vector3(lowestYHit.point.x, lowestYHit.point.y, 0f), Quaternion.identity);
					} else {
						currentWallSlidePS.transform.position = new Vector3(lowestYHit.point.x, lowestYHit.point.y, 0f);
						ParticleSystem ps = currentWallSlidePS.GetComponentInChildren<ParticleSystem>();
						ps.Emit(1);
						lastWallSlidePSEmission = Time.time;
					}
				}
				// If no wall hit, end wallstick/slide
				if (!onWall) {
					state = state.Remove(MovementState.WallSticking | MovementState.WallSliding);
				}
			}
			// If not wall sliding, remove PS
			else if (currentWallSlidePS != null) {
				Destroy(currentWallSlidePS, lastWallSlidePSEmission * .75f);
			}

			// Wall sliding
			if (state.Has(MovementState.WallSticking) && Time.time >= wallSlideTime) {
				velocity = Vector2.down * wallSlideSpeed;
				state = state.Remove(MovementState.WallSticking);
				state = state.Include(MovementState.WallSliding);
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
					velocity = new Vector2(-wallDirection.x * wallJumpAwayDistance, finalJumpSpeed * .8f);
					state = state.Remove(MovementState.WallSticking | MovementState.WallSliding);
					canDoubleJump = false;
				} 
				// Double jump
				else if (!grounded && dashReady && canDoubleJump) {
					velocity = new Vector2(velocity.x, finalJumpSpeed * .75f);
					Instantiate(doubleJumpPS, new Vector2(box.center.x, box.center.y - box.height / 2), Quaternion.identity);
					prevJumpDownTime = 0f;
					canDoubleJump = false;
				}
				state = state.Remove(MovementState.Falling);
				state = state.Include(MovementState.Jumping);
			}
			jumpPressedLastFrame = jumpInput;
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
				if (OnCeilingCollision != null)
						OnCeilingCollision(this, System.EventArgs.Empty);
				velocity = new Vector2(velocity.x, 0);
			}
		}

		// --- Damage ---
		// Apply hit if detected by collider
		if (enemyHurtbox != null && conditionState.Missing(ConditionState.Hit) && conditionState.Missing(ConditionState.Recovering)) {
			conditionState = conditionState.Include(ConditionState.Hit); 
			conditionState = conditionState.Remove(ConditionState.Normal);
			state = state.Remove(MovementState.Dashing | MovementState.WallSticking | MovementState.WallSliding);
			velocity = Vector2.zero;
			StartCoroutine(ApplyHit(enemyHurtbox.damage, enemyHurtbox.knockback, enemyHurtbox.knockbackDirection));
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
			StartCoroutine(CreateDashTrail());
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

	IEnumerator CreateDashTrail() {
		GameObject newTrail = Instantiate(trail, transform.position, transform.rotation);
		SpriteRenderer trailRend = newTrail.GetComponent<SpriteRenderer>();
		trailRend.sprite = rend.sprite;
		trailRend.transform.localScale = rend.transform.localScale;
		Color alphaColor = rend.color;
		alphaColor.a *= .4f;
		trailRend.color = alphaColor;
		yield return StartCoroutine(FadeDashTrail(trailRend));
		Destroy(newTrail);
	}

	IEnumerator FadeDashTrail(SpriteRenderer trailRend) {
		while (trailRend.color.a > 0f) {
			Color alphaColor = trailRend.color;
			alphaColor.a -= .05f;
			trailRend.color = alphaColor;
			yield return null;
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
	/// Sent when another object enters a trigger collider attached to this
	/// object (2D physics only).
	/// </summary>
	/// <param name="other">The other Collider2D involved in this collision.</param>
	void OnTriggerStay2D(Collider2D other) {
		if (other.gameObject.GetComponent<EnemyHurtbox>() && enemyHurtbox == null && conditionState.Missing(ConditionState.Hit) && conditionState.Missing(ConditionState.Recovering)) {					
			if (hitbox.vulnerable && other.gameObject.GetComponentInParent<EnemyAttackController>().touchDamageEnabled) {
				enemyHurtbox = other.gameObject.GetComponent<EnemyHurtbox>();
				enemyHurtbox.knockbackDirection = (transform.position.x > other.transform.position.x) ? Vector2.right : Vector2.left;
			}
		}
	}

	IEnumerator ApplyHit (float damage, float knockback, Vector2 knockbackDir) {
		// Apply damage and knockback from hit
		enemyHurtbox = null;
		playerHealth.TakeDamage(damage);
		yield return StartCoroutine(ApplyKnockback(knockback, knockbackDir));
		yield return StartCoroutine(EndKnockback());
		EndHit();
		
	}

	IEnumerator ApplyKnockback (float knockback, Vector2 knockbackDir) {
		// Force character to move back from source of damage
		while (kbTime < kbEndTime) {
			kbTime += Time.deltaTime;
			// Slow down over the course of the knockback
			velocity = new Vector2(knockbackDir.x * (knockback - (knockback * (kbTime / kbEndTime))), velocity.y);
			yield return null;
		}
	}

	IEnumerator EndKnockback() {
		kbTime = 0;
		conditionState = conditionState.Remove(ConditionState.Hit);
		conditionState = conditionState.Include(ConditionState.Recovering);
		// Blink and enjoy the recovery period
		while (kbTime < kbRecoveryTime * recoveryBlinkSpeed) {
			kbTime += Time.deltaTime;
			rend.enabled = !rend.enabled;
            yield return new WaitForSeconds(recoveryBlinkSpeed);
		}
		// Ensure renderer is enabled upon return
		rend.enabled = true;
	}

	void EndHit() {
		kbTime = 0;
		conditionState = conditionState.Remove(ConditionState.Recovering);
		conditionState = conditionState.Include(ConditionState.Normal);
	}

	/// <summary>
	/// OnGUI is called for rendering and handling GUI events.
	/// This function can be called multiple times per frame (one call per event).
	/// </summary>
	void OnGUI() {
		GUI.Box(new Rect(5,5,80,40), "vel.X: " + velocity.x + "\nvel.Y: " + velocity.y);
		GUI.Box(new Rect(5,50,120,40), "" + playerHealth.hp);
	}
}
