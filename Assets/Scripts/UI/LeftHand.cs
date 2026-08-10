using System;
using UnityEngine;
using static Atlas;
public class LeftHand : MonoBehaviour
{
    public enum State
    { 
        None,
        Writing,
        Erasing,
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
    const float PENCIL_VERTICAL_FREQUENCY = 7f;
    const float PENCIL_VERTICAL_MAGNITUDE = 0.07f;

    public static event Action OnAtStartWritePos;
    public static event Action OnFinishWriting;
    public static event Action OnAtStartErasePos;
    public static event Action OnFinishErasing;
    public static event Action OnAtStationaryPos;

    public CameraStatsSO camStats;
    public TripSO curTrip;

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
    private void OnEnable()
    {
        ColorPicker.OnCloseCluePicker += ReturnAfterColorPickerClose;
        ColorPicker.OnOpenCluePicker += MoveForColorPicker;

        Page.OnEnterColorKeyIcon += MoveToLeftOfPaper;
        Page.OnExitColorKeyIcon += MoveBackToTextBounds;
    }
    private void OnDisable()
    {
        ColorPicker.OnCloseCluePicker -= ReturnAfterColorPickerClose;
        ColorPicker.OnOpenCluePicker -= MoveForColorPicker;
        Page.OnEnterColorKeyIcon -= MoveToLeftOfPaper;
        Page.OnExitColorKeyIcon -= MoveBackToTextBounds;
    }
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
            case State.Writing:
            {
                curPencilTime = 0;
                atTargetPos = false;
                atlasRenderer.UpdateSpriteInputs(atlasRenderer.atlas.motionSprites[notepadData.rotatePencil_clip.keyframeStartIndex].sprite);
                totalPencilTime = (activePage.activePreviewPlayerWriteText.Length + 1) * WRITE_LETTER_TIME;

                MoveToEdgeTextBounds(leftEdge: true);
            }
            break;
            
            case State.Erasing:
            {
                curPencilTime = 0;
                atTargetPos = false;
                atlasRenderer.PlayClipOneShot(notepadData.rotatePencil_clip);

                totalPencilTime = (activePage.activePlayerWriteText.Length + 1) * WRITE_LETTER_TIME;

                MoveToEdgeTextBounds(leftEdge: false);
            }
            break;

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
            case State.Writing:
            {
                if (!atTargetPos)
                {
                    transform.localPosition = Vector3.Lerp(transform.localPosition, targetLocalPos, Time.deltaTime * MOVE_DAMP);

                    float dist = (transform.localPosition - targetLocalPos).sqrMagnitude;
                    if (dist < PENCIL_DISTANCE_THRESHOLD)
                    {
                        OnAtStartWritePos?.Invoke();
                        atTargetPos = true;
                    }
                }
                else
                {
                    curPencilTime += Time.deltaTime;
                    float t = curPencilTime / totalPencilTime;
                    curTextBounds = activePage.GetWritingBoundsOfNewText(activePage.activePlayerWriteText);
                    float curWorldPosX = Mathf.Lerp(curTextBounds.min.x, curTextBounds.max.x, t);
                    float randOffset = Mathf.PerlinNoise(curPencilTime * PENCIL_VERTICAL_FREQUENCY, curPencilTime * PENCIL_VERTICAL_FREQUENCY) * 2 - 1;
                    float curWorldPosY = curTextBounds.center.y + (randOffset * PENCIL_VERTICAL_MAGNITUDE);
                    transform.localPosition = transform.parent.InverseTransformPoint(new Vector3(curWorldPosX, curWorldPosY, transform.position.z));
                    if (t > 1f) OnFinishWriting?.Invoke();
                }
            }
            break;

            case State.Erasing:
            {
                if (!atTargetPos)
                {
                    transform.localPosition = Vector3.Lerp(transform.localPosition, targetLocalPos, Time.deltaTime * MOVE_DAMP);

                    float dist = (transform.localPosition - targetLocalPos).sqrMagnitude;
                    if (dist < PENCIL_DISTANCE_THRESHOLD * PENCIL_DISTANCE_THRESHOLD)
                    {
                        OnAtStartErasePos?.Invoke();
                        atTargetPos = true;
                    }
                }
                else
                {
                    curPencilTime += Time.deltaTime;
                    float t = curPencilTime / totalPencilTime;
                    curTextBounds = activePage.GetWritingBoundsOfNewText(activePage.activePlayerWriteText);
                    float curWorldPosX = Mathf.Lerp(curTextBounds.max.x, curTextBounds.min.x, t);
                    float randOffset = Mathf.PerlinNoise(curPencilTime * PENCIL_VERTICAL_FREQUENCY, curPencilTime * PENCIL_VERTICAL_FREQUENCY) * 2 - 1;
                    float curWorldPosY = curTextBounds.center.y + (randOffset * PENCIL_VERTICAL_MAGNITUDE);
                    transform.localPosition = transform.parent.InverseTransformPoint(new Vector3(curWorldPosX, curWorldPosY, transform.position.z));
                    if (t > 1f) OnFinishErasing?.Invoke();
                }
            }
            break;

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

                        if (activePage.activePlayerWriteTextRenderer != null && !movedForColorPicker)
                        {
                            SetState(State.Stationary);
                            MoveToEdgeTextBounds(leftEdge: true);
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
            case State.Writing:
            {

            }
            break;

            case State.Erasing:
            {
                atlasRenderer.PlayClipOneShotReverse(notepadData.rotatePencil_clip);
                MoveToEdgeTextBounds(leftEdge: true);
            }
            break;

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

            }
            break;

            case State.FlippingDown:
            {

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

        notepadData.leftHandOffScreenLocalPos.y = camStats.camBounds.extents.y - Notepad.ACTIVE_POS.y - camStats.camBounds.size.y - worldPivotOffsetY;
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
    public void ReturnAfterColorPickerClose()
    {
        movedForColorPicker = false;
        SetState(State.Stationary);
        MoveToEdgeTextBounds(leftEdge: true);
    }
    public void MoveForColorPicker()
    {
        movedForColorPicker = true;
        SetState(State.OffScreen);
    }
    public void MoveToLeftOfPaper()
    {
        if (curState == State.Stationary)
        {
            atTargetPos = false;
            Bounds paperBounds = activePage.paperRenderer.GetBounds();
            curTextBounds = activePage.GetCurrentWritingBounds();
            Vector2 writePos = new Vector2();
            writePos.x = paperBounds.min.x;
            writePos.y = curTextBounds.center.y;

            targetLocalPos = transform.parent.InverseTransformPoint(writePos);
            targetLocalPos.z = notepadData.leftHandDepthFront;
        }

    }
    public void MoveBackToTextBounds()
    {
        if (curState == State.Stationary)
        {
            MoveToEdgeTextBounds(leftEdge: true);
        }
    }
    public void MoveToEdgeTextBounds(bool leftEdge)
    {
        atTargetPos = false;
        curTextBounds = activePage.GetCurrentWritingBounds();

        Vector2 writePos = new Vector2();
        writePos.x = leftEdge ? curTextBounds.min.x : curTextBounds.max.x;
        writePos.y = curTextBounds.center.y;

        targetLocalPos = transform.parent.InverseTransformPoint(writePos);
        targetLocalPos.z = notepadData.leftHandDepthFront;
    }
}
