using UnityEngine;
using System.Collections;

public class BgLayer : MonoBehaviour 
{
	public float paralax = 0.05f;
	
	public void scroll(float scrollX, float scrollY)
	{
		GetComponent<Renderer>().material.mainTextureOffset = new Vector2(GetComponent<Renderer>().material.mainTextureOffset.x + scrollX * paralax, GetComponent<Renderer>().material.mainTextureOffset.y + scrollY * paralax);
	}
}
