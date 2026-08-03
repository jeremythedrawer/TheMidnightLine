using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using UnityEngine;
using static Atlas;
using static AtlasUI;
using static NPC;
using static Scenes;
public class Notepad : MonoBehaviour
{
    const int HOLDING_PENCIL_SPRITE_INDEX = 16;

    public const float WRITE_LETTER_TIME = 0.1f;
    public const int MIN_STATION_STOPS = 2;

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
        OnScreen = 1 << 11,
        InUse = 1 << 12,
    }

    public static event Action OnFinishRevealingOutcomes;
    public static event Action OnWriteColorMarkerFirstTime;

    public PlayerInputsSO playerInputs;
    public TripSO trip;
    public NPCsDataSO npcData;
    public CameraStatsSO camStats;
    public SpyStatsSO spyStats;
    public SceneData sceneData;
    public GameEventDataSO gameEventData;
    public OptionsSO colorsData;

    public NotepadData notepadData;
    
    public AtlasRenderer rightHand_renderer;
    public AtlasRenderer frontFingers_renderer;
    public AtlasRenderer bindingRingsRend;
    public AtlasRenderer leftHand_renderer;

    public TextAsset namesJSON;

    public Page traitorPagePrefab;
    public Page promptPage;
    public Page colorKeyPage;

    [Header("Generated")]

    public Page[] pages;
    
    public Page activePage;
    public Page nextPage;

    public ColorPicker colorPicker;

    public TraitorProfile activeTraitorProfile;
    
    public NameData nameData;
    
    public Bounds curWritingBounds;

    public Vector3 leftHandTargetLocalPos;
    
    public KeyframeState curKeyframeState;

    public int activePageIndex;
    public int lastPageIndex;
    
    public int traitorOutcomesRevealed;

    public float tabWorldDepthBack;

    public float totalPencilTime;
    public float curPencilTime;
    public float revealClock;

    public bool atStartPencilPos;
    public bool atOffCameraPos;

    private void Start()
    {
        SceneController.KeepNotepad(this);
        gameObject.SetActive(false);
    }
    private void OnEnable()
    {
        Scenes.OnLoadStart += CreateNPCProfiles;
        Scenes.OnLoadTrip1 += Reinit;
        Scenes.OnLoadScore += Reinit;
    }
    private void OnDisable()
    {
        Scenes.OnLoadStart -= CreateNPCProfiles;

        Scenes.OnLoadTrip1 -= Reinit;
        
        Scenes.OnLoadScore -= Reinit;
    }
    private void Update()
    {
        UpdateState();

        if ((notepadData.subState & SubState.InUse) != 0)
        {
            ChooseState();
        }
        else
        {
            if ((notepadData.subState & SubState.OnScreen) != 0 && transform.localPosition == notepadData.inactiveLocalPos)
            {
                notepadData.subState &= ~(SubState.OnScreen);
                SetLeftHandOffScreen();
            }
        }
    }
    public void SetLeftHandOffScreen()
    {
        leftHandTargetLocalPos = notepadData.leftHandFlipPos;
        leftHand_renderer.UpdateSpriteInputs(leftHand_renderer.atlas.motionSprites[notepadData.handFlipPage_clip.keyframeStartIndex].sprite);
    }
    public void Init()
    {
        gameObject.SetActive(true);
        AtlasUI.PromptStringDict = InitEnumToStringDict<TripPrompt>();
        npcData.behaviourStringDict = InitEnumToStringDict<Behaviours>();

        
        activePage = promptPage;
        
        
        tabWorldDepthBack = rightHand_renderer.transform.position.z - 0.5f;

        Vector3 flipWorldPos = new Vector3();
        flipWorldPos.x = bindingRingsRend.transform.localPosition.x;
        flipWorldPos.y = bindingRingsRend.transform.localPosition.y;
        flipWorldPos.z = leftHand_renderer.transform.localPosition.z;
        
        notepadData.leftHandFlipPos = flipWorldPos;
        notepadData.completedUnlocks = UnlockType.None;
        notepadData.subState = SubState.None;
        notepadData.handFlipPage_clip = leftHand_renderer.atlas.clipDict[(int)NotepadMotion.FlipHand];
        notepadData.rotatePencil_clip = leftHand_renderer.atlas.clipDict[(int)NotepadMotion.RotatingPencil];
        notepadData.leftHandDepthFront = bindingRingsRend.transform.localPosition.z - 1;
        notepadData.activePageDepth = bindingRingsRend.transform.localPosition.z + 1;
        notepadData.leftHandDepthBack = rightHand_renderer.transform.localPosition.z + 1;
        notepadData.leftHandOffScreenLocalPos.x = -notepadData.activeLocalPos.x * 0.5f;

        notepadData.activeLocalPos = new Vector3(3.57998657f, 1, 5);

        SimpleSprite holdingPencilSprite = leftHand_renderer.atlas.motionSprites[HOLDING_PENCIL_SPRITE_INDEX].sprite;
        float worldPivotOffsetY = holdingPencilSprite.worldSize.y * (1 - holdingPencilSprite.uvPivot.y);
        notepadData.leftHandOffScreenLocalPos.y = camStats.camBounds.extents.y - notepadData.activeLocalPos.y - camStats.camBounds.size.y - worldPivotOffsetY;
        notepadData.leftHandOffScreenLocalPos.z = leftHand_renderer.transform.localPosition.z;

        float halfCamWidth = camStats.camBounds.extents.x;
        float halfCamHeight = camStats.camBounds.extents.y;
        float binderBoundsOffsetX = bindingRingsRend.bounds.max.x - transform.position.x;
        notepadData.inactiveLocalPos = new Vector3(halfCamWidth - binderBoundsOffsetX, -halfCamHeight + NOTEPAD_INACTIVE_OFFSET, notepadData.activeLocalPos.z);

        float bindingRingsHeight = bindingRingsRend.bounds.size.y;
        notepadData.hoverLocalPos = new Vector3(notepadData.inactiveLocalPos.x, notepadData.inactiveLocalPos.y + bindingRingsHeight, notepadData.activeLocalPos.z);

        notepadData.curState = NotepadState.Stationary;
        
        leftHandTargetLocalPos = notepadData.leftHandOffScreenLocalPos;
        
        CreatePages();
    }
    private void Reinit()
    {
        SkipToPage(0);

        notepadData.curState = NotepadState.Stationary;
        notepadData.subState = SubState.None;
        leftHandTargetLocalPos = notepadData.leftHandOffScreenLocalPos;
        colorPicker = SceneController.GetClueColorPicker();
    }
    public void EnterNotepad()
    {
        notepadData.subState |= (SubState.InUse | SubState.OnScreen);
        EnterState(NotepadState.None);

        if (activePage.pageType == PageType.ColorKey)
        {
            if ((notepadData.completedUnlocks & UnlockType.Color) == 0 && (trip.curUnlocks & UnlockType.Color) != 0)
            {
                activePage.InitNextColorRow(0);
                activePage.SwitchActivePLayerWriteTextRenderer(0);
                curWritingBounds = activePage.GetWritingBounds();
                SetLeftHandTargetPosToWritingBounds();
            }
            else if ((notepadData.completedUnlocks & UnlockType.MultiColor) == 0 && (trip.curUnlocks & UnlockType.MultiColor) != 0)
            {
                activePage.InitNextColorRow(1);
                activePage.SwitchActivePLayerWriteTextRenderer(1);
                curWritingBounds = activePage.GetWritingBounds();
                SetLeftHandTargetPosToWritingBounds();
            }
        }

        spyStats.checkingNotepad = true;
    }
    public void ExitNotepad()
    {
        Bounds rendBounds = leftHand_renderer.GetBounds();
        Vector4 uvPivot = leftHand_renderer.sprite.uvPivot;
        Vector3 spritePivotOffset = new Vector3(rendBounds.extents.x * (1 - uvPivot.x), rendBounds.size.y * (1 - uvPivot.y));
        leftHandTargetLocalPos = notepadData.leftHandOffScreenLocalPos + spritePivotOffset;
        notepadData.subState &= ~(SubState.InUse);
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
                if (ToReveal())
                {
                    SetState(NotepadState.Revealing);
                }
                else if (ToFlipUp())
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
        activePage.gameObject.SetActive(true);

        for (int i = 0; i < pages.Length; i++)
        {
            Page page = pages[i];
            page.paperRenderer.UpdateSpriteInputsByIndex(12);
            page.TogglePageContentTopHalf(true);
            page.TogglePageContentBottomHalf(true);
            page.SetPageDepth(bindingRingsRend.transform.localPosition.z + 3);
        }
        activePage.SetPageDepth(notepadData.activePageDepth);

        notepadData.subState &= ~(SubState.CanFlipDown | SubState.CanWillFlipDown | SubState.IsFlippingDown);
        curKeyframeState = KeyframeState.None;
    }
    private void UpdateState()
    {
        switch (notepadData.curState)
        {
            case NotepadState.FlippingUp:
            {
                if ((notepadData.subState & SubState.CanWillFlipUp) == 0)
                {
                    notepadData.subState |= SubState.CanWillFlipUp;
                }
                else
                {
                    if (activePageIndex < lastPageIndex - 1 && playerInputs.notepadPreviewAnswerAndFlip.y == 1)
                    {
                        notepadData.subState |= SubState.WillFlipUp;
                        notepadData.subState &= ~(SubState.WillFlipDown);
                    }
                    if (activePageIndex > 0 && playerInputs.notepadPreviewAnswerAndFlip.y == -1)
                    {
                        notepadData.subState |= SubState.WillFlipDown;
                        notepadData.subState &= ~(SubState.WillFlipUp);
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
                            nextPage.SetPlayerWriteTextAlphaBottom(normAmount: 0);
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
                            nextPage.SetPlayerWriteTextAlphaTop(normAmount: 0);
                        }
                        activePage.TogglePageContentTopHalf(false);
                        curKeyframeState = KeyframeState.TogglePageContentsTopHalf;
                    }
                    break;

                    case 7:
                    {
                        if (curKeyframeState == KeyframeState.ChangeDepth) return;
                        activePage.SetPageDepth(notepadData.leftHandDepthBack + 1);
                        leftHand_renderer.SetLocalDepth(notepadData.leftHandDepthBack);
                        curKeyframeState = KeyframeState.ChangeDepth;

                    }
                    break;
                }
                if (leftHand_renderer.curFrameIndex == notepadData.handFlipPage_clip.keyframeEndIndex)
                {
                    if (curKeyframeState == KeyframeState.None) return;

                    activePage.gameObject.SetActive(false);
                    activePageIndex++;
                    activePage = pages[activePageIndex];
                    activePage.SetPageDepth(notepadData.leftHandDepthFront + 2);

                    notepadData.subState &= ~(SubState.CanFlipUp | SubState.CanWillFlipUp | SubState.IsFlippingUp);
                    curKeyframeState = KeyframeState.None;
                }
            }
            break;
            case NotepadState.FlippingDown:
            {
                if ((notepadData.subState & SubState.CanWillFlipDown) == 0)
                {
                    notepadData.subState |= SubState.CanWillFlipDown;
                }
                else
                {
                    if (activePageIndex < lastPageIndex && playerInputs.notepadPreviewAnswerAndFlip.y == 1)
                    {
                        notepadData.subState |= SubState.WillFlipUp;
                        notepadData.subState &= ~(SubState.WillFlipDown);
                    }
                    else if (activePageIndex > 1 && playerInputs.notepadPreviewAnswerAndFlip.y == -1)
                    {
                        notepadData.subState |= SubState.WillFlipDown;
                        notepadData.subState &= ~(SubState.WillFlipUp);
                    }
                }
                switch (leftHand_renderer.curFrameIndex)
                {
                    case 0:
                    {
                        if (curKeyframeState == KeyframeState.None) return;

                        activePage.gameObject.SetActive(false);
                        activePageIndex--;
                        activePage = pages[activePageIndex];

                        notepadData.subState &= ~(SubState.CanFlipDown | SubState.CanWillFlipDown | SubState.IsFlippingDown);

                        curKeyframeState = KeyframeState.None;
                    }
                    break;
                    case 2:
                    {
                        if (curKeyframeState == KeyframeState.TogglePageContentsBottomHalf) return;
                        pages[activePageIndex - 1].TogglePageContentBottomHalf(true);

                        if (pages[activePageIndex - 1].activePlayerWriteText == "")
                        {
                            pages[activePageIndex - 1].SetPlayerWriteTextAlphaBottom(normAmount: 0);
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
                            pages[activePageIndex - 1].SetPlayerWriteTextAlphaTop(normAmount: 0);
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

                        leftHand_renderer.SetLocalDepth(notepadData.leftHandDepthFront);
                        pages[activePageIndex - 1].SetPageDepth(notepadData.leftHandDepthFront + 2);


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

                    if (t > 1f) notepadData.subState &= ~(SubState.WriteToggle);
                }

                if (activePageIndex < lastPageIndex && playerInputs.notepadPreviewAnswerAndFlip.y == 1)
                {
                    notepadData.subState |= SubState.WillFlipUp;
                    notepadData.subState &= ~(SubState.WillFlipDown);
                }
                else if (activePageIndex > 0 && playerInputs.notepadPreviewAnswerAndFlip.y == -1)
                {
                    notepadData.subState |= SubState.WillFlipDown;
                    notepadData.subState &= ~(SubState.WillFlipUp);
                }
            }
            break;
            case NotepadState.Erasing:
            {
                if(!atStartPencilPos)
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

                    if (activePage.activePlayerWriteTextRenderer.text.Length == 0) notepadData.subState &= ~(SubState.EraseToggle);
                }

                if (activePageIndex < lastPageIndex && playerInputs.notepadPreviewAnswerAndFlip.y == 1)
                {
                    notepadData.subState |= SubState.WillFlipUp;
                    notepadData.subState &= ~(SubState.WillFlipDown);
                }
                else if (activePageIndex > 0 && playerInputs.notepadPreviewAnswerAndFlip.y == -1)
                {
                    notepadData.subState |= SubState.WillFlipDown;
                    notepadData.subState &= ~(SubState.WillFlipUp);
                }
            }
            break;
            case NotepadState.Stationary:
            {
                if (sceneData.activeSceneType == SceneType.Trip && activePage.activePlayerWriteText == "" && playerInputs.notepadPreviewAnswerAndFlip.x != 0)
                {
                    activePage.SwitchActivePreviewPlayerWriteText((int)playerInputs.notepadPreviewAnswerAndFlip.x);
                }
                HandleStationaryLeftHandMove();
                
                activePage.UpdatePage();
            }
            break;
            case NotepadState.Revealing:
            {
                revealClock += Time.deltaTime;

                activePage.UpdateMugShotReveal(revealClock / REVEAL_TIME);

                if (revealClock > REVEAL_TIME)
                {
                    activePage.UpdateMugShotReveal(1);
                    notepadData.subState &= ~(SubState.RevealToggle);

                    activePage.WriteForPlayerWriteText(trip.stationsDataArray[activeTraitorProfile.npcProfile.disembarkingStationIndex].name);

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
                    if ((notepadData.completedUnlocks & UnlockType.Color) == 0 && (trip.curUnlocks & UnlockType.Color) != 0)
                    {
                        nextPage.InitNextColorRow(0);
                        nextPage.SwitchActivePLayerWriteTextRenderer(0);
                        curWritingBounds = nextPage.GetWritingBounds();
                        SetLeftHandTargetPosToWritingBounds();
                    }
                    else if ((notepadData.completedUnlocks & UnlockType.MultiColor) == 0 && (trip.curUnlocks & UnlockType.MultiColor) != 0)
                    {
                        nextPage.InitNextColorRow(1);
                        nextPage.SwitchActivePLayerWriteTextRenderer(1);
                        curWritingBounds = nextPage.GetWritingBounds();
                        SetLeftHandTargetPosToWritingBounds();
                    }
                }

                leftHand_renderer.SetLocalDepth(notepadData.leftHandDepthFront);
                leftHand_renderer.PlayClipOneShot(notepadData.handFlipPage_clip);

                curKeyframeState = KeyframeState.Start;

                notepadData.subState |= SubState.IsFlippingUp;
                notepadData.subState &= ~(SubState.WillFlipUp);                

                leftHand_renderer.transform.localPosition = notepadData.leftHandFlipPos;
                
            }
            break;
            case NotepadState.FlippingDown:
            {
                activePage.SetPageDepth(rightHand_renderer.transform.localPosition.z - 1);

                pages[activePageIndex - 1].gameObject.SetActive(true);
                leftHand_renderer.PlayClipOneShotReverse(notepadData.handFlipPage_clip);

                curKeyframeState = KeyframeState.Start;
                
                notepadData.subState |= SubState.IsFlippingDown;
                notepadData.subState &= ~(SubState.WillFlipDown);
                notepadData.subState &= ~(SubState.CanFlipUp);

                leftHand_renderer.transform.localPosition = notepadData.leftHandFlipPos;
                leftHand_renderer.SetLocalDepth(notepadData.leftHandDepthBack);
            }
            break;
            case NotepadState.Writing:
            {
                SetLeftHandHoldingPencilSprite();
                curWritingBounds = activePage.GetWritingBounds();
                Vector3 startWriteWorldPos = new Vector3(curWritingBounds.min.x, curWritingBounds.center.y, leftHand_renderer.transform.position.z);
                notepadData.leftHandPencilPos = leftHand_renderer.transform.parent.InverseTransformPoint(startWriteWorldPos);

                activeTraitorProfile = trip.traitorProfiles[activePage.traitorIndex];

                curPencilTime = 0;

                notepadData.subState |= SubState.WriteToggle;
            }
            break;
            case NotepadState.Erasing:
            {
                leftHand_renderer.PlayClipOneShot(notepadData.rotatePencil_clip);

                curWritingBounds = activePage.GetWritingBounds();
                Vector3 startEraseWorldPos = new Vector3(curWritingBounds.max.x, curWritingBounds.center.y, leftHand_renderer.transform.position.z);
                notepadData.leftHandPencilPos = leftHand_renderer.transform.parent.InverseTransformPoint(startEraseWorldPos);

                activeTraitorProfile = trip.traitorProfiles[activePage.traitorIndex];

                curPencilTime = 0;

                notepadData.subState |= SubState.EraseToggle;
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
                if (sceneData.activeSceneType == SceneType.Trip)
                {
                    activePage.SetPreviewPlayerWriteTexts(prevState);
                }
                notepadData.subState |= (SubState.CanFlipUp | SubState.CanFlipDown);
                switch (activePage.pageType)
                {
                    case PageType.ColorKey:
                    {
                        if ((notepadData.completedUnlocks & UnlockType.RuleOut) == 0 && (trip.curUnlocks & UnlockType.RuleOut) != 0)
                        {
                            notepadData.completedUnlocks |= UnlockType.RuleOut;
                            activePage.ShowRuleOutRenderer();
                        }
                    }
                    break;
                }
            }
            break;
            case NotepadState.Revealing:
            {
                revealClock = 0;
                notepadData.subState |= SubState.RevealToggle;

                activeTraitorProfile = trip.traitorProfiles[activePage.traitorIndex];
            }
            break;
        }
    }
    private void SetLeftHandTargetPosToWritingBounds()
    {
        Vector2 writePos = new Vector2(curWritingBounds.min.x, curWritingBounds.center.y);
        leftHandTargetLocalPos = transform.InverseTransformPoint(writePos);
        leftHandTargetLocalPos.z = notepadData.leftHandDepthFront;
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
                traitorProfile.disembarkingStationIndex = Mathf.Min(i + Mathf.CeilToInt(gaussianNormSpawnIndex * stationsLeft) + MIN_STATION_STOPS, trip.stationsDataArray.Length - 1);

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
        for (int i = 0; i < trip.stationsDataArray.Length; i++)
        {
            StationSO station = trip.stationsDataArray[i];

            station.bystanderProfiles = new NPCProfile[station.bystanderSpawnCount];

            for (int j = 0; j < station.bystanderSpawnCount; j++)
            {
                int randIndex = UnityEngine.Random.Range(0, totalNPCProfiles.Count);
                NPCProfile bystanderProfile = totalNPCProfiles[randIndex];

                bystanderProfile.boardingStationIndex = i;

                int stationsLeft = trip.stationsDataArray.Length - i;
                float normSpawnIndex = (float)j / (float)station.bystanderSpawnCount;
                float gaussianNormSpawnIndex = NormalGaussianValue(normSpawnIndex);
                bystanderProfile.disembarkingStationIndex = Mathf.Min(i + Mathf.CeilToInt(gaussianNormSpawnIndex * stationsLeft), trip.stationsDataArray.Length - 1);

                station.bystanderProfiles[j] = bystanderProfile;
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
                        if (activePage.playerWriteTextRenderers[0].completedWritingText && (notepadData.completedUnlocks & UnlockType.Color) == 0 && (trip.curUnlocks & UnlockType.Color) != 0)
                        {
                            notepadData.completedUnlocks |= UnlockType.Color;
                            trip.selectedColorMarkerIndex = 0;

                            SceneController.GetClueColorPicker().Open(activePage.playerWriteRenderers[0], ColorPicker.SelectType.Clue);
                            activePage.SetColorMarkerButtonSprite(0);

                            OnWriteColorMarkerFirstTime?.Invoke();

                            if ((notepadData.completedUnlocks & UnlockType.MultiColor) == 0 && (trip.curUnlocks & UnlockType.MultiColor) != 0)
                            {
                                activePage.InitNextColorRow(1);
                                activePage.SwitchActivePLayerWriteTextRenderer(1);
                                
                                curWritingBounds = activePage.GetWritingBounds();
                                SetLeftHandTargetPosToWritingBounds();

                                Vector3 startWriteWorldPos = new Vector3(curWritingBounds.min.x, curWritingBounds.center.y, notepadData.leftHandDepthFront);
                                leftHandTargetLocalPos = leftHand_renderer.transform.parent.InverseTransformPoint(startWriteWorldPos);
                            }
                        }
                        else if (activePage.playerWriteTextRenderers[1].completedWritingText && (notepadData.completedUnlocks & UnlockType.MultiColor) == 0 && (trip.curUnlocks & UnlockType.MultiColor) != 0)
                        {
                            notepadData.completedUnlocks |= UnlockType.MultiColor;
                            trip.selectedColorMarkerIndex = 1;
                            colorPicker.Open(activePage.playerWriteRenderers[1], ColorPicker.SelectType.Clue);
                            activePage.SetColorMarkerButtonSprite(1);
                        }
                    }
                    break;
                }
                atStartPencilPos = false;

                notepadData.subState &= ~(SubState.WriteToggle);
            }
            break;
            case NotepadState.Erasing:
            {
                activePage.activePlayerWriteText = "";
                leftHand_renderer.PlayClipOneShotReverse(notepadData.rotatePencil_clip);
                atStartPencilPos = false;
                notepadData.subState &= ~(SubState.EraseToggle);

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
        if ((notepadData.subState & SubState.InUse) == 0) return;

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

                if (activePage.activePlayerWriteTextRenderer != null)
                {
                    curWritingBounds = activePage.GetWritingBounds();
                    SetLeftHandTargetPosToWritingBounds();
                }

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
                leftHandTargetLocalPos = notepadData.leftHandOffScreenLocalPos - spritePivotOffset;
            }
            else if (colorsData.enteredState == PickerState.Closed)
            {
                colorsData.enteredState = PickerState.None;
                curWritingBounds = activePage.GetWritingBounds();
                SetLeftHandTargetPosToWritingBounds();
            }
            else if (playerInputs.numpad > 0 && playerInputs.numpad <= trip.unlockedColorMarkerCount)
            {
                activePage.SwitchActivePLayerWriteTextRenderer(playerInputs.numpad - 1);
                curWritingBounds = activePage.GetWritingBounds();
                SetLeftHandTargetPosToWritingBounds();
            }
        }
        else if (playerInputs.numpad != -1)
        {
            activePage.SwitchActivePLayerWriteTextRenderer(playerInputs.numpad - 1);
            curWritingBounds = activePage.GetWritingBounds();
            SetLeftHandTargetPosToWritingBounds();
        }
        leftHand_renderer.transform.localPosition = Vector3.Lerp(leftHand_renderer.transform.localPosition, leftHandTargetLocalPos, Time.deltaTime * LEFTHAND_DAMPING);
    }
    public void SetLeftHandHoldingPencilSprite()
    {
        leftHand_renderer.UpdateSpriteInputs(leftHand_renderer.atlas.motionSprites[notepadData.rotatePencil_clip.keyframeStartIndex].sprite);
    }
    private void CreatePages()
    {
        List<Page> pageList = new List<Page>();
        pageList.Add(promptPage);

        int totalPages = trip.traitorProfiles.Length + 2;
        promptPage.Init(0, totalPages);


        List<int> randIndicesList = new List<int>(trip.traitorProfiles.Length);
        for(int i = 0; i < trip.traitorProfiles.Length; i++)
        {
            randIndicesList.Add(i);
        }

        for (int i = 0; i < trip.traitorProfiles.Length; i++)
        {
            int randIndex = UnityEngine.Random.Range(0, randIndicesList.Count);
            int traitorIndex = randIndicesList[randIndex];
            TraitorProfile traitorProfile = trip.traitorProfiles[traitorIndex];
            randIndicesList.RemoveAt(randIndex);

            Page traitorPage = Instantiate(traitorPagePrefab, transform);
            traitorPage.transform.localPosition = new Vector3(0, 0, notepadData.leftHandDepthBack - 1);
            traitorPage.InitProfile(traitorProfile, i + 1, totalPages);
            traitorPage.traitorIndex = traitorIndex;
            traitorPage.gameObject.name = "Page_" + i;

            pageList.Add(traitorPage);
            traitorPage.gameObject.SetActive(false);
        }
        
        pageList.Add(colorKeyPage);
        colorKeyPage.Init(totalPages - 1, totalPages);

        pages = pageList.ToArray();
        lastPageIndex = pages.Length - 1;

    }
    private float NormalGaussianValue(float t)
    {
        return Mathf.Exp(-(Mathf.Pow(t - 0.5f, 2) / 0.045f)) * 0.5f;
    }
    private bool ToFlipUp()
    {
        return ((playerInputs.notepadPreviewAnswerAndFlip.y == 1 && activePageIndex < lastPageIndex) || (notepadData.subState & (SubState.WillFlipUp | SubState.IsFlippingUp)) != 0) && (notepadData.subState & SubState.CanFlipUp) != 0;
    }
    private bool ToFlipDown()
    {
        return ((playerInputs.notepadPreviewAnswerAndFlip.y == -1 && activePageIndex > 0) || (notepadData.subState & (SubState.WillFlipDown | SubState.IsFlippingDown)) != 0) && (notepadData.subState & SubState.CanFlipDown) != 0;
    }
    private bool ToErase()
    {
        return (sceneData.activeSceneType == SceneType.Trip && playerInputs.notepadPreviewAnswerAndFlip.x != 0 && activePage.activePlayerWriteText != "") || (notepadData.subState & SubState.EraseToggle) != 0;
    }
    private bool ToWrite()
    {
        return (sceneData.activeSceneType == SceneType.Trip && playerInputs.spacebarDown && activePage.activePlayerWriteText == "" && activePage.activePlayerWriteTextRenderer != null) || (notepadData.subState & SubState.WriteToggle) != 0;
    }
    private bool ToReveal()
    {
        if (sceneData.activeSceneType != SceneType.Score) return false;

        AtlasRenderer mugShotRenderer = activePage.playerWriteRenderers[0];
        if (mugShotRenderer.custom.x == 1) return false;

        bool clickedOnMugshot = false;
        if (mugShotRenderer.custom.x == 0 && CursorController.IsInsideBounds(mugShotRenderer.GetBounds(), isClickable: true))
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
        return ((playerInputs.spacebarDown || clickedOnMugshot) && traitorOutcomesRevealed < trip.traitorProfiles.Length) || (notepadData.subState & SubState.RevealToggle) != 0;
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
    
    
    public void FlipToPage(int pageIndex)
    {
        FlippingToPage(pageIndex).Forget();
    }
    private async UniTask FlippingToPage(int pageIndex)
    {
        notepadData.subState &= ~(SubState.CanFlipUp | SubState.CanFlipDown);

        if (activePage.pageIndex < pageIndex)
        {
            while (activePage.pageIndex != pageIndex)
            {
                SetState(NotepadState.FlippingUp);
                while(notepadData.curState == NotepadState.FlippingUp) await UniTask.Yield();
                await UniTask.Yield();
            }
            notepadData.subState |= SubState.CanFlipUp | SubState.CanFlipDown;
        }
        else if (activePage.pageIndex > 0)
        {
            while (activePage.pageIndex != pageIndex)
            {
                SetState(NotepadState.FlippingDown);
                while (notepadData.curState == NotepadState.FlippingUp) await UniTask.Yield();
                await UniTask.Yield();
            }
            notepadData.subState |= SubState.CanFlipUp | SubState.CanFlipDown;
        }
    }
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.InverseTransformPoint(leftHandTargetLocalPos), 0.1f);
    }
}
