using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterUnlocks : MonoBehaviour {

	public bool doubleJump;

	

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}

	public bool DoubleJump {
		get {
			return doubleJump;
		}

		set {
			doubleJump = value;
		}
	}
}
