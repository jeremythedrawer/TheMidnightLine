using Cysharp.Threading.Tasks;
using Proselyte.Sigils;
using System;
using UnityEngine;
using static Atlas;
using static Passenger;
using static AtlasUI;
public class HenchmanBrain : MonoBehaviour
{
    public SpyData spyData;
    public Options options;
    public UIData uiData;

    public AtlasRenderer atlasRenderer;

    public GameEvent onShowKeyIcon;
    public GameEvent onOpenDialogueBubble;

    [Header("Generated")]
    public HenchmanState curState;
    public AtlasClip curClip;
    private void Start()
    {
        SetState(HenchmanState.Idle);
    }
    private void Update()
    {
        UpdateState();
    }
    public void SetState(HenchmanState newstate)
    {
        if (newstate == curState) return;
        ExitState();
        curState = newstate;
        EnterState();
    }
    public void EnterState()
    {
        switch (curState)
        {
            case HenchmanState.Walking:
            {
                curClip = atlasRenderer.atlas.clipDict[(int)HenchmanMotion.Walking];
            }
            break;
            case HenchmanState.OpeningSuitcase:
            {
                curClip = atlasRenderer.atlas.clipDict[(int)HenchmanMotion.OpenSuitcase];
                atlasRenderer.PlayClipOneShot(curClip, callback: SetNotepadText);


            }
            break;
            case HenchmanState.Idle:
            {
                curClip = atlasRenderer.atlas.clipDict[(int)HenchmanMotion.StandingBreathing];


            }
            break;
        }
    }
    public void UpdateState()
    {
        switch (curState)
        {
            case HenchmanState.Walking:
            {
                atlasRenderer.PlayClip(ref curClip);
            }
            break;
            case HenchmanState.OpeningSuitcase:
            {

            }
            break;
            case HenchmanState.Idle:
            {
                atlasRenderer.PlayClip(ref curClip);

                float spyDist = Mathf.Abs(spyData.bounds.center.x - atlasRenderer.bounds.center.x);

                if (spyDist < spyData.interactionDist)
                {
                    SetState(HenchmanState.OpeningSuitcase);
                }
            }
            break;
        }
    }
    public void ExitState()
    {
        switch (curState)
        {
            case HenchmanState.Walking:
            {

            }
            break;
            case HenchmanState.OpeningSuitcase:
            {

            }
            break;
        }
    }

    private void SetKeyIcon()
    {
        uiData.keyBindSpriteIndex = (int)KeybindSpriteIndices.E;
        uiData.keyBindWorldPos.x = atlasRenderer.bounds.center.x;
        uiData.keyBindWorldPos.y = atlasRenderer.bounds.max.y + uiData.keyBindIconWorldSize.y + KEY_ICON_POS_BUFFER;
        uiData.keyBindWorldPos.z = atlasRenderer.bounds.max.z;

        onShowKeyIcon?.Raise();
    }

    private void SetNotepadText()
    {
        uiData.curDialogueText = "Take this";
        uiData.curDialogueBubbleBounds = atlasRenderer.bounds;
        onOpenDialogueBubble?.Raise();

    }
#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        
    }
#endif
}
