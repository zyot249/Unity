using UnityEngine;
using System.Collections;
using Sfs2X.Entities.Data;

public class WeaponShot : GameItem
{
	public int id;
	private float timer = -1f;
	
	public WeaponShot()
	{
		this.velocity = new Velocity(0, 0);
	}

	public void Explode(float posX, float posY)
	{
		this.velocity = new Velocity(0, 0);
		this.position = new Vector2(posX, posY);
		animator.SetBool("Explosion", true);
	}

	public void Destroy()
	{
		timer = 0f;
	}

	public void Update()
	{
		if(timer >= 0f)
		{
			timer += Time.deltaTime;
		}

		if(timer > 1f)
		{
			Destroy(gameObject);
		}
	}
}

