using UnityEngine;
using System.Collections;
using Sfs2X.Entities.Data;
using System;

public class Starship : GameItem 
{
	public int userId;
	public String username;
	private bool _isMine;
	private bool _doThrust;
	private int _thrusterValue;
	private int _rotatingDir;	// Set to -1 if ship is currently rotating counterclockwise, +1 if clockwise and 0 if not rotating


	public Starship()
	{
		this.velocity = new Velocity(0, 0);
		thrusterValue = 0;
	}
	
	public int thrusterValue
	{
		set
		{
			_thrusterValue = value;
		}
		get
		{
			return _thrusterValue;
		}
	}
	
	public bool doThrust
	{
		set
		{
			_doThrust = value;
			thrusterValue = (value ? 2 : 0);
			animator.SetBool("doThrust", value);
		}
		get
		{
			return _doThrust;
		}
	}
	
	public float rotation
	{
		set
		{
			transform.eulerAngles = new Vector3 (0,0,-(value * 180 / Mathf.PI)%360);
		}
		get
		{
			return -transform.rotation.eulerAngles.z * Mathf.PI / 180;
		}
	}
	
	public int rotatingDir
	{
		set
		{
			_rotatingDir = value;
		}
		get
		{
			return _rotatingDir;
		}
	}
	
	public bool isMine
	{
		set
		{
			_isMine = value;
		}
		get
		{
			return _isMine;
		}
	}
	
	public float thrustAcceleration
	{
		get
		{
			// Thrust accceleration is converted from pixels/sec2 to pixels/ms2
			return (float)settings.GetInt("thrustAccel") / 1000000f;
		}
	}
	
	public float maxSpeed
	{
		get
		{
			// Speed is converted from pixels/sec to pixels/ms
			return ((float) settings.GetInt("maxSpeed")) / 1000f;
		}
	}
	
	public float rotationSpeed
	{
		get
		{
			return ((float) settings.GetInt("rotationSpeed") * Mathf.PI / 180f) / 1000f;
		}
	}

}
