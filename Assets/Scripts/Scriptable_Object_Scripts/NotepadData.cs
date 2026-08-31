using UnityEngine;
using static Atlas;
using static AtlasUI;
using static Notepad;

[CreateAssetMenu(fileName = "NotepadData", menuName = "Midnight Line SOs / Notepad")]
public class NotepadData : ScriptableObject
{
    public AtlasClip handFlipPage_clip;
    public AtlasClip rotatePencil_clip;

    public Vector3 leftHandOffScreenLocalPos;
    public Vector3 inactiveLocalPos;
    public Vector3 hoverLocalPos;
    public Vector3 offSceenLocalPos;
    
    public Vector2 leftHandFlipPos;
    
    public NotepadState curState;
    public NotepadState prevState;
    public SubState subState;
    public UnlockType abilityIconsShown;

    public float leftHandDepthFront;
    public float activePageDepth;
    public float leftHandDepthBack;

    public int profileWriteCount;
    public int selectedPatternIndex;

    public bool collected;

    public bool playerHasUsedExitKey;
    public bool playerHasUsedLeftKey;
    public bool playerHasUsedRightKey;
}
