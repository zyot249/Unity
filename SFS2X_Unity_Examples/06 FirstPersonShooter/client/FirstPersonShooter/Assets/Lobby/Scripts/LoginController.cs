using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using System;
using System.Collections;
using System.Collections.Generic;
using Sfs2X;
using Sfs2X.Logging;
using Sfs2X.Util;
using Sfs2X.Core;
using Sfs2X.Entities;

public class LoginController : MonoBehaviour {

	//----------------------------------------------------------
	// Editor public properties
	//----------------------------------------------------------

	[Tooltip("IP address or domain name of the SmartFoxServer 2X instance")]
	public string Host = "127.0.0.1";

	[Tooltip("TCP port listened by the SmartFoxServer 2X instance")]
	public int TcpPort = 9933;

	[Tooltip("UDP port listened by the SmartFoxServer 2X instance")]
	public int UdpPort = 9933;

	[Tooltip("Name of the SmartFoxServer 2X Zone to join")]
	public string Zone = "BasicExamples";

	//----------------------------------------------------------
	// UI elements
	//----------------------------------------------------------

	public InputField nameInput;
	public Toggle invertMouseToggle;
	public Button loginButton;
	public Text errorText;

	//----------------------------------------------------------
	// Private properties
	//----------------------------------------------------------

	private SmartFox sfs;

	//----------------------------------------------------------
	// Unity calback methods
	//----------------------------------------------------------

	void Awake() {
		Application.runInBackground = true;

		// Enable interface
		enableLoginUI(true);

		// Set invert mouse Y option
		invertMouseToggle.isOn = OptionsManager.InvertMouseY;
	}
	
	// Update is called once per frame
	void Update() {
		if (sfs != null)
			sfs.ProcessEvents();
	}

	//----------------------------------------------------------
	// Public interface methods for UI
	//----------------------------------------------------------

	public void OnLoginButtonClick() {
		enableLoginUI(false);
		
		// Set connection parameters
		ConfigData cfg = new ConfigData();
		cfg.Host = Host;
		cfg.Port = TcpPort;
		cfg.Zone = Zone;
		cfg.UdpHost = Host;
		cfg.UdpPort = UdpPort;
		
		// Initialize SFS2X client and add listeners
		sfs = new SmartFox();
		
		sfs.AddEventListener(SFSEvent.CONNECTION, OnConnection);
		sfs.AddEventListener(SFSEvent.CONNECTION_LOST, OnConnectionLost);
		sfs.AddEventListener(SFSEvent.LOGIN, OnLogin);
		sfs.AddEventListener(SFSEvent.LOGIN_ERROR, OnLoginError);
		sfs.AddEventListener(SFSEvent.UDP_INIT, OnUdpInit);
		
		// Connect to SFS2X
		sfs.Connect(cfg);
	}

	//----------------------------------------------------------
	// Private helper methods
	//----------------------------------------------------------
	
	private void enableLoginUI(bool enable) {
		nameInput.interactable = enable;
		invertMouseToggle.interactable = enable;
		loginButton.interactable = enable;
		errorText.text = "";
	}
	
	private void reset() {
		// Remove SFS2X listeners
		// This should be called when switching scenes, so events from the server do not trigger code in this scene
		sfs.RemoveAllEventListeners();
		
		// Enable interface
		enableLoginUI(true);
	}

	//----------------------------------------------------------
	// SmartFoxServer event listeners
	//----------------------------------------------------------

	private void OnConnection(BaseEvent evt) {
		if ((bool)evt.Params["success"])
		{
			// Save reference to SmartFox instance; it will be used in the other scenes
			SmartFoxConnection.Connection = sfs;

			Debug.Log("SFS2X API version: " + sfs.Version);
			Debug.Log("Connection mode is: " + sfs.ConnectionMode);

			// Login
			sfs.Send(new Sfs2X.Requests.LoginRequest(nameInput.text));
		}
		else
		{
			// Remove SFS2X listeners and re-enable interface
			reset();

			// Show error message
			errorText.text = "Connection failed; is the server running at all?";
		}
	}
	
	private void OnConnectionLost(BaseEvent evt) {
		// Remove SFS2X listeners and re-enable interface
		reset();

		string reason = (string) evt.Params["reason"];

		if (reason != ClientDisconnectionReason.MANUAL) {
			// Show error message
			errorText.text = "Connection was lost; reason is: " + reason;
		}
	}
	
	private void OnLogin(BaseEvent evt) {
		// Initialize UDP communication
		// Host and port have been configured in the ConfigData object passed to the SmartFox.Connect method
		sfs.InitUDP();
	}
	
	private void OnLoginError(BaseEvent evt) {
		// Disconnect
		sfs.Disconnect();

		// Remove SFS2X listeners and re-enable interface
		reset();
		
		// Show error message
		errorText.text = "Login failed: " + (string) evt.Params["errorMessage"];
	}
	
	private void OnUdpInit(BaseEvent evt) {
		// Remove SFS2X listeners
		reset();

		if ((bool)evt.Params["success"]) {
			// Set invert mouse Y option
			OptionsManager.InvertMouseY = invertMouseToggle.isOn;

			// Load lobby scene
			SceneManager.LoadScene("Lobby");
		} else {
			// Disconnect
			sfs.Disconnect();
			
			// Show error message
			errorText.text = "UDP initialization failed: " + (string) evt.Params["errorMessage"];
		}
	}
}
