using UnityEngine;
using System.Collections;
using Sfs2X.Entities.Data;

public class GameItem : MonoBehaviour 
{
	public float xx;
	public float yy;
	public Velocity velocity;	// Current speed
	
	public float lastRenderTime;
	public ISFSObject settings;
	public Animator animator;
	
	public Vector2 position
	{
		set
		{
			transform.position = new Vector3(value.x, -value.y, transform.position.z);
		}
		get
		{
			return new Vector2(transform.position.x, -transform.position.y);
		}
	}
}
