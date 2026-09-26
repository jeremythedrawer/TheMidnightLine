using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;
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
        Markers = 1 << 2,
        Profile = 1 << 3,
        Stations = 1 << 4,
    }

    public AtlasRenderer atlasRenderer;
    public AtlasRenderer tripMapLineRenderer;
    public AtlasRenderer profileMugshotRenderer;


    public Transform traitorsGroup;
    public Transform tripMapGroup;
    public Transform profileGroup;
    public Transform markerGroup;

    public IconButton[] mugshotIconButtons;

    public IconButton profileExitButton;

    public AtlasTextRenderer stationNameTextRenderer;
    public AtlasTextRenderer checkNumberTextRenderer;

    public AtlasTextRenderer[] habitTextRenderers;
    
    public Options options;
    public ActionData actionData;
    public UIData uiData;
    public PassengersData passengersData;
    [Header("Generated")]
    public IconButton[] stationIconButtons;

    public StationSO curStationData;
    
    public TraitorProfile curTraitorProfile;

    public Vector3 curLocalPosition;
    
    public Vector3 traitorsGroupPosition;
    public Vector3 tripMapGroupPosition;
    public Vector3 profileGroupPosition;
    public Vector3 markerGroupPosition;

    public Groups curGroups;

    public float activeGroupHeight;
    public float inactiveGroupHeight;

    public float transitionClock;
    public float moveClock;

    public CancellationTokenSource ctsTransition;
    public CancellationTokenSource ctsMove;
    private void OnEnable()
    {
        actionData.onBeginTrip += SetTripMapGroup;
        actionData.onBeginTrip += SetProfileGroup;
        actionData.onAtFirstStation += MoveToActivePosition;
        actionData.onCreatedPassengerProfiles += SetTraitorsGroup;
    }
    private void OnDisable()
    {
        actionData.onBeginTrip -= SetTripMapGroup;
        actionData.onBeginTrip -= SetProfileGroup;
        actionData.onAtFirstStation -= MoveToActivePosition;
        actionData.onCreatedPassengerProfiles -= SetTraitorsGroup;

        ctsTransition?.Cancel();
        ctsTransition?.Dispose();

        ctsMove?.Cancel();
        ctsMove?.Dispose();
    }
    private void Start()
    {
        traitorsGroupPosition = traitorsGroup.localPosition;
        tripMapGroupPosition = tripMapGroup.localPosition;
        profileGroupPosition = profileGroup.localPosition;
        markerGroupPosition = markerGroup.localPosition;

        activeGroupHeight = traitorsGroup.localPosition.y;
        inactiveGroupHeight = profileGroup.localPosition.y;

        transform.localPosition = uiData.inactiveBottomPaneLocalPos;
        curLocalPosition = transform.localPosition;
    }
    private void Update()
    {
        if ((curGroups & Groups.Traitors) != 0)
        {
            for (int i = 0; i < mugshotIconButtons.Length; i++)
            {
                IconButton mugShotButton = mugshotIconButtons[i];
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
        }
    }
    private void SetTraitorsGroup()
    {
        for (int i = 0; i < mugshotIconButtons.Length; i++)
        {
            TraitorProfile traitorProfile = options.curTrip.traitorProfiles[i];

            IconButton mugShotIcon = mugshotIconButtons[i];
            AtlasRenderer mugshotRenderer = mugShotIcon.atlasRenderer;
            
            mugshotRenderer.UpdateSpriteInputsByIndex(traitorProfile.mugShotIndex);

            void OnMouseUp()
            {
                mugShotIcon.MouseUp();
                curTraitorProfile = traitorProfile;
                profileMugshotRenderer.UpdateSpriteInputsByIndex(curTraitorProfile.mugShotIndex);
                for (int j = 0; j < habitTextRenderers.Length; j++)
                {
                    AtlasTextRenderer habitTextRenderer = habitTextRenderers[j];
                    Habits curHabit = GetHabitAtIndex(curTraitorProfile.passengerProfile.habits, j);
                    string habitText = passengersData.habitStringDict[curHabit];
                    habitTextRenderer.SetText(habitText);

                    curGroups |= Groups.Profile;
                    curGroups &= ~(Groups.Traitors | Groups.TripMap);
                }
                TransitionToProfileGroup();
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

                int curChecksToStation = checksToStation - options.curTrip.passengersCheckedTotal;
                string checkNumberText = curChecksToStation.ToString();
                checkNumberTextRenderer.SetText(checkNumberText);
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
    private void SetProfileGroup()
    {
        void OnMouseUpExit()
        {
            profileExitButton.MouseUp();

            TransitionToTratorsAndTripMapGroups();

            curGroups |= (Groups.Traitors | Groups.TripMap);
            curGroups &= ~Groups.Profile;
        }
        profileExitButton.InitButton(onMouseUp: OnMouseUpExit);
    }
    private void TransitionToProfileGroup()
    {
        ctsTransition?.Cancel();
        ctsTransition = new CancellationTokenSource();

        TransitioningToProfileGroup().Forget();
    }
    private void TransitionToTratorsAndTripMapGroups()
    {
        ctsTransition?.Cancel();
        ctsTransition = new CancellationTokenSource();

        TransitioningToTratorsAndTripMapGroups().Forget();
    }
    private void MoveToActivePosition()
    {
        ctsMove?.Cancel();
        ctsMove = new CancellationTokenSource();

        MovingToActivePosition().Forget();
    }
    private async UniTask TransitioningToProfileGroup()
    {
        try
        {
            while(transitionClock < TRANSITION_TIME)
            {
                float t = transitionClock / TRANSITION_TIME;
                t = Curves.EaseInOutCubic(t);

                float curInactiveHeight = Mathf.Lerp(activeGroupHeight, inactiveGroupHeight, t);
                float curActiveHeight = Mathf.Lerp(inactiveGroupHeight, activeGroupHeight, t);
                traitorsGroupPosition.y = curInactiveHeight;
                tripMapGroupPosition.y = curInactiveHeight;
                profileGroupPosition.y = curActiveHeight;

                traitorsGroup.localPosition = traitorsGroupPosition;
                tripMapGroup.localPosition = tripMapGroupPosition;
                profileGroup.localPosition = profileGroupPosition;

                transitionClock += Time.deltaTime;

                await UniTask.Yield(ctsTransition.Token);
            }
        }
        catch(OperationCanceledException)
        {

        }
    }

    private async UniTask TransitioningToTratorsAndTripMapGroups()
    {
        try
        {
            while (transitionClock > 0)
            {
                float t = transitionClock / TRANSITION_TIME;
                t = Curves.EaseInOutCubic(t);

                float curInactiveHeight = Mathf.Lerp(activeGroupHeight, inactiveGroupHeight, t);
                float curActiveHeight = Mathf.Lerp(inactiveGroupHeight, activeGroupHeight, t);
                traitorsGroupPosition.y = curInactiveHeight;
                tripMapGroupPosition.y = curInactiveHeight;
                profileGroupPosition.y = curActiveHeight;

                traitorsGroup.localPosition = traitorsGroupPosition;
                tripMapGroup.localPosition = tripMapGroupPosition;
                profileGroup.localPosition = profileGroupPosition;

                transitionClock -= Time.deltaTime;

                await UniTask.Yield(ctsTransition.Token);
            }
        }
        catch (OperationCanceledException)
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
            curGroups = Groups.TripMap | Groups.Traitors;
        }
    }
}
