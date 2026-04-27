using UnityEngine;

[CreateAssetMenu(fileName = "SpyInputs_SO", menuName = "Midnight Line SOs / Spy Inputs SO")]
public class PlayerInputsSO : ScriptableObject
{
    public int move;
    public bool run;

    public bool notepadKeyDown;
    public Vector2 notepadChooseStationAndFlip;
    public bool notepadConfirmStation;

    public bool ticketCheckKeyDown;
    public bool interact;

    public Vector3 mouseScreenPos;
    public Vector3 mouseWorldPos;
    
    public Vector2 startDragMouseScreenPos;
    public Vector2 endDragMouseScreenPos;

    public bool mouseLeftDown;
    public bool mouseLeftHold;
    public bool mouseLeftUp;

    public bool mouseRightDown;
}
