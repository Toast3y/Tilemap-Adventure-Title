using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterController_2D : MonoBehaviour {

	//private SpriteRenderer spriteRenderer;
	private Rigidbody2D rigidBody;
	private CharacterUnlocks unlocks;
	private Animator anims;
	//private BoxCollider2D hurtbox;

	[Header("Jump Control Variables")]
	public float maxSpeed = 15.0f;
	public float jumpForce = 2.0f;
	public float fallMultiplier = 2.3f;
	public float lowJumpMultiplier = 2.0f;
	public float jumpPenalty = 0.85f;
	public float jumpTurnPenalty = 0.6f;
	public float heavyJumpTurnPenalty = 1.0f;
	public float maxFallVelocity = -20.0f;


	[Header ("Recovery Timers Per Button")]
	public float jumpRecoveryTime = 0.1f;
	public float moveRecoveryTime = 0.1f;

	public float lightMoveRecoveryTime;
	public float heavyMoveRecoveryTime;
	public float specialMoveRecoveryTime;
	public float gunMoveRecoveryTime;

	[Header("Attack and Damage recovery variables")]
	public bool damageRecovery = false;
	public bool moveRecovery = false;
	
	public bool flipChar;
	public bool jumped;
	public bool doubleJumped;
	public bool finalJumped;
	public bool jumpRecovery;
	public bool jumpTurn;

	//Input timers for each input type, registering the last time the button was pressed down, rather than held.
	[Header("Button Down Input Timers")]
	public float lastUp = 0.5f;
	public float lastLeft = 0.5f;
	public float secondLastLeft = 0.5f;
	public float lastRight = 0.5f;
	public float secondLastRight = 0.5f;
	public float lastDown = 0.5f;
	public float lastUpLeft = 0.5f;
	public float lastUpRight = 0.5f;
	public float lastDownLeft = 0.5f;
	public float lastDownRight = 0.5f;
	public float lastNeutral = 0.5f;

	public float lastDPadXAxis = 0f;
	public float lastDPadYAxis = 0f;

	public float lastGunAttack = 0.5f;
	public float lastLightAttack = 0.5f;
	public float lastHeavyAttack = 0.5f;
	public float lastJump = 0.5f;
	public float lastAimBlock = 0.5f;
	public float lastSpecial = 0.5f;

	[Header("Quarter Circle Input Timer")]
	public float quarterCircleReadTime = 0.500f;
	public float inputGapReadTime = 0.150f;


	// Use this for initialization
	void Start() {
		//spriteRenderer = GetComponent<SpriteRenderer>();
		rigidBody = GetComponent<Rigidbody2D>();
		unlocks = GetComponent<CharacterUnlocks>();
		//hurtbox = GetComponent<BoxCollider2D>();
		flipChar = false;
	}

	private void OnDrawGizmos() {
		Gizmos.color = Color.red;
		Gizmos.DrawSphere(new Vector3(transform.position.x, transform.position.y - 0.81f, transform.position.z), 0.1f);
		Gizmos.DrawSphere(new Vector3(transform.position.x - 0.37f, transform.position.y - 0.5f, transform.position.z), 0.05f);
	}

	// Update is called once per frame
	void FixedUpdate() {
		//Character always animated facing left.

		CheckInputs();

		//Recover and reset jumps on landing
		if (!jumpRecovery) {
			if (CheckForFloor()) {
				LandingRecovery();
			}
		}

		ApplyControls();

		ApplyGravity();

		ApplyMovement();

		ApplyJumpControls();


		//Quarter circle test
		/*if (Input.GetButtonDown("Light Attack")) {
			if (DetectSpecialMoveInput()) {
				if (DetectStandingDragonPunchInput()) {
					Debug.Log("Dragon Punch Light: secondLastLeft: " + secondLastLeft + "; LastLeft: " + lastLeft + "; LastDown: " + lastDown + "; LastDownLeft: " + lastDownLeft);
				}
			}
			
		}*/

	}

	private void ApplyJumpControls() {
		//Jumping, apply forces and set recovery timers.
		if (Input.GetButtonDown("Jump") && !finalJumped && !jumpRecovery) {
			//Debug.Log("Jump!");
			rigidBody.velocity = new Vector2(rigidBody.velocity.x, jumpForce);
			StartCoroutine("SetJumpRecoveryTimer");
			SetJumped(true);

			//Check if double jumping is available.
			if (!doubleJumped) {
				SetDoubleJumped(true);
			}
			else {
				SetFinalJumped(true);

				if (unlocks.DoubleJump) {
					jumpTurn = false;
				}
			}

			//Set movement to zero if you jump while holding no direction.
			var movement = Input.GetAxis("Horizontal_DPad");

			if (movement == 0) {
				rigidBody.velocity = new Vector2(0, rigidBody.velocity.y);
			}
		}
	}

	//ApplyControls applies the logic to control the characters special moves.
	//This method needs to be overwritten in every characters class.
	private void ApplyControls() {
		//Button Priority: Special, AimBlock, Heavy+Gun, Jump, Light, Heavy, Gun

		if (lastSpecial == 0f) {
			Debug.Log("Special Attack");
		}
		else if (Input.GetButton("AimBlock")) {
			Debug.Log("AimBlock");
		}
		else if (lastHeavyAttack < 0.033f && lastGunAttack < 0.033f) {
			Debug.Log("Heavy+Gun Attack");
		}
		else if (lastJump == 0f) {
			Debug.Log("Jump");
		}
		else if (lastLightAttack == 0f) {

			if (!DetectSpecialMoveInput()) {
				Debug.Log("Light Attack");
			}
			else {
				Debug.Log("Special Input Attack");
			}
		}
		else if (lastHeavyAttack == 0f) {
			Debug.Log("Heavy Attack");
		}
		else if (lastGunAttack == 0f) {
			Debug.Log("Gun Attack");
			GunAttackAction();
		}

	}

	//ApplyMovement applies forces to characters and details all movement physics.
	private void ApplyMovement() {
		var movement = Input.GetAxis("Horizontal_DPad");

		//Debug.Log("X Axis = " + movement + "; Y Axis: " + Input.GetAxis("Vertical_DPad"));

		//Flip the character sprite if the player changed direction
		if (movement > 0 && !flipChar) {
			FlipRight();
		}
		else if (movement < 0 && flipChar) {
			FlipLeft();
		}

		//Apply forces required based on character state

		//Let the player move as long as you're not executing a special move, or recovering from hitstun
		if (!moveRecovery && !damageRecovery) {
			//If you jumped, punish changing directions.
			if (jumped) {

				//If you did a standing jump and moved, punish it as if you had turned.
				if (movement != 0 && rigidBody.velocity.x == 0) {
					jumpTurn = true;
				}

				if (!jumpTurn) {
					//Give standard velocity to jumps.
					if (movement > 0) {
						rigidBody.velocity = new Vector2(1 * maxSpeed * jumpPenalty, rigidBody.velocity.y);
					}
					else if (movement < 0) {
						rigidBody.velocity = new Vector2(-1 * maxSpeed * jumpPenalty, rigidBody.velocity.y);
					}
					else {
						//Slow movement due to player letting go of controls
						rigidBody.velocity = new Vector2(rigidBody.velocity.x * 0.98f, rigidBody.velocity.y);
					}
				}
				else if (rigidBody.velocity.y < 0) {
					//If player is falling, taper forward movement similar to a punished turn.
					if (movement > 0) {
						//Flip the player so the algorithm can apply multiplicative movement normally
						if (rigidBody.velocity.x < 0) {
							rigidBody.velocity = new Vector2(-rigidBody.velocity.x, rigidBody.velocity.y);
						}

						if ((rigidBody.velocity.x * 0.98f) > (1 * maxSpeed * jumpTurnPenalty)) {
							rigidBody.velocity = new Vector2(rigidBody.velocity.x * 0.98f, rigidBody.velocity.y);
						}
						else {
							rigidBody.velocity = new Vector2(1 * maxSpeed * jumpTurnPenalty, rigidBody.velocity.y);
						}
					}
					else if (movement < 0) {
						//Flip the player so the algorithm can apply multiplicative movement normally
						if (rigidBody.velocity.x < 0) {
							rigidBody.velocity = new Vector2(-rigidBody.velocity.x, rigidBody.velocity.y);
						}

						if ((rigidBody.velocity.x * 0.98f) < (-1 * maxSpeed * jumpTurnPenalty)) {
							rigidBody.velocity = new Vector2(rigidBody.velocity.x * 0.98f, rigidBody.velocity.y);
						}
						else {
							rigidBody.velocity = new Vector2(-1 * maxSpeed * jumpTurnPenalty, rigidBody.velocity.y);
						}
					}
					else {
						//Just force speed to max jump penalty movement.
						if (rigidBody.velocity.x > 0) {
							rigidBody.velocity = new Vector2(1 * maxSpeed * (jumpTurnPenalty - 0.2f), rigidBody.velocity.y);
						}
						else if (rigidBody.velocity.x < 0) {
							rigidBody.velocity = new Vector2(-1 * maxSpeed * (jumpTurnPenalty - 0.2f), rigidBody.velocity.y);
						}
					}
				}
				else {
					//Punish turning mid jump
					if (movement > 0) {
						rigidBody.velocity = new Vector2(1 * maxSpeed * jumpTurnPenalty, rigidBody.velocity.y);
					}
					else if (movement < 0) {
						rigidBody.velocity = new Vector2(-1 * maxSpeed * jumpTurnPenalty, rigidBody.velocity.y);
					}
					else {
						//Standard air movement after turning. Commits some speed to direction change.
						if (rigidBody.velocity.x > 0) {
							rigidBody.velocity = new Vector2(1 * maxSpeed * (jumpTurnPenalty - 0.2f), rigidBody.velocity.y);
						}
						else if (rigidBody.velocity.x < 0) {
							rigidBody.velocity = new Vector2(-1 * maxSpeed * (jumpTurnPenalty - 0.2f), rigidBody.velocity.y);
						}
					}
				}
			}
			else {
				//Standing movement
				if (movement > 0) {
					rigidBody.velocity = new Vector2(1 * maxSpeed, rigidBody.velocity.y);
				}
				else if (movement < 0) {
					rigidBody.velocity = new Vector2(-1 * maxSpeed, rigidBody.velocity.y);
				}
				else {
					rigidBody.velocity = new Vector2(0 * maxSpeed, rigidBody.velocity.y);
				}
			}
		}
		

	}

	//ApplyGravity based on whether the player is in the air, or holding the jump button.
	private void ApplyGravity() {
		//Apply gravity based on whether jump button is held down
		if (rigidBody.velocity.y < 0) {
			rigidBody.velocity += Vector2.up * Physics2D.gravity.y * (fallMultiplier - 1) * Time.deltaTime;
		}
		else if (rigidBody.velocity.y > 0 && !Input.GetButton("Jump")) {
			//Apply gravity sooner if you let go of the jump button.
			rigidBody.velocity += Vector2.up * Physics2D.gravity.y * (lowJumpMultiplier - 1) * Time.deltaTime;
		}
	}

	//Recover from landing on the ground.
	private void LandingRecovery() {
		//Play landing sounds / animations
		SetJumped(false);
		SetFinalJumped(false);
		jumpTurn = false;

		if (unlocks.DoubleJump == true) {
			SetDoubleJumped(false);
		}
	}



	private void SpecialAction() {
		//Check what special attack or weapon is to be used, and start the animation / spawn the objects
	}

	private void AimBlockAction() {
	}

	private void LightAttackAction() {
	}

	private void HeavyAttackAction() {
	}

	private void GunAttackAction() {
		gameObject.GetComponent<ProjectileSpawnerPlayer>().FireWeapon(flipChar);
	}






	private void FlipChar() {
		//Debug.Log("Char flipped");
		flipChar = !flipChar;
		//spriteRenderer.flipX = flipChar;
		//hurtbox.offset.Set(-hurtbox.offset.x, hurtbox.offset.y);
		//transform.localScale.Set(-transform.localScale.x, transform.localScale.y, transform.localScale.z);
	}

	private void FlipRight() {
		flipChar = true;
		transform.localScale = new Vector3(-1, 1, 1);

		if (rigidBody.velocity.y != 0) {
			jumpTurn = true;
		}
	}

	private void FlipLeft() {
		flipChar = false;
		transform.localScale = new Vector3(1, 1, 1);

		if (rigidBody.velocity.y != 0) {
			jumpTurn = true;
		}
	}

	private bool CheckForFloor() {
		//See if the player is on the floor.
		Collider2D[] colliders = Physics2D.OverlapCircleAll(new Vector2(transform.position.x, transform.position.y - 0.83f), 0.1f);

		bool onFloor = false;

		if (colliders.Length > 0) {
			foreach (Collider2D coll in colliders) {
				if (coll.gameObject.tag == "World") {
					onFloor = true;
				}
			}
		}

		return onFloor;
	}

	private bool CheckForWall() {
		//See if the player is colliding with a wall.

		RaycastHit2D[] colliders;

		//Detect based on what side you're facing.
		if (flipChar) {
			colliders = Physics2D.CircleCastAll(new Vector2(transform.position.x + 0.37f, transform.position.y + 0.75f), 0.05f, Vector2.down, 1.25f);
		}
		else {
			colliders = Physics2D.CircleCastAll(new Vector2(transform.position.x - 0.37f, transform.position.y + 0.75f), 0.05f, Vector2.down, 1.25f);
		}
		//RaycastHit2D[] colliders = Physics2D.CircleCastAll(new Vector2(transform.position.x - 0.38f, transform.position.y + 0.75f), 0.05f, Vector2.down, 1.25f);

		bool againstWall = false;

		if (colliders.Length > 0) {
			foreach (RaycastHit2D cast in colliders) {
				if (cast.collider.gameObject.tag == "World") {
					againstWall = true;
				}
			}
		}

		return againstWall;
	}

	//CheckInputs to see if any were pressed at a given time.
	private void CheckInputs() {

		//Gun Attack
		if (Input.GetButtonDown("Gun Attack")) {
			lastGunAttack = 0.0f;
		}
		else {
			lastGunAttack = lastGunAttack + Time.deltaTime;
		}

		//Light Attack
		if (Input.GetButtonDown("Light Attack")) {
			lastLightAttack = 0.0f;
		}
		else {
			lastLightAttack = lastLightAttack + Time.deltaTime;
		}

		//Heavy Attack
		if (Input.GetButtonDown("Heavy Attack")) {
			lastHeavyAttack = 0.0f;
		}
		else {
			lastHeavyAttack = lastHeavyAttack + Time.deltaTime;
		}

		//Jump
		if (Input.GetButtonDown("Jump")) {
			lastJump = 0.0f;
		}
		else {
			lastJump = lastJump + Time.deltaTime;
		}

		//AimBlock
		if (Input.GetButtonDown("AimBlock")) {
			lastAimBlock = 0.0f;
		}
		else {
			lastAimBlock = lastAimBlock + Time.deltaTime;
		}

		//Special
		if (Input.GetButtonDown("Special")) {
			lastSpecial = 0.0f;
		}
		else {
			lastSpecial = lastSpecial + Time.deltaTime;
		}



		//DPad axis reads
		bool x_change, y_change;
		var x_input = Input.GetAxis("Horizontal_DPad");
		var y_input = Input.GetAxis("Vertical_DPad");

		//Check if the player used a different input this frame
		if (x_input != lastDPadXAxis) {
			x_change = true;
			lastDPadXAxis = x_input;
		}
		else {
			x_change = false;
		}

		if (y_input != lastDPadYAxis) {
			y_change = true;
			lastDPadYAxis = y_input;
		}
		else {
			y_change = false;
		}


		if (x_input == 0f && y_input == 0f) {
			//Neutral has no "down" state for the switches, hence has no 
			lastNeutral = 0f;

			//Everything else increases by deltatime
			lastUp = lastUp + Time.deltaTime;
			lastDown = lastDown + Time.deltaTime;
			lastLeft = lastLeft + Time.deltaTime;
			lastRight = lastRight + Time.deltaTime;

			secondLastLeft = secondLastLeft + Time.deltaTime;
			secondLastRight = secondLastRight + Time.deltaTime;

			lastUpLeft = lastUpLeft + Time.deltaTime;
			lastUpRight = lastUpRight + Time.deltaTime;

			lastDownLeft = lastDownLeft + Time.deltaTime;
			lastDownRight = lastDownRight + Time.deltaTime;
		}
		else if (x_change || y_change) {
			//Check the inputs
			//Horizontal inputs
			if (x_input == 1) {
				secondLastRight = lastRight;
				lastRight = 0f;
			}
			else {
				secondLastRight = secondLastRight + Time.deltaTime;
				lastRight = lastRight + Time.deltaTime;
			}

			if (x_input == -1) {
				secondLastLeft = lastLeft;
				lastLeft = 0f;
			}
			else {
				secondLastLeft = secondLastLeft + Time.deltaTime;
				lastLeft = lastLeft + Time.deltaTime;
			}

			//Vertical inputs
			if (y_input == 1) {
				lastUp = 0f;
			}
			else {
				lastUp = lastUp + Time.deltaTime;
			}

			if (y_input == -1) {
				lastDown = 0f;
			}
			else {
				lastDown = lastDown + Time.deltaTime;
			}

			//Diagonal inputs
			//Top Right
			if ((x_input > 0 && x_input != 1) && (y_input > 0 && y_input != 1)) {
				lastUpRight = 0f;
			}
			else {
				lastUpRight = lastUpRight + Time.deltaTime;
			}

			//Top Left
			if ((x_input < 0 && x_input != -1) && (y_input > 0 && y_input != 1)) {
				lastUpLeft = 0f;
			}
			else {
				lastUpLeft = lastUpLeft + Time.deltaTime;
			}

			//Bottom Right
			if ((x_input > 0 && x_input != 1) && (y_input < 0 && y_input != -1)) {
				lastDownRight = 0f;
			}
			else {
				lastDownRight = lastDownRight + Time.deltaTime;
			}

			//Bottom Left
			if ((x_input < 0 && x_input != -1) && (y_input < 0 && y_input != -1)) {
				lastDownLeft = 0f;
			}
			else {
				lastDownLeft = lastDownLeft + Time.deltaTime;
			}

		}
		else {
			//Everything else increases by deltatime as no inputs changed.
			lastNeutral = lastNeutral + Time.deltaTime;

			lastUp = lastUp + Time.deltaTime;
			lastDown = lastDown + Time.deltaTime;
			lastLeft = lastLeft + Time.deltaTime;
			lastRight = lastRight + Time.deltaTime;

			secondLastLeft = secondLastLeft + Time.deltaTime;
			secondLastRight = secondLastRight + Time.deltaTime;

			lastUpLeft = lastUpLeft + Time.deltaTime;
			lastUpRight = lastUpRight + Time.deltaTime;

			lastDownLeft = lastDownLeft + Time.deltaTime;
			lastDownRight = lastDownRight + Time.deltaTime;
		}
	}

	private bool DetectSpecialMoveInput() {
		//Detects if the player input a quarter circle motion on their controller.
		if (lastDown <= quarterCircleReadTime) {
			return true;
		}
		else {
			return false;
		}

	}

	private bool DetectStandingDragonPunchInput() {
		//Dragon punch input means that the second last left/right input will be within input gap time.
		//flipChar guarantees that you must do the input in the direction you pressed it.
		if ((((secondLastLeft - lastDown <= inputGapReadTime) || (lastLeft - lastDown <= inputGapReadTime)) && (lastDown - lastDownLeft <= inputGapReadTime) && !flipChar) || 
			(((secondLastRight - lastDown <= inputGapReadTime) || (lastRight - lastDown <= inputGapReadTime)) && (lastDown - lastDownRight <= inputGapReadTime) && flipChar)) {
			return true;
		}
		else {
			return false;
		}
	}

	private bool DetectQuarterCircleInput() {
		//flipChar guarantees that you must do the input in the direction you pressed it. 
		if (((lastDown - lastDownLeft <= inputGapReadTime) && (lastDownLeft - lastLeft <= inputGapReadTime) && !flipChar) ||
			((lastDown - lastDownRight <= inputGapReadTime) && (lastDownRight - lastRight <= inputGapReadTime) && flipChar)) {

			return true;
		}
		else {
			return false;
		}
	}

	private bool DetectDashInput() {
		//Detect double tap left and double tap right.
		if (((secondLastLeft - lastLeft <= inputGapReadTime) || (secondLastRight - lastRight <= inputGapReadTime)) &&
			((Input.GetAxis("Horizontal_DPad") == 1) || (Input.GetAxis("Horizontal_DPad") == -1))) {

			return true;
		}
		else {
			return false;
		}
	}





	IEnumerator SetJumpRecoveryTimer() {
		jumpRecovery = true;
		yield return new WaitForSeconds(jumpRecoveryTime);
		jumpRecovery = false;
		yield break;
	}

	IEnumerator SetMoveRecoveryTimer() {
		moveRecovery = true;
		yield return new WaitForSeconds(moveRecoveryTime);
		moveRecovery = false;
		yield break;
	}

	IEnumerator SetSpecialMoveRecoveryTimer() {
		moveRecovery = true;
		yield return new WaitForSeconds(specialMoveRecoveryTime);
		moveRecovery = false;
		yield break;
	}

	IEnumerator SetHeavyMoveRecoveryTimer() {
		moveRecovery = true;
		yield return new WaitForSeconds(heavyMoveRecoveryTime);
		moveRecovery = false;
		yield break;
	}

	IEnumerator SetLightMoveRecoveryTimer() {
		moveRecovery = true;
		yield return new WaitForSeconds(lightMoveRecoveryTime);
		moveRecovery = false;
		yield break;
	}

	IEnumerator SetGunMoveRecoveryTimer() {
		moveRecovery = true;
		yield return new WaitForSeconds(gunMoveRecoveryTime);
		moveRecovery = false;
		yield break;
	}

	public void SetMoveTimer(float newtime) {
		moveRecoveryTime = newtime;
	}

	public bool GetJumped() {
		return jumped;
	}

	public void SetJumped(bool Jumped) {
		jumped = Jumped;
	}

	public bool GetDoubleJumped() {
		return doubleJumped;
	}

	public void SetDoubleJumped(bool DoubleJumped) {
		doubleJumped = DoubleJumped;
	}

	public bool GetFinalJumped() {
		return finalJumped;
	}

	public void SetFinalJumped(bool val) {
		finalJumped = val;
	}
}
