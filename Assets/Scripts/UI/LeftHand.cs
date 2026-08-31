using System;
using UnityEngine;
using static Atlas;
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

    public CameraData camStats;
    public TripData curTrip;
    public SceneData sceneData;

    public AtlasRenderer atlasRenderer;
    public NotepadData notepadData;

    [Header("Generated")]
    public Page activePage;

    public Bounds curTextBounds;
    
    public Vector3 targetLocalPos;

    public State curState;

    public float totalPencilTime;
    public float curPencilTime;

    public bool atTargetPos;
    public bool movedForColorPicker;
    private void Update()
    {
        UpdateState();
    }
    public void SetActivePage(Page page)
    {
        activePage = page;
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
                atlasRenderer.UpdateSpriteInputs(atlasRenderer.atlas.motionSprites[notepadData.rotatePencil_clip.keyframeStartIndex].sprite);
            }
            break;

            case State.OffScreen:
            {
                atTargetPos = false;

                Bounds rendBounds = atlasRenderer.GetBounds();
                Vector4 uvPivot = atlasRenderer.sprite.uvPivot;
                Vector3 spritePivotOffset = new Vector3(rendBounds.extents.x * (1 - uvPivot.x), rendBounds.size.y * (1 - uvPivot.y));
                targetLocalPos = notepadData.leftHandOffScreenLocalPos - spritePivotOffset;
            }
            break;

            case State.FlippingUp:
            {
                transform.localPosition = new Vector3(notepadData.leftHandFlipPos.x, notepadData.leftHandFlipPos.y, notepadData.leftHandDepthFront);
                atlasRenderer.PlayClipOneShot(notepadData.handFlipPage_clip);
            }
            break;

            case State.FlippingDown:
            {
                transform.localPosition = new Vector3(notepadData.leftHandFlipPos.x, notepadData.leftHandFlipPos.y, notepadData.leftHandDepthBack);
                atlasRenderer.PlayClipOneShotReverse(notepadData.handFlipPage_clip);
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
                        transform.localPosition = notepadData.leftHandOffScreenLocalPos - spritePivotOffset;

                        atTargetPos = true;

                        bool notepadInUse = (notepadData.subState & Notepad.SubState.InUse) != 0;
                        bool atTripScene = sceneData.activeSceneType == Scenes.SceneType.Trip;
                        if (atTripScene && notepadInUse)
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
                atlasRenderer.UpdateSpriteInputs(atlasRenderer.atlas.motionSprites[notepadData.handFlipPage_clip.keyframeStartIndex].sprite);
            }
            break;

            case State.FlippingDown:
            {
                atlasRenderer.UpdateSpriteInputs(atlasRenderer.atlas.motionSprites[notepadData.handFlipPage_clip.keyframeStartIndex].sprite);
            }
            break;
        }
    }
    public void Init()
    {
        targetLocalPos = notepadData.leftHandOffScreenLocalPos;
        notepadData.handFlipPage_clip = atlasRenderer.atlas.clipDict[(int)NotepadMotion.FlipHand];
        notepadData.rotatePencil_clip = atlasRenderer.atlas.clipDict[(int)NotepadMotion.RotatingPencil];

        SimpleSprite holdingPencilSprite = atlasRenderer.atlas.motionSprites[HOLDING_PENCIL_SPRITE_INDEX].sprite;
        float worldPivotOffsetY = holdingPencilSprite.worldSize.y * (1 - holdingPencilSprite.uvPivot.y);

        notepadData.leftHandOffScreenLocalPos.y = camStats.bounds.extents.y - Notepad.ACTIVE_POS.y - camStats.bounds.size.y - worldPivotOffsetY;
        notepadData.leftHandOffScreenLocalPos.x = -Notepad.ACTIVE_POS.x * 0.5f;
        notepadData.leftHandOffScreenLocalPos.z = transform.localPosition.z;
    }
    public void Reinit()
    {
        targetLocalPos = notepadData.leftHandOffScreenLocalPos;
    }
    public void SetLeftHandOffScreen()
    {
        targetLocalPos = notepadData.leftHandFlipPos;
        atlasRenderer.UpdateSpriteInputs(atlasRenderer.atlas.motionSprites[notepadData.handFlipPage_clip.keyframeStartIndex].sprite);
    }
}
