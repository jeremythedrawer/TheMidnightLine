using UnityEngine;


[CreateAssetMenu(fileName = "UIData", menuName = "Data / UI Data")]
public class UIData : ScriptableObject
{
    public IconButton colorButton;
    public AtlasRenderer tripMapStub;
    public IconButton tripMapStationButton;

    [Header("Generated")]
    public string curDialogueText;

    public Vector3 keyBindWorldPos;
    public Vector3 arrowWorldPos;
    public Vector3 fadeBlackWorldPos;
    public Vector3 inactiveBottomPaneLocalPos;
    public Vector3 activeBottomPanelLocalPos;

    public Vector2 keyBindIconWorldSize;


    public int keyBindSpriteIndex;
}
