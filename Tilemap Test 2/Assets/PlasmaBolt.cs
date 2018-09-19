using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlasmaBolt : Projectile {

	public GameObject spawner;
	private Rigidbody2D rigidBody;

	// Use this for initialization
	void Start () {
		rigidBody = this.GetComponent<Rigidbody2D>();
		Physics2D.IgnoreCollision(gameObject.GetComponent<Collider2D>(), spawner.GetComponent<Collider2D>());

		float angle = transform.eulerAngles.z;
		float rad = angle * Mathf.Deg2Rad;

		Debug.Log("Starting angle: " + angle + "; Radians: " + rad);
	}

	// Update is called once per frame
	void FixedUpdate () {
		if (!preventMovement) {
			rigidBody.velocity = new Vector2(-Mathf.Cos(transform.eulerAngles.z * Mathf.Deg2Rad) * projectileSpeed, -Mathf.Sin(transform.eulerAngles.z * Mathf.Deg2Rad) * projectileSpeed);
			//Debug.Log("Mathf.Cos: " + Mathf.Cos(transform.rotation.z * Mathf.Deg2Rad) + "; Mathf.Sin: " + Mathf.Sin(transform.rotation.z * Mathf.Deg2Rad));
		}
	}

	private void OnCollisionEnter2D(Collision2D collision) {
		if (collision.gameObject.tag == "World") {
			spawner.GetComponent<ProjectileSpawnerPlayer>().DespawnProjectile(this.gameObject);
			SelfDestruct();
		}
	}
}
