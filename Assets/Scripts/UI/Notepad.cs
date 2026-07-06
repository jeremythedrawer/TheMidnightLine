using System;
using System.Collections.Generic;
using UnityEngine;
using static Atlas;
using static AtlasUI;
using static NPC;
using static Scenes;
public class Notepad : MonoBehaviour
{
    const int TAB_HORIZONTAL_SPRITE_INDEX = 21;
    const int TAB_VERTICAL_SPRITE_INDEX = 24;

    public const float WRITE_LETTER_TIME = 0.1f;
    public const int MIN_STATION_STOPS = 1;
    public const int MAX_STATION_STOPS = 2;

    const float LEFTHAND_DAMPING = 7f;
    const float PENCIL_DISTANCE_THRESHOLD = 0.05f;
    const float PENCIL_VERTICAL_FREQUENCY = 7f;
    const float PENCIL_VERTICAL_MAGNITUDE = 0.07f;
    const float REVEAL_TIME = 2f;
    public enum KeyframeState
    {
        None,
        Start,
        PaperClip,
        TogglePageContentsBottomHalf,
        TogglePageContentsTopHalf,
        ChangeDepth,
    }
    public enum TabDirection
    {
        Left, Right, Up, Down
    }
    [Flags] public enum SubState
    {
        None = 0,
        IsFlippingUp = 1 << 0,
        IsFlippingDown = 1 << 1,
        WriteToggle = 1 << 2,
        EraseToggle = 1 << 3,
        RevealToggle = 1 << 4,
        WillFlipUp = 1 << 5,
        WillFlipDown = 1 << 6,
        CanFlipUp = 1 << 7,
        CanFlipDown = 1 << 8,
        CanWillFlipUp = 1 << 9,
        CanWillFlipDown = 1 << 10,

    }

    public static event Action OnFinishRevealingOutcomes;

    public PlayerInputsSO playerInputs;
    public TripSO trip;
    public NPCsDataSO npcData;
    public CameraStatsSO camStats;
    public SpyStatsSO spyStats;
    public SceneData sceneData;
    public GameEventDataSO gameEventData;
    public ColorsSO colorsData;

    public NotepadData notepadData;
    
    public AtlasRenderer rightHand_renderer;
    public AtlasRenderer frontFingers_renderer;
    public AtlasRenderer bindingRings_renderer;
    public AtlasRenderer leftHand_renderer;
    public AtlasRenderer tabRenderer;

    public Transform pageTransform;

    public TextAsset namesJSON;

    public Page traitorPage_prefab;
    public Page promptPage;
    public Page colorKeyPage;

    [Header("Generated")]

    public Page[] pages;
    
    public Page activePage;
    public Page nextPage;

    public ColorPicker colorPicker;

    public TraitorProfile activeTraitorProfile;

    public AtlasClip handFlipPage_clip;
    public AtlasClip rotatePencil_clip;
    
    public NameData nameData;
    
    public Bounds curWritingBounds;
    
    public Vector3 leftHandFlipPos;
    public Vector3 leftHandPencilPos;
    public Vector3 leftHandTargetPos;
    public Vector3 leftHandOffScreenPos;
    
    public KeyframeState curKeyframeState;
    
    public int activePageIndex;
    public int lastPageIndex;
    public int leftHandWorldDepthFront;
    public int leftHandWorldDepthBack;
    
    public int traitorOutcomesRevealed;

    public float tabWorldDepthBack;

    public float totalPencilTime;
    public float curPencilTime;
    public float appearTextClock;
    public float revealClock;

    public bool active;

    public SubState subState;


    public bool atStartPencilPos;
    public bool atOffCameraPos;
    
    private void Awake()
    {
        switch (sceneData.activeSceneType)
        {
            case SceneType.Trip:
            {
                CreateNPCProfiles();
            }
            break;
        }
        activePage = promptPage;
    }

    private void OnEnable()
    {
        switch (sceneData.activeSceneType)
        {
            case SceneType.Score:
            {
                SkipToPage(0);
            }
            break;
        }
    }
    private void OnDisable()
    {
        leftHandTargetPos = leftHandFlipPos;
        leftHand_renderer.UpdateSpriteInputs(leftHand_renderer.atlas.motionSprites[handFlipPage_clip.keyframeStartIndex].sprite);

        if (tabRenderer.enabled)
        {
            if (trip.curUnlocks == notepadData.completedUnlocks)
            {
                tabRenderer.enabled = false;
            }
        }

    }
    private void Start()
    {
        AtlasUI.PromptStringDict = InitEnumToStringDict<TripPrompt>();
        npcData.behaviourStringDict = InitEnumToStringDict<Behaviours>();

        notepadData.curState = NotepadState.None;

        handFlipPage_clip = leftHand_renderer.atlas.clipDict[(int)NotepadMotion.FlipHand];
        rotatePencil_clip = leftHand_renderer.atlas.clipDict[(int)NotepadMotion.RotatingPencil];

        leftHandWorldDepthFront =  (int)(bindingRings_renderer.transform.position.z - 1);
        leftHandWorldDepthBack = (int)(rightHand_renderer.transform.position.z + 1);

        Vector3 flipWorldPos = new Vector3(bindingRings_renderer.transform.position.x, bindingRings_renderer.transform.position.y, leftHand_renderer.transform.position.z);
        leftHandFlipPos = transform.InverseTransformPoint(flipWorldPos);

        Vector3 offscreenLocalPos = new Vector3(-transform.localPosition.x * 0.5f, camStats.camBounds.extents.y - transform.localPosition.y - camStats.camBounds.size.y, leftHand_renderer.transform.localPosition.z);
        leftHandOffScreenPos = offscreenLocalPos;
        leftHandTargetPos = leftHandOffScreenPos;

        switch (sceneData.activeSceneType)
        { 
            case SceneType.Trip:
            {
                CreatePages();
                colorPicker = SceneController.GetColorPicker();
                notepadData.completedUnlocks = UnlockType.None;
            }
            break;
            case SceneType.Score:
            {
                SkipToPage(0);
            }
            break;
        }
        subState = SubState.None;

        tabWorldDepthBack = rightHand_renderer.transform.position.z - 0.5f;
        tabRenderer.enabled = false;
    }
    private void Update()
    {
        if (!active) return;
        ChooseState();
        UpdateState();
    }
    public void EnterNotepad()
    {
        active = true;
        EnterState(NotepadState.None);

        if (activePage.pageType == PageType.ColorKey)
        {
            if ((notepadData.completedUnlocks & UnlockType.RuleOut) == 0 && (trip.curUnlocks & UnlockType.RuleOut) != 0)
            {
                activePage.InitRuleOutRow();
                appearTextClock = APPEAR_TEXT_TIME;

            }
            else if ((notepadData.completedUnlocks & UnlockType.Color) == 0 && (trip.curUnlocks & UnlockType.Color) != 0)
            {
                activePage.InitNextColorRow(1);
                appearTextClock = APPEAR_TEXT_TIME;
                activePage.SwitchActivePLayerWriteTextRenderer(1);
                curWritingBounds = activePage.GetWritingBounds();
                Vector3 startWriteWorldPos = new Vector3(curWritingBounds.min.x, curWritingBounds.center.y, leftHandWorldDepthFront);
                leftHandTargetPos = leftHand_renderer.transform.parent.InverseTransformPoint(startWriteWorldPos);
            }
            else if ((notepadData.completedUnlocks & UnlockType.MultiColor) == 0 && (trip.curUnlocks & UnlockType.MultiColor) != 0)
            {
                activePage.InitNextColorRow(2);
                appearTextClock = APPEAR_TEXT_TIME;
                activePage.SwitchActivePLayerWriteTextRenderer(2);
                curWritingBounds = activePage.GetWritingBounds();
                Vector3 startWriteWorldPos = new Vector3(curWritingBounds.min.x, curWritingBounds.center.y, leftHandWorldDepthFront);
                leftHandTargetPos = leftHand_renderer.transform.parent.InverseTransformPoint(startWriteWorldPos);
            }
        }

        if (trip.curUnlocks != notepadData.completedUnlocks)
        {
            if ((notepadData.completedUnlocks & UnlockType.RuleOut) == 0)
            {
                SetTab(TabDirection.Left, colorKeyPage.playerWriteRenderers[0].GetBounds());
            }
            else if ((notepadData.completedUnlocks & UnlockType.Color) == 0)
            {
                SetTab(TabDirection.Left, colorKeyPage.playerWriteRenderers[1].GetBounds());
            }
            else if ((notepadData.completedUnlocks & UnlockType.MultiColor) == 0)
            {
                SetTab(TabDirection.Left, colorKeyPage.playerWriteRenderers[2].GetBounds());
            }
        }
    }
    public void ExitNotepad()
    {
        Bounds rendBounds = leftHand_renderer.GetBounds();
        Vector4 uvPivot = leftHand_renderer.sprite.uvPivot;
        Vector3 spritePivotOffset = new Vector3(rendBounds.extents.x * (1 - uvPivot.x), rendBounds.size.y * (1 - uvPivot.y));
        leftHandTargetPos = leftHandOffScreenPos + spritePivotOffset;

        active = false;
    }
    private void ChooseState()
    {
        switch (activePage.pageType)
        {
            case PageType.Prompt:
            {
                if (ToFlipUp())
                {
                    SetState(NotepadState.FlippingUp);
                }
                else
                {
                    SetState(NotepadState.Stationary);
                }
            }
            break;

            case PageType.Profile:
            {
                if (ToFlipUp())
                {
                    SetState(NotepadState.FlippingUp);
                }
                else if (ToFlipDown())
                {
                    SetState(NotepadState.FlippingDown);
                }
                else if (ToErase())
                {
                    SetState(NotepadState.Erasing);
                }
                else if (ToWrite())
                {
                    SetState(NotepadState.Writing);
                }
                else if (ToReveal())
                {
                    SetState(NotepadState.Revealing);
                }
                else
                {
                    SetState(NotepadState.Stationary);
                }
            }
            break;

            case PageType.ColorKey:
            {
                if (ToFlipUp())
                {
                    SetState(NotepadState.FlippingUp);
                }
                else if (ToFlipDown())
                {
                    SetState(NotepadState.FlippingDown);
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
    private void SkipToPage(int index)
    {
        activePage.gameObject.SetActive(false);
        activePageIndex = index;
        activePage = pages[activePageIndex];
        activePage.SetPageDepth(leftHandWorldDepthFront + 2);
        if (activePage.pageType == PageType.ColorKey && tabRenderer.enabled)
        {
            tabRenderer.transform.SetParent(transform, worldPositionStays: true);
            tabRenderer.transform.position = new Vector3(tabRenderer.transform.position.x, tabRenderer.transform.position.y, tabWorldDepthBack);
        }

        subState &= ~(SubState.CanFlipDown | SubState.CanWillFlipDown | SubState.IsFlippingDown);
        curKeyframeState = KeyframeState.None;
    }
    private void UpdateState()
    {
        switch (notepadData.curState)
        {
            case NotepadState.FlippingUp:
            {
                if ((subState & SubState.CanWillFlipUp) == 0)
                {
                    subState |= SubState.CanWillFlipUp;
                }
                else
                {
                    if (activePageIndex < lastPageIndex - 1 && playerInputs.notepadPreviewAnswerAndFlip.y == 1)
                    {
                        subState |= SubState.WillFlipUp;
                        subState &= ~(SubState.WillFlipDown);
                    }
                    if (activePageIndex > 0 && playerInputs.notepadPreviewAnswerAndFlip.y == -1)
                    {
                        subState |= SubState.WillFlipDown;
                        subState &= ~(SubState.WillFlipUp);
                    }
                }

                switch (leftHand_renderer.curFrameIndex)
                {
                    case 1:
                    {
                        if (curKeyframeState == KeyframeState.PaperClip) return;
                        activePage.PlayPaperClip();
                        curKeyframeState = KeyframeState.PaperClip;
                    }
                    break;

                    case 3:
                    {
                        if (curKeyframeState == KeyframeState.TogglePageContentsBottomHalf) return;
                        if (nextPage.activePlayerWriteText == "")
                        {
                            nextPage.SetPlayerWriteTextAlphaBottom(normAmount: 1);
                        }
                        activePage.TogglePageContentBottomHalf(false);
                        curKeyframeState = KeyframeState.TogglePageContentsBottomHalf;
                    }
                    break;

                    case 4:
                    {
                        if (curKeyframeState == KeyframeState.TogglePageContentsTopHalf) return;
                        if (nextPage.activePlayerWriteText == "")
                        {
                            nextPage.SetPlayerWriteTextAlphaTop(normAmount: 1);
                        }
                        activePage.TogglePageContentTopHalf(false);
                        curKeyframeState = KeyframeState.TogglePageContentsTopHalf;
                    }
                    break;

                    case 7:
                    {
                        if (curKeyframeState == KeyframeState.ChangeDepth) return;
                        activePage.SetPageDepth(leftHandWorldDepthBack + 1);
                        leftHand_renderer.UpdateWorldDepth(leftHandWorldDepthBack);
                        curKeyframeState = KeyframeState.ChangeDepth;

                    }
                    break;
                }
                if (leftHand_renderer.curFrameIndex == handFlipPage_clip.keyframeEndIndex)
                {
                    if (curKeyframeState == KeyframeState.None) return;

                    activePage.gameObject.SetActive(false);
                    activePageIndex++;
                    activePage = pages[activePageIndex];
                    activePage.SetPageDepth(leftHandWorldDepthFront + 2);

                    subState &= ~(SubState.CanFlipUp | SubState.CanWillFlipUp | SubState.IsFlippingUp);
                    curKeyframeState = KeyframeState.None;
                }
            }
            break;
            case NotepadState.FlippingDown:
            {
                if ((subState & SubState.CanWillFlipDown) == 0)
                {
                    subState |= SubState.CanWillFlipDown;
                }
                else
                {
                    if (activePageIndex < lastPageIndex && playerInputs.notepadPreviewAnswerAndFlip.y == 1)
                    {
                        subState |= SubState.WillFlipUp;
                        subState &= ~(SubState.WillFlipDown);
                    }
                    else if (activePageIndex > 1 && playerInputs.notepadPreviewAnswerAndFlip.y == -1)
                    {
                        subState |= SubState.WillFlipDown;
                        subState &= ~(SubState.WillFlipUp);
                    }
                }
                switch (leftHand_renderer.curFrameIndex)
                {
                    case 0:
                    {
                        if (curKeyframeState == KeyframeState.None) return;
                        
                        if (activePage.pageType == PageType.ColorKey && tabRenderer.enabled)
                        {
                            tabRenderer.transform.SetParent(transform, worldPositionStays: true);
                            tabRenderer.transform.position = new Vector3(tabRenderer.transform.position.x, tabRenderer.transform.position.y, tabWorldDepthBack);
                        }
                        activePage.gameObject.SetActive(false);
                        activePageIndex--;
                        activePage = pages[activePageIndex];

                        subState &= ~(SubState.CanFlipDown | SubState.CanWillFlipDown | SubState.IsFlippingDown);

                        curKeyframeState = KeyframeState.None;
                    }
                    break;
                    case 2:
                    {
                        if (curKeyframeState == KeyframeState.TogglePageContentsBottomHalf) return;
                        pages[activePageIndex - 1].TogglePageContentBottomHalf(true);

                        if (pages[activePageIndex - 1].activePlayerWriteText == "")
                        {
                            pages[activePageIndex - 1].SetPlayerWriteTextAlphaBottom(normAmount: 1);
                        }
                        curKeyframeState = KeyframeState.TogglePageContentsBottomHalf;
                    }
                    break;
                    case 3:
                    {
                        if (curKeyframeState == KeyframeState.TogglePageContentsTopHalf) return;

                        pages[activePageIndex - 1].TogglePageContentTopHalf(true);

                        if (pages[activePageIndex - 1].activePlayerWriteText == "")
                        {
                            pages[activePageIndex - 1].SetPlayerWriteTextAlphaTop(normAmount: 1);
                        }
                        curKeyframeState = KeyframeState.TogglePageContentsTopHalf;
                    }
                    break;
                    case 4:
                    {
                        if (curKeyframeState == KeyframeState.PaperClip) return;

                        pages[activePageIndex - 1].PlayPaperClipReverse();

                        curKeyframeState = KeyframeState.PaperClip;
                    }
                    break;
                    case 6:
                    {
                        if (curKeyframeState == KeyframeState.ChangeDepth) return;

                        leftHand_renderer.UpdateWorldDepth(leftHandWorldDepthFront);
                        pages[activePageIndex - 1].SetPageDepth(leftHandWorldDepthFront + 2);


                        curKeyframeState = KeyframeState.ChangeDepth;
                    }
                    break;
                }
            }
            break;
            case NotepadState.Writing:
            {
                if (!atStartPencilPos)
                {
                    leftHand_renderer.transform.localPosition = Vector3.Lerp(leftHand_renderer.transform.localPosition, leftHandPencilPos, Time.deltaTime * LEFTHAND_DAMPING);

                    float dist = (leftHand_renderer.transform.localPosition - leftHandPencilPos).sqrMagnitude;
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

                    if (t > 1f) subState &= ~(SubState.WriteToggle);
                }

                if (activePageIndex < lastPageIndex && playerInputs.notepadPreviewAnswerAndFlip.y == 1)
                {
                    subState |= SubState.WillFlipUp;
                    subState &= ~(SubState.WillFlipDown);
                }
                else if (activePageIndex > 0 && playerInputs.notepadPreviewAnswerAndFlip.y == -1)
                {
                    subState |= SubState.WillFlipDown;
                    subState &= ~(SubState.WillFlipUp);
                }
            }
            break;
            case NotepadState.Erasing:
            {
                if(!atStartPencilPos)
                {
                    leftHand_renderer.transform.localPosition = Vector3.Lerp(leftHand_renderer.transform.localPosition, leftHandPencilPos, Time.deltaTime * LEFTHAND_DAMPING);

                    float dist = (leftHand_renderer.transform.localPosition - leftHandPencilPos).sqrMagnitude;
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

                    if (activePage.activePlayerWriteTextRenderer.text.Length == 0) subState &= ~(SubState.EraseToggle);
                }

                if (activePageIndex < lastPageIndex && playerInputs.notepadPreviewAnswerAndFlip.y == 1)
                {
                    subState |= SubState.WillFlipUp;
                    subState &= ~(SubState.WillFlipDown);
                }
                else if (activePageIndex > 0 && playerInputs.notepadPreviewAnswerAndFlip.y == -1)
                {
                    subState |= SubState.WillFlipDown;
                    subState &= ~(SubState.WillFlipUp);
                }
            }
            break;
            case NotepadState.Stationary:
            {
                HandleStationaryLeftHandMove();

                if (sceneData.activeSceneType == SceneType.Trip && playerInputs.notepadPreviewAnswerAndFlip.x != 0)
                {
                    activePage.SwitchActivePreviewPlayerWriteText((int)playerInputs.notepadPreviewAnswerAndFlip.x);
                    appearTextClock = APPEAR_TEXT_TIME;
                }
                
                activePage.UpdatePreviewPlayerWriteText(appear: true, ref appearTextClock);

                activePage.UpdatePage();

                if (activePageIndex != 0 && CursorController.IsInsideBounds(activePage.paperCornerLeftButtonRenderer.bounds))
                {
                    if (playerInputs.mouseLeftDown)
                    {
                        activePage.InvertLeftArrowButton(false);
                        subState |= SubState.IsFlippingDown;
                    }
                    else
                    {
                        activePage.InvertLeftArrowButton(true);
                    }
                }
                else
                {
                    activePage.InvertLeftArrowButton(false);
                }

                if (activePageIndex != pages.Length - 1 && CursorController.IsInsideBounds(activePage.paperCornerRightButtonRenderer.bounds))
                {
                    if (playerInputs.mouseLeftDown)
                    {
                        activePage.InvertRightArrowButton(false);
                        subState |= SubState.IsFlippingUp;
                    }
                    else
                    {
                        activePage.InvertRightArrowButton(true);
                    }
                }
                else
                {
                    activePage.InvertRightArrowButton(false);
                }
            }
            break;
            case NotepadState.Revealing:
            {
                revealClock += Time.deltaTime;

                activePage.UpdateMugShotReveal(revealClock / REVEAL_TIME);

                if (revealClock > REVEAL_TIME)
                {
                    activePage.UpdateMugShotReveal(1);
                    subState &= ~(SubState.RevealToggle);
                    
                    traitorOutcomesRevealed++;

                    if (traitorOutcomesRevealed == trip.traitorProfiles.Length)
                    {
                        OnFinishRevealingOutcomes.Invoke();
                    }
                }
            }
            break;
        }
    }
    private void EnterState(NotepadState prevState)
    {
        switch(notepadData.curState)
        {
            case NotepadState.FlippingUp:
            {
                nextPage = pages[activePageIndex + 1];
                nextPage.gameObject.SetActive(true);

                if (nextPage.pageType == PageType.ColorKey)
                {
                    if (tabRenderer.enabled)
                    {
                        tabRenderer.transform.SetParent(nextPage.transform, worldPositionStays: true);
                        tabRenderer.transform.localPosition = new Vector3(tabRenderer.transform.localPosition.x, tabRenderer.transform.localPosition.y, -1);

                        if ((notepadData.completedUnlocks & UnlockType.RuleOut) == 0)
                        {
                            nextPage.InitRuleOutRow();
                            appearTextClock = APPEAR_TEXT_TIME;
                        }
                        else if ((notepadData.completedUnlocks & UnlockType.Color) == 0)
                        {
                            nextPage.InitNextColorRow(1);
                            appearTextClock = APPEAR_TEXT_TIME;

                            nextPage.SwitchActivePLayerWriteTextRenderer(1);
                            curWritingBounds = nextPage.GetWritingBounds();
                            Vector3 startWriteWorldPos = new Vector3(curWritingBounds.min.x, curWritingBounds.center.y, leftHandWorldDepthFront);
                            leftHandTargetPos = leftHand_renderer.transform.parent.InverseTransformPoint(startWriteWorldPos);
                        }
                        else if ((notepadData.completedUnlocks & UnlockType.MultiColor) == 0)
                        {
                            nextPage.InitNextColorRow(2);
                            appearTextClock = APPEAR_TEXT_TIME;

                            nextPage.SwitchActivePLayerWriteTextRenderer(2);
                            curWritingBounds = nextPage.GetWritingBounds();
                            Vector3 startWriteWorldPos = new Vector3(curWritingBounds.min.x, curWritingBounds.center.y, leftHandWorldDepthFront);
                            leftHandTargetPos = leftHand_renderer.transform.parent.InverseTransformPoint(startWriteWorldPos);
                        }
                    }

                }

                leftHand_renderer.UpdateWorldDepth(leftHandWorldDepthFront);
                leftHand_renderer.PlayClipOneShot(handFlipPage_clip);

                curKeyframeState = KeyframeState.Start;

                subState |= SubState.IsFlippingUp;
                subState &= ~(SubState.WillFlipUp);                

                leftHand_renderer.transform.localPosition = leftHandFlipPos;
                
            }
            break;
            case NotepadState.FlippingDown:
            {
                activePage.SetPageDepth(rightHand_renderer.transform.position.z - 1);

                pages[activePageIndex - 1].gameObject.SetActive(true);
                leftHand_renderer.PlayClipOneShotReverse(handFlipPage_clip);

                curKeyframeState = KeyframeState.Start;
                
                subState |= SubState.IsFlippingDown;
                subState &= ~(SubState.WillFlipDown);

                leftHand_renderer.transform.localPosition = leftHandFlipPos;
                leftHand_renderer.UpdateWorldDepth(leftHandWorldDepthBack);
            }
            break;
            case NotepadState.Writing:
            {
                SetLeftHandHoldingPencilSprite();

                curWritingBounds = activePage.GetWritingBounds();
                Vector3 startWriteWorldPos = new Vector3(curWritingBounds.min.x, curWritingBounds.center.y, leftHand_renderer.transform.position.z);
                leftHandPencilPos = leftHand_renderer.transform.parent.InverseTransformPoint(startWriteWorldPos);

                activeTraitorProfile = trip.traitorProfiles[activePage.traitorIndex];

                curPencilTime = 0;

                subState |= SubState.WriteToggle;
            }
            break;
            case NotepadState.Erasing:
            {
                leftHand_renderer.PlayClipOneShot(rotatePencil_clip);

                curWritingBounds = activePage.GetWritingBounds();
                Vector3 startEraseWorldPos = new Vector3(curWritingBounds.max.x, curWritingBounds.center.y, leftHand_renderer.transform.position.z);
                leftHandPencilPos = leftHand_renderer.transform.parent.InverseTransformPoint(startEraseWorldPos);

                activeTraitorProfile = trip.traitorProfiles[activePage.traitorIndex];

                curPencilTime = 0;

                subState |= SubState.EraseToggle;
            }
            break;
            case NotepadState.Stationary:
            {
                if (activePage.playerWriteTextRenderers.Length > 0 && prevState != NotepadState.Writing && prevState != NotepadState.Erasing)
                {
                    Bounds rendBounds = leftHand_renderer.GetBounds();
                    Vector4 uvPivot = leftHand_renderer.sprite.uvPivot;
                    Vector3 spritePivotOffset = new Vector3(rendBounds.extents.x * (1 - uvPivot.x), rendBounds.size.y * (1 - uvPivot.y));
                    leftHandTargetPos = leftHandOffScreenPos - spritePivotOffset;
                    atOffCameraPos = false;
                }

                if (activePage.activePlayerWriteText == "")
                {
                    activePage.SetPreviewPlayerWriteTexts(prevState);
                    appearTextClock = APPEAR_TEXT_TIME;
                }

                subState |= (SubState.CanFlipUp | SubState.CanFlipDown);
            }
            break;
            case NotepadState.Revealing:
            {
                revealClock = 0;
                subState |= SubState.RevealToggle;

                activeTraitorProfile = trip.traitorProfiles[activePage.traitorIndex];
            }
            break;
        }
    }
    private void CreateNPCProfiles()
    {
        nameData = JsonUtility.FromJson<NameData>(namesJSON.text);
        List<NPCProfile> totalNPCProfiles = new List<NPCProfile>();
        List<NPCProfile> bystanderProfiles = new List<NPCProfile>();

        for (int i = 0; i < trip.npcDataArray.Length; i++)
        {
            NPCSO npc = trip.npcDataArray[i];

            int behaviourValue = (int)npc.behaviours;

            int[] validFlags = new int[32];
            int flagCount = 0;

            for (int j = 0; j < 32; j++)
            {
                int flag = 1 << j;

                if ((behaviourValue & flag) != 0)
                {
                    validFlags[flagCount] = flag;
                    flagCount++;
                }
            }
            for (int j = 0; j < flagCount; j++)
            {
                Behaviours firstBehaviour = (Behaviours)validFlags[j];

                for (int k = j; k < flagCount; k++)
                {
                    Behaviours secondBehaviour = (Behaviours)validFlags[k];
                    Behaviours twoBehaviours = firstBehaviour | secondBehaviour;
                    string name = GenerateName(npc.gender, npc.ethnicity);

                    NPCProfile npcProfile = new NPCProfile
                    {
                        behaviours = twoBehaviours,
                        npcPrefabIndex = i,
                    };

                    if (k == j)
                    {
                        bystanderProfiles.Add(npcProfile);
                    }
                    else
                    {
                        totalNPCProfiles.Add(npcProfile);
                    }
                }
            }
        }

        int totalTraitorsInTrip = 0;

        for (int i = 0; i < trip.stationsDataArray.Length; i++)
        {
            StationSO station = trip.stationsDataArray[i];
            totalTraitorsInTrip += station.traitorSpawnCount;
        }
        trip.traitorProfiles = new TraitorProfile[totalTraitorsInTrip];;

        int traitorIndex = 0;
        for (int i = 0; i < trip.stationsDataArray.Length; i++)
        {
            StationSO station = trip.stationsDataArray[i];

            for (int j = 0; j < station.traitorSpawnCount; j++)
            {
                int randProfileIndex = UnityEngine.Random.Range(0, totalNPCProfiles.Count);
                NPCProfile traitorProfile = totalNPCProfiles[randProfileIndex];
                traitorProfile.boardingStationIndex = i;

                int stationsLeft = trip.stationsDataArray.Length - i;
                float normSpawnIndex = UnityEngine.Random.Range(0, stationsLeft + 1) / (float)stationsLeft;
                float gaussianNormSpawnIndex = NormalGaussianValue(normSpawnIndex);
                traitorProfile.disembarkingStationIndex = i + Mathf.CeilToInt(gaussianNormSpawnIndex * stationsLeft) + MIN_STATION_STOPS;

                NPCSO traitor = trip.npcDataArray[traitorProfile.npcPrefabIndex];

                trip.traitorProfiles[traitorIndex] = new TraitorProfile()
                {
                    npcProfile = traitorProfile,
                    mugShotIndex = traitor.mugShotIndex,
                };

                totalNPCProfiles.RemoveAt(randProfileIndex);

                for (int k = totalNPCProfiles.Count - 1; k >= 0; k--)
                {
                    if (totalNPCProfiles[k].npcPrefabIndex != traitorProfile.npcPrefabIndex) continue;

                    bystanderProfiles.Add(totalNPCProfiles[k]);
                    totalNPCProfiles.RemoveAt(k);
                }

                traitorIndex++;
            }
        }

        for (int i = 0; i < trip.stationsDataArray.Length; i++)
        {
            StationSO station = trip.stationsDataArray[i];
            station.accompliceProfiles = new NPCProfile[station.accompliceSpawnCount];

            for (int j = 0; j < station.accompliceSpawnCount; j++)
            {
                int randPrefabIndex = UnityEngine.Random.Range(0, trip.npcDataArray.Length);
                NPCProfile accompliceProfile = new NPCProfile();

                accompliceProfile.npcPrefabIndex = randPrefabIndex;
                accompliceProfile.boardingStationIndex = i;
                accompliceProfile.disembarkingStationIndex = trip.stationsDataArray.Length - 1;

                station.accompliceProfiles[j] = accompliceProfile;
            }

        }

        activePageIndex = 0;

        totalNPCProfiles.AddRange(bystanderProfiles);

        int profileIndex = 0;
        for (int i = 0; i < trip.stationsDataArray.Length; i++)
        {
            StationSO station = trip.stationsDataArray[i];

            station.bystanderProfiles = new NPCProfile[station.bystanderSpawnCount];

            for (int j = 0; j < station.bystanderSpawnCount; j++)
            {
                NPCProfile bystanderProfile = totalNPCProfiles[profileIndex];

                bystanderProfile.boardingStationIndex = i;

                int stationsLeft = trip.stationsDataArray.Length - i;
                float normSpawnIndex = (float)j / (float)station.bystanderSpawnCount;
                float gaussianNormSpawnIndex = NormalGaussianValue(normSpawnIndex);
                bystanderProfile.disembarkingStationIndex = i + Mathf.CeilToInt(gaussianNormSpawnIndex * stationsLeft);

                station.bystanderProfiles[j] = bystanderProfile;

                profileIndex++;
                profileIndex %= totalNPCProfiles.Count;
            }
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
                switch(activePage.pageType)
                {
                    case PageType.Profile:
                    {
                        if(activeTraitorProfile.npcProfile.disembarkingStationIndex == activePage.playerWriteIndex)
                        {
                            activeTraitorProfile.found = true;
                            trip.traitorProfiles[activePage.traitorIndex] = activeTraitorProfile;
                            
                            activePage.playerWriteRenderers[0].customBit |= (int)ColorBits.Diagonal;
                        }
                        else
                        {
                            activeTraitorProfile.found = false;
                            trip.traitorProfiles[activePage.traitorIndex] = activeTraitorProfile;

                            activePage.playerWriteRenderers[0].customBit &= ~((int)ColorBits.Diagonal);
                        }
                    }
                    break;

                    case PageType.ColorKey:
                    {
                        if (tabRenderer.enabled)
                        {
                            if (activePage.playerWriteTextRenderers[0].completedWritingText && (notepadData.completedUnlocks & UnlockType.RuleOut) == 0)
                            {
                                notepadData.completedUnlocks |= UnlockType.RuleOut;
                                activePage.UnlockColorRow(0);
                                if (notepadData.completedUnlocks != trip.curUnlocks)
                                {
                                    if ((notepadData.completedUnlocks & UnlockType.Color) == 0)
                                    {
                                        SetTab(TabDirection.Left, activePage.playerWriteRenderers[1].bounds);
                                        activePage.InitNextColorRow(1);
                                        appearTextClock = APPEAR_TEXT_TIME;

                                        activePage.SwitchActivePLayerWriteTextRenderer(1);
                                        curWritingBounds = activePage.GetWritingBounds();
                                        Vector3 startWriteWorldPos = new Vector3(curWritingBounds.min.x, curWritingBounds.center.y, leftHandWorldDepthFront);
                                        leftHandTargetPos = leftHand_renderer.transform.parent.InverseTransformPoint(startWriteWorldPos);
                                    }
                                }
                            }
                            else if (activePage.playerWriteTextRenderers[1].completedWritingText && (notepadData.completedUnlocks & UnlockType.Color) == 0)
                            {
                                notepadData.completedUnlocks |= UnlockType.Color;
                                trip.selectedColorMarkerIndex = 0;

                                colorPicker.Open(activePage.playerWriteRenderers[1], openAllColors: true);
                                activePage.UnlockColorRow(1);

                                if (notepadData.completedUnlocks != trip.curUnlocks)
                                {
                                    if ((notepadData.completedUnlocks & UnlockType.MultiColor) == 0)
                                    {
                                        SetTab(TabDirection.Left, activePage.playerWriteRenderers[2].bounds);
                                        activePage.InitNextColorRow(2);
                                        appearTextClock = APPEAR_TEXT_TIME;

                                        activePage.SwitchActivePLayerWriteTextRenderer(2);
                                        curWritingBounds = activePage.GetWritingBounds();
                                        Vector3 startWriteWorldPos = new Vector3(curWritingBounds.min.x, curWritingBounds.center.y, leftHandWorldDepthFront);
                                        leftHandTargetPos = leftHand_renderer.transform.parent.InverseTransformPoint(startWriteWorldPos);
                                    }
                                }

                            }
                            else if (activePage.playerWriteTextRenderers[2].completedWritingText && (notepadData.completedUnlocks & UnlockType.MultiColor) == 0)
                            {
                                notepadData.completedUnlocks |= UnlockType.MultiColor;
                                trip.selectedColorMarkerIndex = 1;
                                colorPicker.Open(activePage.playerWriteRenderers[2], openAllColors: true);
                                activePage.UnlockColorRow(2);
                            }
                        }
                        else if (activePage.activePlayerWriteRowIndex > 0 && activePage.playerWriteRenderers[activePage.activePlayerWriteRowIndex] && trip.selectedClueMarkerColors[activePage.activePlayerWriteRowIndex - 1] == Color.black)
                        {
                            colorPicker.Open(activePage.playerWriteRenderers[activePage.activePlayerWriteRowIndex], openAllColors: true);
                        }
                    }
                    break;
                }
                atStartPencilPos = false;

                subState &= ~(SubState.WriteToggle);
            }
            break;
            case NotepadState.Erasing:
            {
                activePage.activePlayerWriteText = "";
                leftHand_renderer.PlayClipOneShotReverse(rotatePencil_clip);
                atStartPencilPos = false;
                subState &= ~(SubState.EraseToggle);

                switch (activePage.pageType)
                {
                    case PageType.Profile:
                    {
                        activeTraitorProfile.found = false;
                        trip.traitorProfiles[activePage.traitorIndex] = activeTraitorProfile;
                     
                        activePage.playerWriteRenderers[0].customBit &= ~((int)ColorBits.Diagonal);
                    }
                    break;
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
            float dist = (leftHand_renderer.transform.localPosition - leftHandTargetPos).sqrMagnitude;

            if (dist < PENCIL_DISTANCE_THRESHOLD * PENCIL_DISTANCE_THRESHOLD)
            {
                SetLeftHandHoldingPencilSprite();

                Bounds rendBounds = leftHand_renderer.GetBounds();
                Vector4 uvPivot = leftHand_renderer.sprite.uvPivot;
                Vector3 spritePivotOffset = new Vector3(rendBounds.extents.x * (1 - uvPivot.x), rendBounds.size.y * (1 - uvPivot.y));
                leftHand_renderer.transform.localPosition = leftHandOffScreenPos - spritePivotOffset;

                curWritingBounds = activePage.GetWritingBounds();
                Vector3 startWriteWorldPos = new Vector3(curWritingBounds.min.x, curWritingBounds.center.y, leftHandWorldDepthFront);
                leftHandTargetPos = transform.InverseTransformPoint(startWriteWorldPos);

                atOffCameraPos = true;
            }
        }
        else if (activePage.pageType == PageType.ColorKey)
        {
            if ((trip.curUnlocks & UnlockType.RuleOut) == 0) return;

            if (colorsData.enteredState == PickerState.Opened || colorsData.enteredState == PickerState.Opening)
            {
                colorsData.enteredState = PickerState.None;

                Bounds rendBounds = leftHand_renderer.GetBounds();
                Vector4 uvPivot = leftHand_renderer.sprite.uvPivot;
                Vector3 spritePivotOffset = new Vector3(rendBounds.extents.x * (1 - uvPivot.x), rendBounds.size.y * (1 - uvPivot.y));
                leftHandTargetPos = leftHandOffScreenPos - spritePivotOffset;
            }
            else if (colorsData.enteredState == PickerState.Closed)
            {
                colorsData.enteredState = PickerState.None;

                Vector3 startWriteWorldPos = new Vector3(activePage.playerWriteRenderers[activePage.activePlayerWriteRowIndex].GetBounds().min.x, curWritingBounds.center.y, leftHandWorldDepthFront);
                leftHandTargetPos = leftHand_renderer.transform.parent.InverseTransformPoint(startWriteWorldPos);
            }
            else if (playerInputs.numpad > 0 && playerInputs.numpad <= activePage.playerWriteTextRenderers.Length)
            {
                activePage.SwitchActivePLayerWriteTextRenderer(playerInputs.numpad - 1);
                curWritingBounds = activePage.GetWritingBounds();
                Vector3 startWriteWorldPos = new Vector3(curWritingBounds.min.x, curWritingBounds.center.y, leftHandWorldDepthFront);
                leftHandTargetPos = leftHand_renderer.transform.parent.InverseTransformPoint(startWriteWorldPos);
            }
        }
        else if (playerInputs.numpad != -1)
        {
            activePage.SwitchActivePLayerWriteTextRenderer(playerInputs.numpad - 1);
            curWritingBounds = activePage.GetWritingBounds();
            Vector3 startWriteWorldPos = new Vector3(curWritingBounds.min.x, curWritingBounds.center.y, leftHandWorldDepthFront);
            leftHandTargetPos = leftHand_renderer.transform.parent.InverseTransformPoint(startWriteWorldPos);
        }
        leftHand_renderer.transform.localPosition = Vector3.Lerp(leftHand_renderer.transform.localPosition, leftHandTargetPos, Time.deltaTime * LEFTHAND_DAMPING);
    }
    public void SetLeftHandHoldingPencilSprite()
    {
        leftHand_renderer.UpdateSpriteInputs(leftHand_renderer.atlas.motionSprites[rotatePencil_clip.keyframeStartIndex].sprite);
    }
    private void CreatePages()
    {
        List<Page> pageList = new List<Page>();
        pageList.Add(promptPage);
        promptPage.Init();


        List<int> randIndicesList = new List<int>(trip.traitorProfiles.Length);
        for(int i = 0; i < trip.traitorProfiles.Length; i++)
        {
            randIndicesList.Add(i);
        }

        for (int i = 0; i < trip.traitorProfiles.Length; i++)
        {
            int randIndex = UnityEngine.Random.Range(0, randIndicesList.Count);
            TraitorProfile traitorProfile = trip.traitorProfiles[randIndicesList[randIndex]];
            randIndicesList.RemoveAt(randIndex);

            Vector3 pagePos = pageTransform.position;
            pagePos.z += 3;
            Page traitorPage = Instantiate(traitorPage_prefab, pagePos, Quaternion.identity, pageTransform);
            traitorPage.InitProfile(traitorProfile);
            traitorPage.traitorIndex = i;
            traitorPage.gameObject.name = "Page_" + i;
            traitorPage.pageNumberRenderer.SetText("Page " + (i + 1));

            pageList.Add(traitorPage);
            traitorPage.gameObject.SetActive(false);
        }
        
        pageList.Add(colorKeyPage);
        colorKeyPage.Init();

        pages = pageList.ToArray();
        lastPageIndex = pages.Length - 1;

    }
    private void SetTab(TabDirection direction, Bounds bounds)
    {
        tabRenderer.enabled = true;
        Vector3 worldPos = new Vector3();
        if (activePage.pageType == PageType.ColorKey)
        {
            worldPos.z = leftHandWorldDepthFront;
        }
        else
        {
            worldPos.z = tabWorldDepthBack;
        }

        Bounds paperBounds = colorKeyPage.paperRenderer.GetBounds();

        switch (direction)
        {
            case TabDirection.Up:
            {
                tabRenderer.UpdateSpriteInputsByIndex(TAB_VERTICAL_SPRITE_INDEX);
                tabRenderer.FlipHSimple(true);
                worldPos.x = bounds.center.x;
                worldPos.y = paperBounds.min.y + tabRenderer.bounds.extents.y;
            }
            break;
            case TabDirection.Down:
            {
                tabRenderer.UpdateSpriteInputsByIndex(TAB_VERTICAL_SPRITE_INDEX);
                tabRenderer.FlipHSimple(false);
                worldPos.x = bounds.center.x;
                worldPos.y = paperBounds.max.y - tabRenderer.bounds.extents.y;

            }
            break;
            case TabDirection.Left:
            {
                tabRenderer.UpdateSpriteInputsByIndex(TAB_HORIZONTAL_SPRITE_INDEX);
                tabRenderer.FlipHSimple(false);
                worldPos.x = paperBounds.max.x - tabRenderer.bounds.extents.x;
                worldPos.y = bounds.center.y;
            }
            break;
            case TabDirection.Right:
            {
                tabRenderer.UpdateSpriteInputsByIndex(TAB_HORIZONTAL_SPRITE_INDEX);
                tabRenderer.FlipHSimple(true);
                worldPos.x = paperBounds.min.x + tabRenderer.bounds.extents.x;
                worldPos.y = bounds.center.y;
            }
            break;
        }
        tabRenderer.transform.position = worldPos;
    }

    private float NormalGaussianValue(float t)
    {
        return Mathf.Exp(-(Mathf.Pow(t - 0.5f, 2) / 0.045f)) * 0.5f;
    }
    private bool ToFlipUp()
    {
        return ((playerInputs.notepadPreviewAnswerAndFlip.y == 1 && activePageIndex < lastPageIndex) || (subState & (SubState.WillFlipUp | SubState.IsFlippingUp)) != 0) && (subState & SubState.CanFlipUp) != 0;
    }
    private bool ToFlipDown()
    {
        return ((playerInputs.notepadPreviewAnswerAndFlip.y == -1 && activePageIndex > 0) || (subState & (SubState.WillFlipDown | SubState.IsFlippingDown)) != 0) && (subState & SubState.CanFlipDown) != 0;
    }
    private bool ToErase()
    {
        return (sceneData.activeSceneType == SceneType.Trip && playerInputs.notepadPreviewAnswerAndFlip.x != 0 && activePage.activePlayerWriteText != "") || (subState & SubState.EraseToggle) != 0;
    }
    private bool ToWrite()
    {
        return (sceneData.activeSceneType == SceneType.Trip && playerInputs.spacebarDown && activePage.activePlayerWriteText == "") || (subState & SubState.WriteToggle) != 0;
    }
    private bool ToReveal()
    {
        AtlasRenderer mugShotRenderer = activePage.playerWriteRenderers[0];
        bool clickedOnMugshot = false;
        if (mugShotRenderer.custom.x == 0 && CursorController.IsInsideBounds(mugShotRenderer.GetBounds()))
        {
            if (playerInputs.mouseLeftDown)
            {
                mugShotRenderer.custom.w = 0;
                clickedOnMugshot = true;
            }
            else
            {
                mugShotRenderer.custom.w = 1;
                clickedOnMugshot = false;
            }
        }
        else
        {
            mugShotRenderer.custom.w = 0;
        }
        return (sceneData.activeSceneType == SceneType.Score && (playerInputs.spacebarDown || clickedOnMugshot) && traitorOutcomesRevealed < trip.traitorProfiles.Length) || (subState & SubState.RevealToggle) != 0;
    }

    private string GenerateName(Gender gender, Ethnicity ethnicity)
    {
        string genderString = gender.ToString();
        string ethnicityString = ethnicity.ToString();
        List<FirstName> firstNamesList = new List<FirstName>();

        for (int i = 0; i < nameData.firstNames.Length; i++)
        {
            FirstName fn = nameData.firstNames[i];
            if (fn.gender.Equals(genderString, StringComparison.OrdinalIgnoreCase) &&
                fn.ethnicity.Equals(ethnicityString, StringComparison.OrdinalIgnoreCase))
            {
                firstNamesList.Add(fn);
            }
        }

        if (firstNamesList.Count == 0) return "NoFirstName";

        int firstNameIndex = UnityEngine.Random.Range(0, firstNamesList.Count);
        string firstName = firstNamesList[firstNameIndex].name;

        List<LastName> lastNameList = new List<LastName>();
        for (int i = 0; i < nameData.lastNames.Length; i++)
        {
            LastName ln = nameData.lastNames[i];
            if (ln.ethnicity.Equals(ethnicityString, StringComparison.OrdinalIgnoreCase))
            {
                lastNameList.Add(ln);
            }
        }
        if (lastNameList.Count == 0) return firstName;

        int lastNameIndex = UnityEngine.Random.Range(0, lastNameList.Count);
        string lastName = lastNameList[lastNameIndex].name;

        return firstName + " " + lastName;
    }
    
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.InverseTransformPoint(leftHandTargetPos), 0.1f);
    }
}
