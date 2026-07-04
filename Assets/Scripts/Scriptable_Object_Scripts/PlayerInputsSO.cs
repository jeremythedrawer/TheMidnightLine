using UnityEngine;

[CreateAssetMenu(fileName = "SpyInputs_SO", menuName = "Midnight Line SOs / Spy Inputs SO")]
public class PlayerInputsSO : ScriptableObject
{
    public Vector3 mouseScreenPos;
    public Vector3 mouseWorldPos;
    
    public Vector2 notepadPreviewAnswerAndFlip;
    public Vector2 startDragMouseScreenPos;
    public Vector2 endDragMouseScreenPos;

    public int move;
    public int numpad;
    
    public bool notepadKeyDown;
    public bool notepadConfirmAnswer;
    public bool ticketCheckKeyDown;
    public bool ticketCheckKeyHold;
    public bool ticketCheckKeyUp;
    public bool interact;
    public bool moveUp;
    public bool moveDown;

    public bool mouseLeftDown;
    public bool mouseLeftHold;
    public bool mouseLeftUp;
    
    public bool mouseRightDown;
    public bool mouseRightUp;
}
