using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using Sfs2X.Entities;

public class ChatPanel : MonoBehaviour {

	public Image stateIcon;
	public Button closeButton;
	public ScrollRect scrollRect;
	public Text chatText;
	public Button sendButton;
	public InputField messageInput;
	public Text title;

	public Sprite IconAvailable;
	public Sprite IconAway;
	public Sprite IconOccupied;
	public Sprite IconOffline;
	public Sprite IconBlocked;

	private Buddy _buddy;

	public Buddy buddy
	{
		set {
			this._buddy = value;

			if (_buddy != null) {
				// Set panel name
				this.name = _buddy.Name;

				// Set panel title
				title.text = "Chat with " + (_buddy.NickName != null && _buddy.NickName != "" ? _buddy.NickName : _buddy.Name);

				// Set status icon and enable controls
				if (buddy.IsBlocked) {
					stateIcon.sprite = IconBlocked;
					messageInput.interactable = false;
					sendButton.interactable = false;
				}
				else
				{
					if (!buddy.IsOnline) {
						stateIcon.sprite = IconOffline;
						messageInput.interactable = false;
						sendButton.interactable = false;
					}
					else {
						string state = buddy.State;
						
						if (state == "Available")
							stateIcon.sprite = IconAvailable;
						else if (state == "Away")
							stateIcon.sprite = IconAway;
						else if (state == "Occupied")
							stateIcon.sprite = IconOccupied;

						messageInput.interactable = true;
						sendButton.interactable = true;
					}
				}
			}
		}

		get {
			return this._buddy;
		}
	}

	public void addMessage(string message) {
		chatText.text += message + "\n";

		Canvas.ForceUpdateCanvases();

		// Scroll to bottom
		scrollRect.verticalNormalizedPosition = 0;
	}
}
