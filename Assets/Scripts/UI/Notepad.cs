using System;
using System.Collections.Generic;
using UnityEngine;
using static Atlas;
using static NPC;
using static AtlasUI;
public class Notepad : MonoBehaviour
{
    public const float WRITE_LETTER_TIME = 0.1f;
    
    const float LEFTHAND_DAMPING = 10f;
    const float PENCIL_DISTANCE_THRESHOLD = 0.05f;
    const float PENCIL_VERTICAL_FREQUENCY = 7f;
    const float PENCIL_VERTICAL_MAGNITUDE = 0.07f;

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
    public NotepadData notepadData;
    
    public AtlasRenderer rightHand_renderer;
    public AtlasRenderer frontFingers_renderer;
    public AtlasRenderer bindingRings_renderer;
    public AtlasRenderer leftHand_renderer;

    public Transform pageTransform;

    public TextAsset namesJSON;

    public Page traitorPage_prefab;
    public Page promptPage;
    public Page confirmPage;

    [Header("Generated")]
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
    
    public Bounds curWritingBounds;
    
    public int leftHandDepthFront;
    public int leftHandDepthBack;
    
    public int createPageIndex;

    public int previewAnswerIndex;

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

    private void Start()
    {
        nameData = JsonUtility.FromJson<NameData>(namesJSON.text);
        npcData.behaviourDescDict = InitializeEnumToStringDict<Behaviours>();
        AtlasUI.promptDict = InitializeEnumToStringDict<TripPrompt>();

        CreateNPCProfiles();

        notepadData.curState = NotepadState.None;

        
        handFlipPage_clip = leftHand_renderer.atlas.clipDict[(int)NotepadMotion.FlipHand];
        rotatePencil_clip = leftHand_renderer.atlas.clipDict[(int)NotepadMotion.RotatingPencil];

        leftHandDepthFront =  (int)(bindingRings_renderer.transform.position.z - 1);
        leftHandDepthBack = (int)(rightHand_renderer.transform.position.z + 1);

        Vector3 flipWorldPos = new Vector3(bindingRings_renderer.transform.position.x, bindingRings_renderer.transform.position.y, leftHand_renderer.transform.position.z);
        leftHandFlipPos = leftHand_renderer.transform.parent.InverseTransformPoint(flipWorldPos);
        leftHandPencilInactivePos = leftHand_renderer.transform.localPosition;
        leftHandTargetPos = leftHandPencilInactivePos;
        float distLeftHandToOffScreen = camStats.camBounds.min.y - (leftHand_renderer.bounds.max.y - leftHand_renderer.transform.position.y);
        Vector3 offscreenWorldPos = new Vector3(leftHand_renderer.transform.position.x, distLeftHandToOffScreen, leftHand_renderer.transform.position.z);
        leftHandOffScreenPos = leftHand_renderer.transform.parent.InverseTransformPoint(offscreenWorldPos);

        activePage = promptPage;
    }
    private void Update()
    {
        ChooseState();
        UpdateState();
    }
    private void ChooseState()
    {
        if ((activePage.pageType != PageType.Confirm && (playerInputs.notepadChoosePromptInputAndFlip.y == 1 || willFlipUp) && !writeToggle && !eraseToggle) || flipToggle == 1)
        {
            SetState(NotepadState.FlippingUp);
        }
        else if ((activePage.pageType != PageType.Prompt && (playerInputs.notepadChoosePromptInputAndFlip.y == -1 || willFlipDown) && !writeToggle && !eraseToggle) || flipToggle == -1)
        {
            SetState(NotepadState.FlippingDown);
        }
        else if (activePage.pageType != PageType.Prompt)
        {
            if ((playerInputs.notepadChoosePromptInputAndFlip.x != 0 && activePage.answerIndex != -1) || eraseToggle)
            {
                SetState(NotepadState.Erasing);
            }
            else if ((playerInputs.notepadConfirmStation && activePage.answerIndex == -1) || writeToggle)
            {
                SetState(NotepadState.Writing);
            }
            else
            {
                SetState(NotepadState.Stationary);
            }
        }
        else
        {
            SetState(NotepadState.Stationary);
        }
    }
    private void SetState(NotepadState newState)
    {
        if (notepadData.curState == newState) return;
        ExitState();
        notepadData.curState = newState;
        EnterState();
    }
    private void EnterState()
    {
        switch(notepadData.curState)
        {
            case NotepadState.FlippingUp:
            {
                Page traitorPageAhead = pages[activePageIndex + 1];
                traitorPageAhead.gameObject.SetActive(true);

                leftHand_renderer.UpdateDepthRealtime(leftHandDepthFront);
                leftHand_renderer.PlayClipOneShot(handFlipPage_clip);

                curKeyframeState = KeyframeState.Start;
                flipToggle = 1;

                leftHand_renderer.transform.localPosition = leftHandFlipPos;
                
                holdingPencil = false;
                willFlipUp = false;
            }
            break;
            case NotepadState.FlippingDown:
            {
                activePage.SetPageDepth(rightHand_renderer.transform.position.z - 1);
                activePageIndex--;
                activePage = pages[activePageIndex];
                activePage.gameObject.SetActive(true);
                leftHand_renderer.PlayClipOneShotReverse(handFlipPage_clip);

                curKeyframeState = KeyframeState.Start;
                
                flipToggle = -1;

                leftHand_renderer.transform.localPosition = leftHandFlipPos;
                leftHand_renderer.UpdateDepthRealtime(leftHandDepthBack);
                
                holdingPencil = false;
                willFlipDown = false;
            }
            break;
            case NotepadState.Writing:
            {
                leftHand_renderer.UpdateSpriteInputs(leftHand_renderer.atlas.motionSprites[rotatePencil_clip.keyframeStartIndex].sprite);

                curWritingBounds = activePage.GetWritingBounds();
                Vector3 startWriteWorldPos = new Vector3(curWritingBounds.min.x, curWritingBounds.center.y, leftHand_renderer.transform.position.z);
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
            case NotepadState.Erasing:
            {
                leftHand_renderer.PlayClipOneShot(rotatePencil_clip);

                curWritingBounds = activePage.GetWritingBounds();
                Vector3 startEraseWorldPos = new Vector3(curWritingBounds.max.x, curWritingBounds.center.y, leftHand_renderer.transform.position.z);
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
            case NotepadState.Stationary:
            {
                Debug.Log(activePage.answerText);
                if (activePage.answerText == "")
                {
                    switch (activePage.pageType)
                    { 
                        case PageType.Profile:
                        {
                            activePage.SetPreviewAnswerText(previewAnswerIndex);
                            appearTextClock = APPEAR_TEXT_TIME;
                        }
                        break;

                        case PageType.Confirm:
                        {
                            activePage.SetPreviewSignatureText();
                            appearTextClock = APPEAR_TEXT_TIME;
                        }
                        break;
                    }
                }
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
                leftHandTargetPos = leftHandFlipPos;
            }
            break;

            case NotepadState.FlippingDown:
            {
                leftHandTargetPos = leftHandFlipPos;
            }
            break;

            case NotepadState.Writing:
            {
                leftHandTargetPos = leftHandPencilInactivePos;
                
                if (playerInputs.notepadChoosePromptInputAndFlip.x != 0)
                {
                    previewAnswerIndex += (int)playerInputs.notepadChoosePromptInputAndFlip.x;
                    previewAnswerIndex = (previewAnswerIndex + trip.stationsDataArray.Length) % trip.stationsDataArray.Length;
                }

                atStartPencilPos = false;
                writeToggle = false;
            }
            break;
            case NotepadState.Erasing:
            {
                leftHandTargetPos = leftHandPencilInactivePos;

                activePage.answerIndex = -1;
                leftHand_renderer.PlayClipOneShotReverse(rotatePencil_clip);
                atStartPencilPos = false;
                eraseToggle = false;
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
                        activePage.SetPageDepth(leftHandDepthBack + 1);
                        leftHand_renderer.UpdateDepthRealtime(leftHandDepthBack);
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
                    activePage.SetPageDepth(leftHandDepthFront + 2);
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
                        activePage.SetPageDepth(leftHandDepthFront + 2);

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
                        switch(activePage.pageType)
                        {
                            case PageType.Profile:
                            {
                                activePage.WriteAnswerText(previewAnswerIndex);
                            }
                            break;
                            case PageType.Confirm:
                            {
                                activePage.WriteSignature();
                            }
                            break;
                        }
                        totalPencilTime = activePage.answerText.Length * WRITE_LETTER_TIME;
                        atStartPencilPos = true;
                    }
                }
                else
                {
                    curPencilTime += Time.deltaTime;
                    float t = curPencilTime / totalPencilTime;
                    float curPosX = Mathf.Lerp(curWritingBounds.min.x, curWritingBounds.max.x, t);
                    float randOffset = Mathf.PerlinNoise(curPencilTime * PENCIL_VERTICAL_FREQUENCY, curPencilTime * PENCIL_VERTICAL_FREQUENCY) * 2 - 1;
                    float curPosY = activePage.answerTextBounds.center.y + (randOffset * PENCIL_VERTICAL_MAGNITUDE);
                    leftHand_renderer.transform.position = new Vector3(curPosX, curPosY, leftHand_renderer.transform.position.z);

                    if (t > 1f) writeToggle = false;
                }

                if (activePageIndex < lastPageIndex && playerInputs.notepadChoosePromptInputAndFlip.y == 1)
                {
                    willFlipUp = true;
                }
                else if (activePageIndex > 0 && playerInputs.notepadChoosePromptInputAndFlip.y == -1)
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
                        activePage.EraseChosenStationText();
                        totalPencilTime = activePage.answerText.Length * WRITE_LETTER_TIME;

                        atStartPencilPos = true; 
                    }
                }
                else
                {
                    curPencilTime += Time.deltaTime;
                    float t = curPencilTime / totalPencilTime;
                    float curPosX = Mathf.Lerp(curWritingBounds.max.x, curWritingBounds.min.x, t);
                    float randOffset = Mathf.PerlinNoise(curPencilTime * PENCIL_VERTICAL_FREQUENCY, curPencilTime * PENCIL_VERTICAL_FREQUENCY) * 2 - 1;
                    float curPosY = activePage.answerTextBounds.center.y + (randOffset * PENCIL_VERTICAL_MAGNITUDE);
                    leftHand_renderer.transform.position = new Vector3(curPosX, curPosY, leftHand_renderer.transform.position.z);

                    if (activePage.activeAnswerTextRenderer.text.Length == 0) eraseToggle = false;
                }

                if (activePageIndex < lastPageIndex && playerInputs.notepadChoosePromptInputAndFlip.y == 1)
                {
                    willFlipUp = true;
                }
                else if (activePageIndex > 0 && playerInputs.notepadChoosePromptInputAndFlip.y == -1)
                {
                    willFlipDown = true;
                }
            }
            break;
            case NotepadState.Stationary:
            {
                if (playerInputs.notepadChoosePromptInputAndFlip.x != 0)
                {
                    previewAnswerIndex += (int)playerInputs.notepadChoosePromptInputAndFlip.x;
                    previewAnswerIndex = (previewAnswerIndex + trip.stationsDataArray.Length) % trip.stationsDataArray.Length;

                    activePage.SetPreviewAnswerText(previewAnswerIndex);
                    appearTextClock = APPEAR_TEXT_TIME;
                }
                activePage.UpdatePreviewAnswerText(appear: true, ref appearTextClock);
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
        pageList.Add(promptPage);
        promptPage.Init();

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

                Vector3 pagePos = pageTransform.position;
                pagePos.z += 3;
                Page traitorPage = Instantiate(traitorPage_prefab, pagePos, Quaternion.identity, pageTransform);
                traitorPage.InitBehaviourClueText(traitorProfile);
                traitorPage.gameObject.name = "Page_" + createPageIndex;
                traitorPage.pageNumberRenderer.SetText("Page " + (createPageIndex + 1));

                pageList.Add(traitorPage);
                traitorPage.gameObject.SetActive(false);
                createPageIndex++;
            }
        }

        pageList.Add(confirmPage);
        confirmPage.Init();
        pages = pageList.ToArray();
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
    public Dictionary<TEnum, string> InitializeEnumToStringDict<TEnum>() where TEnum : Enum
    {
        Dictionary<TEnum, string> dict = new Dictionary<TEnum, string>();

        Array values = Enum.GetValues(typeof(TEnum));

        foreach (TEnum value in values)
        {
            if (Convert.ToInt32(value) == 0) continue;

            dict.Add(value, value.ToString().Replace("_", " "));
        }
        return dict;
    }
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.InverseTransformPoint(leftHandTargetPos), 0.1f);
    }
}
