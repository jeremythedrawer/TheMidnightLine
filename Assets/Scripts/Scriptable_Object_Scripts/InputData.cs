using System;
using UnityEngine;

[CreateAssetMenu(fileName = "SpyInputs_SO", menuName = "Midnight Line SOs / Spy Inputs SO")]
public class InputData : ScriptableObject
{
    public Vector3 mouseScreenPos;
    public Vector3 mouseWorldPos;
    
    public int carouselKeyDownValue;
    public int flipKeyDownValue;
    public int move;
    public int numpad;

    public bool notepadToggleKeyDown;
    public bool notepadToggleKeyUp;

    public bool writeKeyDown;

    public bool ticketCheckKeyDown;
    public bool ticketCheckKeyHold;
    public bool ticketCheckKeyUp;
    
    public bool interactKeyDown;
    
    public bool moveKeyUp;
    public bool moveKeyDown;

    public bool mouseLeftDown;
    public bool mouseLeftHold;
    public bool mouseLeftUp;

    public bool mouseRightDown;
    public bool mouseRightUp;

}
