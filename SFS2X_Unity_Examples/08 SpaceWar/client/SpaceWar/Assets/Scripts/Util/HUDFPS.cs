using UnityEngine;
using System.Collections;

public class HUDFPS : MonoBehaviour 
{
	
	// Attach this to a GUIText to make a frames/second indicator.
	//
	// It calculates frames/second over each updateInterval,
	// so the display does not keep changing wildly.
	//
	// It is also fairly accurate at very low FPS counts (<10).
	// We do this not by simply counting frames per interval, but
	// by accumulating FPS for each frame. This way we end up with
	// correct overall FPS even if the interval renders something like
	// 5.5 frames.

	public	Vector2 textPos = new Vector2(10, 10);
	public  float updateInterval = 0.5F;
	
	private float accum   = 0; // FPS accumulated over the interval
	private int   frames  = 0; // Frames drawn over the interval
	private float timeleft; // Left time for current interval4

	private float fps = 0.0f;

	void Start()
	{
		timeleft = updateInterval;  
	}
	
	void Update()
	{
		timeleft -= Time.deltaTime;
		accum += Time.timeScale/Time.deltaTime;
		++frames;
		
		// Interval ended - update GUI text and start new interval
		if( timeleft <= 0.0 )
		{
			// Calculate frame rate
			fps = accum/frames;

			timeleft = updateInterval;
			accum = 0.0F;
			frames = 0;
		}
	}

	void OnGUI()
	{
		string fpsMsg = System.String.Format("{0:F2} FPS",fps);

		// display two fractional digits (f2 format)
		GUIStyle lStyle = new GUIStyle(GUI.skin.label);
		Vector2 labelSize = lStyle.CalcSize(new GUIContent(fpsMsg));

		GUI.Label(new Rect(textPos.x, textPos.y, labelSize.x, labelSize.y), fpsMsg);
	}
}