using Proselyte.Sigils;
using UnityEngine;


[CreateAssetMenu(fileName = "UIData", menuName = "Data / UI Data")]
public class UIData : ScriptableObject
{
    public string curDialogueText;

    public Bounds curDialogueBubbleBounds;

    public Vector3 keyBindWorldPos;

    public Vector2 keyBindIconWorldSize;
    
    public int keyBindSpriteIndex;
}
