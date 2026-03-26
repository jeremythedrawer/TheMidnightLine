using UnityEditor;
using UnityEngine;
using static Atlas;

[ExecuteAlways]
public class Notepad : MonoBehaviour
{
    public enum State
    {
        None,
        FlippingUp,
        FlippingDown,
    }
    public enum KeyframeState
    {
        None,
        Start,
        PaperClip,
        TogglePageContentsBottomHalf,
        TogglePageContentsTopHalf,
        ChangeDepth,
    }
    const int KEYFRAMEINDEX_PLAY_PAPER_CLIP = 2;
    const int KEYFRAMEINDEX_TOGGLE_PAGE_CONTENTS_1 = 3;
    const int KEYFRAMEINDEX_TOGGLE_PAGE_CONTENTS_2 = 4;
    const int KEYFRAMEINDEX_CHANGE_DEPTH = 6;

    public PlayerInputsSO playerInputs;
    public AtlasUISimpleRenderer rightHandRenderer;
    public AtlasUISimpleRenderer frontFingers;
    public AtlasUISimpleRenderer bindingRings;
    public AtlasUIMotionRenderer leftHandRenderer;

    public Page activePage;
    [Header("Generated")]
    public Bounds totalBounds;
    public AtlasClip leftHandClip;
    public State curState;
    public KeyframeState curKeyframeState;
    public int lastLeftHandKeyframeIndex;
    public int nextDepthFront;
    public int nextDepthBack;
    public int leftHandDepthFront;
    public int leftHandDepthBack;
    private void OnValidate()
    {
        SetTotalBounds();
#if UNITY_EDITOR
        EditorUtility.SetDirty(this);
#endif
    }
    private void Start()
    {
        leftHandClip = leftHandRenderer.renderInput.atlas.clipDict[(int)NotepadMotion.LeftHand];
        lastLeftHandKeyframeIndex = leftHandClip.keyFrames.Length - 1;

        nextDepthFront = rightHandRenderer.renderInput.batchKey.depthOrder - 1;
        nextDepthBack = rightHandRenderer.renderInput.batchKey.depthOrder + 1;

        leftHandDepthFront =  nextDepthFront - 1;
        leftHandDepthBack = nextDepthBack + 1;


    }

    private void Update()
    {
        if (playerInputs.notepad.y == 1)
        {
            curState = State.FlippingUp;
        }
        if (playerInputs.notepad.y == -1)
        {
            curState = State.FlippingDown;
        }

        switch (curState)
        {
            case State.FlippingUp:
            {
                switch (leftHandRenderer.curFrameIndex)
                {
                    case 0:
                    {
                        if (curKeyframeState == KeyframeState.Start) return;
                        leftHandRenderer.PlayClipOneShot(leftHandClip);
                        curKeyframeState = KeyframeState.Start;
                    }
                    break;

                    case 2:
                    {
                        if (curKeyframeState == KeyframeState.PaperClip) return;
                        activePage.PlayPaperClip();
                        curKeyframeState = KeyframeState.PaperClip;
                    }
                    break;

                    case 3:
                    {
                        if(curKeyframeState == KeyframeState.TogglePageContentsBottomHalf) return;
                        activePage.TogglePageContentBottomHalf(false);
                        curKeyframeState = KeyframeState.TogglePageContentsBottomHalf;
                    }
                    break;

                    case 4:
                    {
                        if (curKeyframeState == KeyframeState.TogglePageContentsTopHalf) return;
                        activePage.TogglePageContentTopHalf(false);
                        curKeyframeState = KeyframeState.TogglePageContentsTopHalf;
                    }
                    break;

                    case 7:
                    {
                        if (curKeyframeState == KeyframeState.ChangeDepth) return;
                        leftHandRenderer.renderInput.UpdateDepthRealtime(leftHandDepthBack);
                        curKeyframeState = KeyframeState.ChangeDepth;
                    }
                    break;
                }

                if (leftHandRenderer.curFrameIndex == lastLeftHandKeyframeIndex)
                {
                    if (curKeyframeState == KeyframeState.None) return;
                    curState = State.None;
                    curKeyframeState = KeyframeState.None;
                }
            }
            break;

            case State.FlippingDown:
            {
                switch (leftHandRenderer.curFrameIndex)
                {
                    case 0:
                    {
                        if (curKeyframeState == KeyframeState.None) return;
                        curState = State.None;
                        curKeyframeState = KeyframeState.None;
                    }
                    break;
                    case 2:
                    {
                        if (curKeyframeState == KeyframeState.TogglePageContentsBottomHalf) return;
                        activePage.TogglePageContentBottomHalf(true);
                        curKeyframeState = KeyframeState.TogglePageContentsBottomHalf;
                    }
                    break;

                    case 3:
                    {
                        if (curKeyframeState == KeyframeState.TogglePageContentsTopHalf) return;
                        activePage.TogglePageContentTopHalf(true);
                        curKeyframeState= KeyframeState.TogglePageContentsTopHalf;
                    }
                    break;
                    case 4:
                    {
                        if (curKeyframeState == KeyframeState.PaperClip) return;
                        activePage.PlayPaperClipReverse();
                        curKeyframeState = KeyframeState.PaperClip;
                    }
                    break;
                    case 6:
                    {
                        if (curKeyframeState == KeyframeState.ChangeDepth) return;
                        leftHandRenderer.renderInput.UpdateDepthRealtime(leftHandDepthFront);
                        curKeyframeState = KeyframeState.ChangeDepth;
                    }
                    break;
                }

                if (leftHandRenderer.curFrameIndex == lastLeftHandKeyframeIndex)
                {
                    if (curKeyframeState == KeyframeState.Start) return;
                    leftHandRenderer.PlayClipOneShotReverse(leftHandClip);
                    curKeyframeState = KeyframeState.Start;
                }
            }
            break;
        }
    }
    private void SetTotalBounds()
    {
        if (rightHandRenderer == null || frontFingers == null || bindingRings == null) return;
        totalBounds = rightHandRenderer.renderInput.bounds;
        totalBounds.Encapsulate(frontFingers.renderInput.bounds);
        totalBounds.Encapsulate(bindingRings.renderInput.bounds);
        totalBounds.Encapsulate(leftHandRenderer.renderInput.bounds);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.indigo;
        Gizmos.DrawWireCube(totalBounds.center, totalBounds.size);
    }
}
