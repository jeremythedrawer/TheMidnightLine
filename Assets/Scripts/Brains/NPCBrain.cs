using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.VFX;

public class NPCBrain : MonoBehaviour
{
    public enum State
    {
        Idle,
        Walk,
    }
    
    public enum Type
    {
        Agent,
        Bystander
    }
    [Serializable] public struct ComponentData
    {
        public Rigidbody2D rigidBody;
        public BoxCollider2D boxCollider;
        public Animator animator;
        public SpriteRenderer spriteRenderer;

        internal MaterialPropertyBlock mpb;
        internal SlideDoors slideDoors;
        internal CarriageChairs carriageChairs;
        internal VisualEffect sleepingZs;
    }
    [SerializeField] ComponentData componentData;

    [Serializable] public struct SOData
    {
        public NPCSO npc;
        public NPCsDataSO npcData;
        public LayerSettingsSO layerSettings;
        public TrainSettingsSO trainSettings;
        public TrainStatsSO trainStats;
        public GameEventDataSO gameEventData;
        public SpyInputsSO spyInputs;
        public ClipboardStatsSO clipboardStats;

        internal StationSO startStation;
        internal StationSO endStation;
    }
    [SerializeField] public SOData soData;

    [Serializable] public struct StatData
    {
        internal State curState;
        internal Type type;
        internal NPCTraits.Behaviours behaviours;
        internal Color selectedColor;

        internal bool canBoardTrain;
        internal float targetXVelocity;
        internal float curRunSpeed;
        internal float targetXPos;
        internal float targetDist;
    }
    [SerializeField] public StatData stats;

    [Serializable] public struct InputData
    {
        internal float move;
    }
    [SerializeField] InputData inputData;

    private void Awake()
    {
        SetAnimationEvents();
        componentData.mpb = new MaterialPropertyBlock();
    }
    private void OnEnable()
    {
        soData.gameEventData.OnUnlockSlideDoors.RegisterListener(() => stats.canBoardTrain = true);

        if (stats.behaviours == 0)
        {
            stats.behaviours = NPCTraits.GetBehaviours();
        }
        stats.behaviours |= NPCTraits.Behaviours.Takes_naps;
        if (((stats.behaviours & NPCTraits.Behaviours.Takes_naps) != 0) && componentData.sleepingZs == null)
        {
            componentData.sleepingZs = Instantiate(soData.npcData.sleepingZs, transform);
            componentData.sleepingZs.Stop();
        }

        stats.curState = State.Idle;
        componentData.rigidBody.includeLayers = soData.layerSettings.stationLayers.ground;
        stats.curRunSpeed = 1.0f;

        componentData.mpb.SetFloat(soData.npcData.materialData.zPosID, soData.trainSettings.entityDepthRange.x);
        componentData.spriteRenderer.SetPropertyBlock(componentData.mpb);

        stats.targetXPos = transform.position.x;
    }
    private void OnDisable()
    {
        soData.gameEventData.OnUnlockSlideDoors.UnregisterListener(() => stats.canBoardTrain = true);
    }
    private void Update()
    {
        SelectingStates();
        UpdateStates();

        componentData.mpb.SetTexture(soData.npcData.materialData.mainTexID, componentData.spriteRenderer.sprite.texture);
        componentData.spriteRenderer.SetPropertyBlock(componentData.mpb);

        stats.targetDist = stats.targetXPos - transform.position.x;
        inputData.move = Mathf.Abs(stats.targetDist) > 0.1f ? Mathf.Sign(stats.targetDist) : 0.0f;

    }
    private void FixedUpdate()
    {
        FixedUpdateStates();
        BoardTrain();

        stats.targetXVelocity = soData.npc.moveSpeed * stats.curRunSpeed * inputData.move;
        componentData.rigidBody.linearVelocityX = Mathf.Lerp(componentData.rigidBody.linearVelocityX, stats.targetXVelocity, soData.npc.groundAccelation * Time.fixedDeltaTime);
    }
    private void OnApplicationQuit()
    {
        ResetData();
    }
    private void SelectingStates()
    {
        if (inputData.move != 0)
        {
            SetState(State.Walk);
        }
        else
        {
            SetState(State.Idle);
        }
    }
    private void UpdateStates()
    {
        switch (stats.curState)
        {
            case State.Idle:
            {

            }
            break;
            case State.Walk:
            {
                componentData.spriteRenderer.flipX = inputData.move < 0;
            }
            break;
        }
    }
    private void FixedUpdateStates()
    {
        switch (stats.curState)
        {
            case State.Idle:
            {

            }
            break;
            case State.Walk:
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
        EnterState();

    }
    private void EnterState()
    {
        switch (stats.curState)
        {
            case State.Idle:
            {
                if (componentData.carriageChairs != null)
                {
                    componentData.animator.Play(soData.npcData.animHashData.sittingBreathing);
                }
                else
                {
                    componentData.animator.Play(soData.npcData.animHashData.standingBreathing);
                }
            }
            break;
            case State.Walk:
            {
                componentData.animator.Play(soData.npcData.animHashData.walking);
            }
            break;
        }
    }
    private void ExitState()
    {
        switch (stats.curState)
        {
            case State.Idle:
            {

            }
            break;
            case State.Walk:
            {
            }
            break;
        }

    }
    public void HoverColor()
    {
        if (componentData.rigidBody.includeLayers != soData.layerSettings.trainMask) return;

        if (!soData.clipboardStats.active || soData.clipboardStats.activePageIndex >= soData.clipboardStats.profilePageArray.Length)
        {
            componentData.mpb.SetColor(soData.npcData.materialData.colorID, Color.black + new Color(soData.npcData.hoverColorOffet, soData.npcData.hoverColorOffet, soData.npcData.hoverColorOffet, 0f));
        }
        else if (stats.selectedColor != soData.clipboardStats.profilePageArray[soData.clipboardStats.activePageIndex].color)
        {
            componentData.mpb.SetColor(soData.npcData.materialData.colorID, soData.clipboardStats.profilePageArray[soData.clipboardStats.activePageIndex].color * soData.npcData.hoverColorOffet);
        }
        componentData.spriteRenderer.SetPropertyBlock(componentData.mpb);
    }
    public void SelectColor()
    {
        if (componentData.rigidBody.includeLayers != soData.layerSettings.trainMask) return;

        if (!soData.clipboardStats.active || soData.clipboardStats.activePageIndex >= soData.clipboardStats.profilePageArray.Length)
        {
            stats.selectedColor = Color.black;
        }
        else if (stats.selectedColor != soData.clipboardStats.profilePageArray[soData.clipboardStats.activePageIndex].color)
        {
            stats.selectedColor = soData.clipboardStats.profilePageArray[soData.clipboardStats.activePageIndex].color;
        }
        componentData.mpb.SetColor(soData.npcData.materialData.colorID, stats.selectedColor);
        componentData.spriteRenderer.SetPropertyBlock(componentData.mpb);
    }
    public void ExitColor()
    {
        if (componentData.rigidBody.includeLayers != soData.layerSettings.trainMask) return;
        componentData.mpb.SetColor(soData.npcData.materialData.colorID, stats.selectedColor);
        componentData.spriteRenderer.SetPropertyBlock(componentData.mpb);
    }
    private void BoardTrain()
    {
        if (!stats.canBoardTrain) return; // only board train when you can board and npc is on the station ground

        if (componentData.rigidBody.includeLayers == soData.layerSettings.stationMask)
        {
            if (componentData.slideDoors == null) // find slide door in one frame
            {
                RaycastHit2D slideDoorHit = Physics2D.BoxCast(componentData.boxCollider.bounds.center, new Vector2(soData.npc.maxDistanceDetection, componentData.boxCollider.size.y), 0.0f, transform.right, soData.npc.maxDistanceDetection, soData.layerSettings.trainLayers.slideDoors);

                if (slideDoorHit.collider == null) { Debug.LogError($"{name} did not find a slide door to go to"); return; }

                SlideDoors selectedDoors = slideDoorHit.collider.GetComponent<SlideDoors>();
                stats.targetXPos = selectedDoors.transform.position.x;
                componentData.slideDoors = selectedDoors;

            }
            else if (inputData.move == 0.0f)
            {
                if (componentData.slideDoors.stats.curState == SlideDoors.State.Unlocked)
                {
                    componentData.slideDoors.OpenDoors();
                }
                else if (componentData.slideDoors.stats.curState == SlideDoors.State.Opened) // enter train when slide door is opened
                {
                    float zPos = UnityEngine.Random.Range(soData.trainSettings.entityDepthRange.x, soData.trainSettings.entityDepthRange.y);

                    componentData.mpb.SetFloat(soData.npcData.materialData.zPosID, zPos);
                    componentData.spriteRenderer.SetPropertyBlock(componentData.mpb);
                    componentData.rigidBody.includeLayers = soData.layerSettings.trainMask;                   

                    transform.position = new Vector3(transform.position.x, transform.position.y, zPos);
                    transform.SetParent(null, true);

                    soData.trainStats.curPassengerCount++;

                }
            }
        }
        else if (componentData.rigidBody.includeLayers == soData.layerSettings.trainMask) // now on train
        {
            if (!soData.npcData.boardingNPCQueue.Contains(this) && componentData.carriageChairs == null)
            {
                soData.npcData.boardingNPCQueue.Enqueue(this);
            }
        }
    }
    public void FindCarriageChair()
    {
        Vector2 boxCastSize = new Vector2(soData.npc.maxDistanceDetection * 2, componentData.boxCollider.bounds.size.y);
        RaycastHit2D insideBoundHit = Physics2D.BoxCast(componentData.boxCollider.bounds.center, componentData.boxCollider.bounds.size, 0.0f, transform.right, soData.npc.maxDistanceDetection, soData.layerSettings.trainLayers.insideCarriageBounds);
        RaycastHit2D carriageChairsHit = Physics2D.BoxCast(insideBoundHit.collider.bounds.center, insideBoundHit.collider.bounds.size, 0.0f, transform.right, soData.npc.maxDistanceDetection, soData.layerSettings.trainLayers.carriageChairs);

        CarriageChairs selectedChairs = carriageChairsHit.collider.GetComponent<CarriageChairs>();

        for (int i = 0; i < selectedChairs.chairData.Length; i++)
        {
            if(!selectedChairs.chairData[i].filled)
            {
                stats.targetXPos = selectedChairs.chairData[i].chairXPos;
                selectedChairs.chairData[i].filled = true;

                float zPos = selectedChairs.transform.position.z - 1;
                transform.position = new Vector3(transform.position.x, transform.position.y, zPos);
                componentData.mpb.SetFloat(soData.npcData.materialData.zPosID, zPos);
                componentData.spriteRenderer.SetPropertyBlock(componentData.mpb);

                break;
            }
        }
        componentData.carriageChairs = selectedChairs;
    }
    private void SetAnimationEvents()
    {
        Animations.SetAnimationEvent(soData.npc.animClipDict[soData.npcData.animHashData.standingAboutToEat],nameof(PlayStandingEatingAnimation));
        Animations.SetAnimationEvent(soData.npc.animClipDict[soData.npcData.animHashData.standingBreathing],nameof(PlayRandomStandingIdleAnimations));
        Animations.SetAnimationEvent(soData.npc.animClipDict[soData.npcData.animHashData.standingBlinking], nameof(PlayRandomStandingIdleAnimations));
        Animations.SetAnimationEvent(soData.npc.animClipDict[soData.npcData.animHashData.sittingBreathing], nameof(PlayRandomSittingIdleAnimations));
        Animations.SetAnimationEvent(soData.npc.animClipDict[soData.npcData.animHashData.sittingBlinking], nameof(PlayRandomSittingIdleAnimations));
    }
    private void PlayStandingEatingAnimation()
    {
        componentData.animator.Play(soData.npcData.animHashData.standingEating);
    }
    private void PlayRandomStandingIdleAnimations()
    {
        if(UnityEngine.Random.Range(0, 2) == 0)
        {
            componentData.animator.Play(soData.npcData.animHashData.standingBlinking);
        }
        else
        {
            componentData.animator.Play(soData.npcData.animHashData.standingBreathing);
        }
    }

    private void PlayRandomSittingIdleAnimations()
    {
        if (UnityEngine.Random.Range(0, 2) == 0)
        {
            componentData.animator.Play(soData.npcData.animHashData.sittingBlinking);
        }
        else
        {
            componentData.animator.Play(soData.npcData.animHashData.sittingBreathing);
        }
    }
    private void ResetData()
    {
        componentData.mpb.SetFloat(soData.npcData.materialData.zPosID, 0f);
        componentData.spriteRenderer.SetPropertyBlock(componentData.mpb);
    }
    private void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;
        Gizmos.color = Color.magenta;
        Gizmos.DrawLine(componentData.boxCollider.bounds.center, new Vector2(stats.targetXPos, componentData.boxCollider.bounds.center.y));

        Vector3 typeLabel = componentData.spriteRenderer.bounds.max + Vector3.up * 0.2f;
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
        Handles.Label(stateLabel, componentData.animator.GetCurrentAnimatorClipInfo(0)[0].clip.name.ToString(), stateStyle);

    }
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(componentData.boxCollider.bounds.center, new Vector2(soData.npc.maxDistanceDetection, componentData.boxCollider.bounds.extents.y));
    }

}
