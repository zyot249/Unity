using UnityEngine;
using System.Collections;

public class Background : MonoBehaviour 
{
	public BgLayer layer1;
	public BgLayer layer2;
	public void scroll(float scrollX, float scrollY)
	{
		layer1.scroll(scrollX, scrollY);
		layer2.scroll(scrollX, scrollY);
	}
}
