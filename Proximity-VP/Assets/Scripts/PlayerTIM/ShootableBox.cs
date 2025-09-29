using UnityEngine;
using System.Collections;

public class ShootableBox : MonoBehaviour {

	//The box's current health point total
	public float currentHealth = 3;

	private bool dead = false;

	AudioSource damageSFX;

	void Start()
	{
		damageSFX = GetComponent<AudioSource>();
	}

	void Update()
	{
		if (transform.position.y < -10)
		{
			dead = true;
			currentHealth = 0;
		}
	}

	public void Damage(float damageAmount)
	{
		//subtract damage amount when Damage function is called
		currentHealth -= damageAmount;
		damageSFX.Play();

		if (currentHealth <= 0 && !dead) 
		{
			dead = true;
		}
	}

	void OnTriggerEnter(Collider other)
	{
		if (other.gameObject.tag == "PlayerDamage") Damage(0.5f);
	}
}
