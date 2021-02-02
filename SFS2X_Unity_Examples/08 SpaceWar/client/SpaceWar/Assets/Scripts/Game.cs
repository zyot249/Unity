using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;
using Sfs2X.Entities.Data;



public delegate void ControlShipEvent(ControlEventArgs e);


public class Game : MonoBehaviour
{
	public Background background;
	public WeaponShot weaponObject;
	public Starship shipAstro;
	public Starship shipRaptor;
	public Starship shipViking;

	
	public ISFSObject starshipTypes;
	public ISFSObject weaponTypes;
	
	public GUISkin skin;
	
	private Starship myStarship;
	
	private bool isThrustKeyDown;
	private bool isFire1KeyDown;


	private Dictionary<int, WeaponShot> weaponShots;
	private Dictionary<int, Starship> starships;

	private static readonly int SCROLL_AREA_PADDING = 15; // % of the viewport size
	
	public static event ControlShipEvent Rotate;
	public static event ControlShipEvent Thrust;
	public static event ControlShipEvent Fire;

	//:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
	// Public methods
	//:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::

	public void setStarshipRotating(int userId, int r)
	{
		Starship ship = starships[userId];
		
		if (ship != null)
			ship.rotatingDir = r;
	}

	public void setStarshipPosition(int userId, float x, float y, float vx, float vy, float d, bool t, int elapsed)
	{
		if(starships.ContainsKey(userId))
		{
			Starship ship = starships[userId];

			// Set position and velocity
			ship.xx = x;
			ship.yy = y;
			ship.velocity.vx = vx;
			ship.velocity.vy = vy;
			ship.lastRenderTime = getTimer() - elapsed;
			
			// Set thruster
			ship.doThrust = t;
			
			// Set rotation angle
			ship.rotation = d;
			
			// Render the starship
			// This simulates the starship movement taking into account the elapsed time since the server sent the new position/speed
			// and places the starship in the current coordinates
			renderStarship(ship);
		}
	}

	public int getTimer()
	{
		return (int) Mathf.Round(Time.time * 1000f);
	}

	//:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
	// Private methods
	//:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
	
	/**
	 * Moves a starship to the next position based on applied forces.
	 * 
	 * Fake xx & yy coordinates are used because Sprite x & y get approximated by the Flash Player;
	 * so we do all simulation using fake coordinates and only at the end of the cycle
	 * we assign the calculated values to the real x,y coordinates for stage rendering.
	 */
	private void renderStarship(Starship ship)
	{
		float now = getTimer();
		float elapsed = now - ship.lastRenderTime;

		for (int i = 0; i < elapsed; i++)
		{
			// Ship rotation
			ship.rotation += ship.rotatingDir * ship.rotationSpeed;

			// Thruster force
			if (ship.doThrust)
			{
				ship.velocity.vx += (float) Math.Cos(ship.rotation) * ship.thrustAcceleration;
				ship.velocity.vy += (float) Math.Sin(ship.rotation) * ship.thrustAcceleration;
			}
			
			// Limit speed
			ship.velocity.limitSpeed(ship.maxSpeed);

			// Update ship position due to the calculated velocity
			ship.xx += ship.velocity.vx;
			ship.yy += ship.velocity.vy;

		}
		
		Vector3 newPos = Camera.main.ScreenToWorldPoint(new Vector3(ship.xx, ship.yy, 0));
		newPos = new Vector3(newPos.x - Camera.main.transform.position.x, newPos.y - Camera.main.transform.position.y, 0);

		// Evaluate background scroll amount
		float scrollX = newPos.x - ship.position.x;
		float scrollY = -newPos.y + ship.position.y;
		
		// Update starship sprite position in the container
		ship.position = new Vector2(newPos.x, newPos.y);
		
		ship.lastRenderTime = now;

		if(ship.isMine)
		{

			Vector3 shipOnViewport = Camera.main.WorldToViewportPoint(ship.transform.position);
			float perc = SCROLL_AREA_PADDING / 100f;

			Vector3 newCameraPosition = Camera.main.transform.position;

			
			if(shipOnViewport.x > (1 - perc))
			{
				newCameraPosition.x += ship.transform.position.x - Camera.main.ViewportToWorldPoint(new Vector3(1 - perc,0,0)).x;
			}
			if(shipOnViewport.x < perc)
			{
				newCameraPosition.x -=  Camera.main.ViewportToWorldPoint(new Vector3(perc,0,0)).x - ship.transform.position.x;
			}
			if(shipOnViewport.y > (1 - perc))
			{
				newCameraPosition.y += ship.transform.position.y - Camera.main.ViewportToWorldPoint(new Vector3(0,1 - perc,0)).y;
			}
			if(shipOnViewport.y < perc)
			{
				newCameraPosition.y -=  Camera.main.ViewportToWorldPoint(new Vector3(0,perc,0)).y - ship.transform.position.y;
			}
			
			scrollX += newCameraPosition.x - Camera.main.transform.position.x;
			scrollY += newCameraPosition.y - Camera.main.transform.position.y;

			Camera.main.transform.position = newCameraPosition;
			background.scroll(scrollX, scrollY);
		}
	}

	/**
	 * Moves a weapon shot to the next position based on applied forces.
	 * Same note about fake xx and yy coordinates apply here.
	 */
	private void renderWeaponShot(WeaponShot shot)
	{
		// Update shot position due to the calculated velocity
		shot.xx += shot.velocity.vx;
		shot.yy += shot.velocity.vy;
		shot.transform.position = new Vector3(shot.xx, shot.yy, shot.transform.position.z);


		float now = getTimer();
		float elapsed = now - shot.lastRenderTime;
		
		for (int i = 0; i < elapsed; i++)
		{
			shot.xx += shot.velocity.vx;
			shot.yy += shot.velocity.vy;
		}

		Vector3 newPos = Camera.main.ScreenToWorldPoint(new Vector3(shot.xx, shot.yy, 0));
		newPos = new Vector3(newPos.x - Camera.main.transform.position.x, newPos.y - Camera.main.transform.position.y, 0);

		shot.position = new Vector2(newPos.x, newPos.y);
		shot.lastRenderTime = now;
	}

	public void createWeaponShot(int id, string type, float x, float y, float vx, float vy, int elapsed)
	{
		if(weaponShots.ContainsKey(id)) return;
		WeaponShot shot = Instantiate(weaponObject, new Vector3(x,y,0), Quaternion.identity) as WeaponShot;
		shot.id = id;
		shot.settings = weaponTypes.GetSFSObject(type);

		// Add weapon shot to array container
		weaponShots.Add(id, shot);

		// Set position and velocity
		shot.xx = x;
		shot.yy = y;
		shot.velocity.vx = vx;
		shot.velocity.vy = vy;
		shot.lastRenderTime = getTimer() - elapsed;
	}

	public void createStarship(int userId, string userName, bool isMine, string type)
	{
		if(starships.ContainsKey(userId)) return;
		Starship ship = null;
		switch(type)
		{
		case "Astro":
			ship = Instantiate(shipAstro) as Starship;
			break;
		case "Raptor":
			ship = Instantiate(shipRaptor) as Starship;
			break;
		case "Viking":
			ship = Instantiate(shipViking) as Starship;
			break;
		}
		ship.userId = userId;
		ship.username = userName;
		ship.settings = starshipTypes.GetSFSObject(type);

		// Add starship to array container
		starships.Add(userId, ship);

		if (isMine)
		{
			myStarship = ship;
			myStarship.isMine = true;
		}
	}
	
	public void removeStarship(int userId)
	{
		if(!starships.ContainsKey(userId)) return;
		Starship ship = starships[userId];

		starships.Remove(userId);
		Destroy(ship.gameObject);
		
		if (ship == myStarship)
			myStarship = null;
	}
	
	public void removeWeaponShot(int id)
	{
		if(!weaponShots.ContainsKey(id)) return;
		WeaponShot shot = weaponShots[id];

		// The shot could have already been removed if the explosion was notified by the server before the proximity update
		if (shot != null)
		{
			weaponShots.Remove(id);
			shot.Destroy();
		}
	}

	public void explodeWeaponShot(int id, int posX, int posY)
	{
		if(!weaponShots.ContainsKey(id)) return;
		WeaponShot shot = weaponShots[id];
		
		// The shot could have already been removed if the proximity update was notified before the explosion
		if (shot != null)
		{
			// Remove shot
			removeWeaponShot(id);
		}

		// Show explosion
		Vector3 newPos = Camera.main.ScreenToWorldPoint(new Vector3((float) posX, (float) posY, 0));
		newPos = new Vector3(newPos.x - Camera.main.transform.position.x, newPos.y - Camera.main.transform.position.y, 0);
		shot.Explode(newPos.x, newPos.y);
	}

	public void RemoveAll ()
	{
		if(starships != null)
		{
 			foreach(var ship in starships)
			{
				Starship shp = starships[ship.Key];
				Destroy(shp.gameObject);
				
				if (shp == myStarship)
					myStarship = null;
			}
		}
		
		
		if(weaponShots != null)
		{
			foreach(var shot in weaponShots)
			{
				WeaponShot sht = weaponShots[shot.Key];

				if (sht != null)
				{
					sht.Destroy();
				}
			}
		}

		weaponShots = new Dictionary<int, WeaponShot>();
		starships = new Dictionary<int, Starship>();
	}

	//:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
	// Event handlers
	//:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::

	void Start () 
	{
		weaponShots = new Dictionary<int, WeaponShot>();
		starships = new Dictionary<int, Starship>();
	}
	
	void OnGUI()
	{
		GUI.skin = skin;
		GUI.contentColor = Color.gray;
		GUIStyle tStyle = new GUIStyle(GUI.skin.label);
		tStyle.fontSize = 12;
		
		Vector3 camOffset = Camera.main.WorldToScreenPoint(new Vector3(0,0,0));
		
		if(starships != null)
			foreach(var sh in starships)
		{
			Starship ship = sh.Value as Starship;
			Vector2 labelSize = tStyle.CalcSize(new GUIContent(ship.username));
			GUI.Label(new Rect(ship.xx + camOffset.x - (Screen.width + labelSize.x) / 2f, ship.yy - camOffset.y + Screen.height / 2 + 20, labelSize.x, 40), ship.username, tStyle);
		}
		
		
	}
	
	void Update () 
	{
		if(starships != null)
		{
			foreach(var ship in starships)
			{
				renderStarship(ship.Value);
			}
			
			if(weaponShots != null)
				foreach(var shot in weaponShots)
			{
				renderWeaponShot(shot.Value);
			}
			
			onKeyboardDown();
			onKeyboardUp();
		}
	}
	
	void onKeyboardDown()
	{
		if(myStarship == null) return;
		if (Input.GetKeyDown("left") || Input.GetKeyDown("right"))
		{
			int dir = Input.GetKeyDown("left") ? -1 : 1;
			if(dir != myStarship.rotatingDir)
			{
				// Stop rotation
				setStarshipRotating(myStarship.userId, dir);
				
				// Fire event to send a request to the server
				ControlEventArgs eventArgs = new ControlEventArgs();
				eventArgs.rotationDir = myStarship.rotatingDir;
				
				if (Rotate != null)
					Rotate(eventArgs);
			}
		}
		if (Input.GetKeyDown("up"))
		{
			if (!isThrustKeyDown)
			{
				isThrustKeyDown = true;
				
				// Thrust activation is made of 3 steps:
				// 1) on key down the ship shows a small flame; no actual force is applied to the ship (no trajectory change)
				// 2) request is sent to the server which activates the thrust and sends a position reset event
				// 3) when the event is received the ship shows a bigger flame and the thrust force is applied during the simulation
				myStarship.thrusterValue = 1;
				//myStarship.doThrust = true;
				
				// Fire event to send a request to the server
				ControlEventArgs eventArgs = new ControlEventArgs();
				eventArgs.activate = true;
				
				if (Thrust != null)
					Thrust(eventArgs);
			}
		}
		
		if (Input.GetKeyDown("space"))
		{
			if (!isFire1KeyDown)
			{
				isFire1KeyDown = true;
				ControlEventArgs eventArgs = new ControlEventArgs();
				eventArgs.fire = 1;
				if (Fire != null)
					Fire(eventArgs);
			}
		}
	}
	
	void onKeyboardUp()
	{
		if(myStarship == null) return;
		if (Input.GetKeyUp("left") || Input.GetKeyUp("right"))
		{
			int dir = Input.GetKeyUp("left") ? -1 : 1;
			if(dir == myStarship.rotatingDir)
			{
				// Stop rotation
				setStarshipRotating(myStarship.userId, 0);
				
				// Fire event to send a request to the server
				ControlEventArgs eventArgs = new ControlEventArgs();
				eventArgs.rotationDir = myStarship.rotatingDir;
				
				if (Rotate != null)
					Rotate(eventArgs);
			}
		}
		if (Input.GetKeyUp("up"))
		{
			if (isThrustKeyDown)
			{
				isThrustKeyDown = false;
				
				// Thrust deactivation is made of 3 steps:
				// 1) on key up the ship shows a small flame; the actual force is still applied to the ship (trajectory keeps changing)
				// 2) request is sent to the server which deactivates the thrust and sends a position reset event
				// 3) when the event is received the ship stops showing the flame and the thrust force is not applied anymore
				myStarship.thrusterValue = 1;
				//myStarship.doThrust = false;
				
				// Fire event to send a request to the server
				
				ControlEventArgs eventArgs = new ControlEventArgs();
				eventArgs.activate = false;
				
				if (Thrust != null)
					Thrust(eventArgs);
			}
		}
		
		if (Input.GetKeyUp("space"))
		{
			isFire1KeyDown = false;
		}
	}
}

public class ControlEventArgs : EventArgs
{
	public int rotationDir { get; set; }
	public bool activate { get; set; }
	public int fire { get; set; }
}