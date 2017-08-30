using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIController : MonoBehaviour {

	public Slider health;
	public Image healthFill;
	public Slider uppercutCd;
	public Text uppercutCdText;
	public PlayerHealth ph;
	public PlayerAttackController pac;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		// Health
		health.maxValue = ph.maxHp;
		health.value = ph.hp;
		// Set color based on % health
		float hpTone = .75f;
		if (health.value == 0) {
			healthFill.color = new Color(0,0,0,0);
		} else if (health.value >= health.maxValue / 2) {
			healthFill.color = new Color(1 - ((health.value - (health.maxValue / 2)) / (health.maxValue / 2)) * hpTone, 1 * hpTone, .1f);
		} else {
			healthFill.color = new Color(1 * hpTone, (health.value / (health.maxValue / 2)) * hpTone, .1f);
		}

		// Uppercut cooldown
		uppercutCd.value = uppercutCd.maxValue - Mathf.Clamp((pac.nextUppercutTime - Time.time), 0f, uppercutCd.maxValue);
		if (uppercutCd.value == uppercutCd.maxValue) {
			uppercutCdText.text = "";
		} else {
			uppercutCdText.text = "" + (int)(uppercutCd.maxValue - Mathf.FloorToInt(uppercutCd.value));
		}
	}
}
