using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem.LowLevel;
using static Atlas;
using static AtlasUI;
using static Passenger;
public class Notepad : MonoBehaviour
{
    public const int MIN_STATION_STOPS = 1;

    public static Vector3 ACTIVE_POS = new Vector3(3.57998657f, 1, 5);
    
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
        WillFlipUp = 1 << 4,
        WillFlipDown = 1 << 5,
        CanFlipUp = 1 << 6,
        CanFlipDown = 1 << 7,
        CanWillFlipUp = 1 << 8,
        CanWillFlipDown = 1 << 9,
        InUse = 1 << 10,
    }

    public InputData playerInputs;
    public TripData curTrip;
    public PassengerData npcData;
    public CameraData camStats;
    public SpyData spyStats;
    public Options colorsData;

    public NotepadData notepadData;
    
    public LeftHand leftHand;
    
    public AtlasRenderer rightHand_renderer;
    public AtlasRenderer frontFingers_renderer;
    public AtlasRenderer bindingRingsRend;

    public TextAsset namesJSON;

    public Page traitorPagePrefab;
    public Page promptPage;
    public Page colorKeyPage;

    [Header("Generated")]

    public Page[] pages;
    
    public Page activePage;
    public Page nextPage;

    public ColorPicker clueColorPicker;

    public TraitorProfile activeTraitorProfile;
    
    public NameData nameData;

    public KeyframeState curKeyframeState;

    public int activePageIndex;
    public int lastPageIndex;    
    public int traitorOutcomesRevealed;

    private void Start()
    {
        Init();
    }
    private void OnEnable()
    {
        Scenes.OnLoadTrip1 += Reinit;
        Scenes.OnLoadStart += Init;

        TrainController.OnFinishTripScene += KeepNotepad;
    }
    private void OnDisable()
    {
        Scenes.OnLoadTrip1 -= Reinit;
        Scenes.OnLoadStart -= Init;

        TrainController.OnFinishTripScene -= KeepNotepad;
    }
    private void Update()
    {
        activePage.exitButton.UpdateButton();

        if ((notepadData.subState & SubState.InUse) != 0)
        {
            UpdateState();
            ChooseState();
        }
    }
    public void Init()
    {
        SceneController.KeepNotepad(this);
        colorKeyPage.gameObject.SetActive(false);
        gameObject.SetActive(false);
        CreateNPCProfiles();
    }
    public void PickUpNotepad() 
    {
        gameObject.SetActive(true);

        activePage = promptPage;
        leftHand.SetActivePage(activePage);

        notepadData.abilityIconsShown = UnlockType.None;
        notepadData.subState = SubState.None;
        notepadData.profileWriteCount = 0;

        notepadData.curState = NotepadState.Stationary;

        if (pages == null || pages.Length == 0)
        {
            AtlasUI.PromptStringDict = InitEnumToStringDict<TripPrompt>();
            npcData.habitStringDict = InitEnumToStringDict<Habits>();

            Vector3 flipWorldPos = new Vector3();
            flipWorldPos.x = bindingRingsRend.transform.localPosition.x;
            flipWorldPos.y = bindingRingsRend.transform.localPosition.y;
            flipWorldPos.z = leftHand.transform.localPosition.z;
            notepadData.leftHandFlipPos = flipWorldPos;
            notepadData.leftHandDepthFront = bindingRingsRend.transform.localPosition.z - 1;
            notepadData.leftHandDepthBack = rightHand_renderer.transform.localPosition.z + 1;
            notepadData.activePageDepth = bindingRingsRend.transform.localPosition.z + 1;

            leftHand.Init();

            float halfCamWidth = camStats.bounds.extents.x;
            float halfCamHeight = camStats.bounds.extents.y;
            float binderBoundsOffsetX = bindingRingsRend.bounds.max.x - transform.position.x;
            notepadData.inactiveLocalPos = new Vector3(halfCamWidth - binderBoundsOffsetX, -halfCamHeight + NOTEPAD_INACTIVE_OFFSET, ACTIVE_POS.z);
            notepadData.offSceenLocalPos = new Vector3(notepadData.inactiveLocalPos.x, -halfCamHeight - NOTEPAD_INACTIVE_OFFSET, ACTIVE_POS.z); 
            float bindingRingsHeight = bindingRingsRend.bounds.size.y;
            notepadData.hoverLocalPos = new Vector3(notepadData.inactiveLocalPos.x, notepadData.inactiveLocalPos.y + bindingRingsHeight, ACTIVE_POS.z);

            CreatePages();
        }
        else
        {
            ResetPages();
            Reinit();
        }
    }
    private void KeepNotepad()
    {
        SceneController.KeepNotepad(this);
    }
    private void Reinit()
    {
        SkipToPage(0);
        notepadData.curState = NotepadState.Stationary;
        notepadData.subState = SubState.None;
        clueColorPicker = SceneController.GetClueColorPicker();
        leftHand.Reinit();
    }
    public void EnterNotepad()
    {
        EnterState(NotepadState.None);
        leftHand.SetState(LeftHand.State.OffScreen);
        spyStats.checkingNotepad = true;
    }
    public void ExitNotepad()
    {
        leftHand.SetState(LeftHand.State.OffScreen);
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
                if (ToFlipUp())
                {
                    SetState(NotepadState.FlippingUp);
                }
                else if (ToFlipDown())
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
    public void SkipToPage(int index)
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
                    if (activePageIndex < lastPageIndex - 1 && playerInputs.flipKeyDownValue == 1)
                    {
                        notepadData.subState |= SubState.WillFlipUp;
                        notepadData.subState &= ~(SubState.WillFlipDown);
                    }
                    if (activePageIndex > 0 && playerInputs.flipKeyDownValue == -1)
                    {
                        notepadData.subState |= SubState.WillFlipDown;
                        notepadData.subState &= ~(SubState.WillFlipUp);
                    }
                }

                switch (leftHand.atlasRenderer.curFrameIndex)
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
                        activePage.SetPageDepth(notepadData.leftHandDepthBack + 1);
                        leftHand.atlasRenderer.SetLocalDepth(notepadData.leftHandDepthBack);
                        curKeyframeState = KeyframeState.ChangeDepth;

                    }
                    break;
                }
                if (!leftHand.atlasRenderer.isAnimating)
                {
                    if (curKeyframeState == KeyframeState.None) return;

                    activePage.gameObject.SetActive(false);
                    activePageIndex++;
                    activePage = pages[activePageIndex];
                    leftHand.SetActivePage(activePage);

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
                    if (activePageIndex < lastPageIndex && playerInputs.flipKeyDownValue == 1)
                    {
                        notepadData.subState |= SubState.WillFlipUp;
                        notepadData.subState &= ~(SubState.WillFlipDown);
                    }
                    else if (activePageIndex > 1 && playerInputs.flipKeyDownValue == -1)
                    {
                        notepadData.subState |= SubState.WillFlipDown;
                        notepadData.subState &= ~(SubState.WillFlipUp);
                    }
                }
                switch (leftHand.atlasRenderer.curFrameIndex)
                {
                    case 0:
                    {
                        if (curKeyframeState == KeyframeState.None) return;

                        activePage.gameObject.SetActive(false);
                        activePageIndex--;
                        activePage = pages[activePageIndex];
                        leftHand.SetActivePage(activePage);
                        notepadData.subState &= ~(SubState.CanFlipDown | SubState.CanWillFlipDown | SubState.IsFlippingDown);

                        curKeyframeState = KeyframeState.None;
                    }
                    break;
                    case 2:
                    {
                        if (curKeyframeState == KeyframeState.TogglePageContentsBottomHalf) return;
                        nextPage.TogglePageContentBottomHalf(true);

                        curKeyframeState = KeyframeState.TogglePageContentsBottomHalf;
                    }
                    break;
                    case 3:
                    {
                        if (curKeyframeState == KeyframeState.TogglePageContentsTopHalf) return;

                        nextPage.TogglePageContentTopHalf(true);

                        curKeyframeState = KeyframeState.TogglePageContentsTopHalf;
                    }
                    break;
                    case 4:
                    {
                        if (curKeyframeState == KeyframeState.PaperClip) return;

                        nextPage.PlayPaperClipReverse();

                        curKeyframeState = KeyframeState.PaperClip;
                    }
                    break;
                    case 6:
                    {
                        if (curKeyframeState == KeyframeState.ChangeDepth) return;

                        leftHand.atlasRenderer.SetLocalDepth(notepadData.leftHandDepthFront);
                        nextPage.SetPageDepth(notepadData.leftHandDepthFront + 2);


                        curKeyframeState = KeyframeState.ChangeDepth;
                    }
                    break;
                }
            }
            break;
            case NotepadState.Stationary:
            {

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

                leftHand.SetState(LeftHand.State.FlippingUp);

                curKeyframeState = KeyframeState.Start;
                notepadData.subState |= SubState.IsFlippingUp;
                notepadData.subState &= ~(SubState.WillFlipUp);
            }
            break;
            case NotepadState.FlippingDown:
            {
                activePage.SetPageDepth(rightHand_renderer.transform.localPosition.z - 1);

                nextPage = pages[activePageIndex - 1];
                nextPage.gameObject.SetActive(true);

                curKeyframeState = KeyframeState.Start;
                
                notepadData.subState |= SubState.IsFlippingDown;
                notepadData.subState &= ~(SubState.WillFlipDown);
                notepadData.subState &= ~(SubState.CanFlipUp);

                leftHand.SetState(LeftHand.State.FlippingDown);
            }
            break;
            case NotepadState.Stationary:
            {
                leftHand.SetState(LeftHand.State.OffScreen);
                notepadData.subState |= (SubState.CanFlipUp | SubState.CanFlipDown);
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

            case NotepadState.Stationary:
            {
                clueColorPicker?.Close();
            }
            break;
        }

    }
    private void CreateNPCProfiles()
    {
        nameData = JsonUtility.FromJson<NameData>(namesJSON.text);

        List<NPCProfile> totalNPCProfiles = new List<NPCProfile>();
        List<NPCProfile> bystanderProfiles = new List<NPCProfile>();

        for (int i = 0; i < curTrip.npcDataArray.Length; i++)
        {
            NPCSO npc = curTrip.npcDataArray[i];

            int behaviourValue = (int)npc.behaviours;

            int[] validFlags = new int[BEHAVIOURS_COUNT];
            int flagCount = 0;

            for (int j = 0; j < BEHAVIOURS_COUNT; j++)
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
                Habits firstBehaviour = (Habits)validFlags[j];
                for (int k = j + 1; k < flagCount; k++)
                {
                    Habits secondBehaviour = (Habits)validFlags[k];
                    Habits twoBehaviours = firstBehaviour | secondBehaviour;

                    NPCProfile npcProfile = new NPCProfile
                    {
                        behaviours = twoBehaviours,
                        npcPrefabIndex = i,
                    };
                    totalNPCProfiles.Add(npcProfile);
                }
            }
        }

        int totalTraitorsInTrip = 0;

        for (int i = 0; i < curTrip.stationsDataArray.Length; i++)
        {
            StationSO station = curTrip.stationsDataArray[i];
            totalTraitorsInTrip += station.traitorSpawnCount;
        }
        curTrip.traitorProfiles = new TraitorProfile[totalTraitorsInTrip];;

        int traitorIndex = 0;
        for (int i = 0; i < curTrip.stationsDataArray.Length; i++)
        {
            StationSO station = curTrip.stationsDataArray[i];

            for (int j = 0; j < station.traitorSpawnCount; j++)
            {
                int randProfileIndex = UnityEngine.Random.Range(0, totalNPCProfiles.Count);
                NPCProfile traitorProfile = totalNPCProfiles[randProfileIndex];
                traitorProfile.boardingStationIndex = i;

                int stationsLeft = curTrip.stationsDataArray.Length - i;
                float normSpawnIndex = UnityEngine.Random.Range(0, stationsLeft + 1) / (float)stationsLeft;
                float gaussianNormSpawnIndex = NormalGaussianValue(normSpawnIndex);
                traitorProfile.disembarkingStationIndex = Mathf.Min(i + Mathf.CeilToInt(gaussianNormSpawnIndex * stationsLeft) + MIN_STATION_STOPS, curTrip.stationsDataArray.Length - 1);

                NPCSO traitor = curTrip.npcDataArray[traitorProfile.npcPrefabIndex];

                string name = GenerateName(traitor.gender, traitor.ethnicity);
                curTrip.traitorProfiles[traitorIndex] = new TraitorProfile()
                {
                    npcProfile = traitorProfile,
                    mugShotIndex = traitor.mugShotIndex,
                    fullName = name,
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

        for (int i = 0; i < curTrip.stationsDataArray.Length; i++)
        {
            StationSO station = curTrip.stationsDataArray[i];
            station.accompliceProfiles = new NPCProfile[station.accompliceSpawnCount];

            for (int j = 0; j < station.accompliceSpawnCount; j++)
            {
                int randPrefabIndex = UnityEngine.Random.Range(0, curTrip.npcDataArray.Length);
                NPCProfile accompliceProfile = new NPCProfile();

                accompliceProfile.npcPrefabIndex = randPrefabIndex;
                accompliceProfile.boardingStationIndex = i;
                accompliceProfile.disembarkingStationIndex = curTrip.stationsDataArray.Length - 1;

                station.accompliceProfiles[j] = accompliceProfile;
            }

        }

        activePageIndex = 0;

        totalNPCProfiles.AddRange(bystanderProfiles);
        for (int i = 0; i < curTrip.stationsDataArray.Length; i++)
        {
            StationSO station = curTrip.stationsDataArray[i];

            station.bystanderProfiles = new NPCProfile[station.bystanderSpawnCount];

            for (int j = 0; j < station.bystanderSpawnCount; j++)
            {
                int randIndex = UnityEngine.Random.Range(0, totalNPCProfiles.Count);
                NPCProfile bystanderProfile = totalNPCProfiles[randIndex];

                bystanderProfile.boardingStationIndex = i;

                int stationsLeft = curTrip.stationsDataArray.Length - i;
                float normSpawnIndex = (float)j / (float)station.bystanderSpawnCount;
                float gaussianNormSpawnIndex = NormalGaussianValue(normSpawnIndex);
                bystanderProfile.disembarkingStationIndex = Mathf.Min(i + 1 + Mathf.CeilToInt(gaussianNormSpawnIndex * stationsLeft), curTrip.stationsDataArray.Length - 1);

                station.bystanderProfiles[j] = bystanderProfile;
            }
        }
    }
    private void CreatePages()
    {
        List<Page> pageList = new List<Page>();
        pageList.Add(promptPage);

        int totalPages = curTrip.traitorProfiles.Length + 2;
        promptPage.Init(0, totalPages);

        List<int> randIndicesList = new List<int>(curTrip.traitorProfiles.Length);
        for(int i = 0; i < curTrip.traitorProfiles.Length; i++)
        {
            randIndicesList.Add(i);
        }

        for (int i = 0; i < curTrip.traitorProfiles.Length; i++)
        {
            int randIndex = UnityEngine.Random.Range(0, randIndicesList.Count);
            int traitorIndex = randIndicesList[randIndex];
            TraitorProfile traitorProfile = curTrip.traitorProfiles[traitorIndex];
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
    private void ResetPages()
    {
        List<int> randIndicesList = new List<int>(curTrip.traitorProfiles.Length);

        promptPage.Init(0, pages.Length);
        for (int i = 0; i < curTrip.traitorProfiles.Length; i++)
        {
            randIndicesList.Add(i);
        }

        for (int i = 1; i < pages.Length - 1; i++)
        {
            int randIndex = UnityEngine.Random.Range(0, randIndicesList.Count);
            int traitorIndex = randIndicesList[randIndex];
            TraitorProfile traitorProfile = curTrip.traitorProfiles[traitorIndex];
            randIndicesList.RemoveAt(randIndex);

            Page traitorPage = pages[i];
            traitorPage.transform.SetParent(transform);
            traitorPage.transform.localPosition = new Vector3(0, 0, notepadData.leftHandDepthBack - 1);
            traitorPage.InitProfile(traitorProfile, i, pages.Length);
            traitorPage.traitorIndex = traitorIndex;
            traitorPage.gameObject.name = "Page_" + i;
            traitorPage.gameObject.SetActive(false);
        }
        colorKeyPage.Init(pages.Length - 1, pages.Length);
    }
    private float NormalGaussianValue(float t)
    {
        return Mathf.Exp(-(Mathf.Pow(t - 0.5f, 2) / 0.045f)) * 0.5f;
    }
    private bool ToFlipUp()
    {
        bool canFlipUp = (notepadData.subState & SubState.CanFlipUp) != 0;
        bool validFlipUpInputted = playerInputs.flipKeyDownValue == 1 && activePageIndex < lastPageIndex;
        bool isFlippingUp = (notepadData.subState & (SubState.WillFlipUp | SubState.IsFlippingUp)) != 0;
        
        return (validFlipUpInputted || isFlippingUp) && canFlipUp;
    }
    private bool ToFlipDown()
    {
        bool canFlipDown = (notepadData.subState & SubState.CanFlipDown) != 0;
        bool validFlipDownInputted = playerInputs.flipKeyDownValue == -1 && activePageIndex > 0;
        bool isFlippingDown = (notepadData.subState & (SubState.WillFlipDown | SubState.IsFlippingDown)) != 0;

        return (validFlipDownInputted || isFlippingDown) && canFlipDown;
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
        while(notepadData.curState != NotepadState.Stationary) await UniTask.Yield();

        if (activePage.pageIndex < pageIndex)
        {
            while (activePage.pageIndex != pageIndex)
            {
                notepadData.subState |= SubState.IsFlippingUp;
                while((notepadData.subState & SubState.IsFlippingUp) != 0) await UniTask.Yield();
                await UniTask.Yield();
            }
        }
        else if (activePage.pageIndex > 0)
        {
            while (activePage.pageIndex != pageIndex)
            {
                notepadData.subState |= SubState.IsFlippingDown;
                while ((notepadData.subState & SubState.IsFlippingDown) != 0) await UniTask.Yield();
                await UniTask.Yield();
            }
        }
    }
}
