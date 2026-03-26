using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;
using static Atlas;
using static NPC;
using UnityEngine.VFX;
using UnityEngine.Rendering.Universal;


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
    public MaterialPropertyBlock mpb;
    public CancellationTokenSource ctsFade;
        
    public Carriage curCarriage;

    public StationSO startStation;
    public StationSO endStation;

    [Serializable] public struct StatData
    {
        public string name;
        public Vector2 curSpriteMarkerLocalPosition;
        public Color selectedColor;
        public float targetXVelocity;
        public float targetXPos;
        public float targetDist;
        public float stateClock;
        public float stateDuration;
        public float curAlpha;
        public float targetAlpha;
        public float atlasIndexClock;
        public float move;

        public Behaviours curBehaviour;
        public Behaviours behaviours;
        
        public NPCState curState;
        public Path curPath;
        public Role role;
        public AtlasClip curClip;

        public uint smokerRoomIndex;
        public uint chairPosIndex;
        public int selectedProfileIndex;

        public int curFrameIndex;
        public int prevAtlasIndex;

        public bool startFade;
    }
    [SerializeField] public StatData stats;


    private VisualEffect sleepingZs;
    private VisualEffect smoke;
    private VisualEffect speechBubble;
    private VisualEffect musicNotes;
    private void Awake()
    {
        mpb = new MaterialPropertyBlock();
        ctsFade = new CancellationTokenSource();

        stats.curState = NPCState.Idling;
        stats.curPath = Path.None;
        stats.chairPosIndex = int.MaxValue;
        stats.curAlpha = 1;
        stats.selectedProfileIndex = int.MaxValue;
        atlas = atlasRenderer.renderInput.atlas;
        SetLayer();
        if (stats.behaviours == 0)
        {
            stats.behaviours = GetBehaviours(npc.behaviours);
        }
    }
    private void OnEnable()
    {
        gameEventData.OnStationArrival.RegisterListener(BoardTrain);
        atlasRenderer.renderInput.UpdateDepthRealtime((int)transform.position.z);
        name = NPCManager.GenerateName(npc.gender, npc.ethnicity);

    }
    private void Start()
    {
        if (((stats.behaviours & Behaviours.Takes_naps) != 0) && sleepingZs == null)
        {
            sleepingZs = Instantiate(npcData.sleepingZs_prefab, transform.position, transform.rotation, transform);
            sleepingZs.Stop();
        }
        if (((stats.behaviours & Behaviours.Frequent_smoker) != 0) && smoke == null)
        {
            smoke = Instantiate(npcData.smoke_prefab, transform.position, transform.rotation, transform);
            smoke.Stop();
        }
        if (((stats.behaviours & Behaviours.Listens_to_music) != 0) && musicNotes == null)
        {
            musicNotes = Instantiate(npcData.musicNotes_prefab, transform.position, transform.rotation, transform);
            musicNotes.Stop();
        }
        if (((stats.behaviours & Behaviours.Lots_of_phone_calls) != 0) && speechBubble == null)
        {
            speechBubble = Instantiate(npcData.speechBubble_prefab, transform.position, transform.rotation, transform);
            speechBubble.Stop();
        }

        stats.curClip = atlasRenderer.renderInput.atlas.clipDict[(int)NPCMotion.StandingBreathing];
    }
    private void SetLayer()
    {
        rigidBody.includeLayers = layerSettings.stationMask;
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
        atlasRenderer.PlayClip(stats.curClip);

        if (rigidBody.includeLayers == layerSettings.trainMask)
        {
            stats.stateClock += Time.deltaTime;
        }

        if ((stats.curBehaviour & Behaviours.Frequent_smoker) != 0 && stats.curPath != Path.ToSmokerRoom)
        {
            FindSmokersRoom();
        }
        stats.targetDist = stats.curPath != Path.None ? stats.targetXPos - transform.position.x : 0;
        stats.move = Mathf.Abs(stats.targetDist) > 0.1f ? Mathf.Sign(stats.targetDist) : 0;
    }
    private void FixedUpdate()
    {
        FixedUpdateStates();
        stats.targetXVelocity = npc.moveSpeed  * stats.move;
        rigidBody.linearVelocityX = stats.targetXVelocity;
    }
    private void SelectingStates()
    {
        if (stats.move != 0)
        {
            SetState(NPCState.Walking);
        }
        else if (stats.curBehaviour == 0)
        {
            SetState(NPCState.Idling);
        }
        else if ((stats.curBehaviour & Behaviours.Frequent_smoker) != 0)
        {
            SetState(NPCState.Smoking);
        }
        else if ((stats.curBehaviour & Behaviours.Takes_naps) != 0)
        {
            SetState(NPCState.Sleeping);
        }
        else if ((stats.curBehaviour & Behaviours.Always_hungry) != 0)
        {
            SetState(NPCState.Eating);
        }
        else if ((stats.curBehaviour & Behaviours.Listens_to_music) != 0)
        {
            SetState(NPCState.Music);
        }
        else if ((stats.curBehaviour & Behaviours.Lots_of_phone_calls) != 0)
        {
            SetState(NPCState.Calling);
        }
        else if ((stats.curBehaviour & Behaviours.Enjoys_reading) != 0)
        {
            SetState(NPCState.Reading);
        }
    }
    private void UpdateStates()
    {
        switch (stats.curState)
        {
            case NPCState.Idling:
            {
                if (stats.stateClock > stats.stateDuration)
                {
                    stats.curBehaviour = PickBehaviour();
                }
            }
            break;
            case NPCState.Walking:
            {
                atlasRenderer.renderInput.FlipH(stats.move < 0, atlasRenderer.sprite);
            }
            break;
            case NPCState.Smoking:
            {
                SetMarkerPosition();
                smoke.transform.localPosition = stats.curSpriteMarkerLocalPosition;

                if (stats.stateClock > stats.stateDuration)
                {
                    stats.curBehaviour = PickBehaviour();
                }

            }
            break;
            case NPCState.Sleeping:
            {
                
                SetMarkerPosition();
                sleepingZs.transform.localPosition = stats.curSpriteMarkerLocalPosition;
                
                if (stats.stateClock > stats.stateDuration)
                {
                    stats.curBehaviour = PickBehaviour();
                }
            }
            break;
            case NPCState.Eating:
            {
                if (stats.stateClock > stats.stateDuration)
                {
                    stats.curBehaviour = PickBehaviour();
                }
            }
            break;
            case NPCState.Music:
            {

                SetMarkerPosition();
                musicNotes.transform.localPosition = stats.curSpriteMarkerLocalPosition;
                
                if (stats.stateClock > stats.stateDuration)
                {
                    stats.curBehaviour = PickBehaviour();
                }
            }
            break;
            case NPCState.Calling:
            {
                SetMarkerPosition();
                speechBubble.transform.localPosition = stats.curSpriteMarkerLocalPosition;

                if (stats.stateClock > stats.stateDuration)
                {
                    stats.curBehaviour = PickBehaviour();
                }
            }
            break;
            case NPCState.Reading:
            {
                if (stats.stateClock > stats.stateDuration)
                {
                    stats.curBehaviour = PickBehaviour();
                }
            }
            break;
        }
    }
    private void FixedUpdateStates()
    {
        switch (stats.curState)
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
        if (stats.curState == newState) return;
        ExitState();
        stats.curState = newState;
        stats.stateClock = 0;
        EnterState();
    }
    private void EnterState()
    {
        stats.atlasIndexClock = 0;
        stats.curFrameIndex = 0;

        if (stats.chairPosIndex != int.MaxValue)
        {
            atlasRenderer.renderInput.UpdateDepthRealtime(curCarriage.sittingDepth);
        }
        else if (curCarriage !=  null)
        {
            atlasRenderer.renderInput.UpdateDepthRealtime(curCarriage.standingDepth);
        }
        switch (stats.curState)
        {
            case NPCState.Idling:
            {
                stats.stateDuration = UnityEngine.Random.Range(npc.pickBehaviourDurationRange.x, npc.pickBehaviourDurationRange.y);

                if (stats.chairPosIndex != int.MaxValue)
                {
                    stats.curClip = RandomSittingIdleMotion();
                }
                else
                {
                    stats.curClip = RandomStandingIdleMotion();
                }
            }
            break;
            case NPCState.Walking:
            {
                stats.curClip = atlasRenderer.renderInput.atlas.clipDict[(int)NPCMotion.Walking];
            }
            break;
            case NPCState.Smoking:
            {
                stats.stateDuration = UnityEngine.Random.Range(npc.pickBehaviourDurationRange.x, npc.pickBehaviourDurationRange.y);
                stats.curClip = atlasRenderer.renderInput.atlas.clipDict[(int)NPCMotion.Smoking];
                smoke.Play();
            }
            break;
            case NPCState.Sleeping:
            {
                stats.stateDuration = UnityEngine.Random.Range(npc.pickBehaviourDurationRange.x, npc.pickBehaviourDurationRange.y);

                //sleepingZs.transform.position = new Vector3(meshRenderer.bounds.center.x, meshRenderer.bounds.max.y, transform.position.z - 0.5f);
                sleepingZs.Play();
                if (stats.chairPosIndex != int.MaxValue)
                {
                    stats.curClip = atlas.clipDict[(int)NPCMotion.SittingSleeping];
                }
                else
                {
                    stats.curClip = atlas.clipDict[(int)NPCMotion.StandingSleeping];
                }
            }
            break;
            case NPCState.Eating:
            {
                stats.stateDuration = UnityEngine.Random.Range(npc.pickBehaviourDurationRange.x, npc.pickBehaviourDurationRange.y);

                if (stats.chairPosIndex != int.MaxValue)
                {
                    stats.curClip = atlas.clipDict[(int)NPCMotion.SittingEating];
                }
                else
                {
                    stats.curClip = atlas.clipDict[(int)NPCMotion.StandingEating];
                }
            }
            break;
            case NPCState.Music:
            {
                stats.stateDuration = UnityEngine.Random.Range(npc.pickBehaviourDurationRange.x, npc.pickBehaviourDurationRange.y);

                musicNotes.Play();
                if (stats.chairPosIndex != int.MaxValue)
                {
                    stats.curClip = atlas.clipDict[(int)NPCMotion.SittingMusic];
                }
                else
                {
                    stats.curClip = atlas.clipDict[(int)NPCMotion.StandingMusic];
                }
            }
            break;
            case NPCState.Calling:
            {
                stats.stateDuration = UnityEngine.Random.Range(npc.pickBehaviourDurationRange.x, npc.pickBehaviourDurationRange.y);
                speechBubble.Play();
                if (stats.chairPosIndex != int.MaxValue)
                {
                    stats.curClip = atlas.clipDict[(int)NPCMotion.SittingCalling];
                    transform.position = new Vector3(transform.position.x, transform.position.y, curCarriage.sittingDepth);
                }
                else
                {
                    stats.curClip = atlas.clipDict[(int)NPCMotion.StandingCalling];
                }
            }
            break;
            case NPCState.Reading:
            {
                stats.stateDuration = UnityEngine.Random.Range(npc.pickBehaviourDurationRange.x, npc.pickBehaviourDurationRange.y);
                if (stats.chairPosIndex != int.MaxValue)
                {
                    stats.curClip = atlas.clipDict[(int)NPCMotion.SittingReading];
                    transform.position = new Vector3(transform.position.x, transform.position.y, curCarriage.sittingDepth);
                }
                else
                {

                    stats.curClip = atlas.clipDict[(int)NPCMotion.StandingReading];
                }
            }
            break;
        }
    }
    private void ExitState()
    {
        switch (stats.curState)
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
                curCarriage.smokersRoomData[stats.smokerRoomIndex].npcCount--;
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
            transform.position.x > spyStats.curCarriageMinXPos &&
            transform.position.x < spyStats.curCarriageMaxXPos;

        float targetAlpha = shouldFadeOut ? 0f : 1f;

        if (stats.targetAlpha == targetAlpha) return;
        
        stats.targetAlpha = targetAlpha;;
        ctsFade?.Cancel();
        ctsFade?.Dispose();

        ctsFade = new CancellationTokenSource();
        FadeTo().Forget();
    }
    private async UniTask FadeTo()
    {
        float startAlpha = stats.curAlpha;
        float elapsed = 0f;
        try
        {
            while (elapsed < npcData.fadeTime)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / npcData.fadeTime;

                stats.curAlpha = Mathf.Lerp(startAlpha, stats.targetAlpha, t);
                mpb.SetFloat(materialIDs.ids.alpha, stats.curAlpha);

                await UniTask.Yield(PlayerLoopTiming.Update, ctsFade.Token);
            }

            stats.curAlpha = stats.targetAlpha;
            mpb.SetFloat(materialIDs.ids.alpha, stats.curAlpha);
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
        await UniTask.WaitUntil(() => stats.curState != NPCState.Walking);

        RaycastHit2D slideDoorHit = Physics2D.BoxCast(boxCollider.bounds.center, boxCollider.bounds.size, 0.0f, transform.right, 0.0f, layerSettings.trainLayers.slideDoors);
        RaycastHit2D carriageHit = Physics2D.BoxCast(boxCollider.bounds.center, boxCollider.bounds.size, 0.0f, transform.right, 0.0f, layerSettings.trainLayers.insideCarriageBounds);

#if UNITY_EDITOR
        if (slideDoorHit.collider == null) { Debug.LogError($"{name} did not find a slide door to go to"); return; }
        if (carriageHit.collider == null) { Debug.LogError($"{name} did not find a slide door to go to"); return; }
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
        for (int i = 1; i < trainStats.slideDoorPositions.Length; i++)
        {
            float dist = Mathf.Abs(trainStats.slideDoorPositions[i] - transform.position.x);

            if (dist < shortestDist)
            {
                shortestDist = dist;
                selectedSlideDoorPos = trainStats.slideDoorPositions[i];
            }
        }
        stats.targetXPos = selectedSlideDoorPos;
        stats.curPath = Path.ToSlideDoor;
    }
    private void QueueForChair()
    {
        if (!NPCManager.npcChairList.Contains(this))
        {
            NPCManager.npcChairList.Add(this);
        }
    }
    public void AssignChair(uint chairIndex)
    {
        stats.chairPosIndex = chairIndex;
        curCarriage.chairData[stats.chairPosIndex].filled = true;
        stats.curPath = Path.ToChair;
        stats.targetXPos = curCarriage.chairData[stats.chairPosIndex].xPos;
    }
    public void FindStandingPosition()
    {
        stats.targetXPos = UnityEngine.Random.Range(curCarriage.insideBoundsCollider.bounds.min.x, curCarriage.insideBoundsCollider.bounds.max.x);
    }
    private void FindSmokersRoom()
    {
        if (stats.chairPosIndex != int.MaxValue && curCarriage.chairData[stats.chairPosIndex].filled)
        {
            curCarriage.chairData[stats.chairPosIndex].filled = false;
            stats.chairPosIndex = int.MaxValue;

        }
        if (NPCManager.npcChairList.Contains(this)) NPCManager.npcChairList.Remove(this); // To prevent them from going back to the chair if they are queued

        if (curCarriage.smokersRoomData.Length > 1 && curCarriage.smokersRoomData[1].npcCount < curCarriage.smokersRoomData[0].npcCount) // selected smoker room is based on which room has less npcs
        {
            stats.smokerRoomIndex = 1;
        }
        else
        {
            stats.smokerRoomIndex = 0;
        }
        curCarriage.smokersRoomData[stats.smokerRoomIndex].npcCount++;
        SetStandingDepth();
        stats.curPath = Path.ToSmokerRoom;
        stats.targetXPos = UnityEngine.Random.Range(curCarriage.smokersRoomData[stats.smokerRoomIndex].minXPos, curCarriage.smokersRoomData[stats.smokerRoomIndex].maxXPos);

    }
    private void SetStandingDepth()
    {
        int depth = UnityEngine.Random.Range(trainStats.depthSection_front_min, trainStats.depthSection_back_max);
        atlasRenderer.renderInput.UpdateDepthRealtime(depth);
    }

    private void SetMarkerPosition()
    {
        MarkerPosition[] curSpriteMarkers = stats.curClip.keyFrames[stats.curFrameIndex].motionSprite.markers;
        if (curSpriteMarkers.Length > 0)
        {
            stats.curSpriteMarkerLocalPosition.x = atlasRenderer.renderInput.flipX ? -curSpriteMarkers[0].objectPos.x : curSpriteMarkers[0].objectPos.x;
            stats.curSpriteMarkerLocalPosition.y = curSpriteMarkers[0].objectPos.y;
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
        int behaviourValue = (int)stats.behaviours;
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
#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;

        if (stats.curState == NPCState.Walking)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(boxCollider.bounds.center, new Vector2(stats.targetXPos, boxCollider.bounds.center.y));
        }

        Vector3 typeLabel = transform.position + atlasRenderer.sprite.worldSize + Vector3.up * 0.2f;
        Vector3 stateLabel = typeLabel + Vector3.up;

        GUIStyle typeStyle = new GUIStyle();
        typeStyle.normal.textColor = stats.role == Role.Traitor ? Color.red : Color.green;
        typeStyle.alignment = TextAnchor.UpperCenter;
        typeStyle.fontSize = 10;

        GUIStyle stateStyle = new GUIStyle();
        stateStyle.normal.textColor = stats.role == Role.Traitor ? Color.red : Color.green;
        stateStyle.alignment = TextAnchor.UpperCenter;
        stateStyle.fontSize = 10;

        // Draw the label in Scene view
        Handles.Label(typeLabel, stats.role.ToString(), typeStyle);
        Handles.Label(stateLabel, stats.curClip.ToString(), stateStyle);

    }
#endif

}
