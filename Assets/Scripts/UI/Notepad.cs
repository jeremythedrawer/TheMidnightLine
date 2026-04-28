using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static Atlas;
using static NPC;

public class Notepad : MonoBehaviour
{
    public const float WRITE_LETTER_TIME = 0.1f;
    const float PAGE_SPAWN_POS_Y_OFFSET = 0.2f;
    const float LEFTHAND_DAMPING = 10f;
    const float PENCIL_DISTANCE_THRESHOLD = 0.1f;
    const float PENCIL_VERTICAL_FREQUENCY = 7f;
    const float PENCIL_VERTICAL_MAGNITUDE = 0.16f;
    public enum State
    {
        None,
        Stationary,
        Writing,
        Erasing,
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

    public PlayerInputsSO playerInputs;
    public TripSO trip;
    public NPCsDataSO npcData;
    public CameraStatsSO camStats;

    public AtlasRenderer rightHand_renderer;
    public AtlasRenderer frontFingers_renderer;
    public AtlasRenderer bindingRings_renderer;
    public AtlasRenderer leftHand_renderer;

    public TextAsset namesJSON;

    public Page page_prefab;
    
    [Header("Generated")]
    public State curState;
    public KeyframeState curKeyframeState;
    
    public Page activePage;
    public Page[] pages;
    
    public NameData nameData;
    
    public int activePageIndex;
    public int lastPageIndex;

    
    public AtlasClip handFlipPage_clip;
    public AtlasClip rotatePencil_clip;

    public Vector3 leftHandFlipPos;
    public Vector3 leftHandPencilPos;
    public Vector3 leftHandPencilInactivePos;
    public Vector3 leftHandTargetPos;
    public Vector3 leftHandOffScreenPos;
    
    public Bounds curStationNameBounds;
    
    public int lastLeftHandKeyframeIndex;
    public int leftHandDepthFront;
    public int leftHandDepthBack;
    
    public int createPageIndex;

    public int previewStationIndex;

    public int flipToggle;
    public bool writeToggle;
    public bool eraseToggle;
    public bool willFlipUp;
    public bool willFlipDown;
    public bool atStartPencilPos;
    public bool holdingPencil;
    public float totalPencilTime;
    public float curPencilTime;
    public float appearTextClock;
    private void Awake()
    {
        nameData = JsonUtility.FromJson<NameData>(namesJSON.text);
        npcData.behaviourDescDict = InitializeBehaviourDict();
        CreateNPCProfiles();

    }
    private void Start()
    {
        handFlipPage_clip = leftHand_renderer.atlas.clipDict[(int)NotepadMotion.FlipHand];
        rotatePencil_clip = leftHand_renderer.atlas.clipDict[(int)NotepadMotion.RotatingPencil];

        lastLeftHandKeyframeIndex = handFlipPage_clip.keyFrames.Length - 1;
        leftHandDepthFront =  (int)(bindingRings_renderer.transform.position.z - 1);
        leftHandDepthBack = (int)(rightHand_renderer.transform.position.z + 1);

        Vector3 flipWorldPos = new Vector3(bindingRings_renderer.transform.position.x, bindingRings_renderer.transform.position.y, leftHand_renderer.transform.position.z);
        leftHandFlipPos = leftHand_renderer.transform.parent.InverseTransformPoint(flipWorldPos);
        leftHandPencilInactivePos = leftHand_renderer.transform.localPosition;
        leftHandTargetPos = leftHandPencilInactivePos;
        float distLeftHandToOffScreen = camStats.camWorldBottom - (leftHand_renderer.bounds.max.y - leftHand_renderer.transform.position.y);
        Vector3 offscreenWorldPos = new Vector3(leftHand_renderer.transform.position.x, distLeftHandToOffScreen, leftHand_renderer.transform.position.z);
        leftHandOffScreenPos = leftHand_renderer.transform.parent.InverseTransformPoint(offscreenWorldPos);

        pages[0].HideLeftArrowButton();
        pages[pages.Length - 1].HideRightArrowButton();
    }
    private void Update()
    {
        ChooseState();
        UpdateState();
    }
    private void ChooseState()
    {
        if ((activePageIndex < lastPageIndex && (playerInputs.notepadChooseStationAndFlip.y == 1 || willFlipUp) && !writeToggle && !eraseToggle) || flipToggle == 1)
        {
            SetState(State.FlippingUp);
        }
        else if ((activePageIndex > 0 && (playerInputs.notepadChooseStationAndFlip.y == -1 || willFlipDown) && !writeToggle && !eraseToggle) || flipToggle == -1)
        {
            SetState(State.FlippingDown);
        }
        else if ((playerInputs.notepadChooseStationAndFlip.x != 0 && activePage.chosenStationIndex != -1) || eraseToggle)
        {
            SetState(State.Erasing);
        }
        else if ((playerInputs.notepadConfirmStation && activePage.chosenStationIndex == -1) || writeToggle)
        {
            SetState(State.Writing);
        }
        else
        {
            SetState(State.Stationary);
        }
    }
    private void SetState(State newState)
    {
        if (curState == newState) return;
        ExitState();
        curState = newState;
        EnterState();
    }
    private void EnterState()
    {
        switch(curState)
        {
            case State.FlippingUp:
            {
                Page pageAhead = pages[activePageIndex + 1];
                pageAhead.gameObject.SetActive(true);
                if (pageAhead.chosenStationIndex == -1)
                {
                    pageAhead.chosenStation_renderer.enabled = false;
                }

                leftHand_renderer.UpdateDepthRealtime(leftHandDepthFront);
                leftHand_renderer.PlayClipOneShot(handFlipPage_clip);

                curKeyframeState = KeyframeState.Start;
                flipToggle = 1;

                leftHand_renderer.transform.localPosition = leftHandFlipPos;
                
                holdingPencil = false;
                willFlipUp = false;
            }
            break;
            case State.FlippingDown:
            {
                activePage.UpdatePageDepth((int)(rightHand_renderer.transform.position.z - 1));
                activePageIndex--;
                activePage = pages[activePageIndex];
                activePage.gameObject.SetActive(true);
                if (activePage.chosenStationIndex == -1)
                {
                    activePage.chosenStation_renderer.enabled = false;
                }

                leftHand_renderer.PlayClipOneShotReverse(handFlipPage_clip);

                curKeyframeState = KeyframeState.Start;
                
                flipToggle = -1;

                leftHand_renderer.transform.localPosition = leftHandFlipPos;
                leftHand_renderer.UpdateDepthRealtime(leftHandDepthBack);
                
                holdingPencil = false;
                willFlipDown = false;
            }
            break;
            case State.Writing:
            {
                leftHand_renderer.UpdateSpriteInputs(ref rotatePencil_clip.keyFrames[0].motionSprite.sprite);
                
                curStationNameBounds = activePage.GetStationNameBounds();

                Vector3 startWriteWorldPos = new Vector3(curStationNameBounds.min.x, curStationNameBounds.center.y, leftHand_renderer.transform.position.z);
                leftHandPencilPos = leftHand_renderer.transform.parent.InverseTransformPoint(startWriteWorldPos);

                if (holdingPencil)
                {
                    leftHand_renderer.transform.localPosition = leftHandPencilInactivePos;
                }
                else
                {
                    leftHand_renderer.transform.localPosition = leftHandOffScreenPos;
                    holdingPencil = true;
                }
                
                curPencilTime = 0;
                writeToggle = true;
            }
            break;
            case State.Erasing:
            {
                leftHand_renderer.PlayClipOneShot(rotatePencil_clip);
                
                curStationNameBounds = activePage.GetStationNameBounds();

                Vector3 startEraseWorldPos = new Vector3(curStationNameBounds.max.x, curStationNameBounds.center.y, leftHand_renderer.transform.position.z);
                leftHandPencilPos = leftHand_renderer.transform.parent.InverseTransformPoint(startEraseWorldPos);

                if (holdingPencil)
                {
                    leftHand_renderer.transform.localPosition = leftHandPencilInactivePos;
                }
                else
                {
                    leftHand_renderer.transform.localPosition = leftHandOffScreenPos;
                    holdingPencil = true;
                }
                
                curPencilTime = 0;
                eraseToggle = true;
            }
            break;
            case State.Stationary:
            {
                if (activePage.chosenStationIndex == -1)
                {
                    activePage.SetPreviewStationText(previewStationIndex);
                    appearTextClock = AtlasRendering.APPEAR_TEXT_TIME;
                }
            }
            break;
        }
    }
    private void ExitState()
    {
        switch (curState)
        {
            case State.FlippingUp:
            {
                leftHandTargetPos = leftHandFlipPos;
            }
            break;

            case State.FlippingDown:
            {
                leftHandTargetPos = leftHandFlipPos;
            }
            break;

            case State.Writing:
            {
                leftHandTargetPos = leftHandPencilInactivePos;
                
                if (playerInputs.notepadChooseStationAndFlip.x != 0)
                {
                    previewStationIndex += (int)playerInputs.notepadChooseStationAndFlip.x;
                    previewStationIndex = (previewStationIndex + trip.stationsDataArray.Length) % trip.stationsDataArray.Length;
                }

                atStartPencilPos = false;
                writeToggle = false;
            }
            break;
            case State.Erasing:
            {
                leftHandTargetPos = leftHandPencilInactivePos;

                activePage.chosenStationIndex = -1;
                leftHand_renderer.PlayClipOneShotReverse(rotatePencil_clip);
                atStartPencilPos = false;
                eraseToggle = false;
            }
            break;
        }

    }
    private void UpdateState()
    {
        switch (curState)
        {
            case State.FlippingUp:
            {
                switch (leftHand_renderer.curFrameIndex)
                {
                    case 2:
                    {
                        if (curKeyframeState == KeyframeState.PaperClip) return;
   
                        activePage.PlayPaperClip();
                        curKeyframeState = KeyframeState.PaperClip;
                    }
                    break;

                    case 3:
                    {
                        if (curKeyframeState == KeyframeState.TogglePageContentsBottomHalf) return;

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

                        leftHand_renderer.UpdateDepthRealtime(leftHandDepthBack);
                        activePage.UpdatePageDepth(leftHandDepthBack + 1);
                        curKeyframeState = KeyframeState.ChangeDepth;

                    }
                    break;
                }

                if (leftHand_renderer.curFrameIndex == lastLeftHandKeyframeIndex)
                {
                    if (curKeyframeState == KeyframeState.None) return;

                    activePage.gameObject.SetActive(false);
                    activePageIndex++;
                    activePage = pages[activePageIndex];
                    activePage.UpdatePageDepth(leftHandDepthFront + 2);
                    flipToggle = 0;

                    curKeyframeState = KeyframeState.None;

                }
            }
            break;
            case State.FlippingDown:
            {
                switch (leftHand_renderer.curFrameIndex)
                {
                    case 0:
                    {
                        if (curKeyframeState == KeyframeState.None) return;

                        pages[activePageIndex + 1].gameObject.SetActive(false);
                        flipToggle = 0;

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

                        curKeyframeState = KeyframeState.TogglePageContentsTopHalf;
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

                        leftHand_renderer.UpdateDepthRealtime(leftHandDepthFront);
                        activePage.UpdatePageDepth(leftHandDepthFront + 2);

                        curKeyframeState = KeyframeState.ChangeDepth;
                    }
                    break;
                }
            }
            break;
            case State.Writing:
            {
                if (!atStartPencilPos)
                {
                    leftHand_renderer.transform.localPosition = Vector3.Lerp(leftHand_renderer.transform.localPosition, leftHandPencilPos, Time.deltaTime * LEFTHAND_DAMPING);

                    float dist = (leftHand_renderer.transform.localPosition - leftHandPencilPos).sqrMagnitude;
                    if (dist < PENCIL_DISTANCE_THRESHOLD * PENCIL_DISTANCE_THRESHOLD)
                    {
                        activePage.WriteChosenStationText(previewStationIndex);
                        totalPencilTime = activePage.chosenStationName.Length * WRITE_LETTER_TIME;

                        atStartPencilPos = true;
                    }
                }
                else
                {
                    curPencilTime += Time.deltaTime;
                    float t = curPencilTime / totalPencilTime;
                    float curPosX = Mathf.Lerp(curStationNameBounds.min.x, curStationNameBounds.max.x, t);
                    float randOffset = Mathf.PerlinNoise(curPencilTime * PENCIL_VERTICAL_FREQUENCY, curPencilTime * PENCIL_VERTICAL_FREQUENCY) * 2 - 1;
                    float curPosY = activePage.stationNameBounds.center.y + (randOffset * PENCIL_VERTICAL_MAGNITUDE);
                    leftHand_renderer.transform.position = new Vector3(curPosX, curPosY, leftHand_renderer.transform.position.z);

                    if (t > 1f) writeToggle = false;
                }

                if (activePageIndex < lastPageIndex && playerInputs.notepadChooseStationAndFlip.y == 1)
                {
                    willFlipUp = true;
                }
                else if (activePageIndex > 0 && playerInputs.notepadChooseStationAndFlip.y == -1)
                {
                    willFlipDown = true;
                }
            }
            break;
            case State.Erasing:
            {
                if(!atStartPencilPos)
                {
                    leftHand_renderer.transform.localPosition = Vector3.Lerp(leftHand_renderer.transform.localPosition, leftHandPencilPos, Time.deltaTime * LEFTHAND_DAMPING);

                    float dist = (leftHand_renderer.transform.localPosition - leftHandPencilPos).sqrMagnitude;
                    if (dist < PENCIL_DISTANCE_THRESHOLD * PENCIL_DISTANCE_THRESHOLD)
                    {
                        activePage.EraseChosenStationText();
                        totalPencilTime = activePage.chosenStationName.Length * WRITE_LETTER_TIME;

                        atStartPencilPos = true; 
                    }
                }
                else
                {
                    curPencilTime += Time.deltaTime;
                    float t = curPencilTime / totalPencilTime;
                    float curPosX = Mathf.Lerp(curStationNameBounds.max.x, curStationNameBounds.min.x, t);
                    float randOffset = Mathf.PerlinNoise(curPencilTime * PENCIL_VERTICAL_FREQUENCY, curPencilTime * PENCIL_VERTICAL_FREQUENCY) * 2 - 1;
                    float curPosY = activePage.stationNameBounds.center.y + (randOffset * PENCIL_VERTICAL_MAGNITUDE);
                    leftHand_renderer.transform.position = new Vector3(curPosX, curPosY, leftHand_renderer.transform.position.z);

                    if (activePage.chosenStation_renderer.text.Length == 0) eraseToggle = false;
                }

                if (activePageIndex < lastPageIndex && playerInputs.notepadChooseStationAndFlip.y == 1)
                {
                    willFlipUp = true;
                }
                else if (activePageIndex > 0 && playerInputs.notepadChooseStationAndFlip.y == -1)
                {
                    willFlipDown = true;
                }
            }
            break;
            case State.Stationary:
            {
                if (playerInputs.notepadChooseStationAndFlip.x != 0)
                {
                    previewStationIndex += (int)playerInputs.notepadChooseStationAndFlip.x;
                    previewStationIndex = (previewStationIndex + trip.stationsDataArray.Length) % trip.stationsDataArray.Length;

                    activePage.SetPreviewStationText(previewStationIndex);
                    appearTextClock = AtlasRendering.APPEAR_TEXT_TIME;
                }
                activePage.UpdatePreviewStationText(appear: true, ref appearTextClock);
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

                if (activePageIndex > 0 && CursorController.IsInsideBounds(activePage.paperCornerLeft_renderer.bounds))
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

                if (activePageIndex < lastPageIndex && CursorController.IsInsideBounds(activePage.paperCornerRight_renderer.bounds))
                {
                    if (playerInputs.mouseLeftDown)
                    {
                        activePage.InverRightArrowButton(false);
                        flipToggle = 1;
                    }
                    else
                    {
                        activePage.InverRightArrowButton(true);
                    }
                }
                else
                {
                    activePage.InverRightArrowButton(false);
                }

                leftHand_renderer.transform.localPosition = Vector3.Lerp(leftHand_renderer.transform.localPosition, leftHandTargetPos, Time.deltaTime * LEFTHAND_DAMPING);
            }
            break;
        }
    }
    private void CreateNPCProfiles()
    {
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
            for (int j = 0; j < flagCount; j++)
            {
                Behaviours firstBehaviour = (Behaviours)validFlags[j];

                for (int k = j; k < flagCount; k++)
                {
                    Behaviours secondBehaviour = (Behaviours)validFlags[k];
                    Behaviours twoBehaviours = firstBehaviour | secondBehaviour;
                    string name = GenerateName(npcPrefab.npc.gender, npcPrefab.npc.ethnicity);

                    NPCProfile npcProfile = new NPCProfile
                    {
                        fullName = name,
                        behaviours = twoBehaviours,
                        npcPrefabIndex = i,
                        coveredMugshotIndex = npcPrefab.npc.coveredMugshotIndex,
                        uncoveredMugshotIndex = npcPrefab.npc.uncoveredMugshotIndex,
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

        List<Page> pageList = new List<Page>();
        
        for (int i = 0; i < trip.stationsDataArray.Length; i++)
        {
            StationSO station = trip.stationsDataArray[i];

            station.traitorProfiles = new NPCProfile[station.traitorSpawnAmount];

            for (int j = 0; j < station.traitorSpawnAmount; j++)
            {
                int randProfileIndex = UnityEngine.Random.Range(0, totalNPCProfiles.Count);

                NPCProfile traitorProfile = totalNPCProfiles[randProfileIndex];
                traitorProfile.boardingStationIndex = i;
                traitorProfile.disembarkingStationIndex = UnityEngine.Random.Range(i + trip.minStationsTraitorsTravel, trip.stationsDataArray.Length - 1);
                station.traitorProfiles[j] = traitorProfile;

                totalNPCProfiles.Remove(traitorProfile);
                for (int k = totalNPCProfiles.Count - 1; k >=0 ; k--)
                {
                    if (totalNPCProfiles[k].npcPrefabIndex == traitorProfile.npcPrefabIndex)
                    {
                        bystanderProfiles.Add(totalNPCProfiles[k]);
                        totalNPCProfiles.Remove(totalNPCProfiles[k]);
                    }
                }

                Vector3 pagePos = bindingRings_renderer.transform.position + Vector3.forward;
                if (createPageIndex != 0) pagePos.z += 2;
                pagePos.y += PAGE_SPAWN_POS_Y_OFFSET;
                Page page = Instantiate(page_prefab, pagePos, Quaternion.identity, transform);
                page.Init(traitorProfile);
                page.gameObject.name = "Page_" + createPageIndex;
                page.pageNumber_renderer.SetText("Page " + (createPageIndex + 1));
                pageList.Add(page);
                if (createPageIndex != 0) page.gameObject.SetActive(false);
                createPageIndex++;
            }
        }

        pages = pageList.ToArray();
        activePageIndex = 0;
        activePage = pages[activePageIndex];
        lastPageIndex = pages.Length - 1;

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
                bystanderProfile.disembarkingStationIndex = UnityEngine.Random.Range(i + 1, trip.stationsDataArray.Length - 1);

                totalNPCProfiles.RemoveAt(randProfileIndex);
                station.bystanderProfiles[j] = bystanderProfile;
            }
        }
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
    public Dictionary<Behaviours, string> InitializeBehaviourDict()
    {
        var dict = new Dictionary<Behaviours, string>();

        Array values = Enum.GetValues(typeof(Behaviours));

        foreach (Behaviours value in values)
        {
            if (value == Behaviours.None) continue;

            dict[value] = value.ToString().Replace("_", " ");
        }
        return dict;
    }
}
