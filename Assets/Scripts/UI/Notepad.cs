using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static Atlas;
using static NPC;

public class Notepad : MonoBehaviour
{
    const float PAGE_SPAWN_POS_Y_OFFSET = 0.2f;
    const float WRITE_LETTER_TIME = 0.2f;
    public enum State
    {
        Stationary,
        Writing,
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
    public TextAsset namesJSON;

    public AtlasRenderer rightHand_renderer;
    public AtlasRenderer frontFingers_renderer;
    public AtlasRenderer bindingRings_renderer;
    
    public AtlasRenderer leftHand_renderer;

    public Page page_prefab;
    
    [Header("Generated")]

    public Page activePage;
    public Page[] pages;
    public int activePageIndex;
    public int lastPageIndex;
    public Bounds totalBounds;
    
    public AtlasClip handFlipPage_clip;
    public AtlasClip rotatePencil_clip;
    
    public State curState;
    public KeyframeState curKeyframeState;
    
    public int lastLeftHandKeyframeIndex;
    public int leftHandDepthFront;
    public int leftHandDepthBack;
    public int pageIndex;
    public int backPages;

    public int flipToggle;
    public bool writeToggle;
    public static NameData nameData;

    public Vector2 boundsOffset;

    private void OnValidate()
    {
        SetTotalBounds();
#if UNITY_EDITOR
        EditorUtility.SetDirty(this);
#endif
    }
    private void Awake()
    {
        SetTotalBounds();
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


    }
    private void Update()
    {
        ChooseState();
        UpdateState();
    }
    private void ChooseState()
    {
        if ((activePageIndex < lastPageIndex && playerInputs.notepadChooseStationAndFlip.y == 1) || flipToggle == 1)
        {
            SetState(State.FlippingUp);
        }
        else if ((activePageIndex > 0 && playerInputs.notepadChooseStationAndFlip.y == -1) || flipToggle == -1)
        {
            SetState(State.FlippingDown);
        }
        else if ((playerInputs.notepadChooseStationAndFlip.x != 0) || writeToggle)
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
                pages[activePageIndex + 1].gameObject.SetActive(true);

                leftHand_renderer.UpdateDepthRealtime(leftHandDepthFront);
                leftHand_renderer.PlayClipOneShot(handFlipPage_clip);

                curKeyframeState = KeyframeState.Start;
                flipToggle = 1;
            }
            break;
            case State.FlippingDown:
            {
                leftHand_renderer.UpdateDepthRealtime(leftHandDepthBack);

                activePage.UpdatePageDepth((int)(rightHand_renderer.transform.position.z - 1));
                activePageIndex--;
                activePage = pages[activePageIndex];
                activePage.gameObject.SetActive(true);
                leftHand_renderer.PlayClipOneShotReverse(handFlipPage_clip);

                curKeyframeState = KeyframeState.Start;
                flipToggle = -1;
            }
            break;
            case State.Writing:
            {
                writeToggle = true;

                if (activePage)
                leftHand_renderer.PlayClipOneShot(rotatePencil_clip);
                activePage.SetDisembarkingStationText(0); // TODO actual chosen station index
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
                
            }
            break;

            case State.FlippingDown:
            {
                flipToggle = 0;
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
                    curKeyframeState = KeyframeState.None;

                    flipToggle = 0;
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
                        curKeyframeState = KeyframeState.None;
                        flipToggle = 0;
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
        }
    }
    private void CreateNPCProfiles()
    {
        List<NPCProfile> totalNPCProfiles = new List<NPCProfile>();
        List<NPCProfile> duplicateBehaviourProfiles = new List<NPCProfile>();

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
                        duplicateBehaviourProfiles.Add(npcProfile);
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
                traitorProfile.disembarkingStationIndex = GetExitStationIndex(i, trip.minStationsTraitorsTravel, trip.maxStationsTraitorsTravel);
                totalNPCProfiles.RemoveAt(randProfileIndex);
                station.traitorProfiles[j] = traitorProfile;

                Vector3 pagePos = bindingRings_renderer.transform.position + Vector3.forward;
                if (pageIndex != 0) pagePos.z += 2;
                pagePos.y += PAGE_SPAWN_POS_Y_OFFSET;
                Page page = Instantiate(page_prefab, pagePos, Quaternion.identity, transform);
                page.Init(traitorProfile);
                page.gameObject.name = "Page_" + pageIndex;
                pageList.Add(page);
                if (pageIndex != 0) page.gameObject.SetActive(false);
                pageIndex++;
            }
        }

        pages = pageList.ToArray();
        activePageIndex = 0;
        activePage = pages[activePageIndex];
        lastPageIndex = pages.Length - 1;

        totalNPCProfiles.AddRange(duplicateBehaviourProfiles);

        for (int i = 0; i < trip.stationsDataArray.Length; i++)
        {
            StationSO station = trip.stationsDataArray[i];
            int bystandersToSpawnAtStation = (int)(station.busynessFactor * (float)totalNPCProfiles.Count);
            station.bystanderProfiles = new NPCProfile[bystandersToSpawnAtStation];

            for (int j = 0; j < bystandersToSpawnAtStation; j++)
            {
                if (totalNPCProfiles.Count == 0) { Debug.LogWarning("the total of the station busynessFactor exceeds 1. Total should be between 0 - 1"); return; }
                int randProfileIndex = UnityEngine.Random.Range(0, totalNPCProfiles.Count);

                NPCProfile bystanderProfile = totalNPCProfiles[randProfileIndex];

                bystanderProfile.boardingStationIndex = i;
                bystanderProfile.disembarkingStationIndex = GetExitStationIndex(i, 1, int.MaxValue);

                totalNPCProfiles.RemoveAt(randProfileIndex);
                station.bystanderProfiles[j] = bystanderProfile;
            }
        }
    }
    private void SetTotalBounds()
    {
        if (rightHand_renderer == null || frontFingers_renderer == null || bindingRings_renderer == null) return;
        totalBounds = rightHand_renderer.bounds;
        totalBounds.Encapsulate(frontFingers_renderer.bounds);
        totalBounds.Encapsulate(bindingRings_renderer.bounds);
        totalBounds.Encapsulate(leftHand_renderer.bounds);

        boundsOffset = transform.position - totalBounds.min;
    }
    public string GenerateName(Gender gender, Ethnicity ethnicity)
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
    public int GetExitStationIndex(int startStationIndex, int minStationIndexOffset, int maxStationIndexOffset)
    {
        int maxPossibleIndex = trip.stationsDataArray.Length - 1;
        int max = Mathf.Min(startStationIndex + maxStationIndexOffset, maxPossibleIndex);
        int min = Mathf.Min(startStationIndex + minStationIndexOffset, maxPossibleIndex);
        return UnityEngine.Random.Range(min, max);
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
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireCube(totalBounds.center, totalBounds.size);
    }
}
