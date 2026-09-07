using Cysharp.Threading.Tasks;
using Proselyte.Sigils;
using System;
using UnityEngine;
using static Atlas;
using static Passenger;
using static AtlasUI;
public class HenchmanBrain : MonoBehaviour
{
    public static event Action OnCollectNotepad;

    public Options options;

    public SpyData spyData;
    public UIData uiData;
    public InputData inputData;

    public AtlasRenderer atlasRenderer;

    public GameEvent onShowKeyIcon;
    public GameEvent onHideKeyIcon;
    public GameEvent onOpenDialogueBubble;
    public GameEvent onCloseDialogueBubble;

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
            case HenchmanState.Suitcase:
            {
                curClip = atlasRenderer.atlas.clipDict[(int)HenchmanMotion.OpenSuitcase];
                atlasRenderer.PlayClipOneShot(curClip, callback: FlashSuitcase);
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
            case HenchmanState.Suitcase:
            {
                float rawSpyDist = spyData.bounds.center.x - atlasRenderer.bounds.center.x;
                atlasRenderer.FlipHSimple(rawSpyDist < 0);

                if (!atlasRenderer.isAnimating)
                {
                    float absSpyDist = Mathf.Abs(rawSpyDist);
                    if (absSpyDist < spyData.interactionDist)
                    {
                        if (inputData.talkKeyDown)
                        {
                            SetState(HenchmanState.Idle);
                            HideKeyIcon();
                        }
                        else
                        {
                            SetKeyIcon();
                        }
                    }
                    else
                    {
                        atlasRenderer.PlayClipOneShotReverse(curClip, callback: SetToIdle); 
                    }
                }
            }
            break;
            case HenchmanState.Idle:
            {
                atlasRenderer.PlayClip(ref curClip);
                float rawSpyDist = spyData.bounds.center.x - atlasRenderer.bounds.center.x;
                atlasRenderer.FlipHSimple(rawSpyDist < 0);

                float absSpyDist = Mathf.Abs(rawSpyDist);
                if (absSpyDist < spyData.interactionDist)
                {
                    SetKeyIcon();

                    if (inputData.talkKeyDown)
                    {                   
                        SetState(HenchmanState.Suitcase);
                        HideKeyIcon();
                    }
                }
                else
                {
                    HideKeyIcon();
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
            case HenchmanState.Suitcase:
            {

            }
            break;
        }
    }

    private void SetKeyIcon()
    {
        if (uiData.keyBindSpriteIndex == -1)
        {
            uiData.keyBindSpriteIndex = (int)inputData.interactSpriteIndex;
            uiData.keyBindWorldPos.x = atlasRenderer.bounds.center.x;
            uiData.keyBindWorldPos.y = atlasRenderer.bounds.max.y + uiData.keyBindIconWorldSize.y + KEY_ICON_POS_BUFFER;
            uiData.keyBindWorldPos.z = atlasRenderer.bounds.max.z;
            onShowKeyIcon?.Raise();
        }
    }
    private void HideKeyIcon()
    {
        if (uiData.keyBindSpriteIndex != -1)
        {
            uiData.keyBindSpriteIndex = -1;
            onHideKeyIcon?.Raise();
        }
    }
    private void FlashSuitcase()
    {
        atlasRenderer.customBit |= (int)ColorBits.GreenChannel;
    }
    private void SetDialogueText(string text)
    {
        uiData.curDialogueText = text;
        uiData.curDialogueBubbleBounds = atlasRenderer.bounds;
        onOpenDialogueBubble?.Raise();

    }
    private void SetToIdle()
    {
        SetState(HenchmanState.Idle);
    }
#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        
    }
#endif
}
