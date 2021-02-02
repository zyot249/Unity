using UnityEngine;
using System;
using System.Collections.Generic;
using Sfs2X;
using Sfs2X.Core;
using Sfs2X.Logging;
using Sfs2X.Entities.Data;
using Sfs2X.Requests;
using Sfs2X.Entities;
using Sfs2X.Entities.Variables;
using System.Collections;
using Sfs2X.Requests.MMO;
using Sfs2X.Util;

public class Main : MonoBehaviour 
{
	public GUISkin skin;
	public Texture2D titleTexture;
	public Texture2D buttonPlay;
	public Texture2D buttonAstro;
	public Texture2D buttonRaptor;
	public Texture2D buttonViking;
	public Texture2D buttonSol;

	// SFS2X CONNECTION
	[Tooltip("IP address or domain name of the SmartFoxServer 2X instance")]
	public string Host = "127.0.0.1";
	
	[Tooltip("TCP port listened by the SmartFoxServer 2X instance; used for regular socket connection in all builds except WebGL")]
	public int TcpPort = 9933;
	
	[Tooltip("WebSocket port listened by the SmartFoxServer 2X instance; used for in WebGL build only")]
	public int WSPort = 8080;

	// USER VARIABLES
	private const String UV_MODEL = "sModel";		// Starship model
	private const String UV_X = "x";				// x position
	private const String UV_Y = "y";				// y position
	private const String UV_VX = "vx";				// Velocity x component
	private const String UV_VY = "vy";				// Velocity y component
	private const String UV_DIR = "d";				// Ship direction
	private const String UV_THRUST = "t";			// Ship's thruster is active
	private const String UV_ROTATE = "r";			// Ship rotating direction
	
	// MMOITEM VARIABLES
	private const String IV_TYPE = "iType";			// MMOItem type
	private const String IV_MODEL = "iModel";		// MMOItem model
	private const String IV_X = "x";				// x position
	private const String IV_Y = "y";				// y position
	private const String IV_VX = "vx";				// Velocity x component
	private const String IV_VY = "vy";				// Velocity y component
	
	// MMOITEM TYPES
	private const String ITYPE_WEAPON = "weapon";	// MMOItem of type weapon
	
	// REQUESTS TO SERVER
	private const String REQ_ROTATE = "control.rotate";
	private const String REQ_THRUST = "control.thrust";
	private const String REQ_FIRE = "control.fire";
	
	// RESPONSES FROM SERVER
	private const String RES_SHOT_XPLODE = "shot_xplode";
	
	private SmartFox sfs;
	public LogLevel logLevel = LogLevel.DEBUG;		// Use Unity Inspector to change this value
	
	
	private String username = "";
	private Boolean usernameEnabled = true;
	
	private ISFSObject starshipModels;
	private ISFSObject weaponModels;
	
	private int clientServerLag;

	private int _loginStep = 1;
	private String errorMsg = "";


	private bool playButtonPressed = false;

	private Color guiColor;
	
	private int loginStep
	{
		get
		{
			return _loginStep;
		}
		set
		{
			errorMsg = "";
			playButtonPressed = false;
			_loginStep = value;
			game.RemoveAll();
		}
	}

	public Game game
	{
		get
		{
			return GameObject.FindObjectOfType<Game>();
		}
	}

	//:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
	// User interface event handlers
	//:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::

	void Awake() 
	{
		Application.runInBackground = true;
		Application.targetFrameRate = 25;
	}
	
	void OnGUI()
	{
		GUI.skin = skin;
		guiColor = Color.white;
		GUIStyle tStyle = new GUIStyle(GUI.skin.button);

		if(loginStep < 4)
		{
			GUI.Label(new Rect((Screen.width - titleTexture.width) / 2, 100, titleTexture.width, titleTexture.height), titleTexture);
		}

		if(errorMsg != null)
		{
			GUIStyle lStyle = new GUIStyle(GUI.skin.label);
			Vector2 labelSize = lStyle.CalcSize(new GUIContent(errorMsg));
			GUI.contentColor = Color.red;

			GUI.Label(new Rect((Screen.width - labelSize.x) / 2, Screen.height * 2/3, labelSize.x, labelSize.y), errorMsg);
			GUI.contentColor = Color.white;
		}

		if(loginStep == 1)
		{
			GUI.Label(new Rect((Screen.width - titleTexture.width) / 2 + 20, (Screen.height - titleTexture.height) / 2 + 32, 500, 40), "Username");

			if(usernameEnabled == true)
				username = GUI.TextField(new Rect((Screen.width - titleTexture.width) / 2 + 20, (Screen.height - titleTexture.height) / 2 + 64, titleTexture.width * .6f, buttonPlay.height), username, 25);

			if(playButtonPressed)
			{
				guiColor.a = 0.5f;
				GUI.color = guiColor;
				tStyle.padding = new RectOffset(1,1,1,1);
			}
			else
				tStyle.padding = new RectOffset();

			if (GUI.Button(new Rect((Screen.width - titleTexture.width) / 2 + titleTexture.width * .67f,  (Screen.height - titleTexture.height) / 2 + 64, buttonPlay.width, buttonPlay.height), buttonPlay, tStyle) && !playButtonPressed)
			{
				playButtonPressed = true;
				if (sfs == null)
				{
					// Attempt connection
					DoConnect();
				}
				else
				{
					// Skip the connection and attempt login
					sfs.Send( new LoginRequest(username,"","SpaceWar") );
				}
			}
			guiColor.a = 1f;
			GUI.color = guiColor;
		}

		if(loginStep == 2)
		{
			GUI.Label(new Rect((Screen.width - titleTexture.width) / 2 + 150, (Screen.height - titleTexture.height) * 2/5, titleTexture.width * .6f, buttonPlay.height), "Select your starship");
			Array models = starshipModels.GetKeys();
			for (int i = 0; i < models.Length; i++)
			{
				ISFSObject starship = starshipModels.GetSFSObject(models.GetValue(i) as String);

				Texture2D btnTexture = buttonAstro;
				switch(starship.GetUtfString("model"))
				{
				case "Astro":
					btnTexture = buttonAstro;
					break;
				case "Viking":
					btnTexture = buttonViking;
					break;
				case "Raptor":
					btnTexture = buttonRaptor;
					break;
				}

				GUI.Label(new Rect((Screen.width - titleTexture.width) / 2 + 70 + i * 200, (Screen.height - titleTexture.height) / 2 + 55, 150, 40), starship.GetUtfString("model"));
				if (GUI.Button(new Rect((Screen.width - titleTexture.width) / 2 + 80 + i * 200, (Screen.height - titleTexture.height) / 2, btnTexture.width, btnTexture.height), btnTexture))
				{
					UserVariable shipModelUV = new SFSUserVariable(UV_MODEL, starship.GetUtfString("model"));
					List<UserVariable> userVars = new List<UserVariable>();
					userVars.Add(shipModelUV);
					sfs.Send( new SetUserVariablesRequest(userVars));
				}
			}
		}

		if(loginStep == 3)
		{
			GUI.Label(new Rect((Screen.width - titleTexture.width) / 2 + 140, (Screen.height - titleTexture.height) * 2/5, titleTexture.width * .6f, buttonPlay.height), "Select a solar system");

			// Room select
			for (int i = 0; i < sfs.RoomList.Count; i++)
			{
				Room room = sfs.RoomList[i] as Room;
				GUI.Label(new Rect((Screen.width - titleTexture.width) / 2 + 290 + i * 200, (Screen.height - titleTexture.height) / 2 + 170, 150, 40), room.Name);
				if (GUI.Button(new Rect((Screen.width - titleTexture.width) / 2 + 240 + i * 200, (Screen.height - titleTexture.height) / 2, buttonSol.width, buttonSol.height), buttonSol))
				{
					// Join the corresponding MMORoom
					sfs.Send( new JoinRoomRequest(room.Name));
				}
			}
		}
	}

	//----------------------------------------------------------
	// As Unity is not thread safe, we process the queued up callbacks every physics tick
	//----------------------------------------------------------
	void FixedUpdate() 
	{
		if (sfs != null)
			sfs.ProcessEvents();
	}

	/**
	 * Disconnect from the socket when shutting down the game
	 */
	public void OnApplicationQuit() 
	{
		if(sfs != null)
			sfs.Disconnect();
	}

	//:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
	// SFS2X event handlers
	//:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
	
	/**
	 * Sends a login request to the server right after the connection.
	 */
	public void OnConnection(BaseEvent evt) 
	{
		bool success = (bool)evt.Params["success"];

		if (success) 
		{
			Debug.Log("SFS2X API version: " + sfs.Version);
			Debug.Log("Connection mode is: " + sfs.ConnectionMode);

			sfs.Send( new LoginRequest(username) );
		} 
		else
		{
			loginStep = 1;
			errorMsg = "Unable to connect to " + sfs.Config.Host + ":" + sfs.Config.Port + "\nIs the server running at all?";

			DisposeSFS();
		}
	}
	
	/**
	 * Displays the disconnection reason.
	 */
	public void OnConnectionLost(BaseEvent evt)
	{
		loginStep = 1;
		errorMsg = "Connection lost; reason: " + (string)evt.Params["reason"];

		// Destroy SmartFox instance
		DisposeSFS();

		// Retrieve disconnection reason
		String reason = evt.Params["reason"] as String;
		
		if (reason != ClientDisconnectionReason.MANUAL)
		{
			// Set message to be displayed in connection view
			if (reason == ClientDisconnectionReason.IDLE)
				errorMsg = "A disconnection occurred due to inactivity";
			else if (reason == ClientDisconnectionReason.KICK)
				errorMsg = "You have been kicked by the administrator";
			else if (reason == ClientDisconnectionReason.BAN)
				errorMsg = "You have been banned by the administrator";
			else
				errorMsg = "A disconnection occurred due to unknown reason; please check the server log";
		}

		Debug.Log(errorMsg);
	}
	
	/**
	 * Displays the starship selection screen.
	 * Also, the lag monitor is enabled: this is required to compensate the lag in order to show the starships
	 * and other objects positions as near as possible to the current position in the server-side simulation.
	 */
	public void OnLogin(BaseEvent evt) {
		// Enable lag monitor
		sfs.EnableLagMonitor(true, 1, 5);
		
		// Save username in case it was assigned by the server (guest login for example)
		User user = evt.Params["user"] as User;
		username = user.Name;
		
		ISFSObject data = evt.Params["data"] as ISFSObject;
		
		// Retrieve starship models and weapon models from custom data sent by the Zone Extension
		starshipModels = data.GetSFSObject("starships");
		weaponModels = data.GetSFSObject("weapons");
		
		loginStep = 2;
	}
	
	/**
	 * Displays the login error message.
	 */
	public void OnLoginError(BaseEvent evt) 
	{
		// Re-init the view, so the interface controls are enabled again and the error displayed
		loginStep = 1;

		// Set message to be displayed in connection view
		errorMsg = evt.Params["errorMessage"] as String;
		Debug.Log(errorMsg);

		usernameEnabled = true;
	}
	
	/**
	 * Evaluates the current client-server lag.
	 * Returned value is divided by two because we just need the server to client lag (not client to server to client).
	 */
	public void OnPingPong(BaseEvent evt) 
	{
		clientServerLag = (int) evt.Params["lagValue"] / 2;
	}

	/**
	 * Displays the main game screen.
	 */
	public void OnRoomJoin(BaseEvent evt) 
	{
		game.starshipTypes = starshipModels;
		game.weaponTypes = weaponModels;
		loginStep = 4;
	}
	
	/**
	 * Displays the join error message.
	 */
	public void OnRoomJoinError(BaseEvent evt) 
	{
		// Display error in solar system selection screen
		loginStep = 3;
		errorMsg = evt.Params["errorMessage"] as String;
		Debug.Log(errorMsg);
	}
	
	/**
	 * Displays the connection view in case the current user is kicked from the Room.
	 * An MMORoom kicks users automatically in case their initial position is not set within the configured time (see userMaxLimboSeconds setting on the Room).
	 * 
	 * Actually this will never happen in this game as the position is set in the server side Extension as soon as the user enters the game
	 */
	public void OnUserExitRoom(BaseEvent evt) 
	{
		// Show the title screen
		if (evt.Params["user"] == sfs.MySelf)
		{
			// Set a warning to be displayed in connection view
			loginStep = 1;
			errorMsg = "You have been kicked out of the MMORoom because your initial position wasn't set in time";

			usernameEnabled = false;
		}
	}
	
	/**
	 * Synchronizes the user trajectory with the server-side simulation.
	 * This requires the client-server lag compensation: in other words the elapsed time since the event was sent
	 * by the server is taken into account to guess where the starship is located now.
	 * 
	 * If the user is myself, and the starship doesn't exist yet
	 * (because the MMORoom was just joined), also create the starship.
	 */
	public void OnUserVarsUpdate(BaseEvent evt) 
	{
		User user = evt.Params["user"] as User;
		List<string> changedVars = (List<string>)evt.Params["changedVars"];
		
		if (changedVars.Contains(UV_ROTATE))
		{
			// Make user starship start or stop rotating (excluding current user who controls his starship directly)
			if (user != sfs.MySelf)
			{
				int r1 = user.GetVariable(UV_ROTATE).GetIntValue();
				game.setStarshipRotating(user.Id, r1);
			}
		}
		
		if (changedVars.Contains(UV_X) || changedVars.Contains(UV_Y) ||
		    changedVars.Contains(UV_VX) || changedVars.Contains(UV_VY) ||
		    changedVars.Contains(UV_DIR) || changedVars.Contains(UV_THRUST))
		{
			// Create current user starship if not yet existing
			// For debug purposes, if the AoI is smaller than the viewport size, display it around the current user starship
			if (user == sfs.MySelf)
			{
				game.createStarship(user.Id, user.Name, true, user.GetVariable(UV_MODEL).GetStringValue());
			}
			
			// Reset user starship state in simulator, taking lag into account
			float x = (float) user.GetVariable(UV_X).GetDoubleValue();
			float y = (float) user.GetVariable(UV_Y).GetDoubleValue();
			float vx = (float) user.GetVariable(UV_VX).GetDoubleValue();
			float vy = (float) user.GetVariable(UV_VY).GetDoubleValue();
			float d = (float) user.GetVariable(UV_DIR).GetDoubleValue();
			bool t = user.GetVariable(UV_THRUST).GetBoolValue();
			
			game.setStarshipPosition(user.Id, x, y, vx, vy, d, t, clientServerLag);
			
		}

		if(loginStep == 2)
		{
			loginStep = 3;
		}
	}
	
	/**
	 * Creates/removes the starships of users entering/leaving the current user's Area of Interest (AoI).
	 * Creates/removes the weapon shots corresponding to MMOItems entering/leaving the current user's Area of Interest (AoI).
	 */
	public void OnProximityListUpdate(BaseEvent evt) 
	{

		// Loop the removedUsers list in the event params to remove the starships no more visible
		List<User> removedUsers = (List<User>) evt.Params["removedUsers"];
		
		foreach (User ru in removedUsers)
		{
			game.removeStarship(ru.Id);
		}
		
		// Loop the addedUsers list in the event params to create the starships now visible
		// To the usual lag we add 10ms, which is half the value of the proximityListUpdateMillis setting on the server
		// As we don't know exactly after how much time the update event was fired after the users updated their positions in the MMORoom
		// (could be 0ms up to 20ms), we use half the proximityListUpdateMillis value as a sort of mean value for an additional corretion of the lag
		List<User> addedUsers = (List<User>) evt.Params["addedUsers"];

		foreach (User au in addedUsers)
		{
			// Create starship
			game.createStarship(au.Id, au.Name, false, au.GetVariable(UV_MODEL).GetStringValue());
			
			// Get position-related User Variables
			float x = (float) au.GetVariable(UV_X).GetDoubleValue();
			float y = (float) au.GetVariable(UV_Y).GetDoubleValue();
			float vx = (float) au.GetVariable(UV_VX).GetDoubleValue();
			float vy = (float) au.GetVariable(UV_VY).GetDoubleValue();
			float d = (float) au.GetVariable(UV_DIR).GetDoubleValue();
			bool t = au.GetVariable(UV_THRUST).GetBoolValue();
			int r = au.GetVariable(UV_ROTATE).GetIntValue();
			
			// Set starship rotating flag
			game.setStarshipRotating(au.Id, r);
			
			// Set starship position
			game.setStarshipPosition(au.Id, x, y, vx, vy, d, t, clientServerLag + 10);
		}

		// Loop the removedItems list in the event params to remove the weapon shots no more visible
		// NOTE: sprites might have been already removed in case the shots explode within the AoI of the user
		// (notified by a dedicated Extension response) 

		List<IMMOItem> removedItems = (List<IMMOItem>) evt.Params["removedItems"];
		foreach (IMMOItem ri in removedItems)
		{
			game.removeWeaponShot(ri.Id);
		}


		// Loop the addedItems list in the event params to create those now visible
		// The same note about addedUsers applies here

		List<IMMOItem> addedItems = (List<IMMOItem>) evt.Params["addedItems"];
		foreach (IMMOItem ai in addedItems)
		{
			String type = ai.GetVariable(IV_TYPE).GetStringValue();
			if(type == ITYPE_WEAPON)
			{
				// Get position-related MMOItem Variables
				String im = ai.GetVariable(IV_MODEL).GetStringValue();
				float ix = (float) ai.GetVariable(IV_X).GetDoubleValue();
				float iy = (float) ai.GetVariable(IV_Y).GetDoubleValue();
				float ivx = (float) ai.GetVariable(IV_VX).GetDoubleValue();
				float ivy = (float) ai.GetVariable(IV_VY).GetDoubleValue();
				
				// Create weapon shot
				game.createWeaponShot(ai.Id, im, ix, iy, ivx, ivy, clientServerLag + 10);
			}
		}
	}
	
	/**
	 * Processes the responses sent by the server side Extension.
	 */
	public void OnExtensionResponse(BaseEvent evt) 
	{

		ISFSObject paramsExplode = (ISFSObject) evt.Params["params"];
		String cmd = (String) evt.Params["cmd"];

		// A weapon shot exploded
		if (cmd == RES_SHOT_XPLODE)
		{
			int shotId = paramsExplode.GetInt("id");
			int posX = paramsExplode.GetInt("x");
			int posY = paramsExplode.GetInt("y");
			game.explodeWeaponShot(shotId, posX, posY);
		}

	}
	
	
	//:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
	// In-game event handlers
	// These events are used so that the communication with SmartFoxServer
	// can be handled in this centralized class only
	//:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
	
	/**
	 * Sends the rotation event to the MMORoom Extension.
	 */
	private void Rotate(ControlEventArgs e)
	{
		ISFSObject paramsDir = new SFSObject();
		paramsDir.PutInt("dir", e.rotationDir);
		sfs.Send( new ExtensionRequest(REQ_ROTATE, paramsDir, sfs.LastJoinedRoom));
	}
	
	/**
	 * Sends the thrust event to the MMORoom Extension.
	 */
	private void Thrust(ControlEventArgs e)
	{
		ISFSObject paramsThrust = new SFSObject();
		paramsThrust.PutBool("go", e.activate);
		sfs.Send( new ExtensionRequest(REQ_THRUST, paramsThrust, sfs.LastJoinedRoom));
	}
	
	/**
	 * Sends the fire event to the MMORoom Extension.
	 */
	private void Fire(ControlEventArgs e)
	{
		ISFSObject paramsFire = new SFSObject();
		paramsFire.PutInt("wnum", e.fire);
		sfs.Send( new ExtensionRequest(REQ_FIRE, paramsFire, sfs.LastJoinedRoom));
	}

	//:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
	// Private methods
	//:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::

	/**
	 * Connects to SFS2X.
	 */
	void DoConnect()
	{
		
		// Always create a new instance of the SmartFox class
		#if !UNITY_WEBGL
		sfs = new SmartFox();
		#else
		sfs = new SmartFox(UseWebSocket.WS_BIN);
		#endif
		
		// Add SFS2X event listeners
		sfs.AddEventListener(SFSEvent.CONNECTION, OnConnection);
		sfs.AddEventListener(SFSEvent.CONNECTION_LOST, OnConnectionLost);
		sfs.AddEventListener(SFSEvent.LOGIN, OnLogin);
		sfs.AddEventListener(SFSEvent.LOGIN_ERROR, OnLoginError);
		sfs.AddEventListener(SFSEvent.PING_PONG, OnPingPong);
		sfs.AddEventListener(SFSEvent.ROOM_JOIN, OnRoomJoin);
		sfs.AddEventListener(SFSEvent.ROOM_JOIN_ERROR, OnRoomJoinError);
		sfs.AddEventListener(SFSEvent.USER_EXIT_ROOM, OnUserExitRoom);
		sfs.AddEventListener(SFSEvent.USER_VARIABLES_UPDATE, OnUserVarsUpdate);
		sfs.AddEventListener(SFSEvent.PROXIMITY_LIST_UPDATE, OnProximityListUpdate);
		sfs.AddEventListener(SFSEvent.EXTENSION_RESPONSE, OnExtensionResponse);
		
		errorMsg = "";
		usernameEnabled = false;

		// Set connection parameters
		ConfigData cfg = new ConfigData();
		cfg.Host = Host;
		#if !UNITY_WEBGL
		cfg.Port = TcpPort;
		#else
		cfg.Port = WSPort;
		#endif
		cfg.Zone = "SpaceWar";

		// Connect
		sfs.Connect(cfg);
		
		Game.Rotate += this.Rotate;
		Game.Thrust += this.Thrust;
		Game.Fire += this.Fire;
	}
	
	/**
	 * Remove SFSEvent listeners and nullify SmartFox instance.
	 */
	void DisposeSFS()
	{
		// Remove SFS2X event listeners
		sfs.RemoveEventListener(SFSEvent.CONNECTION_LOST, OnConnectionLost);
		sfs.RemoveEventListener(SFSEvent.LOGIN, OnLogin);
		sfs.RemoveEventListener(SFSEvent.LOGIN_ERROR, OnLoginError);
		sfs.RemoveEventListener(SFSEvent.PING_PONG, OnPingPong);
		sfs.RemoveEventListener(SFSEvent.ROOM_JOIN, OnRoomJoin);
		sfs.RemoveEventListener(SFSEvent.ROOM_JOIN_ERROR, OnRoomJoinError);
		sfs.RemoveEventListener(SFSEvent.USER_EXIT_ROOM, OnUserExitRoom);
		sfs.RemoveEventListener(SFSEvent.USER_VARIABLES_UPDATE, OnUserVarsUpdate);
		sfs.RemoveEventListener(SFSEvent.PROXIMITY_LIST_UPDATE, OnProximityListUpdate);
		sfs.RemoveEventListener(SFSEvent.EXTENSION_RESPONSE, OnExtensionResponse);
		
		// Destroy SmartFox instance
		sfs = null;
		
		usernameEnabled = true;
	}
}
