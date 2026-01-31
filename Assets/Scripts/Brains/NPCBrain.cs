using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;
using UnityEngine.VFX;
using static Atlas;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class NPCBrain : MonoBehaviour
{
    public enum State
    {
        Idling,
        Walking,
        Smoking,
        Sleeping,
        Eating,
        Music,
        Calling,
        Reading
    }
    
    public enum Type
    {
        Agent,
        Bystander
    }

    public enum Path
    {
        Nothing,
        ToSmokerRoom,
        ToChair,
        ToSlideDoor,
    }
    [SerializeField] AtlasSO atlas;
    [SerializeField] NPCsDataSO npcData;
    [SerializeField] LayerSettingsSO layerSettings;
    [SerializeField] TrainSettingsSO trainSettings;
    [SerializeField] TrainStatsSO trainStats;
    [SerializeField] GameEventDataSO gameEventData;
    [SerializeField] PlayerInputsSO spyInputs;
    [SerializeField] SpyStatsSO spyStats;
    [SerializeField] ClipboardStatsSO clipboardStats;
    [SerializeField] MaterialIDSO materialIDs;
    public Rigidbody2D rigidBody;
    public BoxCollider2D boxCollider;
    public Animator animator;
    public SpriteRenderer spriteRenderer;

    public SlideDoors curSlideDoors;
    public MaterialPropertyBlock mpb;
    public CancellationTokenSource ctsFade;
        
    public Carriage curCarriage;
    public VisualEffect sleepingZs;
    public VisualEffect musicNotes;
    public VisualEffect talkingLines;

    public GameObject smoke;
    public NPCSO npc;

    public StationSO startStation;
    public StationSO endStation;

    [Serializable] public struct StatData
    {
        public Color selectedColor;
        public float targetXVelocity;
        public float targetXPos;
        public float targetDist;
        public float stateTimer;
        public float stateDuration;
        public float curAlpha;
        public float targetAlpha;

        public NPCTraits.Behaviours curBehaviour;
        public NPCTraits.Behaviours behaviours;
        
        public State curState;
        public Path curPath;
        public Type type;

        public uint smokerRoomIndex;
        public uint chairPosIndex;
        public int selectedProfileIndex;

        public bool canBoardTrain;
        public bool startFade;

        public int curAtlasIndex;
        public Atlas.AtlasClip curClip;
    }
    [SerializeField] public StatData stats;

    [Serializable] public struct InputData
    {
        internal float move;
    }
    [SerializeField] InputData inputData;
    private void Awake()
    {
        mpb = new MaterialPropertyBlock();
        ctsFade = new CancellationTokenSource();

        stats.curState = State.Idling;
        stats.curPath = Path.Nothing;
        stats.chairPosIndex = int.MaxValue;
        stats.curAlpha = 1;
        stats.selectedProfileIndex = int.MaxValue;

        SetLayer().Forget();
        if (stats.behaviours == 0)
        {
            stats.behaviours = NPCTraits.GetBehaviours(npc.behaviours);
        }
    }
    private void OnEnable()
    {
        gameEventData.OnStationArrival.RegisterListener(() => stats.canBoardTrain = true);
    }

    private void Start()
    {
        mpb.SetFloat(materialIDs.ids.zPos, trainSettings.maxMinWorldZPos.postion);
        if (((stats.behaviours & NPCTraits.Behaviours.Takes_naps) != 0) && sleepingZs == null)
        {
            sleepingZs = Instantiate(npcData.sleepingZs, transform);
            sleepingZs.Stop();
        }
        if (((stats.behaviours & NPCTraits.Behaviours.Frequent_smoker) != 0) && smoke == null)
        {
            smoke = Instantiate(npcData.smoke, transform);
            smoke.SetActive(false);
            //TODO: Make smoke a visual effect
        }
        if (((stats.behaviours & NPCTraits.Behaviours.Listens_to_music) != 0) && musicNotes == null)
        {
            musicNotes = Instantiate(npcData.musicNotes, transform);
            musicNotes.Stop();
        }
        if (((stats.behaviours & NPCTraits.Behaviours.Lots_of_phone_calls) != 0) && talkingLines == null)
        {
            talkingLines = Instantiate(npcData.speechBubble, transform);
            talkingLines.Stop();
        }
    }
    private async UniTask SetLayer()
    {
        rigidBody.gravityScale = 0;
        while (layerSettings.stationMask == 0) await UniTask.Yield();
        rigidBody.includeLayers = layerSettings.stationMask;
        rigidBody.gravityScale = 1;

    }
    private void OnDisable()
    {
        gameEventData.OnStationArrival.UnregisterListener(() => stats.canBoardTrain = true);
    }
    private void Update()
    {
        mpb.SetFloat(materialIDs.ids.zPos, trainSettings.maxMinWorldZPos.postion);
        SelectingStates();
        UpdateStates();
        Fade();
        mpb.SetTexture(materialIDs.ids.mainTex, spriteRenderer.sprite.texture);
        spriteRenderer.SetPropertyBlock(mpb);

        if (rigidBody.includeLayers == layerSettings.trainMask)
        {
            stats.stateTimer += Time.deltaTime;
        }

        if ((stats.curBehaviour & NPCTraits.Behaviours.Frequent_smoker) != 0 && stats.curPath != Path.ToSmokerRoom)
        {
            FindSmokersRoom();
        }

    }
    private void FixedUpdate()
    {
        FixedUpdateStates();
        BoardTrain();
        stats.targetDist = stats.curPath != Path.Nothing ? stats.targetXPos - transform.position.x : 0;
        inputData.move = Mathf.Abs(stats.targetDist) > 0.1f ? Mathf.Sign(stats.targetDist) : 0;
        stats.targetXVelocity = npc.moveSpeed  * inputData.move;
        rigidBody.linearVelocityX = npc.moveSpeed  * inputData.move;
    }
    private void SelectingStates()
    {
        if (inputData.move != 0)
        {
            SetState(State.Walking);
        }
        else if (stats.curBehaviour == 0)
        {
            SetState(State.Idling);
        }
        else if ((stats.curBehaviour & NPCTraits.Behaviours.Frequent_smoker) != 0)
        {
            SetState(State.Smoking);
        }
        else if ((stats.curBehaviour & NPCTraits.Behaviours.Takes_naps) != 0)
        {
            SetState(State.Sleeping);
        }
        else if ((stats.curBehaviour & NPCTraits.Behaviours.Always_hungry) != 0)
        {
            SetState(State.Eating);
        }
        else if ((stats.curBehaviour & NPCTraits.Behaviours.Listens_to_music) != 0)
        {
            SetState(State.Music);
        }
        else if ((stats.curBehaviour & NPCTraits.Behaviours.Lots_of_phone_calls) != 0)
        {
            SetState(State.Calling);
        }
        else if ((stats.curBehaviour & NPCTraits.Behaviours.Enjoys_reading) != 0)
        {
            SetState(State.Reading);
        }
    }
    private void UpdateStates()
    {
        switch (stats.curState)
        {
            case State.Idling:
            {
                if (stats.stateTimer > stats.stateDuration)
                {
                    stats.curBehaviour = PickBehaviour();
                }
            }
            break;
            case State.Walking:
            {
                spriteRenderer.flipX = inputData.move < 0;
                //stats.curAtlasIndex = GetCurrentSpriteIndex(atlas.clipDict[NPCMotion.Walking], ClipType.Loop, atlas.framesPerSecond);
            }
            break;
            case State.Smoking:
            {
                if (stats.stateTimer > stats.stateDuration)
                {
                    stats.curBehaviour = PickBehaviour();
                }
            }
            break;
            case State.Sleeping:
            {
                
                if (stats.stateTimer > stats.stateDuration)
                {
                    stats.curBehaviour = PickBehaviour();
                }
            }
            break;
            case State.Eating:
            {
                if (stats.stateTimer > stats.stateDuration)
                {
                    stats.curBehaviour = PickBehaviour();
                }
            }
            break;
            case State.Music:
            {
                if (stats.stateTimer > stats.stateDuration)
                {
                    stats.curBehaviour = PickBehaviour();
                }
            }
            break;
            case State.Calling:
            {
                if (stats.stateTimer > stats.stateDuration)
                {
                    stats.curBehaviour = PickBehaviour();
                }
            }
            break;
            case State.Reading:
            {
                if (stats.stateTimer > stats.stateDuration)
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
            case State.Idling:
            {

            }
            break;
            case State.Walking:
            {
            }
            break;
        }
    }
    private void SetState(State newState)
    {
        if (stats.curState == newState) return;
        ExitState();
        stats.curState = newState;
        stats.stateTimer = 0;
        EnterState();
    }
    private void EnterState()
    {
        switch (stats.curState)
        {
            case State.Idling:
            {
                stats.stateDuration = UnityEngine.Random.Range(npc.pickBehaviourDurationRange.x, npc.pickBehaviourDurationRange.y);

                if (stats.chairPosIndex != int.MaxValue)
                {
                    animator.Play(npcData.animHashData.sittingBreathing);
                    transform.position = new Vector3(transform.position.x, transform.position.y, curCarriage.chairZPos);
                    mpb.SetFloat(materialIDs.ids.zPos, curCarriage.chairZPos);
                }
                else
                {
                    animator.Play(npcData.animHashData.standingBreathing);
                }
            }
            break;
            case State.Walking:
            {
                animator.speed = npc.moveSpeed;
                animator.Play(npcData.animHashData.walking);
            }
            break;
            case State.Smoking:
            {
                stats.stateDuration = UnityEngine.Random.Range(npc.pickBehaviourDurationRange.x, npc.pickBehaviourDurationRange.y);
                animator.Play(npcData.animHashData.smoking);
                smoke.SetActive(true);
            }
            break;
            case State.Sleeping:
            {
                stats.stateDuration = UnityEngine.Random.Range(npc.pickBehaviourDurationRange.x, npc.pickBehaviourDurationRange.y);
                sleepingZs.transform.position = new Vector3(spriteRenderer.bounds.center.x, spriteRenderer.bounds.max.y, transform.position.z - 0.5f);
                sleepingZs.Reinit();
                sleepingZs.Play();
                if (stats.chairPosIndex != int.MaxValue)
                {
                    animator.Play(npcData.animHashData.sittingSleeping);
                    transform.position = new Vector3(transform.position.x, transform.position.y, curCarriage.chairZPos);
                    mpb.SetFloat(materialIDs.ids.zPos, curCarriage.chairZPos);
                }
                else
                {
                    animator.Play(npcData.animHashData.standingSleeping);
                }
            }
            break;
            case State.Eating:
            {
                stats.stateDuration = UnityEngine.Random.Range(npc.pickBehaviourDurationRange.x, npc.pickBehaviourDurationRange.y);
                if (stats.chairPosIndex != int.MaxValue)
                {
                    animator.Play(npcData.animHashData.sittingAboutToEat);
                    transform.position = new Vector3(transform.position.x, transform.position.y, curCarriage.chairZPos);
                    mpb.SetFloat(materialIDs.ids.zPos, curCarriage.chairZPos);
                }
                else
                {
                    animator.Play(npcData.animHashData.standingAboutToEat);
                }
            }
            break;
            case State.Music:
            {
                stats.stateDuration = UnityEngine.Random.Range(npc.pickBehaviourDurationRange.x, npc.pickBehaviourDurationRange.y);
                musicNotes.transform.position = new Vector3(spriteRenderer.bounds.center.x, spriteRenderer.bounds.max.y, transform.position.z - 0.5f);
                musicNotes.Reinit();
                musicNotes.Play();
                if (stats.chairPosIndex != int.MaxValue)
                {
                    animator.Play(npcData.animHashData.sittingMusic);
                    transform.position = new Vector3(transform.position.x, transform.position.y, curCarriage.chairZPos);
                    mpb.SetFloat(materialIDs.ids.zPos, curCarriage.chairZPos);
                }
                else
                {
                    animator.Play(npcData.animHashData.standingMusic);
                }
            }
            break;
            case State.Calling:
            {
                stats.stateDuration = UnityEngine.Random.Range(npc.pickBehaviourDurationRange.x, npc.pickBehaviourDurationRange.y);
                talkingLines.transform.position = new Vector3(spriteRenderer.bounds.center.x, spriteRenderer.bounds.max.y, transform.position.z - 0.5f);
                talkingLines.Reinit();
                talkingLines.Play();
                if (stats.chairPosIndex != int.MaxValue)
                {
                    animator.Play(npcData.animHashData.sittingCalling);
                    transform.position = new Vector3(transform.position.x, transform.position.y, curCarriage.chairZPos);
                    mpb.SetFloat(materialIDs.ids.zPos, curCarriage.chairZPos);
                }
                else
                {
                    animator.Play(npcData.animHashData.standingCalling);
                }
            }
            break;
            case State.Reading:
            {
                stats.stateDuration = UnityEngine.Random.Range(npc.pickBehaviourDurationRange.x, npc.pickBehaviourDurationRange.y);
                if (stats.chairPosIndex != int.MaxValue)
                {
                    animator.Play(npcData.animHashData.sittingAboutToRead);
                    transform.position = new Vector3(transform.position.x, transform.position.y, curCarriage.chairZPos);
                    mpb.SetFloat(materialIDs.ids.zPos, curCarriage.chairZPos);
                }
                else
                {
                    animator.Play(npcData.animHashData.standingReading);
                }
            }
            break;


        }
    }
    private void ExitState()
    {
        switch (stats.curState)
        {
            case State.Idling:
            {

            }
            break;
            case State.Walking:
            {
                animator.speed = 1;
            }
            break;
            case State.Smoking:
            {
                smoke.SetActive(false);
                curCarriage.smokersRoomData[stats.smokerRoomIndex].npcCount--;
                QueueForChair();
            }
            break;
            case State.Sleeping:
            {
                sleepingZs.Stop();
            }
            break;
            case State.Music:
            {
                musicNotes.Stop();
            }
            break;
            case State.Calling:
            {
                talkingLines.Stop();
            }
            break;
        }

    }
    public void HoverColor()
    {
        if (rigidBody.includeLayers != layerSettings.trainMask) return;
        int curProfilePageIndex = clipboardStats.tempStats.curPageIndex - 1;

        if (!clipboardStats.tempStats.active || curProfilePageIndex >= clipboardStats.profilePageArray.Length || clipboardStats.tempStats.curPageIndex == 0)
        {
            mpb.SetColor(materialIDs.ids.color, Color.black + new Color(npcData.hoverColorOffet, npcData.hoverColorOffet, npcData.hoverColorOffet, 0f));
        }
        else if (stats.selectedColor != clipboardStats.profilePageArray[curProfilePageIndex].color)
        {
            mpb.SetColor(materialIDs.ids.color, clipboardStats.profilePageArray[curProfilePageIndex].color * npcData.hoverColorOffet);
        }
        spriteRenderer.SetPropertyBlock(mpb);
    }
    public void SelectColor()
    {
        if (rigidBody.includeLayers != layerSettings.trainMask) return;
        int curProfilePageIndex = clipboardStats.tempStats.curPageIndex - 1;
        if (!clipboardStats.tempStats.active || curProfilePageIndex >= clipboardStats.profilePageArray.Length || clipboardStats.tempStats.curPageIndex == 0)
        {
            stats.selectedColor = Color.black;

            if (stats.selectedProfileIndex == int.MaxValue) return;
            clipboardStats.profilePageArray[stats.selectedProfileIndex].spyHasSelected = false;
            stats.selectedProfileIndex = int.MaxValue;
        }
        else if (stats.selectedColor != clipboardStats.profilePageArray[curProfilePageIndex].color)
        {
            stats.selectedProfileIndex = curProfilePageIndex;

            stats.selectedColor = clipboardStats.profilePageArray[curProfilePageIndex].color;
            clipboardStats.profilePageArray[stats.selectedProfileIndex].spyHasSelected = true;
        }
        mpb.SetColor(materialIDs.ids.color, stats.selectedColor);
        spriteRenderer.SetPropertyBlock(mpb);
    }
    public void ExitColor()
    {
        if (rigidBody.includeLayers != layerSettings.trainMask) return;
        mpb.SetColor(materialIDs.ids.color, stats.selectedColor);
        spriteRenderer.SetPropertyBlock(mpb);
    }
    private void Fade()
    {
        bool shouldFadeOut =
            rigidBody.includeLayers == layerSettings.stationMask &&
            spyStats.curGroundLayer == layerSettings.trainLayerStruct.ground &&
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
        if (!stats.canBoardTrain || rigidBody.includeLayers == layerSettings.trainMask) return; // only board train when you can board and npc is on the station ground

        if (curCarriage == null) // find slide door in one frame
        {
            RaycastHit2D carriageHit = Physics2D.BoxCast(boxCollider.bounds.center, new Vector2(npc.maxDistanceDetection, boxCollider.size.y), 0.0f, transform.right, npc.maxDistanceDetection, layerSettings.trainLayerStruct.carriage);

            if (carriageHit.collider == null) { Debug.LogError($"{name} did not find a carriage to go to"); return; }

            curCarriage = carriageHit.collider.GetComponent<Carriage>();
        }
        else if (curSlideDoors == null)
        {
            float clostestSlideDoorDists = float.MaxValue;
            float npcXPos = transform.position.x;
            int bestIndex = -1;
            if (startStation.isFrontOfTrain)
            {
                for (int i = 0; i < curCarriage.exteriorSlideDoors.Length; i++)
                {
                    float dist = Mathf.Abs(npcXPos - curCarriage.exteriorSlideDoors[i].transform.position.x);
                    if (dist < clostestSlideDoorDists)
                    {
                        clostestSlideDoorDists = dist;
                        bestIndex = i;
                    }
                }
                if (bestIndex !=  -1)
                {
                    curSlideDoors = curCarriage.exteriorSlideDoors[bestIndex];
                }
            }
            else
            {
                for (int i = 0; i < curCarriage.interiorSlideDoors.Length; i++)
                {
                    float dist = Mathf.Abs(npcXPos - curCarriage.interiorSlideDoors[i].transform.position.x);
                    if (dist < clostestSlideDoorDists)
                    {
                        clostestSlideDoorDists = dist;
                        bestIndex = i;
                    }
                }
                if (bestIndex != -1)
                {
                    curSlideDoors = curCarriage.interiorSlideDoors[bestIndex];
                }
            }

            if (curSlideDoors == null)
            {
                Debug.LogError("No slide door found");
                return;
            }

            stats.curPath = Path.ToSlideDoor;
            stats.targetXPos = curSlideDoors.transform.position.x;
        }
        else if (inputData.move == 0.0f)
        {

            if (curSlideDoors.stats.curState == SlideDoors.State.Unlocked)
            {
                curSlideDoors.OpenDoors();
            }
            else if (curSlideDoors.stats.curState == SlideDoors.State.Opened) // enter train when slide door is opened
            {
                SetStandingDepthAndPosition();
                transform.SetParent(null, true);
                trainStats.curPassengerCount++;
                QueueForChair();
                rigidBody.includeLayers = layerSettings.trainMask;
            }
        }
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
        SetStandingDepthAndPosition();
        stats.curPath = Path.ToSmokerRoom;
        stats.targetXPos = UnityEngine.Random.Range(curCarriage.smokersRoomData[stats.smokerRoomIndex].minXPos, curCarriage.smokersRoomData[stats.smokerRoomIndex].maxXPos);

    }
    private void SetStandingDepthAndPosition()
    {
        float zPos = UnityEngine.Random.Range(trainSettings.maxMinWorldZPos.postion, trainSettings.maxMinWorldZPos.size);
        mpb.SetFloat(materialIDs.ids.zPos, zPos);
        transform.position = new Vector3(transform.position.x, transform.position.y, zPos);
    }
    private void PlaySittingEatingAnimation()
    {
        animator.Play(npcData.animHashData.sittingEating);
    }
    private void PlayStandingEatingAnimation()
    {
        animator.Play(npcData.animHashData.standingEating);
    }
    private void PlaySittingReadingAnimation()
    {
        animator.Play(npcData.animHashData.sittingReading);
    }
    private void PlayRandomStandingIdleAnimations()
    {
        if(UnityEngine.Random.Range(0, 2) == 0)
        {
            animator.Play(npcData.animHashData.standingBlinking);
        }
        else
        {
            animator.Play(npcData.animHashData.standingBreathing);
        }
    }
    private void PlayRandomSittingIdleAnimations()
    {
        if (UnityEngine.Random.Range(0, 2) == 0)
        {
            animator.Play(npcData.animHashData.sittingBlinking);
        }
        else
        {
            animator.Play(npcData.animHashData.sittingBreathing);
        }
    }
    //private void SetSmokePosition(int index)
    //{
    //    float xPos = spriteRenderer.flipX ? -npc.smokeAnimPosData[index].position.x : npc.smokeAnimPosData[index].position.x;
    //    smoke.transform.localPosition = new Vector3(xPos, npc.smokeAnimPosData[index].position.y, 0);
    //}
    private NPCTraits.Behaviours PickBehaviour()
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

        return (NPCTraits.Behaviours)chosenFlag;
    }
#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;
        Gizmos.color = Color.magenta;
        Gizmos.DrawLine(boxCollider.bounds.center, new Vector2(stats.targetXPos, boxCollider.bounds.center.y));

        Vector3 typeLabel = spriteRenderer.bounds.max + Vector3.up * 0.2f;
        Vector3 stateLabel = typeLabel + Vector3.up;

        GUIStyle typeStyle = new GUIStyle();
        typeStyle.normal.textColor = stats.type == Type.Agent ? Color.red : Color.green;
        typeStyle.alignment = TextAnchor.UpperCenter;
        typeStyle.fontSize = 10;

        GUIStyle stateStyle = new GUIStyle();
        stateStyle.normal.textColor = stats.type == Type.Agent ? Color.red : Color.green;
        stateStyle.alignment = TextAnchor.UpperCenter;
        stateStyle.fontSize = 10;

        // Draw the label in Scene view
        Handles.Label(typeLabel, stats.type.ToString(), typeStyle);
        Handles.Label(stateLabel, animator.GetCurrentAnimatorClipInfo(0)[0].clip.name.ToString(), stateStyle);

    }
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(boxCollider.bounds.center, new Vector2(npc.maxDistanceDetection, boxCollider.bounds.extents.y));
    }
#endif

}
