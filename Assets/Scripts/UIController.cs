using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIController : MonoBehaviour {

	public Slider uppercutCd;
	public Text uppercutCdText;
	public PlayerAttackController pac;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		uppercutCd.value = uppercutCd.maxValue - Mathf.Clamp((pac.nextUppercutTime - Time.time), 0f, uppercutCd.maxValue);
		if (uppercutCd.value == uppercutCd.maxValue) {
			uppercutCdText.text = "";
		} else {
			uppercutCdText.text = "" + (int)(uppercutCd.maxValue - Mathf.FloorToInt(uppercutCd.value));
		}
	}
}
