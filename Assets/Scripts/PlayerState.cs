using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerState {
	[System.Flags]
	public enum Movement {
		Idle = 0x01,
		Moving = 0x02,
		Falling = 0x04,
		Landing = 0x08,
		Jumping = 0x10,
		Dashing = 0x20,
		WallSticking = 0x40,
		WallSliding = 0x80,
	};

	[System.Flags]
	public enum Condition {
		Normal = 0x01,
		Hit = 0x02,
		Recovering = 0x04,
		RestrictedAttacking = 0x08,
		FreeAttacking = 0x10,
	}
}
