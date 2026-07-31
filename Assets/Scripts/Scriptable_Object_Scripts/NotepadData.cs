using UnityEngine;
using static Atlas;
using static AtlasUI;
using static Notepad;

[CreateAssetMenu(fileName = "NotepadData", menuName = "Midnight Line SOs / Notepad")]
public class NotepadData : ScriptableObject
{
    public AtlasClip handFlipPage_clip;
    public AtlasClip rotatePencil_clip;

    public NotepadState curState;
    public NotepadState prevState;
    public SubState subState;
    public UnlockType completedUnlocks;

    public Vector3 leftHandFlipPos;
    public Vector3 leftHandPencilPos;
    public Vector3 leftHandOffScreenLocalPos;

    public Vector3 inactiveLocalPos;
    public Vector3 hoverLocalPos;
    public Vector3 activeLocalPos;

    public int leftHandWorldDepthFront;
    public int leftHandWorldDepthBack;

    public bool checkingNotepad;
    public bool collected;
}
