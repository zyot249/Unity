using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;
using System.Collections;
using System.Collections.Generic;
using Sfs2X;
using Sfs2X.Logging;
using Sfs2X.Util;
using Sfs2X.Core;
using Sfs2X.Entities;
using Sfs2X.Entities.Data;
using Sfs2X.Entities.Variables;

public class BuddyMessenger : MonoBehaviour {

	//----------------------------------------------------------
	// Editor public properties
	//----------------------------------------------------------

	[Tooltip("IP address or domain name of the SmartFoxServer 2X instance")]
	public string Host = "127.0.0.1";

	[Tooltip("TCP port listened by the SmartFoxServer 2X instance; used for regular socket connection in all builds except WebGL")]
	public int TcpPort = 9933;

	[Tooltip("WebSocket port listened by the SmartFoxServer 2X instance; used for in WebGL build only")]
	public int WSPort = 8080;

	[Tooltip("Name of the SmartFoxServer 2X Zone to join")]
	public string Zone = "BasicExamples";

	//----------------------------------------------------------
	// UI elements
	//----------------------------------------------------------

	// Login panel components
	public Animator loginPanelAnim;
	public InputField zoneInput;
	public InputField nameInput;
	public Button loginButton;
	public Text errorText;

	// User details panel components
	public Animator userPanelAnim;
	public Text loggedInText;
	public Toggle onlineToggle;
	public InputField nickInput;
	public InputField ageInput;
	public InputField moodInput;
	public Text stateButtonLabel;
	public RectTransform stateDropDown;
	public GameObject stateItemPrefab;

	// Bubby list panel components
	public Animator buddiesPanelAnim;
	public InputField buddyInput;
	public RectTransform buddyListContent;
	public GameObject buddyListItemPrefab;
	public Sprite IconAvailable;
	public Sprite IconAway;
	public Sprite IconOccupied;
	public Sprite IconOffline;
	public Sprite IconBlocked;
	public Sprite IconBlock;
	public Sprite IconUnblock;

	// Chat panel components
	public RectTransform chatPanelsContainer;
	public GameObject chatPanelPrefab;

	//----------------------------------------------------------
	// Private properties
	//----------------------------------------------------------

	private SmartFox sfs;
	private string currentState = "Available";

	private static string BUDDYVAR_AGE = SFSBuddyVariable.OFFLINE_PREFIX + "age";
	private static string BUDDYVAR_MOOD = "mood";

	//----------------------------------------------------------
	// Unity calback methods
	//----------------------------------------------------------

	void Start() {
		// Initialize UI
		zoneInput.text = Zone;
		errorText.text = "";
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
		#if !UNITY_WEBGL
		cfg.Port = TcpPort;
		#else
		cfg.Port = WSPort;
		#endif
		cfg.Zone = zoneInput.text;
		
		// Initialize SFS2X client and add listeners
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

		// Add SFS2X buddy-related event listeners
		// NOTE: for simplicity, most buddy-related events cause the whole
		// buddylist in the interface to be recreated from scratch, also if those
		// events are caused by the current user himself. A more refined approach should
		// update the specific items to which the event refers.
		sfs.AddEventListener(SFSBuddyEvent.BUDDY_LIST_INIT, OnBuddyListInit);
		sfs.AddEventListener(SFSBuddyEvent.BUDDY_ERROR, OnBuddyError);
		sfs.AddEventListener(SFSBuddyEvent.BUDDY_ONLINE_STATE_UPDATE, OnBuddyListUpdate);
		sfs.AddEventListener(SFSBuddyEvent.BUDDY_VARIABLES_UPDATE, OnBuddyListUpdate);
		sfs.AddEventListener(SFSBuddyEvent.BUDDY_ADD, OnBuddyListUpdate);
		sfs.AddEventListener(SFSBuddyEvent.BUDDY_REMOVE, OnBuddyListUpdate);
		sfs.AddEventListener(SFSBuddyEvent.BUDDY_BLOCK, OnBuddyListUpdate);
		sfs.AddEventListener(SFSBuddyEvent.BUDDY_MESSAGE, OnBuddyMessage);
		
		// Connect to SFS2X
		sfs.Connect(cfg);
	}

	/**
	 * Disconnects from server.
	 */
	public void OnDisconnectButtonClick() {
		sfs.Disconnect();
	}

	/**
	 * Makes user panel slide in/out.
	 */
	public void OnUserTabClick() {
		userPanelAnim.SetBool("panelOpen", !userPanelAnim.GetBool("panelOpen"));
	}

	/**
	 * Makes buddies panel slide in/out.
	 */
	public void OnBuddiesTabClick() {
		buddiesPanelAnim.SetBool("panelOpen", !buddiesPanelAnim.GetBool("panelOpen"));
	}

	/**
	 * Changes the currently selected state in user panel.
	 */
	public void OnStateItemClick(string stateValue) {
		currentState = stateValue;
		stateButtonLabel.text = currentState;
		stateDropDown.gameObject.SetActive(false);
	}

	/**
	 * Makes current user go online/offline in the buddy list system.
	 */
	public void OnOnlineToggleChange(bool isChecked) {
		sfs.Send(new Sfs2X.Requests.Buddylist.GoOnlineRequest(isChecked));
	}

	/**
	 * Sets the current user details in the buddy system.
	 * This can be done if the current user is online in the buddy system only.
	 */
	public void OnSetDetailsButtonClick() {
		List<BuddyVariable> buddyVars = new List<BuddyVariable>();
		buddyVars.Add(new SFSBuddyVariable(ReservedBuddyVariables.BV_NICKNAME, nickInput.text));
		buddyVars.Add(new SFSBuddyVariable(BUDDYVAR_AGE, Convert.ToInt32(ageInput.text)));
		buddyVars.Add(new SFSBuddyVariable(BUDDYVAR_MOOD, moodInput.text));
		buddyVars.Add(new SFSBuddyVariable(ReservedBuddyVariables.BV_STATE, currentState));

		sfs.Send(new Sfs2X.Requests.Buddylist.SetBuddyVariablesRequest(buddyVars));
	}

	/**
	 * Adds a buddy to the current user's buddy list.
	 */
	public void OnAddBuddyButtonClick() {
		if (buddyInput.text != "") {
			sfs.Send(new Sfs2X.Requests.Buddylist.AddBuddyRequest(buddyInput.text));
			buddyInput.text = "";
		}
	}

	/**
	 * Start a chat with a buddy.
	 */
	public void OnChatBuddyButtonClick(string buddyName) {
		// Check if panel is already open; if yes bring it to front
		Transform panel = chatPanelsContainer.Find(buddyName);

		if (panel == null) {
			GameObject newChatPanel = Instantiate(chatPanelPrefab) as GameObject;
			ChatPanel chatPanel = newChatPanel.GetComponent<ChatPanel>();

			chatPanel.buddy = sfs.BuddyManager.GetBuddyByName(buddyName);
			chatPanel.closeButton.onClick.AddListener(() => OnChatCloseButtonClick(buddyName));
			chatPanel.sendButton.onClick.AddListener(() => OnSendMessageButtonClick(buddyName));
			chatPanel.messageInput.onEndEdit.AddListener(val => OnSendMessageKeyPress(buddyName));

			/*
			chatPanel.messageInput.onEndEdit.AddListener(val =>
				{
					if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
						Debug.Log("End edit on enter");
				});
	*/
			newChatPanel.transform.SetParent(chatPanelsContainer, false);

		} else {
			panel.SetAsLastSibling();
		}
	}

	/**
	 * Sends a chat message to a buddy when SEND button is pressed.
	 */
	public void OnSendMessageButtonClick(string buddyName) {
		// Get panel
		Transform panel = chatPanelsContainer.Find(buddyName);
		
		if (panel != null) {
			ChatPanel chatPanel = panel.GetComponent<ChatPanel>();

			string message = chatPanel.messageInput.text;

			// Add a custom parameter containing the recipient name,
			// so that we are able to write messages in the proper chat tab
			ISFSObject _params = new SFSObject();
			_params.PutUtfString("recipient", buddyName);

			Buddy buddy = sfs.BuddyManager.GetBuddyByName(buddyName);
			
			sfs.Send(new Sfs2X.Requests.Buddylist.BuddyMessageRequest(message, buddy, _params));

			chatPanel.messageInput.text = "";
			chatPanel.messageInput.ActivateInputField();
			chatPanel.messageInput.Select();
		}
	}

	/**
	 * Sends a chat message to a buddy when ENTER key is pressed.
	 */
	public void OnSendMessageKeyPress(string buddyName) {
		if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
			OnSendMessageButtonClick(buddyName);
	}

	/**
	 * Destroys a chat panel.
	 */
	public void OnChatCloseButtonClick(string panelName) {
		Transform panel = chatPanelsContainer.Find(panelName);
		if (panel != null)
			UnityEngine.Object.Destroy(panel.gameObject);
	}
	
	/**
	 * Blocks/unblocks a buddy.
	 */
	public void OnBlockBuddyButtonClick(string buddyName) {
		bool isBlocked = sfs.BuddyManager.GetBuddyByName(buddyName).IsBlocked;

		sfs.Send(new Sfs2X.Requests.Buddylist.BlockBuddyRequest(buddyName, !isBlocked));
	}
	
	/**
	 * Removes a user from the buddy list.
	 */
	public void OnRemoveBuddyButtonClick(string buddyName) {
		sfs.Send(new Sfs2X.Requests.Buddylist.RemoveBuddyRequest(buddyName));
	}

	//----------------------------------------------------------
	// Private helper methods
	//----------------------------------------------------------
	
	private void enableLoginUI(bool enable) {
		zoneInput.interactable = enable;
		nameInput.interactable = enable;
		loginButton.interactable = enable;
		errorText.text = "";
	}
	
	private void reset() {
		// Remove SFS2X listeners
		sfs.RemoveEventListener(SFSEvent.CONNECTION, OnConnection);
		sfs.RemoveEventListener(SFSEvent.CONNECTION_LOST, OnConnectionLost);
		sfs.RemoveEventListener(SFSEvent.LOGIN, OnLogin);
		sfs.RemoveEventListener(SFSEvent.LOGIN_ERROR, OnLoginError);
		sfs.RemoveEventListener(SFSBuddyEvent.BUDDY_LIST_INIT, OnBuddyListInit);
		sfs.RemoveEventListener(SFSBuddyEvent.BUDDY_ERROR, OnBuddyError);
		sfs.RemoveEventListener(SFSBuddyEvent.BUDDY_ONLINE_STATE_UPDATE, OnBuddyListUpdate);
		sfs.RemoveEventListener(SFSBuddyEvent.BUDDY_VARIABLES_UPDATE, OnBuddyListUpdate);
		sfs.RemoveEventListener(SFSBuddyEvent.BUDDY_ADD, OnBuddyListUpdate);
		sfs.RemoveEventListener(SFSBuddyEvent.BUDDY_REMOVE, OnBuddyListUpdate);
		sfs.RemoveEventListener(SFSBuddyEvent.BUDDY_BLOCK, OnBuddyListUpdate);
		sfs.RemoveEventListener(SFSBuddyEvent.BUDDY_MESSAGE, OnBuddyMessage);
		
		sfs = null;
		
		// Enable interface
		enableLoginUI(true);
	}

	//----------------------------------------------------------
	// SmartFoxServer event listeners
	//----------------------------------------------------------

	private void OnConnection(BaseEvent evt) {
		if ((bool)evt.Params["success"]) {
			Debug.Log("SFS2X API version: " + sfs.Version);
			Debug.Log("Connection mode is: " + sfs.ConnectionMode);

			// Login
			sfs.Send(new Sfs2X.Requests.LoginRequest(nameInput.text));
		} else {
			// Remove SFS2X listeners and re-enable interface
			reset();

			// Show error message
			errorText.text = "Connection failed; is the server running at all?";
		}
	}
	
	private void OnConnectionLost(BaseEvent evt) {
		// Show login panel
		loginPanelAnim.SetBool("loggedIn", false);

		// Hide user and buddies panels
		userPanelAnim.SetBool("loggedIn", false);
		userPanelAnim.SetBool("panelOpen", false);
		buddiesPanelAnim.SetBool("loggedIn", false);
		buddiesPanelAnim.SetBool("panelOpen", false);

		// Remove SFS2X listeners and re-enable interface
		reset();

		string reason = (string) evt.Params["reason"];

		if (reason != ClientDisconnectionReason.MANUAL) {
			// Show error message
			errorText.text = "Connection was lost; reason is: " + reason;
		}
	}
	
	private void OnLogin(BaseEvent evt) {
		User user = (User) evt.Params["user"];

		// Hide login panel
		loginPanelAnim.SetBool("loggedIn", true);

		// Show user and buddies panel tabs
		userPanelAnim.SetBool("loggedIn", true);
		buddiesPanelAnim.SetBool("loggedIn", true);

		// Set "Logged in as" text
		loggedInText.text = "Logged in as " + user.Name;

		// Initialize buddy list system
		sfs.Send(new Sfs2X.Requests.Buddylist.InitBuddyListRequest());
	}
	
	private void OnLoginError(BaseEvent evt) {
		// Disconnect
		sfs.Disconnect();

		// Remove SFS2X listeners and re-enable interface
		reset();
		
		// Show error message
		errorText.text = "Login failed: " + (string)evt.Params["errorMessage"];
	}

	private void OnBuddyError(BaseEvent evt) {
		Debug.LogError("The following error occurred in the buddy list system: " + (string)evt.Params["errorMessage"]);
	}

	/**
	 * Initializes interface when buddy list data is received.
	 */
	private void OnBuddyListInit(BaseEvent evt) {
		// Populate list of buddies
		OnBuddyListUpdate(evt);
		
		// Set current user details as buddy

		// Nick
		nickInput.text = (sfs.BuddyManager.MyNickName != null ? sfs.BuddyManager.MyNickName : "");
		
		// States
		foreach (string state in sfs.BuddyManager.BuddyStates) {
			string stateValue = state;
			GameObject newDropDownItem = Instantiate(stateItemPrefab) as GameObject;
			BuddyStateItemButton stateItem = newDropDownItem.GetComponent<BuddyStateItemButton>();
			stateItem.stateValue = stateValue;
			stateItem.label.text = stateValue;
			
			stateItem.button.onClick.AddListener(() => OnStateItemClick(stateValue));
			
			newDropDownItem.transform.SetParent(stateDropDown, false);

			// Set current state
			if (sfs.BuddyManager.MyState == state) {
				OnStateItemClick(state);
			}
		}

		// Online
		onlineToggle.isOn = sfs.BuddyManager.MyOnlineState;
		
		// Buddy variables
		BuddyVariable age = sfs.BuddyManager.GetMyVariable(BUDDYVAR_AGE);
		ageInput.text = ((age != null && !age.IsNull()) ? Convert.ToString(age.GetIntValue()) : "");
		
		BuddyVariable mood = sfs.BuddyManager.GetMyVariable(BUDDYVAR_MOOD);
		moodInput.text = ((mood != null && !mood.IsNull()) ? mood.GetStringValue() : "");
	}

	/**
	 * Populates the buddy list.
	 */
	private void OnBuddyListUpdate(BaseEvent evt) {

		// Remove current list content
		for (int i = buddyListContent.childCount - 1; i >= 0; --i) {
			GameObject.Destroy(buddyListContent.GetChild(i).gameObject);
		}
		buddyListContent.DetachChildren();

		// Recreate list content
		foreach (Buddy buddy in sfs.BuddyManager.BuddyList) {
			GameObject newListItem = Instantiate(buddyListItemPrefab) as GameObject;

			BuddyListItem buddylistItem = newListItem.GetComponent<BuddyListItem>();

			// Nickname
			buddylistItem.mainLabel.text = (buddy.NickName != null && buddy.NickName != "") ? buddy.NickName : buddy.Name;

			// Age
			BuddyVariable age = buddy.GetVariable(BuddyMessenger.BUDDYVAR_AGE);
			buddylistItem.mainLabel.text += (age != null && !age.IsNull()) ? " (" + age.GetIntValue() + " yo)" : "";

			// Mood
			BuddyVariable mood = buddy.GetVariable(BuddyMessenger.BUDDYVAR_MOOD);
			buddylistItem.moodLabel.text = (mood != null && !mood.IsNull()) ? mood.GetStringValue() : "";

			// Icon
			if (buddy.IsBlocked) {
				buddylistItem.stateIcon.sprite = IconBlocked;
				buddylistItem.chatButton.interactable = false;
				buddylistItem.blockButton.transform.GetChild(0).GetComponentInChildren<Image>().sprite = IconUnblock;
			}
			else
			{
				buddylistItem.blockButton.transform.GetChild(0).GetComponentInChildren<Image>().sprite = IconBlock;

				if (!buddy.IsOnline) {
					buddylistItem.stateIcon.sprite = IconOffline;
					buddylistItem.chatButton.interactable = false;
				}
				else {
					string state = buddy.State;
					
					if (state == "Available")
						buddylistItem.stateIcon.sprite = IconAvailable;
					else if (state == "Away")
						buddylistItem.stateIcon.sprite = IconAway;
					else if (state == "Occupied")
						buddylistItem.stateIcon.sprite = IconOccupied;
				}
			}

			// Buttons
			string buddyName = buddy.Name; // Required or the listeners will always receive the last buddy name
			buddylistItem.removeButton.onClick.AddListener(() => OnRemoveBuddyButtonClick(buddyName));
			buddylistItem.blockButton.onClick.AddListener(() => OnBlockBuddyButtonClick(buddyName));
			buddylistItem.chatButton.onClick.AddListener(() => OnChatBuddyButtonClick(buddyName));

			buddylistItem.buddyName = buddyName;

			// Add item to list
			newListItem.transform.SetParent(buddyListContent, false);

			// Also update chat panel if open
			Transform panel = chatPanelsContainer.Find(buddyName);
			
			if (panel != null) {
				ChatPanel chatPanel = panel.GetComponent<ChatPanel>();
				chatPanel.buddy = buddy;
			}
		}
	}

	/**
	 * Handles messages receive from buddies.
	 */
	private void OnBuddyMessage(BaseEvent evt) {
		bool isItMe = (bool)evt.Params["isItMe"];
		Buddy sender = (Buddy)evt.Params["buddy"];
		string message = (string)evt.Params["message"];

		Buddy buddy;
		if (isItMe)
		{
			string buddyName = (evt.Params["data"] as ISFSObject).GetUtfString("recipient");
			buddy = sfs.BuddyManager.GetBuddyByName(buddyName);
		}
		else
			buddy = sender;

		if (buddy != null) {
			// Open panel if needed
			OnChatBuddyButtonClick(buddy.Name);

			// Print message
			Transform panel = chatPanelsContainer.Find(buddy.Name);
			ChatPanel chatPanel = panel.GetComponent<ChatPanel>();
			chatPanel.addMessage("<b>" + (isItMe ? "You" : buddy.Name) + ":</b> " + message);
		}
	}
}
