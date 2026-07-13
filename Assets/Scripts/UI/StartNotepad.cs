using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using static Atlas;
using static AtlasUI;
using static Scenes;

public class StartNotepad : MonoBehaviour
{
    public PlayerInputsSO playerInputs;
    public TripSO trip;
    public CameraStatsSO camStats;
    public SceneData sceneData;
    public GameEventDataSO gameEventData;
    public ColorsSO colorsData;
    public NotepadData notepadData;

    public Material fadeBlackMaterial;

    public ColorPicker colorPicker;

    public AtlasRenderer rightHand_renderer;
    public AtlasRenderer frontFingers_renderer;
    public AtlasRenderer bindingRingsRend;
    public AtlasRenderer leftHand_renderer;

    public Transform pageTransform;

    public Page startPage;
    public Page optionsPage;

    [Header("Generated")]

    public Page[] pages;

    public Page activePage;
    public Page nextPage;


    public Bounds curWritingBounds;

    public Vector3 leftHandTargetLocalPos;

    public NotepadKeyframeState curKeyframeState;
    public NotepadSubState subState;

    public int activePageIndex;
    public int lastPageIndex;

    public float totalPencilTime;
    public float curPencilTime;
    public float appearTextClock;


    public bool atStartPencilPos;
    public bool atOffCameraPos;

    public CancellationTokenSource ctsFadeBlack;


    private void Start()
    {
        CreatePages();

        notepadData.handFlipPage_clip = leftHand_renderer.atlas.clipDict[(int)NotepadMotion.FlipHand];
        notepadData.rotatePencil_clip = leftHand_renderer.atlas.clipDict[(int)NotepadMotion.RotatingPencil];
        notepadData.leftHandWorldDepthFront = (int)(bindingRingsRend.transform.position.z - 1);
        notepadData.leftHandWorldDepthBack = (int)(rightHand_renderer.transform.position.z + 1);

        Vector3 flipWorldPos = new Vector3();
        flipWorldPos.x = bindingRingsRend.transform.position.x;
        flipWorldPos.y = bindingRingsRend.transform.position.y;
        flipWorldPos.z = leftHand_renderer.transform.position.z;

        notepadData.leftHandFlipPos = transform.InverseTransformPoint(flipWorldPos);

        SimpleSprite holdingPencilSprite = leftHand_renderer.atlas.motionSprites[HOLDING_PENCIL_SPRITE_INDEX].sprite;
        float worldPivotOffsetY = holdingPencilSprite.worldSize.y * (1 - holdingPencilSprite.uvPivot.y);

        notepadData.leftHandOffScreenLocalPos.x = -NotepadActiveLocalPos.x * 0.5f;
        notepadData.leftHandOffScreenLocalPos.y = camStats.camBounds.extents.y - NotepadActiveLocalPos.y - camStats.camBounds.size.y - worldPivotOffsetY;
        notepadData.leftHandOffScreenLocalPos.z = leftHand_renderer.transform.localPosition.z;

        notepadData.curState = NotepadState.None;

        leftHandTargetLocalPos = notepadData.leftHandOffScreenLocalPos;
        activePage = startPage;
    }
    private void Update()
    {
        if ((subState & NotepadSubState.InUse) != 0)
        {
            ChooseState();
            UpdateState();
        }
        else
        {
            if ((subState & NotepadSubState.OnScreen) != 0 && transform.localPosition == NotepadInactiveLocalPos)
            {
                subState &= ~(NotepadSubState.OnScreen);
                OffScreen();
            }
        }
    }
    private void OnApplicationQuit()
    {
        notepadData.checkingNotepad = false;
    }
    public void OffScreen()
    {
        leftHandTargetLocalPos = notepadData.leftHandFlipPos;
        leftHand_renderer.UpdateSpriteInputs(leftHand_renderer.atlas.motionSprites[notepadData.handFlipPage_clip.keyframeStartIndex].sprite);
    }
    public void EnterNotepad()
    {
        subState |= (NotepadSubState.InUse | NotepadSubState.OnScreen);
        notepadData.checkingNotepad = true;
        EnterState(NotepadState.None);
    }
    public void ExitNotepad()
    {
        Bounds rendBounds = leftHand_renderer.GetBounds();
        Vector4 uvPivot = leftHand_renderer.sprite.uvPivot;
        Vector3 spritePivotOffset = new Vector3(rendBounds.extents.x * (1 - uvPivot.x), rendBounds.size.y * (1 - uvPivot.y));
        leftHandTargetLocalPos = notepadData.leftHandOffScreenLocalPos + spritePivotOffset;
        subState &= ~(NotepadSubState.InUse);
        colorPicker.Close();
    }
    private void ChooseState()
    {
        switch (activePage.pageType)
        {
            case PageType.Start:
            {
                if (ToFlipUp())
                {
                    SetState(NotepadState.FlippingUp);
                }
                else if (ToErase())
                {
                    SetState(NotepadState.Erasing);
                }
                else if (ToWrite())
                {
                    SetState(NotepadState.Writing);
                }
                else
                {
                    SetState(NotepadState.Stationary);
                }
            }
            break;

            case PageType.Options:
            {
                if (ToFlipDown())
                {
                    SetState(NotepadState.FlippingDown);
                }
                else
                {
                    SetState(NotepadState.Stationary);
                }
            }
            break;
        }
    }
    private void SetState(NotepadState newState)
    {
        if (notepadData.curState == newState) return;
        ExitState();
        notepadData.prevState = notepadData.curState;
        notepadData.curState = newState;
        EnterState(notepadData.prevState);
    }
    private void UpdateState()
    {
        switch (notepadData.curState)
        {
            case NotepadState.FlippingUp:
            {
                if ((subState & NotepadSubState.CanWillFlipUp) == 0)
                {
                    subState |= NotepadSubState.CanWillFlipUp;
                }
                else
                {
                    if (activePageIndex < lastPageIndex - 1 && playerInputs.notepadPreviewAnswerAndFlip.y == 1)
                    {
                        subState |= NotepadSubState.WillFlipUp;
                        subState &= ~(NotepadSubState.WillFlipDown);
                    }
                    if (activePageIndex > 0 && playerInputs.notepadPreviewAnswerAndFlip.y == -1)
                    {
                        subState |= NotepadSubState.WillFlipDown;
                        subState &= ~(NotepadSubState.WillFlipUp);
                    }
                }

                switch (leftHand_renderer.curFrameIndex)
                {
                    case 1:
                    {
                        if (curKeyframeState == NotepadKeyframeState.PaperClip) return;
                        activePage.PlayPaperClip();
                        curKeyframeState = NotepadKeyframeState.PaperClip;
                    }
                    break;

                    case 3:
                    {
                        if (curKeyframeState == NotepadKeyframeState.TogglePageContentsBottomHalf) return;
                        if (nextPage.activePlayerWriteText == "")
                        {
                            nextPage.SetPlayerWriteTextAlphaBottom(normAmount: 1);
                        }
                        activePage.TogglePageContentBottomHalf(false);
                        curKeyframeState = NotepadKeyframeState.TogglePageContentsBottomHalf;
                    }
                    break;

                    case 4:
                    {
                        if (curKeyframeState == NotepadKeyframeState.TogglePageContentsTopHalf) return;
                        if (nextPage.activePlayerWriteText == "")
                        {
                            nextPage.SetPlayerWriteTextAlphaTop(normAmount: 1);
                        }
                        activePage.TogglePageContentTopHalf(false);
                        curKeyframeState = NotepadKeyframeState.TogglePageContentsTopHalf;
                    }
                    break;

                    case 7:
                    {
                        if (curKeyframeState == NotepadKeyframeState.ChangeDepth) return;
                        activePage.SetPageDepth(notepadData.leftHandWorldDepthBack + 1);
                        leftHand_renderer.UpdateWorldDepth(notepadData.leftHandWorldDepthBack);
                        curKeyframeState = NotepadKeyframeState.ChangeDepth;

                    }
                    break;
                }
                if (leftHand_renderer.curFrameIndex == notepadData.handFlipPage_clip.keyframeEndIndex)
                {
                    if (curKeyframeState == NotepadKeyframeState.None) return;

                    activePage.gameObject.SetActive(false);
                    activePageIndex++;
                    activePage = pages[activePageIndex];
                    activePage.SetPageDepth(notepadData.leftHandWorldDepthFront + 2);

                    subState &= ~(NotepadSubState.CanFlipUp | NotepadSubState.CanWillFlipUp | NotepadSubState.IsFlippingUp);
                    curKeyframeState = NotepadKeyframeState.None;
                }
            }
            break;
            case NotepadState.FlippingDown:
            {
                if ((subState & NotepadSubState.CanWillFlipDown) == 0)
                {
                    subState |= NotepadSubState.CanWillFlipDown;
                }
                else
                {
                    if (activePageIndex < lastPageIndex && playerInputs.notepadPreviewAnswerAndFlip.y == 1)
                    {
                        subState |= NotepadSubState.WillFlipUp;
                        subState &= ~(NotepadSubState.WillFlipDown);
                    }
                    else if (activePageIndex > 1 && playerInputs.notepadPreviewAnswerAndFlip.y == -1)
                    {
                        subState |= NotepadSubState.WillFlipDown;
                        subState &= ~(NotepadSubState.WillFlipUp);
                    }
                }
                switch (leftHand_renderer.curFrameIndex)
                {
                    case 0:
                    {
                        if (curKeyframeState == NotepadKeyframeState.None) return;


                        activePage.gameObject.SetActive(false);
                        activePageIndex--;
                        activePage = pages[activePageIndex];

                        subState &= ~(NotepadSubState.CanFlipDown | NotepadSubState.CanWillFlipDown | NotepadSubState.IsFlippingDown);

                        curKeyframeState = NotepadKeyframeState.None;
                    }
                    break;
                    case 2:
                    {
                        if (curKeyframeState == NotepadKeyframeState.TogglePageContentsBottomHalf) return;
                        pages[activePageIndex - 1].TogglePageContentBottomHalf(true);

                        if (pages[activePageIndex - 1].activePlayerWriteText == "")
                        {
                            pages[activePageIndex - 1].SetPlayerWriteTextAlphaBottom(normAmount: 1);
                        }
                        curKeyframeState = NotepadKeyframeState.TogglePageContentsBottomHalf;
                    }
                    break;
                    case 3:
                    {
                        if (curKeyframeState == NotepadKeyframeState.TogglePageContentsTopHalf) return;

                        pages[activePageIndex - 1].TogglePageContentTopHalf(true);

                        if (pages[activePageIndex - 1].activePlayerWriteText == "")
                        {
                            pages[activePageIndex - 1].SetPlayerWriteTextAlphaTop(normAmount: 1);
                        }
                        curKeyframeState = NotepadKeyframeState.TogglePageContentsTopHalf;
                    }
                    break;
                    case 4:
                    {
                        if (curKeyframeState == NotepadKeyframeState.PaperClip) return;

                        pages[activePageIndex - 1].PlayPaperClipReverse();

                        curKeyframeState = NotepadKeyframeState.PaperClip;
                    }
                    break;
                    case 6:
                    {
                        if (curKeyframeState == NotepadKeyframeState.ChangeDepth) return;

                        leftHand_renderer.UpdateWorldDepth(notepadData.leftHandWorldDepthFront);
                        pages[activePageIndex - 1].SetPageDepth(notepadData.leftHandWorldDepthFront + 2);


                        curKeyframeState = NotepadKeyframeState.ChangeDepth;
                    }
                    break;
                }
            }
            break;
            case NotepadState.Writing:
            {
                if (!atStartPencilPos)
                {
                    leftHand_renderer.transform.localPosition = Vector3.Lerp(leftHand_renderer.transform.localPosition, notepadData.leftHandPencilPos, Time.deltaTime * LEFTHAND_DAMPING);

                    float dist = (leftHand_renderer.transform.localPosition - notepadData.leftHandPencilPos).sqrMagnitude;
                    if (dist < PENCIL_DISTANCE_THRESHOLD * PENCIL_DISTANCE_THRESHOLD)
                    {
                        activePage.WritePlayerWriteText();
                        totalPencilTime = (activePage.activePlayerWriteText.Length + 1) * WRITE_LETTER_TIME;
                        atStartPencilPos = true;
                    }
                }
                else
                {
                    curPencilTime += Time.deltaTime;
                    float t = curPencilTime / totalPencilTime;
                    curWritingBounds = activePage.GetWritingBounds();
                    float curPosX = Mathf.Lerp(curWritingBounds.min.x, curWritingBounds.max.x, t);
                    float randOffset = Mathf.PerlinNoise(curPencilTime * PENCIL_VERTICAL_FREQUENCY, curPencilTime * PENCIL_VERTICAL_FREQUENCY) * 2 - 1;
                    float curPosY = curWritingBounds.center.y + (randOffset * PENCIL_VERTICAL_MAGNITUDE);

                    Vector3 worldPos = new Vector3(curPosX, curPosY, leftHand_renderer.transform.position.z);

                    leftHand_renderer.transform.position = worldPos;

                    if (t > 1f) subState &= ~(NotepadSubState.WriteToggle);
                }

                if (activePageIndex < lastPageIndex && playerInputs.notepadPreviewAnswerAndFlip.y == 1)
                {
                    subState |= NotepadSubState.WillFlipUp;
                    subState &= ~(NotepadSubState.WillFlipDown);
                }
                else if (activePageIndex > 0 && playerInputs.notepadPreviewAnswerAndFlip.y == -1)
                {
                    subState |= NotepadSubState.WillFlipDown;
                    subState &= ~(NotepadSubState.WillFlipUp);
                }
            }
            break;
            case NotepadState.Erasing:
            {
                if (!atStartPencilPos)
                {
                    leftHand_renderer.transform.localPosition = Vector3.Lerp(leftHand_renderer.transform.localPosition, notepadData.leftHandPencilPos, Time.deltaTime * LEFTHAND_DAMPING);

                    float dist = (leftHand_renderer.transform.localPosition - notepadData.leftHandPencilPos).sqrMagnitude;
                    if (dist < PENCIL_DISTANCE_THRESHOLD * PENCIL_DISTANCE_THRESHOLD)
                    {
                        activePage.ErasePlayerWriteText();
                        totalPencilTime = activePage.activePlayerWriteText.Length * WRITE_LETTER_TIME;

                        atStartPencilPos = true;
                    }
                }
                else
                {
                    curPencilTime += Time.deltaTime;
                    float t = curPencilTime / totalPencilTime;
                    curWritingBounds = activePage.GetWritingBounds();
                    float curPosX = Mathf.Lerp(curWritingBounds.max.x, curWritingBounds.min.x, t);
                    float randOffset = Mathf.PerlinNoise(curPencilTime * PENCIL_VERTICAL_FREQUENCY, curPencilTime * PENCIL_VERTICAL_FREQUENCY) * 2 - 1;
                    float curPosY = curWritingBounds.center.y + (randOffset * PENCIL_VERTICAL_MAGNITUDE);
                    leftHand_renderer.transform.position = new Vector3(curPosX, curPosY, leftHand_renderer.transform.position.z);

                    if (activePage.activePlayerWriteTextRenderer.text.Length == 0) subState &= ~(NotepadSubState.EraseToggle);
                }

                if (activePageIndex < lastPageIndex && playerInputs.notepadPreviewAnswerAndFlip.y == 1)
                {
                    subState |= NotepadSubState.WillFlipUp;
                    subState &= ~(NotepadSubState.WillFlipDown);
                }
                else if (activePageIndex > 0 && playerInputs.notepadPreviewAnswerAndFlip.y == -1)
                {
                    subState |= NotepadSubState.WillFlipDown;
                    subState &= ~(NotepadSubState.WillFlipUp);
                }
            }
            break;
            case NotepadState.Stationary:
            {
                HandleStationaryLeftHandMove();
                HandlePlayerInputs();
                activePage.UpdatePreviewPlayerWriteText(appear: true, ref appearTextClock);
                activePage.UpdatePage();
            }
            break;
        }
    }
    private void EnterState(NotepadState prevState)
    {
        switch (notepadData.curState)
        {
            case NotepadState.FlippingUp:
            {
                nextPage = pages[activePageIndex + 1];
                nextPage.gameObject.SetActive(true);

                leftHand_renderer.UpdateWorldDepth(notepadData.leftHandWorldDepthFront);
                leftHand_renderer.PlayClipOneShot(notepadData.handFlipPage_clip);

                curKeyframeState = NotepadKeyframeState.Start;

                subState |= NotepadSubState.IsFlippingUp;
                subState &= ~(NotepadSubState.WillFlipUp);

                leftHand_renderer.transform.localPosition = notepadData.leftHandFlipPos;

            }
            break;
            case NotepadState.FlippingDown:
            {
                activePage.SetPageDepth(rightHand_renderer.transform.position.z - 1);

                pages[activePageIndex - 1].gameObject.SetActive(true);
                leftHand_renderer.PlayClipOneShotReverse(notepadData.handFlipPage_clip);

                curKeyframeState = NotepadKeyframeState.Start;

                subState |= NotepadSubState.IsFlippingDown;
                subState &= ~(NotepadSubState.WillFlipDown);
                subState &= ~(NotepadSubState.CanFlipUp);

                leftHand_renderer.transform.localPosition = notepadData.leftHandFlipPos;
                leftHand_renderer.UpdateWorldDepth(notepadData.leftHandWorldDepthBack);
            }
            break;
            case NotepadState.Writing:
            {
                SetLeftHandHoldingPencilSprite();

                curWritingBounds = activePage.GetWritingBounds();
                Vector3 startWriteWorldPos = new Vector3(curWritingBounds.min.x, curWritingBounds.center.y, leftHand_renderer.transform.position.z);
                notepadData.leftHandPencilPos = leftHand_renderer.transform.parent.InverseTransformPoint(startWriteWorldPos);

                curPencilTime = 0;

                subState |= NotepadSubState.WriteToggle;
            }
            break;
            case NotepadState.Erasing:
            {
                leftHand_renderer.PlayClipOneShot(notepadData.rotatePencil_clip);

                curWritingBounds = activePage.GetWritingBounds();
                Vector3 startEraseWorldPos = new Vector3(curWritingBounds.max.x, curWritingBounds.center.y, leftHand_renderer.transform.position.z);
                notepadData.leftHandPencilPos = leftHand_renderer.transform.parent.InverseTransformPoint(startEraseWorldPos);

                curPencilTime = 0;

                subState |= NotepadSubState.EraseToggle;
            }
            break;
            case NotepadState.Stationary:
            {
                if (activePage.playerWriteTextRenderers.Length > 0 && prevState != NotepadState.Writing && prevState != NotepadState.Erasing)
                {
                    Bounds rendBounds = leftHand_renderer.GetBounds();
                    Vector4 uvPivot = leftHand_renderer.sprite.uvPivot;
                    Vector3 spritePivotOffset = new Vector3(rendBounds.extents.x * (1 - uvPivot.x), rendBounds.size.y * (1 - uvPivot.y));
                    leftHandTargetLocalPos = notepadData.leftHandOffScreenLocalPos - spritePivotOffset;
                    atOffCameraPos = false;
                }

                if (activePage.activePlayerWriteText == "")
                {
                    activePage.SetPreviewPlayerWriteTexts(prevState);
                    appearTextClock = APPEAR_TEXT_TIME;
                }

                subState |= (NotepadSubState.CanFlipUp | NotepadSubState.CanFlipDown);
            }
            break;
        }
    }
    private void ExitState()
    {
        switch (notepadData.curState)
        {
            case NotepadState.FlippingUp:
            {

            }
            break;

            case NotepadState.FlippingDown:
            {
            }
            break;

            case NotepadState.Writing:
            {
                switch (activePage.pageType)
                {
                    case PageType.Start:
                    {
                        if (activePage.playerWriteTextRenderers[0].completedWritingText)
                        {
                            if (activePage.playerWriteIndex == 0)
                            {
                                FadeBlack(fadeBlackMaterial, ctsFadeBlack, toFadeBlack: true);
                                Scenes.SetScene(sceneData, SceneType.Trip, sceneIndex: 2);
                            }
                            else
                            {
                                Application.Quit();
                            }
                        }
                    }
                    break;
                }
                atStartPencilPos = false;

                subState &= ~(NotepadSubState.WriteToggle);
            }
            break;
            case NotepadState.Erasing:
            {
                activePage.activePlayerWriteText = "";
                leftHand_renderer.PlayClipOneShotReverse(notepadData.rotatePencil_clip);
                atStartPencilPos = false;
                subState &= ~(NotepadSubState.EraseToggle);

                switch (activePage.pageType)
                {

                }
            }
            break;

            case NotepadState.Stationary:
            {
                colorPicker?.Close();
            }
            break;
        }

    }
    private void HandleStationaryLeftHandMove()
    {
        if (sceneData.activeSceneType != SceneType.Trip) return;
        if (activePage.playerWriteTextRenderers.Length > 0 && !atOffCameraPos)
        {
            float dist = (leftHand_renderer.transform.localPosition - leftHandTargetLocalPos).sqrMagnitude;

            if (dist < PENCIL_DISTANCE_THRESHOLD * PENCIL_DISTANCE_THRESHOLD)
            {
                SetLeftHandHoldingPencilSprite();

                Bounds rendBounds = leftHand_renderer.GetBounds();
                Vector4 uvPivot = leftHand_renderer.sprite.uvPivot;
                Vector3 spritePivotOffset = new Vector3(rendBounds.extents.x * (1 - uvPivot.x), rendBounds.size.y * (1 - uvPivot.y));
                leftHand_renderer.transform.localPosition = notepadData.leftHandOffScreenLocalPos - spritePivotOffset;

                curWritingBounds = activePage.GetWritingBounds();
                Vector3 startWriteWorldPos = new Vector3(curWritingBounds.min.x, curWritingBounds.center.y, notepadData.leftHandWorldDepthFront);
                leftHandTargetLocalPos = transform.InverseTransformPoint(startWriteWorldPos);

                atOffCameraPos = true;
            }
        }
        else if (playerInputs.numpad != -1)
        {
            activePage.SwitchActivePLayerWriteTextRenderer(playerInputs.numpad - 1);
            curWritingBounds = activePage.GetWritingBounds();
            Vector3 startWriteWorldPos = new Vector3(curWritingBounds.min.x, curWritingBounds.center.y, notepadData.leftHandWorldDepthFront);
            leftHandTargetLocalPos = leftHand_renderer.transform.parent.InverseTransformPoint(startWriteWorldPos);
        }
        leftHand_renderer.transform.localPosition = Vector3.Lerp(leftHand_renderer.transform.localPosition, leftHandTargetLocalPos, Time.deltaTime * LEFTHAND_DAMPING);
    }
    private void HandlePlayerInputs()
    {
        if (playerInputs.notepadPreviewAnswerAndFlip.x != 0)
        {
            activePage.SwitchActivePreviewPlayerWriteText((int)playerInputs.notepadPreviewAnswerAndFlip.x);
            appearTextClock = APPEAR_TEXT_TIME;
        }
        bool atButton = false;
        if (activePageIndex != 0 && CursorController.IsInsideBounds(activePage.paperCornerLeftButtonRenderer.bounds, isClickable: true))
        {
            if (playerInputs.mouseLeftDown)
            {
                activePage.InvertLeftArrowButton(false);
                subState |= NotepadSubState.IsFlippingDown;
            }
            else
            {
                activePage.InvertLeftArrowButton(true);
            }
            atButton = true;
        }
        else
        {
            activePage.InvertLeftArrowButton(false);
        }
        if (atButton) return;

        if (activePageIndex != pages.Length - 1 && CursorController.IsInsideBounds(activePage.paperCornerRightButtonRenderer.bounds, isClickable: true))
        {
            if (playerInputs.mouseLeftDown)
            {
                activePage.InvertRightArrowButton(false);
                subState |= NotepadSubState.IsFlippingUp;
            }
            else
            {
                activePage.InvertRightArrowButton(true);
            }
            atButton = true;
        }
        else
        {
            activePage.InvertRightArrowButton(false);
        }
        if (atButton) return;
        if (sceneData.activeSceneType != SceneType.Trip) return;

        if (activePage.switchLeftButtonRenderer != null && CursorController.IsInsideBounds(activePage.switchLeftButtonRenderer.bounds, isClickable: true))
        {
            if (playerInputs.mouseLeftDown)
            {
                activePage.InvertSwitchLeftButton(false);
                activePage.SwitchActivePreviewPlayerWriteText(-1);
                appearTextClock = APPEAR_TEXT_TIME;
            }
            else
            {
                activePage.InvertSwitchLeftButton(true);
            }
            atButton = true;
        }
        else
        {
            activePage.InvertSwitchLeftButton(false);
        }
        if (atButton) return;

        if (activePage.switchRightButtonRenderer != null && CursorController.IsInsideBounds(activePage.switchRightButtonRenderer.bounds, isClickable: true))
        {
            if (playerInputs.mouseLeftDown)
            {
                activePage.InvertSwitchRightButton(false);
                activePage.SwitchActivePreviewPlayerWriteText(1);
                appearTextClock = APPEAR_TEXT_TIME;
            }
            else
            {
                activePage.InvertSwitchRightButton(true);
            }
        }
        else
        {
            activePage.InvertSwitchRightButton(false);
        }

        if (CursorController.IsInsideBounds(activePage.exitButton_renderer.bounds, isClickable: true))
        {
            if (playerInputs.mouseLeftHold)
            {
                activePage.InvertExitButton(invert: false);
            }
            else
            {
                activePage.InvertExitButton(invert: true);
            }
            if (playerInputs.mouseLeftUp)
            {
                notepadData.checkingNotepad = false;
            }
        }
        else
        {
            activePage.InvertExitButton(invert: false);
        }
    }
    public void SetLeftHandHoldingPencilSprite()
    {
        leftHand_renderer.UpdateSpriteInputs(leftHand_renderer.atlas.motionSprites[notepadData.rotatePencil_clip.keyframeStartIndex].sprite);
    }
    private void CreatePages()
    {
        pages = new Page[]
        {
            startPage,
            optionsPage,
        };
        startPage.Init();
        optionsPage.Init();

        lastPageIndex = pages.Length - 1;

    }
    private void SetFadeToBlack()
    {
        SetFadeBlack(fadeBlackMaterial, toFadeBlack: true);
        Scenes.SetScene(sceneData, Scenes.SceneType.Trip, sceneIndex: 2);
    }
    private bool ToFlipUp()
    {
        return ((playerInputs.notepadPreviewAnswerAndFlip.y == 1 && activePageIndex < lastPageIndex) || (subState & (NotepadSubState.WillFlipUp | NotepadSubState.IsFlippingUp)) != 0) && (subState & NotepadSubState.CanFlipUp) != 0;
    }
    private bool ToFlipDown()
    {
        return ((playerInputs.notepadPreviewAnswerAndFlip.y == -1 && activePageIndex > 0) || (subState & (NotepadSubState.WillFlipDown | NotepadSubState.IsFlippingDown)) != 0) && (subState & NotepadSubState.CanFlipDown) != 0;
    }
    private bool ToErase()
    {
        return (sceneData.activeSceneType == SceneType.Trip && playerInputs.notepadPreviewAnswerAndFlip.x != 0 && activePage.activePlayerWriteText != "") || (subState & NotepadSubState.EraseToggle) != 0;
    }
    private bool ToWrite()
    {
        return (sceneData.activeSceneType == SceneType.Trip && playerInputs.spacebarDown && activePage.activePlayerWriteText == "") || (subState & NotepadSubState.WriteToggle) != 0;
    }
}
