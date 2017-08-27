using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerStateManager : MonoBehaviour {
	
	public PlayerState.Movement moveState;
	public PlayerState.Condition condState;

	/// <summary>
	/// Start is called on the frame when a script is enabled just before
	/// any of the Update methods is called the first time.
	/// </summary>
	void Start() {
		moveState = PlayerState.Movement.Idle;
		condState = PlayerState.Condition.Normal;
	}
}
