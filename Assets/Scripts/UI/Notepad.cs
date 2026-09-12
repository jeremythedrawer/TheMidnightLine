using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.InputSystem.LowLevel;
using static Atlas;
using static AtlasUI;
using static Passenger;
public class Notepad : MonoBehaviour
{
    public const int MIN_STATION_STOPS = 1;
    public const float MOVE_TIME = 1.5f;

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

    public InputData inputData;
    public PassengersData passengerData;
    public CameraData camData;
    public SpyData spyStats;
    public Options options;

    public NotepadData notepadData;
    
    public LeftHand leftHand;
    
    public AtlasRenderer rightHand_renderer;
    public AtlasRenderer frontFingers_renderer;
    public AtlasRenderer bindingRingsRend;

    public AudioSource audioSource;
    public AudioData audioData;

    public TextAsset namesJSON;

    public Page frontPage;

    [Header("Generated")]
    public Page[] pages;
    
    public Page activePage;
    public Page nextPage;

    public ColorPicker clueColorPicker;

    public CancellationTokenSource ctsMove;

    public TraitorProfile activeTraitorProfile;
    
    public NameData nameData;

    public Vector3 curLocalPos;

    public int lastPageIndex;    
    public int traitorOutcomesRevealed;

    public bool canExitState;

    private void OnEnable()
    {
        Page.OnMouseUpExitButton += HandlePageExitButton;
    }
    private void OnDisable()
    {
        notepadData.subState = SubState.None;
        notepadData.collected = false;
        Graphics.Blit(Texture2D.whiteTexture, notepadData.pageFlipRT);
        Page.OnMouseUpExitButton -= HandlePageExitButton;
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
        CreateNPCProfiles();
        PickUpNotepad();
        CreatePages();
        InitPageFlipCompute();
    }
    private void InitPageFlipCompute()
    {
        notepadData.pageFlipRT.Release();
        notepadData.pageFlipRT.width = Screen.width /4;
        notepadData.pageFlipRT.height = Screen.height/4;
        notepadData.pageFlipRT.enableRandomWrite = true;
        notepadData.pageFlipRT.Create();

        Graphics.Blit(Texture2D.whiteTexture, notepadData.pageFlipRT);

        notepadData.pageFlipThreadGroupX = Mathf.CeilToInt(notepadData.pageFlipRT.width / 8.0f);
        notepadData.pageFlipThreadGroupY = Mathf.CeilToInt(notepadData.pageFlipRT.height / 8.0f);

        notepadData.pageFlipKernel = notepadData.pageFlipCompute.FindKernel("CSPageFlip");
        notepadData.pagePropergateKernel = notepadData.pageFlipCompute.FindKernel("CSPropagate");

        notepadData.pageFlipCompute.SetTexture(notepadData.pageFlipKernel, "_SDFTexture", notepadData.pageFlipRT);
        notepadData.pageFlipCompute.SetTexture(notepadData.pageFlipKernel, "_POVUITexture", leftHand.atlasRenderer.batchKey.texture);

        notepadData.pageFlipCompute.SetTexture(notepadData.pagePropergateKernel, "_SDFTexture", notepadData.pageFlipRT);
        
        notepadData.pageFlipCompute.SetVector("_SDFTextureSize", new Vector4(notepadData.pageFlipRT.width, notepadData.pageFlipRT.height, 0, 0));

        Shader.SetGlobalTexture("_PageFlipMaskTexture", notepadData.pageFlipRT);
    }
    public void PickUpNotepad() 
    {
        Vector3 notepadStartPos = new Vector3();
        notepadStartPos.x = camData.bounds.size.x;
        notepadStartPos.y = notepadData.activeLocalPos.y;
        notepadStartPos.z = notepadData.activeLocalPos.z;
        transform.localPosition = notepadStartPos;

        activePage = frontPage;
        leftHand.SetActivePage(activePage);

        notepadData.subState = SubState.InUse;
        notepadData.curState = NotepadState.Stationary;
        notepadData.collected = true;

        AtlasUI.PromptStringDict = InitEnumToStringDict<TripPrompt>();
        passengerData.habitStringDict = InitEnumToStringDict<Habits>();

        Vector3 flipWorldPos = new Vector3();
        flipWorldPos.x = bindingRingsRend.transform.localPosition.x;
        flipWorldPos.y = bindingRingsRend.transform.localPosition.y;
        flipWorldPos.z = leftHand.transform.localPosition.z;
        notepadData.leftHandFlipPos = flipWorldPos;
        notepadData.leftHandDepthFront = bindingRingsRend.transform.localPosition.z - 1;
        notepadData.leftHandDepthBack = rightHand_renderer.transform.localPosition.z + 1;
        notepadData.activePageDepth = bindingRingsRend.transform.localPosition.z + 1;

        curLocalPos.z = transform.localPosition.z;



        leftHand.Init();

        MoveToPosition(notepadData.activeLocalPos);
        EnterNotepad();
    }
    public void EnterNotepad()
    {
        EnterState(NotepadState.None);
        leftHand.SetState(LeftHand.State.OffScreen);
        notepadData.subState |= SubState.InUse;
    }
    public void ExitNotepad()
    {
        leftHand.SetState(LeftHand.State.OffScreen);
        notepadData.subState &= ~(SubState.InUse);
    }
    private void ChooseState()
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
        activePage = pages[index];
        activePage.gameObject.SetActive(true);

        for (int i = 0; i < pages.Length; i++)
        {
            Page page = pages[i];
            page.paperRenderer.UpdateSpriteInputsByIndex(12);
            page.SetPageDepth(bindingRingsRend.transform.localPosition.z + 3);
        }
        activePage.SetPageDepth(notepadData.activePageDepth);

        notepadData.subState &= ~(SubState.CanFlipDown | SubState.CanWillFlipDown | SubState.IsFlippingDown);
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
                    if (activePage.pageIndex < lastPageIndex - 1 && inputData.flipKeyDownValue == 1)
                    {
                        notepadData.subState |= SubState.WillFlipUp;
                        notepadData.subState &= ~(SubState.WillFlipDown);
                    }
                    if (activePage.pageIndex > 0 && inputData.flipKeyDownValue == -1)
                    {
                        notepadData.subState |= SubState.WillFlipDown;
                        notepadData.subState &= ~(SubState.WillFlipUp);
                    }
                }

                if (!leftHand.atlasRenderer.isAnimating)
                {
                    activePage.gameObject.SetActive(false);
                    activePage = nextPage;
                    leftHand.SetActivePage(activePage);

                    activePage.SetPageDepth(notepadData.leftHandDepthFront + 2);

                    notepadData.subState &= ~(SubState.CanFlipUp | SubState.CanWillFlipUp | SubState.IsFlippingUp);
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
                    if (activePage.pageIndex < lastPageIndex && inputData.flipKeyDownValue == 1)
                    {
                        notepadData.subState |= SubState.WillFlipUp;
                        notepadData.subState &= ~(SubState.WillFlipDown);
                    }
                    else if (activePage.pageIndex > 1 && inputData.flipKeyDownValue == -1)
                    {
                        notepadData.subState |= SubState.WillFlipDown;
                        notepadData.subState &= ~(SubState.WillFlipUp);
                    }
                }
                if (!leftHand.atlasRenderer.isAnimating)
                {
                    activePage.gameObject.SetActive(false);
                    activePage = nextPage;
                    leftHand.SetActivePage(activePage);
                    notepadData.subState &= ~(SubState.CanFlipDown | SubState.CanWillFlipDown | SubState.IsFlippingDown);
                }
                switch (leftHand.atlasRenderer.curFrameIndex)
                {
                    case 6:
                    {
                        nextPage.SetPageDepth(notepadData.leftHandDepthFront + 2);
                    }
                    break;
                }
            }
            break;
            case NotepadState.Stationary:
            {
                activePage.UpdatePage();
                UpdateNaturalPos(notepadData.activeLocalPos, ref curLocalPos);
                transform.localPosition = Vector3.Lerp(transform.localPosition, curLocalPos, Time.deltaTime * MOVE_DAMP);
                if (inputData.notepadToggleKeyUp)
                {
                    if (!canExitState)
                    {
                        canExitState = true;
                    }
                    else
                    {
                        notepadData.subState ^= SubState.InUse;
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
                activePage.SetInvertNotepadMaskBit(invert: true);

                nextPage = pages[activePage.pageIndex + 1];
                nextPage.gameObject.SetActive(true);
                nextPage.SetInvertNotepadMaskBit(invert: false);
                
                leftHand.SetNextPage(nextPage);
                leftHand.SetState(LeftHand.State.FlippingUp);

                notepadData.subState |= SubState.IsFlippingUp;
                notepadData.subState &= ~(SubState.WillFlipUp);


            }
            break;
            case NotepadState.FlippingDown:
            {
                activePage.SetPageDepth(rightHand_renderer.transform.localPosition.z - 1);
                activePage.SetInvertNotepadMaskBit(invert: true);

                nextPage = pages[activePage.pageIndex - 1];
                nextPage.gameObject.SetActive(true);
                nextPage.SetInvertNotepadMaskBit(invert: false);
                leftHand.SetNextPage(nextPage);

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

        for (int i = 0; i < options.curTrip.passengers.Length; i++)
        {
            PassengerData npc = options.curTrip.passengers[i];

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

        for (int i = 0; i < options.curTrip.stationsDataArray.Length; i++)
        {
            StationSO station = options.curTrip.stationsDataArray[i];
            totalTraitorsInTrip += station.traitorSpawnCount;
        }
        options.curTrip.traitorProfiles = new TraitorProfile[totalTraitorsInTrip];;

        int traitorIndex = 0;
        for (int i = 0; i < options.curTrip.stationsDataArray.Length; i++)
        {
            StationSO station = options.curTrip.stationsDataArray[i];

            for (int j = 0; j < station.traitorSpawnCount; j++)
            {
                int randProfileIndex = UnityEngine.Random.Range(0, totalNPCProfiles.Count);
                NPCProfile traitorProfile = totalNPCProfiles[randProfileIndex];
                traitorProfile.boardingStationIndex = i;

                int stationsLeft = options.curTrip.stationsDataArray.Length - i;
                float normSpawnIndex = UnityEngine.Random.Range(0, stationsLeft + 1) / (float)stationsLeft;
                float gaussianNormSpawnIndex = NormalGaussianValue(normSpawnIndex);
                traitorProfile.disembarkingStationIndex = Mathf.Min(i + Mathf.CeilToInt(gaussianNormSpawnIndex * stationsLeft) + MIN_STATION_STOPS, options.curTrip.stationsDataArray.Length - 1);

                PassengerData traitor = options.curTrip.passengers[traitorProfile.npcPrefabIndex];

                string name = GenerateName(traitor.gender, traitor.ethnicity);
                options.curTrip.traitorProfiles[traitorIndex] = new TraitorProfile()
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

        for (int i = 0; i < options.curTrip.stationsDataArray.Length; i++)
        {
            StationSO station = options.curTrip.stationsDataArray[i];
            station.accompliceProfiles = new NPCProfile[station.accompliceSpawnCount];

            for (int j = 0; j < station.accompliceSpawnCount; j++)
            {
                int randPrefabIndex = UnityEngine.Random.Range(0, options.curTrip.passengers.Length);
                NPCProfile accompliceProfile = new NPCProfile();

                accompliceProfile.npcPrefabIndex = randPrefabIndex;
                accompliceProfile.boardingStationIndex = i;
                accompliceProfile.disembarkingStationIndex = options.curTrip.stationsDataArray.Length - 1;

                station.accompliceProfiles[j] = accompliceProfile;
            }

        }

        totalNPCProfiles.AddRange(bystanderProfiles);
        for (int i = 0; i < options.curTrip.stationsDataArray.Length; i++)
        {
            StationSO station = options.curTrip.stationsDataArray[i];

            station.bystanderProfiles = new NPCProfile[station.bystanderSpawnCount];

            for (int j = 0; j < station.bystanderSpawnCount; j++)
            {
                int randIndex = UnityEngine.Random.Range(0, totalNPCProfiles.Count);
                NPCProfile bystanderProfile = totalNPCProfiles[randIndex];

                bystanderProfile.boardingStationIndex = i;

                int stationsLeft = options.curTrip.stationsDataArray.Length - i;
                float normSpawnIndex = (float)j / (float)station.bystanderSpawnCount;
                float gaussianNormSpawnIndex = NormalGaussianValue(normSpawnIndex);
                bystanderProfile.disembarkingStationIndex = Mathf.Min(i + 1 + Mathf.CeilToInt(gaussianNormSpawnIndex * stationsLeft), options.curTrip.stationsDataArray.Length - 1);

                station.bystanderProfiles[j] = bystanderProfile;
            }
        }
    }
    private void CreatePages()
    {
        List<Page> pageList = new List<Page>();

        pageList.Add(frontPage);

        int totalPages = options.curTrip.traitorProfiles.Length + 2;
        frontPage.Init(pageIndexInput: 0);

        notepadData.pageCount = 1 + options.curTrip.traitorProfiles.Length;

        List<int> randIndicesList = new List<int>(options.curTrip.traitorProfiles.Length);
        for(int i = 0; i < options.curTrip.traitorProfiles.Length; i++)
        {
            randIndicesList.Add(i);
        }

        for (int i = 0; i < options.curTrip.traitorProfiles.Length; i++)
        {
            int randIndex = UnityEngine.Random.Range(0, randIndicesList.Count);
            int traitorIndex = randIndicesList[randIndex];
            TraitorProfile traitorProfile = options.curTrip.traitorProfiles[traitorIndex];
            randIndicesList.RemoveAt(randIndex);

            ProfilePage traitorPage = Instantiate(notepadData.profilePagePrefab, transform);
            traitorPage.transform.localPosition = new Vector3(0, 0, notepadData.leftHandDepthBack - 1);

            int pageIndex = i + 1;
            traitorPage.InitProfile(traitorProfile, pageIndex);
            traitorPage.traitorIndex = traitorIndex;
            traitorPage.gameObject.name = "Page_" + pageIndex;

            pageList.Add(traitorPage.page);
            traitorPage.gameObject.SetActive(false);
        }

        pages = pageList.ToArray();
        lastPageIndex = pages.Length - 1;
    }
    private float NormalGaussianValue(float t)
    {
        return Mathf.Exp(-(Mathf.Pow(t - 0.5f, 2) / 0.045f)) * 0.5f;
    }
    private bool ToFlipUp()
    {
        bool canFlipUp = (notepadData.subState & SubState.CanFlipUp) != 0;
        bool validFlipUpInputted = inputData.flipKeyDownValue == 1 && activePage.pageIndex < lastPageIndex;
        bool isFlippingUp = (notepadData.subState & (SubState.WillFlipUp | SubState.IsFlippingUp)) != 0;
        
        return (validFlipUpInputted || isFlippingUp) && canFlipUp;
    }
    private bool ToFlipDown()
    {
        bool canFlipDown = (notepadData.subState & SubState.CanFlipDown) != 0;
        bool validFlipDownInputted = inputData.flipKeyDownValue == -1 && activePage.pageIndex > 0;
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
    private void HandlePageExitButton()
    {
        notepadData.subState ^= SubState.InUse;

        if ((notepadData.subState & SubState.InUse) == 0)
        {
            MoveToPosition(notepadData.inactiveLocalPos);
        }
        else 
        {
            MoveToPosition(notepadData.activeLocalPos);
        }
    }
    public void MoveToPosition(Vector3 pos)
    {
        audioSource.PlayOneShot(audioData.sweep);
        audioSource.volume = audioData.soundEffectsVolume;

        ctsMove?.Cancel();
        ctsMove = new CancellationTokenSource();
        MovingToPosition(pos).Forget();
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
    private async UniTask MovingToPosition(Vector3 pos)
    {
        float clock = 0;
        Vector2 startPos = transform.localPosition;
        try
        {
            while (clock < audioData.sweep.length)
            {
                clock += Time.deltaTime;
                float t = clock / audioData.sweep.length;
                t = Curves.EaseOutT(t, 4);
                curLocalPos.x = Mathf.Lerp(startPos.x, pos.x, t);
                curLocalPos.y = Mathf.Lerp(startPos.y, pos.y, t);
                transform.localPosition = curLocalPos;
                await UniTask.Yield(ctsMove.Token);
            }
            transform.localPosition = pos;
        }
        catch (OperationCanceledException)
        { 
        
        }
    }
}
