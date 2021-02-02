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
using Sfs2X.Requests;
using Sfs2X.Entities.Data;

public class GameController : MonoBehaviour {

	//----------------------------------------------------------
	// UI elements and public properties
	//----------------------------------------------------------

	public Animator chatPanelAnim;
	public ScrollRect chatScrollView;
	public Text chatText;
	public CanvasGroup chatControls;
	public Text stateText;
	public Button restartButton;

	//----------------------------------------------------------
	// Private properties
	//----------------------------------------------------------
	
	private enum GameState {
		WAITING_FOR_PLAYERS = 0,
		RUNNING,
		GAME_WON,
		GAME_LOST,
		GAME_TIE,
		GAME_DISRUPTED
	};

	private SmartFox sfs;
	private bool shuttingDown;
	private TrisGame trisGame;

	//----------------------------------------------------------
	// Unity calback methods
	//----------------------------------------------------------

	void Awake() {
		Application.runInBackground = true;
		
		if (SmartFoxConnection.IsInitialized) {
			sfs = SmartFoxConnection.Connection;
		} else {
			SceneManager.LoadScene("Login");
			return;
		}

		sfs.AddEventListener(SFSEvent.CONNECTION_LOST, OnConnectionLost);
		sfs.AddEventListener(SFSEvent.PUBLIC_MESSAGE, OnPublicMessage);
		sfs.AddEventListener(SFSEvent.USER_ENTER_ROOM, OnUserEnterRoom);
		sfs.AddEventListener(SFSEvent.USER_EXIT_ROOM, OnUserExitRoom);
		
		setCurrentGameState(GameState.WAITING_FOR_PLAYERS);

		// Create game logic controller instance
		trisGame = new TrisGame();
		trisGame.InitGame(sfs);
	}
	
	// Update is called once per frame
	void Update() {
		if (sfs != null)
			sfs.ProcessEvents();
	}

	void OnApplicationQuit() {
		shuttingDown = true;
	}

	//----------------------------------------------------------
	// Public interface methods for UI
	//----------------------------------------------------------

	public void OnChatTabClick() {
		chatPanelAnim.SetBool("panelOpen", !chatPanelAnim.GetBool("panelOpen"));
	}

	public void OnSendMessageButtonClick() {
		InputField msgField = (InputField) chatControls.GetComponentInChildren<InputField>();

		if (msgField.text != "") {
			// Send public message to Room
			sfs.Send(new Sfs2X.Requests.PublicMessageRequest(msgField.text));

			// Reset message field
			msgField.text = "";
			msgField.ActivateInputField();
			msgField.Select();
		}
	}

	public void OnSendMessageKeyPress() {
		if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
			OnSendMessageButtonClick();
	}

	public void OnRestartButtonClick() {
		trisGame.RestartGame();
	}
	
	public void OnLeaveGameButtonClick() {
		// Remove SFS2X listeners
		reset();

		// Destroy current game
		trisGame.DestroyGame();

		// Leave current room
		sfs.Send(new Sfs2X.Requests.LeaveRoomRequest());

		// Return to lobby scene
		SceneManager.LoadScene("Lobby");
	}
	
	//----------------------------------------------------------
	// Public methods
	//----------------------------------------------------------

	public void SetStartGame() {
		printSystemMessage("Game started! May the best player win");
		setCurrentGameState(GameState.RUNNING);
		restartButton.interactable = false;
	}

	public void SetPlayerTurnMessage(string turnMsg) {
		stateText.text = turnMsg;
	}
	
	public void SetGameInterrupted() {
		setCurrentGameState(GameState.GAME_DISRUPTED);
	}
	
	public void SetGameOver(string result) {
		string message = "Game over!";

		if ( result == "win" ) {
			setCurrentGameState(GameState.GAME_WON);
			printSystemMessage(message + "\nCongratulations, you won!");
		} else if ( result == "loss" ) {
			setCurrentGameState(GameState.GAME_LOST);
			printSystemMessage(message + "\nToo bad, you've lost!");
		} else {
			setCurrentGameState(GameState.GAME_TIE);
			printSystemMessage(message + "\nIt's a tie!");
		}

		restartButton.interactable = true;
	}

	//----------------------------------------------------------
	// Private helper methods
	//----------------------------------------------------------

	private void setCurrentGameState(GameState state) {
		if (state == GameState.WAITING_FOR_PLAYERS) {
			stateText.text = "Waiting for your opponent";
		} else if (state == GameState.RUNNING) {
			// Nothing to do; the state text is updated by the TrisGame instance
		} else if (state == GameState.GAME_DISRUPTED) {
			stateText.text = "Opponent disconnected; waiting for new player";
		} else {
			stateText.text = "GAME OVER";

			if (state == GameState.GAME_LOST) {
				stateText.text += "\nYou've lost!";
			} else if (state == GameState.GAME_WON) {
				stateText.text += "\nYou won!";
			} else if (state == GameState.GAME_TIE) {
				stateText.text += "\nIt's a tie!";
			}
		}
	}
	
	private void reset() {
		// Remove SFS2X listeners
		sfs.RemoveAllEventListeners();
	}

	private void printSystemMessage(string message) {
		chatText.text += "<color=#808080ff>" + message + "</color>\n";

		Canvas.ForceUpdateCanvases();

		// Scroll view to bottom
		chatScrollView.verticalNormalizedPosition = 0;
	}
	
	private void printUserMessage(User user, string message) {
		chatText.text += "<b>" + (user == sfs.MySelf ? "You" : user.Name) + ":</b> " + message + "\n";

		Canvas.ForceUpdateCanvases();

		// Scroll view to bottom
		chatScrollView.verticalNormalizedPosition = 0;
	}

	//----------------------------------------------------------
	// SmartFoxServer event listeners
	//----------------------------------------------------------
	
	private void OnConnectionLost(BaseEvent evt) {
		// Remove SFS2X listeners
		reset();

		if (shuttingDown == true)
			return;

		// Return to login scene
		SceneManager.LoadScene("Login");
	}
	
	private void OnPublicMessage(BaseEvent evt) {
		User sender = (User) evt.Params["sender"];
		string message = (string) evt.Params["message"];

		printUserMessage(sender, message);
	}
	
	private void OnUserEnterRoom(BaseEvent evt) {
		User user = (User) evt.Params["user"];

		// Show system message
		printSystemMessage("User " + user.Name + " entered the room");
	}
	
	private void OnUserExitRoom(BaseEvent evt) {
		User user = (User) evt.Params["user"];

		if (user != sfs.MySelf) {
			// Show system message
			printSystemMessage("User " + user.Name + " left the room");
		}
	}
}
