using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "State")]
public class State : ScriptableObject
{
    [TextArea(14, 20)] [SerializeField] string storyText;

    public string getStateStory()
    {
        return storyText;
    }
}
