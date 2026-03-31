using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;
using static Atlas;
using static NPC;
using UnityEngine.VFX;
using UnityEngine.Rendering.Universal;
using System.Collections.Generic;



#if UNITY_EDITOR
using UnityEditor;
#endif

public class NPCBrain : MonoBehaviour
{
    public NPCSO npc;
    public AtlasMotionRenderer atlasRenderer;
    [SerializeField] NPCsDataSO npcData;
    [SerializeField] LayerSettingsSO layerSettings;
    [SerializeField] TrainSettingsSO trainSettings;
    [SerializeField] TrainStatsSO trainStats;
    [SerializeField] GameEventDataSO gameEventData;
    [SerializeField] PlayerInputsSO spyInputs;
    [SerializeField] SpyStatsSO spyStats;
    [SerializeField] MaterialIDSO materialIDs;

    public Rigidbody2D rigidBody;
    public BoxCollider2D boxCollider;

    [Header("Generated")]
    public AtlasSO atlas;
    public SlideDoors curSlideDoors;
    public CancellationTokenSource ctsFade;
        
    public Carriage curCarriage;

    public StationSO startStation;
    public StationSO endStation;

    public string fullName;
    public Vector2 curSpriteMarkerLocalPosition;
    public float targetXVelocity;
    public float targetXPos;
    public float targetDist;
    public float behaviourClock;
    public float stateDuration;
    public float targetAlpha;
    public float atlasIndexClock;
    public float move;

    public Behaviours curBehaviour;

    public NPCProfile profile;
        
    public NPCState curState;
    public Path curPath;
    public Role role;
    public int smokerRoomIndex;
    public int chairPosIndex;
    public int selectedProfileIndex;

    public int curFrameIndex;
    public int prevAtlasIndex;

    public bool startFade;

    public AtlasClip curClip;

    private VisualEffect sleepingZs;
    private VisualEffect smoke;
    private VisualEffect speechBubble;
    private VisualEffect musicNotes;
    private void Awake()
    {
        ctsFade = new CancellationTokenSource();
        curState = NPCState.Idling;
        curPath = Path.None;
        chairPosIndex = int.MaxValue;
        selectedProfileIndex = int.MaxValue;
        atlas = atlasRenderer.renderInput.atlas;
        rigidBody.includeLayers = layerSettings.stationMask;
    }
    private void OnEnable()
    {
       gameEventData.OnStationArrival.RegisterListener(BoardTrain);
    }
    private void Start()
    {
        if (((profile.behaviours & Behaviours.Takes_naps) != 0) && sleepingZs == null)
        {
            sleepingZs = Instantiate(npcData.sleepingZs_prefab, transform.position, transform.rotation, transform);
            sleepingZs.Stop();
        }
        if (((profile.behaviours & Behaviours.Frequent_smoker) != 0) && smoke == null)
        {
            smoke = Instantiate(npcData.smoke_prefab, transform.position, transform.rotation, transform);
            smoke.Stop();
        }
        if (((profile.behaviours & Behaviours.Listens_to_music) != 0) && musicNotes == null)
        {
            musicNotes = Instantiate(npcData.musicNotes_prefab, transform.position, transform.rotation, transform);
            musicNotes.Stop();
        }
        if (((profile.behaviours & Behaviours.Lots_of_phone_calls) != 0) && speechBubble == null)
        {
            speechBubble = Instantiate(npcData.speechBubble_prefab, transform.position, transform.rotation, transform);
            speechBubble.Stop();
        }

        curClip = atlasRenderer.renderInput.atlas.clipDict[(int)NPCMotion.StandingBreathing];

        atlasRenderer.renderInput.UpdateDepthRealtime((int)transform.position.z);
    }
    private void OnDisable()
    {
       gameEventData.OnStationArrival.UnregisterListener(BoardTrain);
    }
    private void Update()
    {
        SelectingStates();
        UpdateStates();
        Fade();
        atlasRenderer.PlayClip(curClip);

        if (rigidBody.includeLayers == layerSettings.trainMask)
        {
            behaviourClock += Time.deltaTime;
        }

        if ((curBehaviour & Behaviours.Frequent_smoker) != 0 && curPath != Path.ToSmokerRoom)
        {
            FindSmokersRoom();
        }
        targetDist = curPath != Path.None ? targetXPos - transform.position.x : 0;
        move = Mathf.Abs(targetDist) > 0.1f ? Mathf.Sign(targetDist) : 0;
    }
    private void FixedUpdate()
    {
        FixedUpdateStates();
        targetXVelocity = npc.moveSpeed  * move;
        rigidBody.linearVelocityX = targetXVelocity;
    }
    private void SelectingStates()
    {
        if (move != 0)
        {
            SetState(NPCState.Walking);
        }
        else if (curBehaviour == 0)
        {
            SetState(NPCState.Idling);
        }
        else if ((curBehaviour & Behaviours.Frequent_smoker) != 0)
        {
            SetState(NPCState.Smoking);
        }
        else if ((curBehaviour & Behaviours.Takes_naps) != 0)
        {
            SetState(NPCState.Sleeping);
        }
        else if ((curBehaviour & Behaviours.Always_hungry) != 0)
        {
            SetState(NPCState.Eating);
        }
        else if ((curBehaviour & Behaviours.Listens_to_music) != 0)
        {
            SetState(NPCState.Music);
        }
        else if ((curBehaviour & Behaviours.Lots_of_phone_calls) != 0)
        {
            SetState(NPCState.Calling);
        }
        else if ((curBehaviour & Behaviours.Enjoys_reading) != 0)
        {
            SetState(NPCState.Reading);
        }
    }
    private void UpdateStates()
    {
        switch (curState)
        {
            case NPCState.Idling:
            {
                if (behaviourClock > stateDuration)
                {
                    curBehaviour = PickBehaviour();
                }
            }
            break;
            case NPCState.Walking:
            {
                atlasRenderer.renderInput.FlipH(move < 0, atlasRenderer.sprite);
            }
            break;
            case NPCState.Smoking:
            {
                SetMarkerPosition();
                smoke.transform.localPosition = curSpriteMarkerLocalPosition;

                if (behaviourClock > stateDuration)
                {
                    curBehaviour = PickBehaviour();
                }

            }
            break;
            case NPCState.Sleeping:
            {
                
                SetMarkerPosition();
                sleepingZs.transform.localPosition = curSpriteMarkerLocalPosition;
                
                if (behaviourClock > stateDuration)
                {
                    curBehaviour = PickBehaviour();
                }
            }
            break;
            case NPCState.Eating:
            {
                if (behaviourClock  > stateDuration)
                {
                    curBehaviour = PickBehaviour();
                }
            }
            break;
            case NPCState.Music:
            {

                SetMarkerPosition();
                musicNotes.transform.localPosition = curSpriteMarkerLocalPosition;
                
                if (behaviourClock > stateDuration)
                {
                    curBehaviour = PickBehaviour();
                }
            }
            break;
            case NPCState.Calling:
            {
                SetMarkerPosition();
                speechBubble.transform.localPosition = curSpriteMarkerLocalPosition;

                if (behaviourClock > stateDuration)
                {
                    curBehaviour = PickBehaviour();
                }
            }
            break;
            case NPCState.Reading:
            {
                if (behaviourClock > stateDuration)
                {
                    curBehaviour = PickBehaviour();
                }
            }
            break;
        }
    }
    private void FixedUpdateStates()
    {
        switch (curState)
        {
            case NPCState.Idling:
            {

            }
            break;
            case NPCState.Walking:
            {
                //TODO: move depth if collided with another npc
            }
            break;
        }
    }
    private void SetState(NPCState newState)
    {
        if (curState == newState) return;
        ExitState();
        curState = newState;
        behaviourClock = 0;
        EnterState();
    }
    private void EnterState()
    {
        atlasIndexClock = 0;
        curFrameIndex = 0;

        if (chairPosIndex != int.MaxValue)
        {
            atlasRenderer.renderInput.UpdateDepthRealtime(curCarriage.sittingDepth);
        }
        else if (curCarriage !=  null)
        {
            atlasRenderer.renderInput.UpdateDepthRealtime(curCarriage.standingDepth);
        }
        switch (curState)
        {
            case NPCState.Idling:
            {
                stateDuration = UnityEngine.Random.Range(npc.pickBehaviourDurationRange.x, npc.pickBehaviourDurationRange.y);

                if (chairPosIndex != int.MaxValue)
                {
                    curClip = RandomSittingIdleMotion();
                }
                else
                {
                    curClip = RandomStandingIdleMotion();
                }
            }
            break;
            case NPCState.Walking:
            {
                curClip = atlasRenderer.renderInput.atlas.clipDict[(int)NPCMotion.Walking];
            }
            break;
            case NPCState.Smoking:
            {
                stateDuration = UnityEngine.Random.Range(npc.pickBehaviourDurationRange.x, npc.pickBehaviourDurationRange.y);
                curClip = atlasRenderer.renderInput.atlas.clipDict[(int)NPCMotion.Smoking];
                smoke.Play();
            }
            break;
            case NPCState.Sleeping:
            {
                stateDuration = UnityEngine.Random.Range(npc.pickBehaviourDurationRange.x, npc.pickBehaviourDurationRange.y);

                //sleepingZs.transform.position = new Vector3(meshRenderer.bounds.center.x, meshRenderer.bounds.max.y, transform.position.z - 0.5f);
                sleepingZs.Play();
                if (chairPosIndex != int.MaxValue)
                {
                    curClip = atlas.clipDict[(int)NPCMotion.SittingSleeping];
                }
                else
                {
                    curClip = atlas.clipDict[(int)NPCMotion.StandingSleeping];
                }
            }
            break;
            case NPCState.Eating:
            {
                stateDuration = UnityEngine.Random.Range(
                npc.pickBehaviourDurationRange.x, 
                npc.pickBehaviourDurationRange.y);

                if (chairPosIndex != int.MaxValue)
                {
                    curClip = atlas.clipDict[(int)NPCMotion.SittingEating];
                }
                else
                {
                    curClip = atlas.clipDict[(int)NPCMotion.StandingEating];
                }
            }
            break;
            case NPCState.Music:
            {
                stateDuration = UnityEngine.Random.Range(npc.pickBehaviourDurationRange.x, npc.pickBehaviourDurationRange.y);

                musicNotes.Play();
                if (chairPosIndex != int.MaxValue)
                {
                    curClip = atlas.clipDict[(int)NPCMotion.SittingMusic];
                }
                else
                {
                    curClip = atlas.clipDict[(int)NPCMotion.StandingMusic];
                }
            }
            break;
            case NPCState.Calling:
            {
                stateDuration = UnityEngine.Random.Range(npc.pickBehaviourDurationRange.x, npc.pickBehaviourDurationRange.y);
                speechBubble.Play();
                if (chairPosIndex != int.MaxValue)
                {
                    curClip = atlas.clipDict[(int)NPCMotion.SittingCalling];
                    transform.position = new Vector3(transform.position.x, transform.position.y, curCarriage.sittingDepth);
                }
                else
                {
                    curClip = atlas.clipDict[(int)NPCMotion.StandingCalling];
                }
            }
            break;
            case NPCState.Reading:
            {
                stateDuration = UnityEngine.Random.Range(npc.pickBehaviourDurationRange.x, npc.pickBehaviourDurationRange.y);
                if (chairPosIndex != int.MaxValue)
                {
                    curClip = atlas.clipDict[(int)NPCMotion.SittingReading];
                    transform.position = new Vector3(transform.position.x, transform.position.y, curCarriage.sittingDepth);
                }
                else
                {

                    curClip = atlas.clipDict[(int)NPCMotion.StandingReading];
                }
            }
            break;
        }
    }
    private void ExitState()
    {
        switch (curState)
        {
            case NPCState.Idling:
            {

            }
            break;
            case NPCState.Walking:
            {
            }
            break;
            case NPCState.Smoking:
            {
                smoke.Stop();
                curCarriage.smokersRoomData[smokerRoomIndex].npcCount--;
                QueueForChair();
            }
            break;
            case NPCState.Sleeping:
            {
                sleepingZs.Stop();
            }
            break;
            case NPCState.Music:
            {
                musicNotes.Stop();
            }
            break;
            case NPCState.Calling:
            {
                speechBubble.Stop();
            }
            break;
        }

    }
    private void Fade()
    {
        bool shouldFadeOut =
            rigidBody.includeLayers == layerSettings.stationMask &&
            spyStats.curGroundLayer == layerSettings.trainLayers.ground &&
            spyStats.curWorldPos.z > transform.position.z &&
            transform.position.x > spyStats.curLocationBounds.min.x &&
            transform.position.x < spyStats.curLocationBounds.max.x;

        float targAlpha = shouldFadeOut ? 0f : 1f;

        if (targetAlpha == targAlpha) return;
        
        targetAlpha = targAlpha;
        ctsFade?.Cancel();
        ctsFade?.Dispose();

        ctsFade = new CancellationTokenSource();
        FadeTo().Forget();
    }
    private async UniTask FadeTo()
    {
        float startAlpha = atlasRenderer.renderInput.custom.x;
        float elapsed = 0f;
        try
        {
            while (elapsed < npcData.fadeTime)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / npcData.fadeTime;

                atlasRenderer.renderInput.custom.x = Mathf.Lerp(startAlpha, targetAlpha, t);

                await UniTask.Yield(PlayerLoopTiming.Update, ctsFade.Token);
            }

            atlasRenderer.renderInput.custom.x = targetAlpha;
        }
        catch (OperationCanceledException)
        {
        }
    }
    private void BoardTrain()
    {
        if (rigidBody.includeLayers == layerSettings.trainMask) return;
        BoardingTrain().Forget();
    }
    private async UniTask BoardingTrain()
    {
        FindSlideDoor();
        float randomStartMoveTime = UnityEngine.Random.Range(0.3f, 1f);
        await UniTask.WaitForSeconds(randomStartMoveTime);
        await UniTask.WaitUntil(() => curState != NPCState.Walking);

        RaycastHit2D slideDoorHit = Physics2D.BoxCast(boxCollider.bounds.center, boxCollider.bounds.size, 0.0f, transform.right, 0.0f, layerSettings.trainLayers.slideDoors);
        RaycastHit2D carriageHit = Physics2D.BoxCast(boxCollider.bounds.center, boxCollider.bounds.size, 0.0f, transform.right, 0.0f, layerSettings.trainLayers.insideCarriageBounds);

#if UNITY_EDITOR
        if (slideDoorHit.collider == null) { Debug.LogError($"{fullName} did not find a slide door to go to"); return; }
        if (carriageHit.collider == null) { Debug.LogError($"{fullName} did not find a slide door to go to"); return; }
#endif
        curSlideDoors = slideDoorHit.collider.GetComponent<SlideDoors>();
        await UniTask.WaitUntil(() => curSlideDoors.curState != SlideDoors.State.Locked);
        if (curSlideDoors.curState == SlideDoors.State.Unlocked)
        {
            curSlideDoors.OpenDoors();
        }

        await UniTask.WaitUntil(() => curSlideDoors.curState == SlideDoors.State.Opened);
        SetStandingDepth();
        transform.SetParent(null, true);
        trainStats.curPassengerCount++;
        QueueForChair();

        curCarriage = trainStats.carriageDict[carriageHit.collider];
        rigidBody.includeLayers = layerSettings.trainMask;
    }
    private void FindSlideDoor()
    {
        float shortestDist = float.MaxValue;
        float selectedSlideDoorPos = float.MaxValue;
        for (int i = 0; i < trainStats.slideDoorPositions.Length; i++)
        {
            float dist = Mathf.Abs(trainStats.slideDoorPositions[i] - transform.position.x);

            if (dist < shortestDist)
            {
                shortestDist = dist;
                selectedSlideDoorPos = trainStats.slideDoorPositions[i];
            }
        }
        targetXPos = selectedSlideDoorPos;
        curPath = Path.ToSlideDoor;
    }
    private void QueueForChair()
    {
        if (!NPCManager.npcChairList.Contains(this))
        {
            NPCManager.npcChairList.Add(this);
        }
    }
    public void AssignChair(int chairIndex)
    {
        chairPosIndex = chairIndex;
        curCarriage.chairData[chairPosIndex].filled = true;
        curPath = Path.ToChair;
        targetXPos = curCarriage.chairData[chairPosIndex].xPos;
    }
    public void FindStandingPosition()
    {
        targetXPos = UnityEngine.Random.Range(curCarriage.insideBoundsCollider.bounds.min.x, curCarriage.insideBoundsCollider.bounds.max.x);
    }
    private void FindSmokersRoom()
    {
        if (chairPosIndex != int.MaxValue && curCarriage.chairData[chairPosIndex].filled)
        {
            curCarriage.chairData[chairPosIndex].filled = false;
            chairPosIndex = int.MaxValue;

        }
        if (NPCManager.npcChairList.Contains(this)) NPCManager.npcChairList.Remove(this); // To prevent them from going back to the chair if they are queued

        if (curCarriage.smokersRoomData.Length > 1 && curCarriage.smokersRoomData[1].npcCount < curCarriage.smokersRoomData[0].npcCount) // selected smoker room is based on which room has less npcs
        {
            smokerRoomIndex = 1;
        }
        else
        {
            smokerRoomIndex = 0;
        }
        curCarriage.smokersRoomData[smokerRoomIndex].npcCount++;
        SetStandingDepth();
        curPath = Path.ToSmokerRoom;
        targetXPos = UnityEngine.Random.Range(curCarriage.smokersRoomData[smokerRoomIndex].minXPos, curCarriage.smokersRoomData[smokerRoomIndex].maxXPos);
    }
    private void SetStandingDepth()
    {
        int depth = UnityEngine.Random.Range(trainStats.depthSection_front_min, trainStats.depthSection_back_max);
        atlasRenderer.renderInput.UpdateDepthRealtime(depth);
    }
    private void SetMarkerPosition()
    {
        MarkerPosition[] curSpriteMarkers = curClip.keyFrames[curFrameIndex].motionSprite.markers;
        if (curSpriteMarkers.Length > 0)
        {
            curSpriteMarkerLocalPosition.x = atlasRenderer.renderInput.flipX ? -curSpriteMarkers[0].objectPos.x : curSpriteMarkers[0].objectPos.x;
            curSpriteMarkerLocalPosition.y = curSpriteMarkers[0].objectPos.y;
        }
    }
    private AtlasClip RandomStandingIdleMotion()
    {
        if (UnityEngine.Random.Range(0, 2) == 0)
        {
            return  atlas.clipDict[(int)NPCMotion.StandingBreathing];
        }
        else
        {
            return atlas.clipDict[(int)NPCMotion.StandingBlinking];

        }
    }
    private AtlasClip RandomSittingIdleMotion()
    {
        if (UnityEngine.Random.Range(0, 2) == 0)
        {
            return atlas.clipDict[(int)NPCMotion.SittingBreathing];
        }
        else
        {
            return atlas.clipDict[(int)NPCMotion.SittingBlinking];
        }
    }
    private Behaviours PickBehaviour()
    {
        int behaviourValue = (int)profile.behaviours;
        int[] flags = new int[32];
        int flagCount = 1;

        for (int i = 0; i < flags.Length; i++)
        {
            int flag = 1 << i;
            if ((behaviourValue & flag) != 0)
            {
                flags[flagCount] = flag;
                flagCount++;
            }
        }
        int chosenFlag = flags[UnityEngine.Random.Range(0, flagCount)];

        return (Behaviours)chosenFlag;
    }
    public Behaviours GetRandomBehaviours(Behaviours npcBehaviours)
    {
        Behaviours[] allBehaviours = (Behaviours[])Enum.GetValues(typeof(Behaviours));

        List<Behaviours> allowedBehaviours = new List<Behaviours>();
        for (int i = 0; i < allBehaviours.Length; i++)
        {
            Behaviours b = allBehaviours[i];
            if (b != 0 && npcBehaviours.HasFlag(b))
            {
                allowedBehaviours.Add(b);
            }
        }

        Behaviours firstBehave = allowedBehaviours[UnityEngine.Random.Range(0, allowedBehaviours.Count)];
        if (allowedBehaviours.Count == 1)
        {
            return firstBehave;
        }

        Behaviours secondBehave;
        do
        {
            secondBehave = allowedBehaviours[UnityEngine.Random.Range(0, allowedBehaviours.Count)];
        }
        while (secondBehave == firstBehave);

        Behaviours behaviours = firstBehave | secondBehave;
        return behaviours;
    }
#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;

        if (curState == NPCState.Walking)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(boxCollider.bounds.center, new Vector2(targetXPos, boxCollider.bounds.center.y));
        }

        Vector3 typeLabel = transform.position + atlasRenderer.sprite.worldSize + Vector3.up * 0.2f;
        Vector3 stateLabel = typeLabel + Vector3.up;

        GUIStyle typeStyle = new GUIStyle();
        typeStyle.normal.textColor = role == Role.Traitor ? Color.red : Color.green;
        typeStyle.alignment = TextAnchor.UpperCenter;
        typeStyle.fontSize = 10;

        GUIStyle stateStyle = new GUIStyle();
        stateStyle.normal.textColor = role == Role.Traitor ? Color.red : Color.green;
        stateStyle.alignment = TextAnchor.UpperCenter;
        stateStyle.fontSize = 10;

        // Draw the label in Scene view
        Handles.Label(typeLabel, role.ToString(), typeStyle);
        Handles.Label(stateLabel, curClip.ToString(), stateStyle);

    }
#endif

}
