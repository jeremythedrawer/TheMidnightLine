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
    public UIData uiData;
    public AudioData audioData;

    public AudioSource audioSource;
    public AudioSource keybindAudioSource;

    public Menu startMenu;
    public Menu optionsMenu;
    public Menu mapMenu;

    public CountryMap countryMap;

    public GameEvent onBeginTrip;
    public GameEvent onShowKeyIcon;
    public GameEvent onHideKeyIcon;

    public AtlasRenderer keybindRenderer;

    [Header("Generated")]

    public string curLocationText;

    public bool canExitState;
    public bool atOptions;
    
    public CancellationTokenSource ctsFadeBlack;


    [Header("Editor")]
    public bool skipOutcomeSequence;
    private void OnEnable()
    {        
        Menu.OnClickToMap += HandleBeginClick;
        Menu.OnClickOptions += SetToOptionsMenuState;
        Menu.OnClickBackToStartMenu += SetToStartMenuState;

        onBeginTrip.RegisterListener(SetToNoneState);
        onBeginTrip.RegisterListener(DisappearBuildings);
        onBeginTrip.RegisterListener(LowerMusicVolume);

        onShowKeyIcon.RegisterListener(SetKeyBindIcon);
        
        onHideKeyIcon.RegisterListener(HideKeybindIcon);

        SliderController.OnChangeMusicVolume += SetMusicVolume;
    }
    private void OnDisable()
    {
        Menu.OnClickToMap -= HandleBeginClick;
        Menu.OnClickOptions -= SetToOptionsMenuState;
        Menu.OnClickBackToStartMenu -= SetToStartMenuState;

        onBeginTrip.UnregisterListener(SetToNoneState);
        onBeginTrip.UnregisterListener(DisappearBuildings);
        onBeginTrip.UnregisterListener(LowerMusicVolume);
        
        onShowKeyIcon.UnregisterListener(SetKeyBindIcon);
        
        onHideKeyIcon.UnregisterListener(HideKeybindIcon);

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

        uiData.keyBindIconWorldSize = keybindRenderer.sprite.worldSize;
        uiData.keyBindSpriteIndex = -1;

        audioSource.clip = audioData.menu;   
        audioSource.volume = audioData.musicVolume;
        audioSource.Play();


        keybindRenderer.customBit |= (int)ColorBits.GreenChannel;

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
        }
    }
    private void SetToNoneState()
    {
        SetState(UIState.None);
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
        audioSource.volume = audioData.musicVolume;
    }
    private void DisappearBuildings()
    {
        for (int i = 0; i < buildingRenderers.Length; i++)
        {
            AtlasRenderer building = buildingRenderers[i];
            building.ChangeCustom(options.dayNightTransitionTime, newValue: 0, customChannel: 4);
        }
    }
    private void LowerMusicVolume()
    {
        LoweringMusicVolume().Forget();
    }
    private void SetKeyBindIcon()
    {
        keybindRenderer.enabled = true;

        keybindRenderer.transform.position = uiData.keyBindWorldPos;
        keybindRenderer.UpdateSpriteInputsByIndex(uiData.keyBindSpriteIndex);
        
        keybindAudioSource.volume = audioData.soundEffectsVolume;
        keybindAudioSource.PlayOneShot(audioData.cursorHover);
    }
    private void HideKeybindIcon()
    {
        keybindRenderer.enabled = false;
    }
    private async UniTask LoweringMusicVolume()
    {
        float clock = 0f;
        float time = 1f;
        while(clock < time)
        {
            clock += Time.deltaTime;
            float t = 1 - Mathf.Pow(clock / time, 2);
            audioSource.volume = t * audioData.musicVolume;
            await UniTask.Yield();
        }
    }
}
