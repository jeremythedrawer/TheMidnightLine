using UnityEngine;
using static Atlas;
using static AtlasUI;
using static Notepad;

[CreateAssetMenu(fileName = "NotepadData", menuName = "Midnight Line SOs / Notepad")]
public class NotepadData : ScriptableObject
{
    public Notepad notepadPrefab;
    public IconButton pageNumberIconButtonPrefab;
    public Page frontPagePrefab;
    public ProfilePage profilePagePrefab;

    public RenderTexture pageFlipRT;
    public ComputeShader pageFlipCompute;
    [Header("Generated")]
    public AtlasClip handFlipPageClip;

    public Vector3 leftHandOffScreenLocalPos;
    public Vector3 inactiveLocalPos;
    public Vector3 hoverLocalPos;
    public Vector3 offSceenLocalPos;
    
    public Vector2 leftHandFlipPos;
    
    public NotepadState curState;
    public NotepadState prevState;
    public SubState subState;

    public float leftHandDepthFront;
    public float activePageDepth;
    public float leftHandDepthBack;

    public int selectedPatternIndex;
    public int pageCount;

    public int pageFlipThreadGroupX;
    public int pageFlipThreadGroupY;

    public int pageFlipKernel;

    public bool collected;

    public bool playerHasUsedExitKey;
    public bool playerHasUsedLeftKey;
    public bool playerHasUsedRightKey;
}
