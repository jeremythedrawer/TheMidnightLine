using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;
using static Atlas;
using static AtlasUI;
using static Passenger;

public class BottomPanel : MonoBehaviour
{
    const float TRANSITION_TIME = 1f;
    const float MOVE_TIME = 1f;
    const float STATION_BUTTON_ROW_MOVE_TIME = 0.25f;
    const float OUTCOME_TIME = 2f;
    [Flags] public enum Groups
    { 
        None = 0,
        Traitors = 1 << 0,
        TripMap = 1 << 1,
        Profile = 1 << 3,
        Station = 1 << 4,
    }

    public AtlasRenderer atlasRenderer;

    public Options options;
    public ActionData actionData;
    public UIData uiData;
    public PassengersData passengersData;
    public CursorData cursorData;

    [Header("Trip Map")]
    public Transform tripMapGroup;
    public AtlasRenderer tripMapLineRenderer;
    [Header("Traitors")]
    public IconButton[] traitorMugshotIconButtons;
    public Transform traitorsGroup;
    [Header("Profile")]
    public Transform profileGroup;
    public AtlasRenderer profileMugshotRenderer;
    public IconButton profileExitButton;
    public AtlasTextRenderer[] profileHabitTextRenderers;
    public AtlasTextRenderer profileWhatStationTextRenderer;
    public TextButton[] profileStationButtons;
    public TextButton suspectingButton;
    [Header("Station")]
    public Transform stationGroup;
    public AtlasTextRenderer stationNameTextRenderer;
    public AtlasTextRenderer[] stationPlaceTextRenderers;
    public AtlasRenderer stationMapRenderer;
    public IconButton stationExitButton;
    public TextButton stationNextButton;
    public TextButton stationPreviousButton;
    
    [Header("Generated")]
    public IconButton[] stationIconButtons;

    public StationSO curStationData;

    public float[] profileStationButtonPosYs;

    public TraitorProfile curTraitorProfile;

    public Vector3 curLocalPosition;

    public Groups curGroups;

    public float activeGroupHeight;
    public float inactiveGroupHeight;
    public float moveClock;

    public int selectedProfileStationButtonIndex;

    public bool profilStationButtonsActive;

    public CancellationTokenSource ctsTransitionLeft;
    public CancellationTokenSource ctsTransitionRight;
    public CancellationTokenSource ctsMove;
    public CancellationTokenSource ctsProfileStationButtonTransition;
    public CancellationTokenSource ctsRevealTraitorOutcome;

    private void OnEnable()
    {
        actionData.onBeginTrip += SetTripMapGroup;
        actionData.onBeginTrip += SetProfileGroup;
        actionData.onBeginTrip += SetStationGroup;

        actionData.onAtFirstStation += MoveToActivePosition;

        actionData.onCreatedPassengerProfiles += SetTraitorsGroup;

        actionData.onSuspectPassenger += ShowStationButtons;
    }
    private void OnDisable()
    {
        actionData.onBeginTrip -= SetTripMapGroup;
        actionData.onBeginTrip -= SetProfileGroup;
        actionData.onBeginTrip -= SetStationGroup;

        actionData.onAtFirstStation -= MoveToActivePosition;

        actionData.onCreatedPassengerProfiles -= SetTraitorsGroup;

        actionData.onSuspectPassenger -= ShowStationButtons;

        CleanUpCancelTokens();
    }
    private void Start()
    {
        Init();
    }
    private void Update()
    {
        suspectingButton.UpdateButton();

        if ((curGroups & Groups.Traitors) != 0)
        {
            for (int i = 0; i < traitorMugshotIconButtons.Length; i++)
            {
                IconButton mugShotButton = traitorMugshotIconButtons[i];
                mugShotButton.UpdateButton();
            }
        }

        if ((curGroups & Groups.TripMap) != 0)
        {
            for (int i = 0; i < stationIconButtons.Length; i++)
            {
                IconButton stationIconButton = stationIconButtons[i];
                stationIconButton.UpdateButton();
            }
        }

        if ((curGroups & Groups.Profile) != 0)
        {
            profileExitButton.UpdateButton();
            suspectingButton.UpdateButton();

            if (profilStationButtonsActive)
            {
                for (int i = 0; i < profileStationButtons.Length; i++)
                {
                    TextButton stationButton = profileStationButtons[i];
                    stationButton.UpdateButton();
                }
            }
        }

        if ((curGroups & Groups.Station) != 0)
        {
            stationExitButton.UpdateButton();
            stationNextButton.UpdateButton();
            stationPreviousButton.UpdateButton();
        }
    }
    private void Init()
    {
        activeGroupHeight = traitorsGroup.localPosition.y;
        inactiveGroupHeight = profileGroup.localPosition.y;

        transform.localPosition = uiData.inactiveBottomPaneLocalPos;
        curLocalPosition = transform.localPosition;
        profileWhatStationTextRenderer.SetText("");
    }
    private void CleanUpCancelTokens()
    {
        ctsTransitionLeft?.Cancel();
        ctsTransitionLeft?.Dispose();

        ctsTransitionRight?.Cancel();
        ctsTransitionRight?.Dispose();
        
        ctsMove?.Cancel();
        ctsMove?.Dispose();

        ctsProfileStationButtonTransition?.Cancel();
        ctsProfileStationButtonTransition?.Dispose();

        ctsRevealTraitorOutcome?.Cancel();
        ctsRevealTraitorOutcome?.Dispose();
    }
    private void ShowStationButtons()
    {
        if (profileWhatStationTextRenderer.text != "") return;
        
        void ShowStationButtons()
        {
            ctsProfileStationButtonTransition?.Cancel();
            ctsProfileStationButtonTransition = new CancellationTokenSource();

            for (int i = 0; i < profileStationButtons.Length; i++)
            {
                TextButton stationButton = profileStationButtons[i];
                string stationName = options.curTrip.stationsDataArray[i].name;
                stationButton.textRenderer.SetText(stationName);
                Vector3 localPos = new Vector3();
                localPos.x = stationButton.transform.localPosition.x;
                localPos.y = profileWhatStationTextRenderer.transform.localPosition.y;
                localPos.z = stationButton.transform.localPosition.z;
                stationButton.transform.localPosition = localPos;
            }

            ShowingStationButtons().Forget();
        }
        
        string whatStationText = "What station are they going to?";
        
        profileWhatStationTextRenderer.WriteText(whatStationText, writeLetterTime: 0.02f, callback: ShowStationButtons);
    }
    private void SetTraitorsGroup()
    {
        for (int i = 0; i < traitorMugshotIconButtons.Length; i++)
        {
            TraitorProfile traitorProfile = options.curTrip.traitorProfiles[i];

            IconButton mugShotIcon = traitorMugshotIconButtons[i];
            AtlasRenderer mugshotRenderer = mugShotIcon.atlasRenderer;
            
            mugshotRenderer.UpdateSpriteInputsByIndex(traitorProfile.mugShotIndex);

            int index = i;
            void OnMouseUp()
            {
                mugShotIcon.MouseUp();
                curTraitorProfile = options.curTrip.traitorProfiles[index];
                profileMugshotRenderer.UpdateSpriteInputsByIndex(curTraitorProfile.mugShotIndex);

                for (int j = 0; j < profileHabitTextRenderers.Length; j++)
                {
                    AtlasTextRenderer habitTextRenderer = profileHabitTextRenderers[j];
                    Habits curHabit = GetHabitAtIndex(curTraitorProfile.passengerProfile.habits, j);
                    string habitText = passengersData.habitStringDict[curHabit];
                    habitTextRenderer.SetText(habitText);

                }
                
                profileStationButtons[selectedProfileStationButtonIndex].backgroundRenderer.customBit &= ~(int)ColorBits.Meridia;
                if (curTraitorProfile.selectedStationIndex != -1)
                {
                    selectedProfileStationButtonIndex = curTraitorProfile.selectedStationIndex; 
                    profileStationButtons[curTraitorProfile.selectedStationIndex].backgroundRenderer.customBit |= (int)ColorBits.Meridia;
                }
                TransitionGroup(profileGroup, traitorsGroup, ctsTransitionLeft);
                curGroups |= Groups.Profile;
                curGroups &= ~Groups.Traitors;
            }
            void OnMouseExit()
            {
                atlasRenderer.customBit &= ~(int)ColorBits.GreenChannel;
            }
            mugShotIcon.InitButton(onMouseUp: OnMouseUp, onExit: OnMouseExit);
        }
    }
    private void SetTripMapGroup()
    {
        Bounds tripMapLineBounds = tripMapLineRenderer.bounds;
        float startXPos = -tripMapLineRenderer.bounds.extents.x;
        float stationSegment = tripMapLineRenderer.bounds.size.x / (options.curTrip.stationsDataArray.Length - 1);
        float stubSegment = stationSegment / 3f;

        Vector3 localPos = new Vector3();
        localPos.y = 0;
        localPos.z = 0;

        stationIconButtons = new IconButton[options.curTrip.stationsDataArray.Length];    
        for (int i = 0; i < stationIconButtons.Length; i++)
        {
            StationSO stationData = options.curTrip.stationsDataArray[i];
            IconButton stationIconButton = Instantiate(uiData.tripMapStationButton, tripMapLineRenderer.transform);
            localPos.x = startXPos + (stationSegment * i);

            SimpleSprite stationMapSprite = stationMapRenderer.atlas.simpleSprites[i];
            void OnMouseUp()
            {
                stationIconButton.MouseUp();
                curStationData = stationData;
                stationNameTextRenderer.SetText(curStationData.name);
                stationMapRenderer.UpdateSpriteInputs(stationMapSprite);
                for (int j = 0; j < stationPlaceTextRenderers.Length; j++)
                {
                    AtlasTextRenderer textRenderer = stationPlaceTextRenderers[j];
                    
                    string place = curStationData.places[j];
                    textRenderer.SetText(place);
                    
                    Vector2 placePos = stationMapSprite.customPositions[j];
                    
                    Vector3 placeLocalPos = new Vector3();
                    placeLocalPos.x = placePos.x;
                    placeLocalPos.y = placePos.y;
                    placeLocalPos.z = textRenderer.transform.localPosition.z;

                    textRenderer.transform.localPosition = placeLocalPos;
                }
                TransitionGroup(stationGroup, tripMapGroup, ctsTransitionRight);
                curGroups |= Groups.Station;
                curGroups &= ~Groups.TripMap;
            }
            stationIconButton.InitButton(onMouseUp: OnMouseUp);
            stationIconButton.transform.localPosition = localPos;
            stationIconButtons[i] = stationIconButton;

            if (i == 0) continue;

            for (int j = 0; j < 2; j++)
            {
                AtlasRenderer stubRenderer = Instantiate(uiData.tripMapStub, tripMapLineRenderer.transform);
                localPos.x -= stubSegment;
                stubRenderer.transform.localPosition = localPos;
            }
        }
    }
    private void SetStationGroup()
    {
        void ExitMouseUp()
        {
            stationExitButton.MouseUp();
            TransitionGroup(tripMapGroup, stationGroup, ctsTransitionRight);
            curGroups |= Groups.TripMap;
            curGroups &= ~Groups.Station;
        }
        void PrevStationMouseUp()
        {
            stationPreviousButton.MouseUp();
            int prevStationIndex = curStationData.stationIndex - 1;
            if (prevStationIndex < 0) prevStationIndex = options.curTrip.stationsDataArray.Length - 1;

            curStationData = options.curTrip.stationsDataArray[prevStationIndex];
            SimpleSprite stationMapSprite = stationMapRenderer.atlas.simpleSprites[prevStationIndex];

            stationNameTextRenderer.SetText(curStationData.name);
            
            stationMapRenderer.UpdateSpriteInputs(stationMapSprite);

            for (int j = 0; j < stationPlaceTextRenderers.Length; j++)
            {
                AtlasTextRenderer textRenderer = stationPlaceTextRenderers[j];

                string place = curStationData.places[j];
                textRenderer.SetText(place);

                Vector2 placePos = stationMapSprite.customPositions[j];

                Vector3 placeLocalPos = new Vector3();
                placeLocalPos.x = placePos.x;
                placeLocalPos.y = placePos.y;
                placeLocalPos.z = textRenderer.transform.localPosition.z;

                textRenderer.transform.localPosition = placeLocalPos;
            }
        }
        void NextStationMouseUp()
        {
            stationNextButton.MouseUp();
            int nextStationIndex = curStationData.stationIndex + 1;
            if (nextStationIndex == options.curTrip.stationsDataArray.Length) nextStationIndex = 0;
            curStationData = options.curTrip.stationsDataArray[nextStationIndex];
            SimpleSprite stationMapSprite = stationMapRenderer.atlas.simpleSprites[nextStationIndex];

            stationNameTextRenderer.SetText(curStationData.name);

            stationMapRenderer.UpdateSpriteInputs(stationMapSprite);

            for (int j = 0; j < stationPlaceTextRenderers.Length; j++)
            {
                AtlasTextRenderer textRenderer = stationPlaceTextRenderers[j];

                string place = curStationData.places[j];
                textRenderer.SetText(place);

                Vector2 placePos = stationMapSprite.customPositions[j];

                Vector3 placeLocalPos = new Vector3();
                placeLocalPos.x = placePos.x;
                placeLocalPos.y = placePos.y;
                placeLocalPos.z = textRenderer.transform.localPosition.z;

                textRenderer.transform.localPosition = placeLocalPos;
            }
        }
        stationExitButton.InitButton(onMouseUp: ExitMouseUp);
        stationPreviousButton.InitButton(onMouseUp:  PrevStationMouseUp);
        stationNextButton.InitButton(onMouseUp:  NextStationMouseUp);
    }
    private void SetProfileGroup()
    {
        void OnMouseUpExit()
        {
            profileExitButton.MouseUp();
            
            cursorData.curPassengerSelectionMode = CursorData.PassengerSelectionMode.Unmasking;
            suspectingButton.backgroundRenderer.customBit &= ~(int)ColorBits.Meridia;
            suspectingButton.textRenderer.SetText("Suspect");
            HideStationButtons();
            PassengerBrain.UnsuspectActivePassenger();
            actionData.onUnsuspect?.Invoke();

            curGroups |= Groups.Traitors;
            curGroups &= ~Groups.Profile;
            TransitionGroup(traitorsGroup, profileGroup, ctsTransitionLeft);
        }
        profileExitButton.InitButton(onMouseUp: OnMouseUpExit);
        
        void SuspectingMouseUp()
        {
            suspectingButton.MouseUp();
            if (cursorData.curPassengerSelectionMode == CursorData.PassengerSelectionMode.Suspecting)
            {
                if (curTraitorProfile.selectedStationIndex == -1)
                {
                    cursorData.curPassengerSelectionMode = CursorData.PassengerSelectionMode.Unmasking;
                    suspectingButton.backgroundRenderer.customBit &= ~(int)ColorBits.Meridia;
                    suspectingButton.textRenderer.SetText("Suspect");
                    HideStationButtons();
                    PassengerBrain.UnsuspectActivePassenger();
                    actionData.onUnsuspect?.Invoke();
                }
                else
                {
                    bool selectedCorrectStation = curTraitorProfile.selectedStationIndex == curTraitorProfile.passengerProfile.disembarkingStationIndex;
                    bool selectedCorrectPassneger = curTraitorProfile.passengerProfile.id == PassengerBrain.ActivePassenger.profile.id;
                    if (selectedCorrectPassneger && selectedCorrectStation)
                    {
                        profileMugshotRenderer.customBit |= (int)ColorBits.RedChannel;
                    }
                    else
                    {
                       profileMugshotRenderer.customBit |= (int)ColorBits.BlueChannel;
                    }

                    ctsRevealTraitorOutcome?.Cancel();
                    ctsRevealTraitorOutcome = new CancellationTokenSource();
                    RevealingTraitorOutcome().Forget();
                }
            }
            else
            {
                cursorData.curPassengerSelectionMode = CursorData.PassengerSelectionMode.Suspecting;
                suspectingButton.backgroundRenderer.customBit |= (int)ColorBits.Meridia;
                suspectingButton.textRenderer.SetText("Cancel");
                actionData.onSuspect?.Invoke();                
            }
        }
        suspectingButton.InitButton(onMouseUp: SuspectingMouseUp);

        int profileStationButtonRows = Mathf.CeilToInt(profileStationButtons.Length * 0.5f);
        profileStationButtonPosYs = new float[profileStationButtonRows];
        for (int i = 0; i < profileStationButtonRows; i++)
        {
            TextButton profileStationButton = profileStationButtons[i];
            profileStationButtonPosYs[i] = profileStationButton.transform.localPosition.y;
        }

        for (int i = 0; i < profileStationButtons.Length; i++)
        {
            TextButton stationButton = profileStationButtons[i];
            int index = i;

            void MouseUp()
            {
                stationButton.MouseUp();
                stationButton.backgroundRenderer.customBit ^= (int)ColorBits.Meridia;
                
                if (curTraitorProfile.selectedStationIndex == index)
                {
                    curTraitorProfile.selectedStationIndex = -1;
                    suspectingButton.textRenderer.SetText("Cancel");
                }
                else
                {
                    if (curTraitorProfile.selectedStationIndex != -1)
                    {
                        profileStationButtons[curTraitorProfile.selectedStationIndex].backgroundRenderer.customBit &= ~(int)ColorBits.Meridia;
                    }
                    curTraitorProfile.selectedStationIndex = index;
                    selectedProfileStationButtonIndex = index;

                    suspectingButton.textRenderer.SetText("Submit");
                }
                options.curTrip.traitorProfiles[curTraitorProfile.traitorIndex] = curTraitorProfile;
            }
            stationButton.InitButton(onMouseUp: MouseUp);
            stationButton.textRenderer.SetText("");
        }
    }
    private void TransitionGroup(Transform toGroup, Transform fromGroup, CancellationTokenSource cts)
    {
        cts?.Cancel();
        cts = new CancellationTokenSource();

        TransitioningGroups(toGroup, fromGroup, cts).Forget();
    }
    private void MoveToActivePosition()
    {
        ctsMove?.Cancel();
        ctsMove = new CancellationTokenSource();

        MovingToActivePosition().Forget();
    }
    private void HideStationButtons()
    {
        ctsProfileStationButtonTransition?.Cancel();
        ctsProfileStationButtonTransition = new CancellationTokenSource();
        HidingStationButtons().Forget();
    }
    private async UniTask TransitioningGroups(Transform toGroup, Transform fromGroup, CancellationTokenSource cts)
    {
        try
        {
            Vector3 toPosition = toGroup.localPosition;
            Vector3 fromPosition = fromGroup.localPosition;

            float transitionClock = Mathf.InverseLerp(inactiveGroupHeight, activeGroupHeight, toPosition.y) * TRANSITION_TIME;

            while(transitionClock < TRANSITION_TIME)
            {
                float t = transitionClock / TRANSITION_TIME;
                t = Curves.EaseInOutCubic(t);

                float curInactiveHeight = Mathf.Lerp(activeGroupHeight, inactiveGroupHeight, t);
                float curActiveHeight = Mathf.Lerp(inactiveGroupHeight, activeGroupHeight, t);
                fromPosition.y = curInactiveHeight;
                toPosition.y = curActiveHeight;

                toGroup.localPosition = toPosition;
                fromGroup.localPosition = fromPosition;

                transitionClock += Time.deltaTime;

                await UniTask.Yield(cts.Token);
            }
        }
        catch(OperationCanceledException)
        {

        }
    }
    private async UniTask MovingToActivePosition()
    {
        try
        {
            while (moveClock < MOVE_TIME)
            {
                float t = moveClock / MOVE_TIME;
                t = Curves.EaseInOutCubic(t);

                float currHeight = Mathf.Lerp(uiData.inactiveBottomPaneLocalPos.y, uiData.activeBottomPanelLocalPos.y, t);
                curLocalPosition.y = currHeight;
                transform.localPosition = curLocalPosition;

                moveClock += Time.deltaTime;

                await UniTask.Yield(ctsMove.Token);
            }
            curGroups = Groups.TripMap | Groups.Traitors;
        }
        catch (OperationCanceledException)
        {
        }
    }
    private async UniTask ShowingStationButtons()
    {
        float clock = 0;
        Vector3 localPos = new Vector3();

        int curRowIndex = profileStationButtonPosYs.Length - 1;
        float prevTargetPosY = profileWhatStationTextRenderer.transform.localPosition.y;
        float curTargetPosY =  profileStationButtonPosYs[curRowIndex];
        try
        {
            while(true)
            {
                float t = clock / STATION_BUTTON_ROW_MOVE_TIME;
                t = Curves.EaseInOutCubic(t);
                clock += Time.deltaTime;

                localPos.y = Mathf.Lerp(prevTargetPosY, curTargetPosY, t);
                for (int i = 0; i < profileStationButtons.Length; i++)
                {
                    TextButton stationButton = profileStationButtons[i];                
                    float rowIndex = i % profileStationButtonPosYs.Length;

                    if (rowIndex > curRowIndex) continue;

                    localPos.x = stationButton.transform.localPosition.x;
                    localPos.z = profileWhatStationTextRenderer.transform.localPosition.z + ((profileStationButtonPosYs.Length - rowIndex) * 0.02f);
                    stationButton.transform.localPosition = localPos;
                }

                if (clock > STATION_BUTTON_ROW_MOVE_TIME)
                {
                    curRowIndex--;
                    if (curRowIndex < 0) break;
                    prevTargetPosY = curTargetPosY;
                    curTargetPosY = profileStationButtonPosYs[curRowIndex];
                    clock = 0;                
                }
                await UniTask.Yield(ctsProfileStationButtonTransition.Token);
            }
            profilStationButtonsActive = true;
        }
        catch(OperationCanceledException)
        {

        }
    }
    private async UniTask HidingStationButtons()
    {
        float clock = 0;
        Vector3 localPos = new Vector3();

        int curRowIndex = 1;
        float prevTargetPosY = profileStationButtonPosYs[0];
        float curTargetPosY = profileStationButtonPosYs[curRowIndex];
        try
        {
            while (true)
            {
                float t = clock / STATION_BUTTON_ROW_MOVE_TIME;
                t = Curves.EaseInOutCubic(t);
                clock += Time.deltaTime;

                localPos.y = Mathf.Lerp(prevTargetPosY, curTargetPosY, t);
                for (int i = 0; i < profileStationButtons.Length; i++)
                {
                    TextButton stationButton = profileStationButtons[i];
                    float rowIndex = i % profileStationButtonPosYs.Length;

                    if (rowIndex >= curRowIndex) continue;

                    localPos.x = stationButton.transform.localPosition.x;
                    localPos.z = profileWhatStationTextRenderer.transform.localPosition.z + ((profileStationButtonPosYs.Length - rowIndex) * 0.02f);
                    stationButton.transform.localPosition = localPos;
                }

                if (clock > STATION_BUTTON_ROW_MOVE_TIME)
                {
                    curRowIndex++;
                    clock = 0;
                    prevTargetPosY = curTargetPosY;
                    if (curRowIndex == profileStationButtonPosYs.Length)
                    {
                        curTargetPosY = profileWhatStationTextRenderer.transform.localPosition.y;
                    }
                    else if (curRowIndex > profileStationButtonPosYs.Length)
                    {
                        break;
                    }
                    else
                    {
                        curTargetPosY = profileStationButtonPosYs[curRowIndex];
                    }
                }
                await UniTask.Yield(ctsProfileStationButtonTransition.Token);
            }

            profilStationButtonsActive = false;
            for (int i = 0; i < profileStationButtons.Length; i++)
            {
                TextButton stationButton = profileStationButtons[i];
                stationButton.textRenderer.SetText("");
            }
            profileWhatStationTextRenderer.EraseText(writeLetterTime: 0.02f);
        }
        catch (OperationCanceledException) { }
    }
    private async UniTask RevealingTraitorOutcome()
    {
        float clock = 0;
        try
        {
            while(clock < OUTCOME_TIME)
            {
                float t = clock / OUTCOME_TIME;
                clock += Time.deltaTime;
                profileMugshotRenderer.custom.x = t;

                await UniTask.Yield(ctsRevealTraitorOutcome.Token);
            }

            cursorData.curPassengerSelectionMode = CursorData.PassengerSelectionMode.Unmasking;
            suspectingButton.backgroundRenderer.customBit &= ~(int)ColorBits.Meridia;
            suspectingButton.textRenderer.SetText("Suspect");
            HideStationButtons();
            actionData.onUnsuspect?.Invoke();
        }
        catch
        {

        }

    }
}
