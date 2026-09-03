using Cysharp.Threading.Tasks;
using Proselyte.Sigils;
using System;
using System.Threading;

using UnityEngine;

using static AtlasUI;
using static Passenger;

public class WorldUIController : MonoBehaviour
{
    public AtlasRenderer[] buildingRenderers;

    public CameraData camData;
    public InputData inputData;
    public NotepadData notepadData;
    public Options options;
    public SpyData spyData;
    public PassengersData passengerData;
    public CursorData cursorData;

    public AudioSource audioSource;    

    public Menu startMenu;
    public Menu optionsMenu;
    public Menu mapMenu;

    public CountryMap countryMap;

    public GameEvent onBeginTrip;

    [Header("Generated")]

    public string curLocationText;
    public Vector3[] outcomePageInactivePositions;

    public TraitorProfile curTraitorProfile;

    public Vector3 naturalMovePos;
    public Vector3 outcomePageActivePos;
    public Vector3 outcomePageStartPos;

    public float outcomePageEndPosX;
    public float outcomePageHoverPosY;

    public int curTraitorsShown;
    public int curTraitorProfilesReviewed;
    public int outcomePageCompletedMask;

    public bool canExitState;
    public bool atOptions;
    public bool outcomeSetUpCompleted;

    public Page[] profilePages;

    public Notepad notepad;
    
    public Page hoveredPage;
    public Page activePage;
    
    public CancellationTokenSource ctsFadeBlack;
    public CancellationTokenSource ctsNotepad;
    public CancellationTokenSource ctsOutcomePageMove;


    [Header("Editor")]
    public bool skipOutcomeSequence;
    private void OnEnable()
    {        
        SpyBrain.OnOpenNotepad += SetToNotepadState;
        SpyBrain.OnCloseNotepad += SetToNoneState;
        
        Menu.OnClickToMap += HandleBeginClick;
        Menu.OnClickOptions += SetToOptionsMenuState;
        Menu.OnClickBackToStartMenu += SetToStartMenuState;

        onBeginTrip.RegisterListener(SetToNoneState);
        onBeginTrip.RegisterListener(DisappearBuildings);

        FadeBlack.OnFinishFadeOut += SetToNoneStateFromOutcome;

        SliderController.OnChangeMusicVolume += SetMusicVolume;
    }
    private void OnDisable()
    {
        SpyBrain.OnOpenNotepad -= SetToNotepadState;
        SpyBrain.OnCloseNotepad -= SetToNoneState;

        Menu.OnClickToMap -= HandleBeginClick;
        Menu.OnClickOptions -= SetToOptionsMenuState;
        Menu.OnClickBackToStartMenu -= SetToStartMenuState;

        onBeginTrip.UnregisterListener(SetToNoneState);
        onBeginTrip.UnregisterListener(DisappearBuildings);
        
        SliderController.OnChangeMusicVolume -= SetMusicVolume;
    }
    private void Start()
    {
        Init();
    }
    private void Update()
    {
        UpdateState();
    }
    private void Init()
    {
        Shader.SetGlobalFloat(options.dayNightID, 1);


        audioSource.clip = options.music.menu;   
        audioSource.volume = options.music.volume;
        audioSource.Play();
        SetState(UIState.StartMenu);
    }
    private void SetState(UIState newState)
    {
        if (camData.curUIState == newState) return;
        ExitState();
        camData.curUIState = newState;
        EnterState();
    }
    public void EnterState()
    {
        canExitState = false;
        switch (camData.curUIState)
        {
            case UIState.StartMenu:
            {
                camData.curLocationBounds = startMenu.bounds;
            }
            break;
            case UIState.OptionsMenu:
            {
                camData.curLocationBounds = optionsMenu.bounds;
            }
            break;
            
            case UIState.MapMenu:
            {
                camData.curLocationBounds = mapMenu.bounds;
            }
            break;

            case UIState.Notepad:
            {
                notepad.EnterNotepad();
                naturalMovePos = Notepad.ACTIVE_POS;
                ctsNotepad?.Cancel();
            }
            break;

            case UIState.Outcome:
            {

            }
            break;
            case UIState.None:
            {
            }
            break;
        }
    }
    private void UpdateState()
    {
        switch (camData.curUIState)
        {
            case UIState.Notepad:
            {
                UpdateNaturalPos(Notepad.ACTIVE_POS, ref naturalMovePos);
                notepad.transform.localPosition = Vector3.Lerp(notepad.transform.localPosition, naturalMovePos, Time.deltaTime * MOVE_DAMP);
                if ((notepad.transform.localPosition - naturalMovePos).sqrMagnitude < 0.05f) notepadData.subState |= Notepad.SubState.InUse;
            }
            break;

            case UIState.None:
            {
                if (notepadData.collected)
                {
                    if (notepad.transform.parent != transform) return;
                    if (canExitState && cursorData.IsInsideBounds(notepad.activePage.paperRenderer.bounds, isClickable: true))
                    {
                        ctsNotepad?.Cancel();
                        notepad.transform.localPosition = Vector3.Lerp(notepad.transform.localPosition, notepadData.hoverLocalPos, Time.deltaTime * MOVE_DAMP);
                    }
                    else
                    {
                        notepad.transform.localPosition = Vector3.Lerp(notepad.transform.localPosition, notepadData.inactiveLocalPos, Time.deltaTime * MOVE_DAMP);
                    }
                }
            }
            break;

            case UIState.StartMenu:
            {
                startMenu.UpdateMenu();
            }
            break;

            case UIState.OptionsMenu:
            {
                optionsMenu.UpdateMenu();
            }
            break;

            case UIState.MapMenu:
            {
                mapMenu.UpdateMenu();
                countryMap.UpdateButtons();
            }
            break;

            case UIState.Outcome:
            {
            }
            break;
        }
        canExitState = true;
    }
    private void ExitState()
    {
        switch (camData.curUIState)
        {
            case UIState.StartMenu:
            {
                spyData.playerInputsEnabled = true;
            }
            break;
            case UIState.OptionsMenu:
            {
                spyData.playerInputsEnabled = true;
            }
            break;
            case UIState.Notepad:
            {
                notepad.ExitNotepad();
            }
            break;
            case UIState.Outcome:
            {

            }
            break;
        }
    }
    private void SetToNoneStateFromOutcome()
    {
        if(camData.curUIState == UIState.Outcome)
        {
            SetState(UIState.None);
        }
    }
    private void SetToNoneState()
    {
        SetState(UIState.None);
    }
    private void SetToNotepadState()
    {
        SetState(UIState.Notepad);
    }
    private void SetToStartMenuState()
    { 
        SetState(UIState.StartMenu);
    }
    private void SetToOptionsMenuState()
    {
        SetState(UIState.OptionsMenu);
    }
    private void HandleBeginClick()
    {
        if (options.thirdPointRegion.trips[0].completed)
        {
            SetState(UIState.MapMenu);
        }
        else
        {
            SetState(UIState.None);
        }
    }
    private void SetMusicVolume()
    {
        audioSource.volume = options.music.volume;
    }
    private void DisappearBuildings()
    {
        for (int i = 0; i < buildingRenderers.Length; i++)
        {
            AtlasRenderer building = buildingRenderers[i];
            building.ChangeCustom(options.dayNightTransitionTime, newValue: 0, customChannel: 4);
        }
    }
}
