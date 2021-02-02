using System.Collections;
using System;

public class Velocity 
{
	// Speed values expressed in pixels/millis
	// Direction expressed in radians
	
	public float vx = 0;
	public float vy = 0;
	
	public Velocity(float vx, float vy)
	{
		this.vx = vx;
		this.vy = vy;
	}
	
	public float speed
	{
		get
		{
			return (float) Math.Sqrt(Math.Pow(vx, 2) + Math.Pow(vy, 2));
		}
	}
	
	public float direction
	{
		get
		{
			return (float) (Math.Atan2(vy, vx));
		}
	}
	
	public void limitSpeed(float maxSpeed)
	{
		if (speed > maxSpeed)
		{
			float dir = direction;
			
			vx = (float) Math.Cos(dir) * maxSpeed;
			vy = (float) Math.Sin(dir) * maxSpeed;
		}
	}
	
	public string toComponentsString()
	{
		return "(" + vx + "," + vy + ")";
	}
	
	public string toVectorString()
	{
		return "[" + speed + "," + direction + " rad]";
	}

}
