using System;
using UnityEngine;

using static AtlasUI;

[CreateAssetMenu(fileName = "SpyInputs_SO", menuName = "Midnight Line SOs / Spy Inputs SO")]
public class InputData : ScriptableObject
{
    public Vector3 mouseScreenPos;
    public Vector3 mouseWorldPos;
    
    public int carouselKeyDownValue;
    public int flipKeyDownValue;
    public int move;
    public int numpad;

    public KeybindSpriteIndex interactSpriteIndex;

    public bool notepadToggleKeyDown;
    public bool notepadToggleKeyUp;

    public bool talkKeyDown;
    public bool talkKeyHold;
    public bool talkKeyUp;
    
    public bool interactKeyDown;
    
    public bool moveKeyUp;
    public bool moveKeyDown;

    public bool mouseLeftDown;
    public bool mouseLeftHold;
    public bool mouseLeftUp;

    public bool mouseRightDown;
    public bool mouseRightUp;

}
