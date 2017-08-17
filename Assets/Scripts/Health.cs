using System.Collections;
using System.Collections.Generic;
using UnityEngine;

abstract public class Health : MonoBehaviour {
	public float hp;
	public float regenRate;
	public bool canRegenHealth;

	public void TakeDamage(float damage) {
		hp = (hp - damage < 0) ? 0 : hp - damage;
	}

	void Die() {
		Debug.Log("Implement me, I'm Die()!");
	}
}
