using UnityEngine;

[CreateAssetMenu(fileName = "SpyInputs_SO", menuName = "Midnight Line SOs / Spy Inputs SO")]
public class PlayerInputsSO : ScriptableObject
{
    public int move;
    public int numpad;
    public bool notepadKeyDown;
    public Vector2 notepadPreviewAnswerAndFlip;
    public bool notepadConfirmAnswer;

    public bool ticketCheckKeyDown;
    public bool ticketCheckKeyUp;
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
