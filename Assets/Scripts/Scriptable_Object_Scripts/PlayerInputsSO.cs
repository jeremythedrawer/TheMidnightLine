using UnityEngine;

[CreateAssetMenu(fileName = "SpyInputs_SO", menuName = "Midnight Line SOs / Spy Inputs SO")]
public class PlayerInputsSO : ScriptableObject
{
    public int move;
    public bool run;

    public bool notepadToggle;
    public Vector2 notepadChooseStationAndFlip;
    public bool notepadConfirmStation;

    public bool ticketPressed;
    public bool interact;

    public Vector2 mouseScreenPos;
    public Vector2 mouseWorldPos;
    
    public Vector2 startDragMouseScreenPos;
    public Vector2 endDragMouseScreenPos;

    public bool mouseLeftDown;
    public bool mouseLeftPress;
    public bool mouseLeftUp;
}
