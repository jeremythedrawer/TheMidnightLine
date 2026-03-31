using UnityEngine;

[CreateAssetMenu(fileName = "SpyInputs_SO", menuName = "Midnight Line SOs / Spy Inputs SO")]
public class PlayerInputsSO : ScriptableObject
{
    public int move;
    public bool jump;
    public bool run;
    public bool interact;
    public bool ticket;
    public bool map;
    public Vector2 notepad;
    public Vector2 mouseScreenPos;
    public Vector2 mouseWorldPos;
    public Vector2 startDragMouseScreenPos;
    public Vector2 endDragMouseScreenPos;
    public bool mouseLeftDown;
    public bool mouseLeftPress;
    public bool mouseLeftUp;
    public bool cancel;
}
