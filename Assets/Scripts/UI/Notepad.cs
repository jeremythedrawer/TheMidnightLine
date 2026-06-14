using System;
using System.Collections.Generic;
using UnityEngine;
using static Atlas;
using static NPC;
using static AtlasUI;
using static Spy;
using static Scenes;
using System.Linq;
public class Notepad : MonoBehaviour
{
    public const float WRITE_LETTER_TIME = 0.1f;
    public const int MIN_STATION_STOPS = 1;

    const float LEFTHAND_DAMPING = 10f;
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

    public PlayerInputsSO playerInputs;
    public TripSO trip;
    public NPCsDataSO npcData;
    public CameraStatsSO camStats;
    public SpyStatsSO spyStats;
    public SceneData sceneData;
    public GameEventDataSO gameEventData;

    public NotepadData notepadData;
    
    public AtlasRenderer rightHand_renderer;
    public AtlasRenderer frontFingers_renderer;
    public AtlasRenderer bindingRings_renderer;
    public AtlasRenderer leftHand_renderer;

    public Transform pageTransform;

    public TextAsset namesJSON;

    public Page traitorPage_prefab;
    public Page promptPage;
    public Page colorKeyPage;
    public Page confirmPage;

    [Header("Generated")]
    public KeyframeState curKeyframeState;

    public Page activePage;
    public Page[] pages;
    public TraitorProfile activeTraitorProfile;

    public NameData nameData;
    
    public int activePageIndex;
    public int lastPageIndex;

    public AtlasClip handFlipPage_clip;
    public AtlasClip rotatePencil_clip;

    public Vector3 leftHandFlipPos;
    public Vector3 leftHandPencilPos;
    public Vector3 leftHandTargetPos;
    public Vector3 leftHandOffScreenPos;
    
    public Bounds curWritingBounds;
    
    public int leftHandWorldDepthFront;
    public int leftHandWorldDepthBack;

    public int flipToggle;
    public bool writeToggle;
    public bool revealToggle;
    public bool eraseToggle;
    public bool willFlipUp;
    public bool willFlipDown;
    public bool atStartPencilPos;
    public bool atOffCameraPos;
    public float totalPencilTime;
    public float curPencilTime;
    public float appearTextClock;
    public float revealClock;
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
        leftHandFlipPos = leftHand_renderer.transform.parent.InverseTransformPoint(flipWorldPos);
        float distLeftHandToOffScreen = camStats.camBounds.min.y - (leftHand_renderer.bounds.max.y - leftHand_renderer.transform.position.y);
        Vector3 offscreenWorldPos = new Vector3(leftHand_renderer.transform.position.x, distLeftHandToOffScreen, leftHand_renderer.transform.position.z);
        leftHandOffScreenPos = leftHand_renderer.transform.parent.InverseTransformPoint(offscreenWorldPos);
        leftHandTargetPos = leftHandOffScreenPos;

        activePage = promptPage;
        switch (sceneData.activeSceneType)
        { 
            case SceneType.Trip:
            {
                CreatePages();
            }
            break;
            case SceneType.Score:
            {
            }
            break;
        }


    }
    private void Update()
    {
        ChooseState();
        UpdateState();
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

            case PageType.Confirm:
            {
                if (ToFlipDown())
                {
                    SetState(NotepadState.FlippingDown);
                }
                else if (ToErase())
                {
                    SetState(NotepadState.Erasing);
                }
                else if (ToWrite())
                {
                    if (spyStats.curLocationState != LocationState.Station)
                    {
                        SetState(NotepadState.Writing);
                    }
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
    private bool ToFlipUp()
    {
        return ((playerInputs.notepadPreviewAnswerAndFlip.y == 1 || willFlipUp) && !writeToggle && !eraseToggle && !revealToggle) || flipToggle == 1;
    }
    private bool ToFlipDown()
    {
        return ((playerInputs.notepadPreviewAnswerAndFlip.y == -1 || willFlipDown) && !writeToggle && !eraseToggle && !revealToggle) || flipToggle == -1;
    }
    private bool ToErase()
    {
        return (sceneData.activeSceneType == SceneType.Trip && playerInputs.notepadPreviewAnswerAndFlip.x != 0 && activePage.playerWriteText != "") || eraseToggle;
    }
    private bool ToWrite()
    {
        return (sceneData.activeSceneType == SceneType.Trip && playerInputs.notepadConfirmAnswer && activePage.playerWriteText == "") || writeToggle;
    }
    private bool ToReveal()
    {
        return (sceneData.activeSceneType == SceneType.Score && playerInputs.notepadConfirmAnswer) || revealToggle;
    }
    private void SetState(NotepadState newState)
    {
        if (notepadData.curState == newState) return;
        ExitState();
        notepadData.prevState = notepadData.curState;
        notepadData.curState = newState;
        EnterState(notepadData.prevState);
    }
    private void EnterState(NotepadState prevState)
    {
        switch(notepadData.curState)
        {
            case NotepadState.FlippingUp:
            {
                Page traitorPageAhead = pages[activePageIndex + 1];
                traitorPageAhead.gameObject.SetActive(true);

                leftHand_renderer.UpdateDepthRealtime(leftHandWorldDepthFront);
                leftHand_renderer.PlayClipOneShot(handFlipPage_clip);

                curKeyframeState = KeyframeState.Start;
                flipToggle = 1;

                leftHand_renderer.transform.localPosition = leftHandFlipPos;
                
                willFlipUp = false;
            }
            break;
            case NotepadState.FlippingDown:
            {
                activePage.SetPageDepth(rightHand_renderer.transform.position.z - 1);

                pages[activePageIndex - 1].gameObject.SetActive(true);
                leftHand_renderer.PlayClipOneShotReverse(handFlipPage_clip);

                curKeyframeState = KeyframeState.Start;
                
                flipToggle = -1;

                leftHand_renderer.transform.localPosition = leftHandFlipPos;
                leftHand_renderer.UpdateDepthRealtime(leftHandWorldDepthBack);
                
                willFlipDown = false;
            }
            break;
            case NotepadState.Writing:
            {
                leftHand_renderer.UpdateSpriteInputs(leftHand_renderer.atlas.motionSprites[rotatePencil_clip.keyframeStartIndex].sprite);
                curWritingBounds = activePage.GetWritingBounds();
                Vector3 startWriteWorldPos = new Vector3(curWritingBounds.min.x, curWritingBounds.center.y, leftHand_renderer.transform.position.z);
                leftHandPencilPos = leftHand_renderer.transform.parent.InverseTransformPoint(startWriteWorldPos);

                activeTraitorProfile = trip.traitorProfiles[activePage.traitorIndex];

                curPencilTime = 0;
                writeToggle = true;
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
                eraseToggle = true;
            }
            break;
            case NotepadState.Stationary:
            {
                if (activePage.playerWriteTextRenderers.Length > 0 && prevState != NotepadState.Writing && prevState != NotepadState.Erasing)
                {
                    Debug.Log("left hand going off screen");
                    leftHandTargetPos = leftHandOffScreenPos;
                    atOffCameraPos = false;
                }

                if (activePage.playerWriteText == "")
                {
                    activePage.SetPreviewPlayerWriteTexts(prevState);
                    appearTextClock = APPEAR_TEXT_TIME;
                }
            }
            break;
            case NotepadState.Revealing:
            {
                revealClock = 0;
                revealToggle = true;

                activeTraitorProfile = trip.traitorProfiles[activePage.traitorIndex];
            }
            break;
        }
    }
    private void ExitState()
    {
        switch (notepadData.curState)
        {
            case NotepadState.Writing:
            {

                switch(activePage.pageType)
                {
                    case PageType.Confirm:
                    {
                        spyStats.signedNotepad = true;
                    }
                    break;

                    case PageType.Profile:
                    {
                        if(activeTraitorProfile.npcProfile.disembarkingStationIndex == activePage.playerWriteIndex)
                        {
                            activeTraitorProfile.found = true;
                        }
                        trip.traitorProfiles[activePage.traitorIndex] = activeTraitorProfile;
                    }
                    break;
                }
                atStartPencilPos = false;
                writeToggle = false;
            }
            break;
            case NotepadState.Erasing:
            {
                activePage.playerWriteText = "";
                leftHand_renderer.PlayClipOneShotReverse(rotatePencil_clip);
                atStartPencilPos = false;
                eraseToggle = false;

                switch (activePage.pageType)
                {
                    case PageType.Profile:
                    {
                        activeTraitorProfile.found = false;
                        trip.traitorProfiles[activePage.traitorIndex] = activeTraitorProfile;
                    }
                    break;

                }
            }
            break;
        }

    }
    private void UpdateState()
    {
        switch (notepadData.curState)
        {
            case NotepadState.FlippingUp:
            {
                Page pageAhead = pages[activePageIndex + 1];
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
                        if (pageAhead.playerWriteText == "")
                        {
                            pageAhead.SetPlayerWriteTextAlphaBottom(normAmount: 1);
                        }
                        activePage.TogglePageContentBottomHalf(false);
                        curKeyframeState = KeyframeState.TogglePageContentsBottomHalf;
                    }
                    break;

                    case 4:
                    {
                        if (curKeyframeState == KeyframeState.TogglePageContentsTopHalf) return;
                        if (pageAhead.playerWriteText == "")
                        {
                            pageAhead.SetPlayerWriteTextAlphaTop(normAmount: 1);
                        }
                        activePage.TogglePageContentTopHalf(false);
                        curKeyframeState = KeyframeState.TogglePageContentsTopHalf;
                    }
                    break;

                    case 7:
                    {
                        if (curKeyframeState == KeyframeState.ChangeDepth) return;
                        activePage.SetPageDepth(leftHandWorldDepthBack + 1);
                        leftHand_renderer.UpdateDepthRealtime(leftHandWorldDepthBack);
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
                    flipToggle = 0;

                    curKeyframeState = KeyframeState.None;

                }
            }
            break;
            case NotepadState.FlippingDown:
            {

                switch (leftHand_renderer.curFrameIndex)
                {
                    case 0:
                    {
                        if (curKeyframeState == KeyframeState.None) return;
                        activePage.gameObject.SetActive(false);
                        activePageIndex--;
                        activePage = pages[activePageIndex];

                        flipToggle = 0;

                        curKeyframeState = KeyframeState.None;
                    }
                    break;
                    case 2:
                    {
                        if (curKeyframeState == KeyframeState.TogglePageContentsBottomHalf) return;

                        pages[activePageIndex - 1].TogglePageContentBottomHalf(true);
                        if (pages[activePageIndex - 1].playerWriteText == "")
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

                        if (pages[activePageIndex - 1].playerWriteText == "")
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

                        leftHand_renderer.UpdateDepthRealtime(leftHandWorldDepthFront);
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
                        totalPencilTime = (activePage.playerWriteText.Length + 1) * WRITE_LETTER_TIME;
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

                    if (t > 1f) writeToggle = false;
                }

                if (activePageIndex < lastPageIndex && playerInputs.notepadPreviewAnswerAndFlip.y == 1)
                {
                    willFlipUp = true;
                }
                else if (activePageIndex > 0 && playerInputs.notepadPreviewAnswerAndFlip.y == -1)
                {
                    willFlipDown = true;
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
                        totalPencilTime = activePage.playerWriteText.Length * WRITE_LETTER_TIME;

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

                    if (activePage.activePlayerWriteTextRenderer.text.Length == 0) eraseToggle = false;
                }

                if (activePageIndex < lastPageIndex && playerInputs.notepadPreviewAnswerAndFlip.y == 1)
                {
                    willFlipUp = true;
                }
                else if (activePageIndex > 0 && playerInputs.notepadPreviewAnswerAndFlip.y == -1)
                {
                    willFlipDown = true;
                }
            }
            break;
            case NotepadState.Stationary:
            {
                if (activePage.playerWriteTextRenderers.Length > 0 && !atOffCameraPos)
                {
                    float dist = (leftHand_renderer.transform.localPosition - leftHandTargetPos).sqrMagnitude;
                    if (dist < PENCIL_DISTANCE_THRESHOLD * PENCIL_DISTANCE_THRESHOLD)
                    {
                        leftHand_renderer.UpdateSpriteInputs(leftHand_renderer.atlas.motionSprites[rotatePencil_clip.keyframeStartIndex].sprite);
                        curWritingBounds = activePage.GetWritingBounds();
                        Vector3 startWriteWorldPos = new Vector3(curWritingBounds.min.x, curWritingBounds.center.y, leftHandWorldDepthFront);
                        leftHandTargetPos = leftHand_renderer.transform.parent.InverseTransformPoint(startWriteWorldPos);

                        atOffCameraPos = true;
                    }
                }

                leftHand_renderer.transform.localPosition = Vector3.Lerp(leftHand_renderer.transform.localPosition, leftHandTargetPos, Time.deltaTime * LEFTHAND_DAMPING);

                if (playerInputs.notepadPreviewAnswerAndFlip.x != 0)
                {
                    activePage.SwitchActivePreviewPlayerWriteText((int)playerInputs.notepadPreviewAnswerAndFlip.x);
                    appearTextClock = APPEAR_TEXT_TIME;
                }

                if (activePage.playerWriteText == "")
                {
                    activePage.UpdatePreviewPlayerWriteText(appear: true, ref appearTextClock);
                }

                if (CursorController.IsInsideBounds(activePage.exitButton_renderer.bounds))
                {
                    if (playerInputs.mouseLeftDown)
                    {
                        activePage.InvertExitButton(invert: false, pointDown: true);
                        SpyBrain.ToggleNotepad(false);
                    }
                    else
                    {
                        activePage.InvertExitButton(invert: true, pointDown: true);
                    }
                }
                else
                {
                    activePage.InvertExitButton(invert: false, pointDown: false);
                }

                if (activePage.pageType != PageType.Prompt && CursorController.IsInsideBounds(activePage.paperCornerLeftButtonRenderer.bounds))
                {
                    if (playerInputs.mouseLeftDown)
                    {
                        activePage.InvertLeftArrowButton(false);
                        flipToggle = -1;
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

                if (activePage.pageType != PageType.Confirm && CursorController.IsInsideBounds(activePage.paperCornerRightButtonRenderer.bounds))
                {
                    if (playerInputs.mouseLeftDown)
                    {
                        activePage.InvertRightArrowButton(false);
                        flipToggle = 1;
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

                if (activeTraitorProfile.found)
                {
                    activePage.UpdateMugShotReveal(revealClock / REVEAL_TIME);
                }
                if (revealClock > REVEAL_TIME)
                {
                    if (activeTraitorProfile.found)
                    {
                        gameEventData.OnTraitorsFoundScoreUpdate.Raise();
                    }
                    revealToggle = false;
                }
            }
            break;
        }
    }
    private void CreateNPCProfiles()
    {
        nameData = JsonUtility.FromJson<NameData>(namesJSON.text);
        List<NPCProfile> totalNPCProfiles = new List<NPCProfile>();
        List<NPCProfile> bystanderProfiles = new List<NPCProfile>();

        for (int i = 0; i < trip.npc_prefabsArray.Length; i++)
        {
            NPCBrain npcPrefab = trip.npc_prefabsArray[i];

            int behaviourValue = (int)npcPrefab.npc.behaviours;

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
            for (int j = 0; j < flagCount - 1; j++)
            {
                Behaviours firstBehaviour = (Behaviours)validFlags[j];

                for (int k = j; k < flagCount - 1; k++)
                {
                    Behaviours secondBehaviour = (Behaviours)validFlags[k];
                    Behaviours twoBehaviours = firstBehaviour | secondBehaviour;
                    string name = GenerateName(npcPrefab.npc.gender, npcPrefab.npc.ethnicity);

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
            totalTraitorsInTrip += station.traitorSpawnAmount;
        }
        trip.traitorProfiles = new TraitorProfile[totalTraitorsInTrip];

        int stationIndex = 0;
        int traitorsAtStation = 0;
        for (int i = 0; i < totalTraitorsInTrip; i++)
        {
            StationSO station = trip.stationsDataArray[stationIndex];

            int randProfileIndex = UnityEngine.Random.Range(0, totalNPCProfiles.Count);
            NPCProfile traitorProfile = totalNPCProfiles[randProfileIndex];
            traitorProfile.boardingStationIndex = stationIndex;
            traitorProfile.disembarkingStationIndex = UnityEngine.Random.Range(Mathf.Min(stationIndex + MIN_STATION_STOPS, trip.stationsDataArray.Length - 1) , trip.stationsDataArray.Length - 1);

            NPCSO traitor = trip.npc_prefabsArray[traitorProfile.npcPrefabIndex].npc;

            trip.traitorProfiles[i] = new TraitorProfile()
            {
                npcProfile = traitorProfile,
                coveredMugshotIndex = traitor.coveredMugshotIndex,
                uncoveredMugshotIndex = traitor.uncoveredMugshotIndex,
            };
            totalNPCProfiles.RemoveAt(randProfileIndex);
            for (int j = totalNPCProfiles.Count - 1; j >= 0; j--)
            {
                if (totalNPCProfiles[j].npcPrefabIndex != traitorProfile.npcPrefabIndex) continue;

                bystanderProfiles.Add(totalNPCProfiles[j]);
                totalNPCProfiles.RemoveAt(j);
            }

            if (traitorsAtStation == station.traitorSpawnAmount)
            {
                stationIndex++;
                traitorsAtStation = 0;
            }

            traitorsAtStation++;
        }

        activePageIndex = 0;
        lastPageIndex = pages.Length + 1;

        totalNPCProfiles.AddRange(bystanderProfiles);

        for (int i = 0; i < trip.stationsDataArray.Length; i++)
        {
            StationSO station = trip.stationsDataArray[i];
            int bystandersToSpawnAtStation = (int)(station.busynessFactor * totalNPCProfiles.Count);
            station.bystanderProfiles = new NPCProfile[bystandersToSpawnAtStation];

            for (int j = 0; j < bystandersToSpawnAtStation; j++)
            {
                if (totalNPCProfiles.Count == 0) { Debug.LogWarning("the total of the station busynessFactor exceeds 1. Total should be between 0 - 1"); return; }
                int randProfileIndex = UnityEngine.Random.Range(0, totalNPCProfiles.Count);

                NPCProfile bystanderProfile = totalNPCProfiles[randProfileIndex];

                bystanderProfile.boardingStationIndex = i;
                bystanderProfile.disembarkingStationIndex = UnityEngine.Random.Range(Mathf.Min(stationIndex + MIN_STATION_STOPS, trip.stationsDataArray.Length - 1), trip.stationsDataArray.Length - 1);

                totalNPCProfiles.RemoveAt(randProfileIndex);
                station.bystanderProfiles[j] = bystanderProfile;
            }
        }
    }
    private void CreatePages()
    {
        List<Page> pageList = new List<Page>();
        pageList.Add(promptPage);
        promptPage.Init();
        pageList.Add(colorKeyPage);
        colorKeyPage.Init();

        for (int i = 0; i < trip.traitorProfiles.Length; i++)
        {
            TraitorProfile traitorProfile = trip.traitorProfiles[i];

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
        pageList.Add(confirmPage);
        confirmPage.Init();
        pages = pageList.ToArray();
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
