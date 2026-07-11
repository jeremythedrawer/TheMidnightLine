using UnityEngine;
using static Atlas;
using static AtlasUI;

[CreateAssetMenu(fileName = "NotepadData", menuName = "Midnight Line SOs / Notepad")]
public class NotepadData : ScriptableObject
{
    public AtlasClip handFlipPage_clip;
    public AtlasClip rotatePencil_clip;

    public NotepadState curState;
    public NotepadState prevState;
    public UnlockType completedUnlocks;

    public Vector3 leftHandFlipPos;
    public Vector3 leftHandPencilPos;
    public Vector3 leftHandOffScreenLocalPos;

    public int leftHandWorldDepthFront;
    public int leftHandWorldDepthBack;

}
