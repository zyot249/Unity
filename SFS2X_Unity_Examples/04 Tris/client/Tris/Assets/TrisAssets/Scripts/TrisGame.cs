using UnityEngine;
using System;
using System.Collections;
using Sfs2X;
using Sfs2X.Core;
using Sfs2X.Requests;
using Sfs2X.Entities.Data;

public class TrisGame {

	private SmartFox sfs;
	private int whoseTurn;
	private int myPlayerID;

	public TrisGame() {
		// Nothing to do
	}

	/**
	 * Initialize the game.
	 */
	public void InitGame(SmartFox smartFox) {
		// Register to SmartFox events
		sfs = smartFox;
		sfs.AddEventListener(SFSEvent.EXTENSION_RESPONSE, OnExtensionResponse);

		// Setup my properties
		myPlayerID = sfs.MySelf.PlayerId;

		// Reset game board
		ResetGameBoard();

		// Tell extension I'm ready to play
		sfs.Send(new ExtensionRequest("ready", new SFSObject(), sfs.LastJoinedRoom));
	}

	/**
	 * Destroy the game instance.
	 */
	public void DestroyGame() {
		sfs.RemoveEventListener(SFSEvent.EXTENSION_RESPONSE, OnExtensionResponse);
	}

	/**
	 * Start the game.
	 */
	private void StartGame(int whoseTurn, int p1Id, int p2Id, string p1Name, string p2Name) {
		this.whoseTurn = whoseTurn;

		// Reset game board
		ResetGameBoard();

		// Update interface
		SetTurnMessage();

		// Enable game board
		EnableBoard(true);

		// Reset interface if this is a restart
		GameController controller = (GameController)GameObject.Find("Controller").GetComponent<GameController>();
		controller.SetStartGame();
	}

	/**
	 * Set the "Player's turn" status message.
	 */
	private void SetTurnMessage() {
		// Update interface
		GameController controller = (GameController)GameObject.Find("Controller").GetComponent<GameController>();
		controller.SetPlayerTurnMessage((myPlayerID == whoseTurn) ? "It's your turn" : "It's your opponent's turn");
	}

	/**
	 * Clear the game board.
	 */
	public void ResetGameBoard() {
		for ( int i = 1; i <= 3; i++ ) {
			for ( int j = 1; j <= 3; j++ ) {
				GameObject tile = GameObject.Find("Tile" + i + j);
				TileController ctrl = (TileController)tile.GetComponent("TileController");
				// Player 1 gets the ring
				if ( myPlayerID == 1 ) {
					ctrl.Reset(this, false);
				} else {
					ctrl.Reset(this, true);
				}
			}
		}
	}

	/**
	 * Enable board click.
	 */
	private void EnableBoard(bool enable) {
		if ( myPlayerID == whoseTurn ) {
			for ( int i = 1; i <= 3; i++ ) {
				for ( int j = 1; j <= 3; j++ ) {
					GameObject tile = GameObject.Find("Tile" + i + j);
					TileController ctrl = (TileController)tile.GetComponent("TileController");
					ctrl.Enable(enable);
				}
			}
		}
	}

	/**
	 * On board click, send move to other players.
	 */
	public void PlayerMoveMade(int tileX, int tileY) {
		EnableBoard(false);

		SFSObject obj = new SFSObject();
		obj.PutInt("x", tileX);
		obj.PutInt("y", tileY);
		
		sfs.Send(new ExtensionRequest("move", obj, sfs.LastJoinedRoom));
	}

	/**
	 * Handle the opponent move.
	 */
	private void MoveReceived(int movingPlayer, int x, int y) {
		whoseTurn = ( movingPlayer == 1 ) ? 2 : 1;

		if ( movingPlayer != myPlayerID ) {
			GameObject tile = GameObject.Find("Tile" + x + y);
			TileController ctrl = (TileController)tile.GetComponent("TileController");
			ctrl.SetEnemyMove();
		}

		// Update interface
		SetTurnMessage();

		// Enable interface
		EnableBoard(true);
	}

	/**
	 * Declare game winner.
	 */
	private void ShowWinner(string cmd, int winnerId) {
		// Update interface
		GameController controller = (GameController)GameObject.Find("Controller").GetComponent<GameController>();

		if ( cmd == "win" ) {
			if ( myPlayerID == winnerId ) {
				// I WON! In the next match, it will be my turn first
				controller.SetGameOver("win");
			} else {
				// I've LOST! Next match I will be the second to move
				controller.SetGameOver("loss");
			}
		} else if ( cmd == "tie" ) {
			controller.SetGameOver("tie");
		}

		// Disable game board
		EnableBoard(false);
	}

	/**
	 * Restart the game.
	 */
	public void RestartGame() {
		sfs.Send(new ExtensionRequest("restart", new SFSObject(), sfs.LastJoinedRoom));
	}

	/**
	 * One of the players left the game.
	 */
	private void UserLeft() {
		// Update interface
		GameController controller = (GameController)GameObject.Find("Controller").GetComponent<GameController>();
		controller.SetGameInterrupted();
	}

	//------------------------------------------------------------------------------------

	/**
	 * Handle responses from server side Extension.
	 */
	public void OnExtensionResponse(BaseEvent evt) {
		string cmd = (string)evt.Params["cmd"];
		SFSObject dataObject = (SFSObject)evt.Params["params"];
		
		switch ( cmd ) {
			case "start":
				StartGame(dataObject.GetInt("t"),
					dataObject.GetInt("p1i"),
					dataObject.GetInt("p2i"),
					dataObject.GetUtfString("p1n"),
					dataObject.GetUtfString("p2n")
					);
				break;

			case "stop":
				UserLeft();
				break;

			case "move":
				MoveReceived(dataObject.GetInt("t"), dataObject.GetInt("x"), dataObject.GetInt("y"));
				break;

			case "win":
				ShowWinner(cmd, (int)dataObject.GetInt("w"));
				break;
					
			case "tie":
				ShowWinner(cmd, -1);
				break;
		}
	}
}
