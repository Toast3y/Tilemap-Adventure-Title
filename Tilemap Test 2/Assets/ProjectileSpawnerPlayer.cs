using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProjectileSpawnerPlayer : MonoBehaviour {

	public GameObject PlasmaBolt;

	public int maxProjectiles = 3;

	public float refireRate = 0.1f;

	public bool refire = false;

	private List<GameObject> projectiles = new List<GameObject>();

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}

	private void OnDrawGizmos() {
		Gizmos.color = Color.blue;
		//Gizmos.DrawSphere(new Vector3(transform.position.x, transform.position.y - 0.81f, transform.position.z), 0.1f);
		Gizmos.DrawSphere(new Vector3(transform.position.x + 0.37f, transform.position.y + 0.3f, transform.position.z), 0.05f);
	}

	public void FireWeapon(bool flipChar) {
		if (projectiles.Count < maxProjectiles && !refire) {
			GameObject proj = GameObject.Instantiate(PlasmaBolt);
			proj.GetComponent<PlasmaBolt>().spawner = this.gameObject;
			projectiles.Add(proj);
			StartCoroutine("SetRefireTimer");

			if (flipChar) {
				//Flying Right
				proj.transform.position = new Vector3(transform.position.x + 0.37f, transform.position.y + 0.32f, transform.position.z);
				proj.transform.rotation = Quaternion.Euler(0f, 0f, 180f);
			}
			else {
				//Flying Left
				proj.transform.position = new Vector3(transform.position.x - 0.37f, transform.position.y + 0.32f, transform.position.z);
				proj.transform.rotation = Quaternion.Euler(0f, 0f, 0f);
			}


			//proj.transform.position = new Vector3(transform.position.x - 0.37f, transform.position.y + 0.3f, transform.position.z);
		}
	}

	public void DespawnProjectile(GameObject proj) {
		projectiles.Remove(proj);
	}

	IEnumerator SetRefireTimer() {
		refire = true;
		yield return new WaitForSeconds(refireRate);
		refire = false;
		yield break;
	}
}
