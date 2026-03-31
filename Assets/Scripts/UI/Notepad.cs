using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static Atlas;
using static NPC;

public class Notepad : MonoBehaviour
{
    public enum State
    {
        None,
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

    public AtlasUISimpleRenderer rightHand_renderer;
    public AtlasUISimpleRenderer frontFingers_renderer;
    public AtlasUISimpleRenderer bindingRings_renderer;
    
    public AtlasUIMotionRenderer leftHand_renderer;

    public Page page_prefab;
    
    [Header("Generated")]

    public Page activePage;
    public Page[] pages;
    public int activePageIndex;
    public int lastPageIndex;
    public Bounds totalBounds;
    
    public AtlasClip leftHandClip;
    
    public State curState;
    public KeyframeState curKeyframeState;
    
    public int lastLeftHandKeyframeIndex;
    public int leftHandDepthFront;
    public int leftHandDepthBack;
    public int frontPages;
    public int backPages;
    
    public static NameData nameData;
    private void OnValidate()
    {
        SetTotalBounds();
#if UNITY_EDITOR
        EditorUtility.SetDirty(this);
#endif
    }
    private void Awake()
    {
        nameData = JsonUtility.FromJson<NameData>(namesJSON.text);
        npcData.appearanceDescDict = InitializeAppearanceDict();
        npcData.behaviourDescDict = InitializeBehaviourDict();
        CreateNPCProfiles();

    }
    private void Start()
    {
        leftHandClip = leftHand_renderer.renderInput.atlas.clipDict[(int)NotepadMotion.LeftHand];
        lastLeftHandKeyframeIndex = leftHandClip.keyFrames.Length - 1;

        leftHandDepthFront =  (int)(bindingRings_renderer.transform.position.z - 1);
        leftHandDepthBack = (int)(rightHand_renderer.transform.position.z + 1);


    }
    private void Update()
    {
        if (activePageIndex < lastPageIndex && playerInputs.notepad.y == 1 && curState != State.FlippingUp)
        {
            curState = State.FlippingUp;
            if (curKeyframeState == KeyframeState.Start) return;
            
            pages[activePageIndex + 1].gameObject.SetActive(true);

            leftHand_renderer.renderInput.UpdateDepthRealtime(leftHandDepthFront);
            leftHand_renderer.PlayClipOneShot(leftHandClip);
            
            curKeyframeState = KeyframeState.Start;
        }
        else if (activePageIndex > 0 && playerInputs.notepad.y == -1 && curState != State.FlippingDown)
        {
            curState = State.FlippingDown;
            if (curKeyframeState == KeyframeState.Start) return;

            leftHand_renderer.renderInput.UpdateDepthRealtime(leftHandDepthBack);

            activePage.UpdatePageDepth((int)(rightHand_renderer.transform.position.z - 1));
            activePageIndex--;
            activePage = pages[activePageIndex];
            activePage.gameObject.SetActive(true);
            leftHand_renderer.PlayClipOneShotReverse(leftHandClip);
            
            curKeyframeState = KeyframeState.Start;
        }

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
                        if(curKeyframeState == KeyframeState.TogglePageContentsBottomHalf) return;
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
                        leftHand_renderer.renderInput.UpdateDepthRealtime(leftHandDepthBack);
                        activePage.UpdatePageDepth(leftHandDepthBack + 1);
                        curKeyframeState = KeyframeState.ChangeDepth;
                    }
                    break;
                }

                if (leftHand_renderer.curFrameIndex == lastLeftHandKeyframeIndex)
                {
                    if (curKeyframeState == KeyframeState.None) return;
                    curState = State.None;
                    activePage.gameObject.SetActive(false);
                    activePageIndex++;
                    activePage = pages[activePageIndex];
                    activePage.UpdatePageDepth(leftHandDepthFront + 2);
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
                        curState = State.None;
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
                        curKeyframeState= KeyframeState.TogglePageContentsTopHalf;
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
                        leftHand_renderer.renderInput.UpdateDepthRealtime(leftHandDepthFront);
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
        
        for (int i = 0; i < trip.stationsDataArray.Length; i++)
        {
            StationSO station = trip.stationsDataArray[i];
            station.bystanderProfiles.Clear();
            station.traitorProfiles.Clear();
        }

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
                    Appearance appearence = npcPrefab.npc.appearence;
                    string name = GenerateName(npcPrefab.npc.gender, npcPrefab.npc.ethnicity);
                    NPCProfile npcProfile = new NPCProfile
                    {
                        fullName = name,
                        behaviours = twoBehaviours,
                        appearence = appearence,
                        npcPrefabIndex = npcPrefab.npc.mugshotIndex,
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

            for (int j = 0; j < trip.stationsDataArray[i].traitorSpawnAmount; j++)
            {
                int randProfileIndex = UnityEngine.Random.Range(0, totalNPCProfiles.Count);

                NPCProfile traitorProfile = totalNPCProfiles[randProfileIndex];
                traitorProfile.startStationIndex = i;
                traitorProfile.departureStationIndex = GetExitStationIndex(i, trip.minStationsTraitorsTravel, trip.maxStationsTraitorsTravel);
                totalNPCProfiles.RemoveAt(randProfileIndex);
                station.traitorProfiles.Add(traitorProfile);

                Vector3 pagePos = bindingRings_renderer.transform.position + Vector3.forward;
                if (frontPages != 0) pagePos.z += 2;
                Page page = Instantiate(page_prefab, pagePos, Quaternion.identity, transform);
                page.Init(traitorProfile);
                page.gameObject.name = "Page_" + frontPages;
                pageList.Add(page);
                if (frontPages != 0) page.gameObject.SetActive(false);
                frontPages++;
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

            for (int j = 0; j < bystandersToSpawnAtStation; j++)
            {
                if (totalNPCProfiles.Count == 0) { Debug.LogWarning("the total of the station busynessFactor exceeds 1. Total should be between 0 - 1"); return; }
                int randProfileIndex = UnityEngine.Random.Range(0, totalNPCProfiles.Count);

                NPCProfile bystanderProfile = totalNPCProfiles[randProfileIndex];

                bystanderProfile.startStationIndex = i;
                bystanderProfile.departureStationIndex = GetExitStationIndex(i, 0, int.MaxValue);

                totalNPCProfiles.RemoveAt(randProfileIndex);
                station.bystanderProfiles.Add(bystanderProfile);
            }
        }
    }
    private void SetTotalBounds()
    {
        if (rightHand_renderer == null || frontFingers_renderer == null || bindingRings_renderer == null) return;
        totalBounds = rightHand_renderer.renderInput.bounds;
        totalBounds.Encapsulate(frontFingers_renderer.renderInput.bounds);
        totalBounds.Encapsulate(bindingRings_renderer.renderInput.bounds);
        totalBounds.Encapsulate(leftHand_renderer.renderInput.bounds);
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
    public Dictionary<Appearance, string> InitializeAppearanceDict()
    {
        var dict = new Dictionary<Appearance, string>();

        Array values = Enum.GetValues(typeof(Appearance));

        foreach (Appearance value in values)
        {
            if (value == Appearance.None) continue;

            dict[value] = value.ToString().Replace("_", " ");
        }
        return dict;
    }
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.indigo;
        Gizmos.DrawWireCube(totalBounds.center, totalBounds.size);
    }
}
