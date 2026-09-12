using System;
using UnityEngine;
using static Atlas;
using static Notepad;
public class LeftHand : MonoBehaviour
{
    public enum State
    { 
        None,
        Stationary,
        OffScreen,
        FlippingUp,
        FlippingDown
    }

    public const float WRITE_LETTER_TIME = 0.1f;

    const int HOLDING_PENCIL_SPRITE_INDEX = 16;
    const int RESTING_SPRITE_INDEX = 0;
    const float PENCIL_DISTANCE_THRESHOLD = 0.0025f;
    const float MOVE_DAMP = 7f;

    public static event Action OnAtStationaryPos;

    public Options options;
    public CameraData camStats;
    public NotepadData notepadData;
    public AudioData audioData;

    public RenderTexture flipPageRenderTexture;

    public AtlasRenderer atlasRenderer;
    public AudioSource audioSource;

    [Header("Generated")]
    public Page activePage;
    public Page nextPage;
    
    public Vector3 targetLocalPos;

    public State curState;

    public bool atTargetPos;
    private void OnEnable()
    {
        atlasRenderer.onChangeKeyframe += HandSpriteChange;
    }
    private void OnDisable()
    {
        atlasRenderer.onChangeKeyframe -= HandSpriteChange;
    }
    private void Update()
    {
        UpdateState();
    }
    public void SetActivePage(Page page)
    {
        activePage = page;
    }
    public void SetNextPage(Page page) 
    { 
        nextPage = page; 
    }
    public void SetState(State newState)
    {
        if (newState == curState) return;
        ExitState();
        curState = newState;
        EnterState();
    }
    public void EnterState()
    {
        switch(curState)
        {
            case State.Stationary:
            {
                atTargetPos = false;
            }
            break;

            case State.OffScreen:
            {
                atTargetPos = false;

                Bounds rendBounds = atlasRenderer.GetBounds();
                Vector4 uvPivot = atlasRenderer.sprite.uvPivot;
                Vector3 spritePivotOffset = new Vector3(rendBounds.extents.x * (1 - uvPivot.x), rendBounds.size.y * (1 - uvPivot.y));
                //targetLocalPos = notepadData.leftHandOffScreenLocalPos - spritePivotOffset;
            }
            break;

            case State.FlippingUp:
            {
                transform.localPosition = new Vector3(notepadData.leftHandFlipPos.x, notepadData.leftHandFlipPos.y, notepadData.leftHandDepthFront);
                
                atlasRenderer.PlayClipOneShot(notepadData.handFlipPageClip);
                
                Graphics.Blit(Texture2D.blackTexture, notepadData.pageFlipRT);
                notepadData.pageFlipCompute.SetBool("_Reverse", false);
            }
            break;

            case State.FlippingDown:
            {
                transform.localPosition = new Vector3(notepadData.leftHandFlipPos.x, notepadData.leftHandFlipPos.y, notepadData.leftHandDepthBack);
                
                atlasRenderer.PlayClipOneShotReverse(notepadData.handFlipPageClip);

                Graphics.Blit(Texture2D.blackTexture, notepadData.pageFlipRT);
                notepadData.pageFlipCompute.SetBool("_Reverse", true);
            }
            break;
        }
    }
    public void UpdateState()
    {
        switch (curState)
        {
            case State.Stationary:
            {
                if (!atTargetPos)
                {
                    transform.localPosition = Vector3.Lerp(transform.localPosition, targetLocalPos, Time.deltaTime * MOVE_DAMP);

                    float dist = (transform.localPosition - targetLocalPos).sqrMagnitude;
                    if (dist < PENCIL_DISTANCE_THRESHOLD)
                    {
                        OnAtStationaryPos?.Invoke();
                        atTargetPos = true;
                    }
                }
            }
            break;

            case State.OffScreen:
            {
                if (!atTargetPos)
                {
                    transform.localPosition = Vector3.Lerp(transform.localPosition, targetLocalPos, Time.deltaTime * MOVE_DAMP);
                    float dist = (transform.localPosition - targetLocalPos).sqrMagnitude;
                    if (dist < PENCIL_DISTANCE_THRESHOLD)
                    {
                        atlasRenderer.UpdateSpriteInputsByIndex(RESTING_SPRITE_INDEX);
                        Bounds rendBounds = atlasRenderer.GetBounds();
                        Vector4 uvPivot = atlasRenderer.sprite.uvPivot;
                        Vector3 spritePivotOffset = new Vector3(rendBounds.extents.x * (1 - uvPivot.x), rendBounds.size.y * (1 - uvPivot.y));
                       // transform.localPosition = notepadData.leftHandOffScreenLocalPos - spritePivotOffset;

                        atTargetPos = true;

                        bool notepadInUse = (notepadData.subState & Notepad.SubState.InUse) != 0;
                        if (notepadInUse)
                        {
                            SetState(State.Stationary);
                        }
                    }
                }
            }
            break;

            case State.FlippingUp:
            {

            }
            break;

            case State.FlippingDown:
            {

            }
            break;
        }
    }
    public void ExitState()
    {
        switch (curState)
        {
            case State.Stationary:
            {

            }
            break;

            case State.OffScreen:
            {

            }
            break;

            case State.FlippingUp:
            {
                atlasRenderer.UpdateSpriteInputs(atlasRenderer.atlas.motionSprites[notepadData.handFlipPageClip.keyframeStartIndex].sprite);

            }
            break;

            case State.FlippingDown:
            {
                atlasRenderer.UpdateSpriteInputs(atlasRenderer.atlas.motionSprites[notepadData.handFlipPageClip.keyframeStartIndex].sprite);
            }
            break;
        }
    }
    private void HandSpriteChange(MotionSprite curSprite)
    {
        switch(curSprite.sprite.index)
        {
            case 2:
            {
                if (curState == State.FlippingDown)
                {
                    Graphics.Blit(Texture2D.whiteTexture, notepadData.pageFlipRT);
                }
            }
            break;
            case 3:
            case 4:
            case 5:
            {
                DispatchPageFlipCompute();
            }
            break;

            case 6:
            {
                if (curState == State.FlippingDown)
                {
                    transform.localPosition = new Vector3(transform.localPosition.x, transform.localPosition.y, notepadData.leftHandDepthFront);
                    nextPage.SetPageDepth(notepadData.leftHandDepthFront + 2);
                }
                DispatchPageFlipCompute();
            }
            break;

            case 7:
            {
                if (curState == State.FlippingUp)
                {
                    activePage.SetPageDepth(notepadData.leftHandDepthBack + 1);
                    transform.localPosition = new Vector3(transform.localPosition.x, transform.localPosition.y, notepadData.leftHandDepthBack);

                    DispatchPageFlipCompute();
                }
            }
            break;

            case 8:
            {
                if (curState == State.FlippingUp)
                {
                    DispatchPageFlipCompute();
                }
            }
            break;
        }
    }
    private void DispatchPageFlipCompute()
    {
        Vector3 viewportMin = Camera.main.WorldToViewportPoint(atlasRenderer.bounds.min);
        Vector3 viewportMax = Camera.main.WorldToViewportPoint(atlasRenderer.bounds.max);
        Vector2 viewportSize = viewportMax - viewportMin;

        notepadData.pageFlipCompute.SetVector("_ViewportRect", new Vector4(viewportMin.x, viewportMin.y, viewportSize.x, viewportSize.y));

        notepadData.pageFlipCompute.SetVector("_UVSizeAndPos", atlasRenderer.uvSizeAndPosition);

        notepadData.pageFlipCompute.Dispatch(notepadData.pageFlipKernel, notepadData.pageFlipThreadGroupX, notepadData.pageFlipThreadGroupY, 1);
        notepadData.pageFlipCompute.Dispatch(notepadData.pagePropergateKernel, notepadData.pageFlipThreadGroupX, notepadData.pageFlipThreadGroupY, 1);
    }
    public void Init()
    {
        //targetLocalPos = notepadData.leftHandOffScreenLocalPos;
        notepadData.handFlipPageClip = atlasRenderer.atlas.clipDict[(int)NotepadMotion.FlipHand];

        SimpleSprite holdingPencilSprite = atlasRenderer.atlas.motionSprites[HOLDING_PENCIL_SPRITE_INDEX].sprite;
        float worldPivotOffsetY = holdingPencilSprite.worldSize.y * (1 - holdingPencilSprite.uvPivot.y);

       // notepadData.leftHandOffScreenLocalPos.y = camStats.bounds.extents.y - Notepad.ACTIVE_POS.y - camStats.bounds.size.y - worldPivotOffsetY;
       // notepadData.leftHandOffScreenLocalPos.x = -Notepad.ACTIVE_POS.x * 0.5f;
       // notepadData.leftHandOffScreenLocalPos.z = transform.localPosition.z;
    }
    public void Reinit()
    {
        //targetLocalPos = notepadData.leftHandOffScreenLocalPos;
    }
    public void SetLeftHandOffScreen()
    {
        targetLocalPos = notepadData.leftHandFlipPos;
        atlasRenderer.UpdateSpriteInputs(atlasRenderer.atlas.motionSprites[notepadData.handFlipPageClip.keyframeStartIndex].sprite);
    }
}
