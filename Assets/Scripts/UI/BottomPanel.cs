using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;
using static AtlasUI;
using static Passenger;

public class BottomPanel : MonoBehaviour
{
    const float TRANSITION_TIME = 1f;
    const float MOVE_TIME = 1f;
    [Flags] public enum Groups
    { 
        None = 0,
        Traitors = 1 << 0,
        TripMap = 1 << 1,
        Profile = 1 << 3,
        Station = 1 << 4,
    }

    public AtlasRenderer atlasRenderer;
    public TextButton unmaskingButton;
    public TextButton suspectingButton;

    public Options options;
    public ActionData actionData;
    public UIData uiData;
    public PassengersData passengersData;

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
    public TextButton[] profileStationButtons;
    [Header("Station")]
    public Transform stationGroup;
    public AtlasTextRenderer stationNameTextRenderer;
    public AtlasTextRenderer[] stationPlaceTextRenderers;
    public IconButton stationExitButton;
    
    [Header("Generated")]
    public IconButton[] stationIconButtons;

    public StationSO curStationData;
    
    public TraitorProfile curTraitorProfile;

    public Vector3 curLocalPosition;

    public Groups curGroups;

    public float activeGroupHeight;
    public float inactiveGroupHeight;

    public float moveClock;

    public int selectedProfileStationButtonIndex;

    public CancellationTokenSource ctsTransitionLeft;
    public CancellationTokenSource ctsTransitionRight;
    public CancellationTokenSource ctsMove;
    private void OnEnable()
    {
        actionData.onBeginTrip += SetTripMapGroup;
        actionData.onBeginTrip += SetProfileGroup;
        actionData.onBeginTrip += SetStationGroup;
        actionData.onAtFirstStation += MoveToActivePosition;
        actionData.onCreatedPassengerProfiles += SetTraitorsGroup;
    }
    private void OnDisable()
    {
        actionData.onBeginTrip -= SetTripMapGroup;
        actionData.onBeginTrip -= SetProfileGroup;
        actionData.onBeginTrip -= SetStationGroup;
        actionData.onAtFirstStation -= MoveToActivePosition;
        actionData.onCreatedPassengerProfiles -= SetTraitorsGroup;

        ctsTransitionLeft?.Cancel();
        ctsTransitionLeft?.Dispose();

        ctsTransitionRight?.Cancel();
        ctsTransitionRight?.Dispose();
        
        ctsMove?.Cancel();
        ctsMove?.Dispose();
    }
    private void Start()
    {
        activeGroupHeight = traitorsGroup.localPosition.y;
        inactiveGroupHeight = profileGroup.localPosition.y;

        transform.localPosition = uiData.inactiveBottomPaneLocalPos;
        curLocalPosition = transform.localPosition;
    }
    private void Update()
    {
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
            for (int i = 0; i < profileStationButtons.Length; i++)
            {
                TextButton stationButton = profileStationButtons[i];
                stationButton.UpdateButton();
            }
        }

        if ((curGroups & Groups.Station) != 0)
        {
            stationExitButton.UpdateButton();
        }
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

            mugShotIcon.InitButton(onMouseUp: OnMouseUp);
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

            int checksToStation = i * 3;
            void OnMouseUp()
            {
                stationIconButton.MouseUp();
                curStationData = stationData;
                stationNameTextRenderer.SetText(curStationData.name);

                for (int j = 0; j < stationPlaceTextRenderers.Length; j++)
                {
                    AtlasTextRenderer textRenderer = stationPlaceTextRenderers[j];
                    string place = curStationData.places[j];
                    textRenderer.SetText(place);
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
        void MouseUp()
        {
            stationExitButton.MouseUp();
            TransitionGroup(tripMapGroup, stationGroup, ctsTransitionRight);
            curGroups |= Groups.TripMap;
            curGroups &= ~Groups.Station;
        }
        stationExitButton.InitButton(onMouseUp: MouseUp);
    }
    private void SetProfileGroup()
    {
        void OnMouseUpExit()
        {
            profileExitButton.MouseUp();

            TransitionGroup(traitorsGroup, profileGroup, ctsTransitionLeft);

            curGroups |= Groups.Traitors;
            curGroups &= ~Groups.Profile;
        }
        profileExitButton.InitButton(onMouseUp: OnMouseUpExit);

        for (int i = 0; i < profileStationButtons.Length; i++)
        {
            TextButton stationButton = profileStationButtons[i];
            int index = i;

            void MouseUp()
            {
                stationButton.MouseUpText();
                stationButton.backgroundRenderer.customBit ^= (int)ColorBits.Meridia;
                
                if (curTraitorProfile.selectedStationIndex == index)
                {
                    curTraitorProfile.selectedStationIndex = -1;
                }
                else
                {
                    if (curTraitorProfile.selectedStationIndex != -1)
                    {
                        profileStationButtons[curTraitorProfile.selectedStationIndex].backgroundRenderer.customBit &= ~(int)ColorBits.Meridia;
                    }
                    curTraitorProfile.selectedStationIndex = index;
                    selectedProfileStationButtonIndex = index;
                }
                options.curTrip.traitorProfiles[curTraitorProfile.traitorIndex] = curTraitorProfile;
            }
            stationButton.InitButton(onMouseUp: MouseUp);
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
}
