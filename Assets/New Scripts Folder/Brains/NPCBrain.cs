using Proselyte.Sigils;
using System;
using UnityEngine;
using static SpyBrain;

public class NPCBrain : MonoBehaviour
{
    public enum State
    {
        Idle,
        Walk,
        Run,
        Dead,
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
        internal RuntimeAnimatorController animController;
        internal SlideDoors slideDoors;
        internal CarriageChairs carriageChairs;
    }
    [SerializeField] ComponentData componentData;

    [Serializable] public struct SOData
    {
        public NPCSettingsSO settings;
        public LayerSettingsSO layerSettings;
        public TrainSettingsSO trainSettings;
    }
    [SerializeField] SOData soData;
    [Serializable] public struct StateData
    {
        internal State curState;
        internal Type type;
    }
    [SerializeField] StateData stateData;

    [Serializable] public struct StatData
    {
        internal bool canBoardTrain;
        internal float targetXVelocity;
        internal float curRunSpeed;
        internal float targetXPos;
        internal float targetDist;
    }
    [SerializeField] StatData statData;

    [Serializable] public struct InputData
    {
        internal bool jump;
        internal float move;
        internal bool run;
    }
    [SerializeField] InputData inputData;

    [Serializable] public struct GameEventData
    {
        public GameEvent OnUnlockSlideDoors;
    }
    [SerializeField] GameEventData gameEventData;

    [Serializable] public struct AnimClipData
    {
        public AnimationClip startRunClip;

        internal int calmBreathingHash;
        internal int calmBlinkingHash;
        internal int sitBreathingHash;
        internal int sitBlinkingHash;
        internal int walkHash;
        internal int startRunHash;
        internal int runHash;
        internal int deadHash;
        internal int panicBreathingHash;
        internal int panicBlinkingHash;
    }
    [SerializeField] AnimClipData animClipData;

    [Serializable] public struct MaterialData
    {
        internal int colorID;
        internal int zPosID;
        internal int mainTexID;
    }
    [SerializeField] MaterialData materialData;
    private void Awake()
    {
        componentData.animController = componentData.animator.runtimeAnimatorController;
        componentData.mpb = new MaterialPropertyBlock();

        animClipData.calmBreathingHash = Animator.StringToHash("CalmBreathing");
        animClipData.calmBlinkingHash = Animator.StringToHash("CalmBlinking");
        animClipData.sitBreathingHash = Animator.StringToHash("SitBreathing");
        animClipData.sitBlinkingHash = Animator.StringToHash("SitBlinking");
        animClipData.walkHash = Animator.StringToHash("Walk");
        animClipData.startRunHash = Animator.StringToHash("StartRun");
        animClipData.runHash = Animator.StringToHash("Run");
        animClipData.deadHash = Animator.StringToHash("Dead");
        animClipData.panicBreathingHash = Animator.StringToHash("PanicBreathing");
        animClipData.panicBlinkingHash = Animator.StringToHash("PanicBlinking");

        materialData.colorID = Shader.PropertyToID("_Color");
        materialData.zPosID = Shader.PropertyToID("_ZPos");
        materialData.mainTexID = Shader.PropertyToID("_MainTex");

        BrainUtils.SetAnimationEvent(animClipData.startRunClip, nameof(PlayRunningClip));
    }
    private void OnEnable()
    {
        gameEventData.OnUnlockSlideDoors.RegisterListener(() => statData.canBoardTrain = true);
    }
    private void OnDisable()
    {
        gameEventData.OnUnlockSlideDoors.UnregisterListener(() => statData.canBoardTrain = true);
    }
    private void Start()
    {
        stateData.curState = State.Idle;
        componentData.rigidBody.includeLayers = soData.layerSettings.stationLayers.ground;
        statData.curRunSpeed = 1.0f;

        componentData.mpb.SetFloat(materialData.zPosID, soData.trainSettings.entityDepthRange.x);
        componentData.spriteRenderer.SetPropertyBlock(componentData.mpb);

        statData.targetXPos = transform.position.x;

    }
    private void Update()
    {
        SelectingStates();
        UpdateStates();
        componentData.mpb.SetTexture(materialData.mainTexID, componentData.spriteRenderer.sprite.texture);
        componentData.spriteRenderer.SetPropertyBlock(componentData.mpb);

        statData.targetDist = statData.targetXPos - transform.position.x;
        inputData.move = Mathf.Abs(statData.targetDist) > 0.1f ? Mathf.Sign(statData.targetDist) : 0.0f;
    }
    private void FixedUpdate()
    {
        FixedUpdateStates();
        BoardTrain();

        statData.targetXVelocity = soData.settings.moveSpeed * statData.curRunSpeed * inputData.move;
        componentData.rigidBody.linearVelocityX = Mathf.Lerp(componentData.rigidBody.linearVelocityX, statData.targetXVelocity, soData.settings.groundAccelation * Time.fixedDeltaTime);
    }
    private void SelectingStates()
    {
        if (inputData.move != 0 && !inputData.run)
        {
            SetState(State.Walk);
        }
        else if (inputData.move != 0 && inputData.run) 
        {
            SetState(State.Run);
        }
        else
        {
            SetState(State.Idle);
        }
    }
    private void UpdateStates()
    {
        switch (stateData.curState)
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
            case State.Run:
            {
                componentData.spriteRenderer.flipX = inputData.move < 0;
            }
            break;
        }
    }
    private void FixedUpdateStates()
    {
        switch (stateData.curState)
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
        if (stateData.curState == newState) return;
        ExitState();
        stateData.curState = newState;
        EnterState();

    }
    private void EnterState()
    {
        switch (stateData.curState)
        {
            case State.Idle:
            {
                if (componentData.carriageChairs != null)
                {
                    componentData.animator.Play(animClipData.sitBreathingHash);
                }
                else
                {
                    componentData.animator.Play(animClipData.calmBreathingHash);
                }
            }
            break;
            case State.Walk:
            {
                componentData.animator.Play(animClipData.walkHash);
            }
            break;
            case State.Run:
            {
                componentData.animator.Play(animClipData.startRunHash);
                statData.curRunSpeed = soData.settings.runSpeedMultiplier;
            }
            break;
        }
    }
    private void ExitState()
    {
        switch (stateData.curState)
        {
            case State.Idle:
            {

            }
            break;
            case State.Walk:
            {
            }
            break;
            case State.Run:
            {
                statData.curRunSpeed = 1.0f;
            }
            break;
        }

    }
    private void PlayRunningClip()
    {
        componentData.animator.Play(animClipData.runHash);
    }
    private void BoardTrain()
    {
        if (!statData.canBoardTrain) return; // only board train when you can board and npc is on the station ground

        if (componentData.rigidBody.includeLayers == soData.layerSettings.stationMask)
        {
            if (componentData.slideDoors == null) // find slide door in one frame
            {
                RaycastHit2D slideDoorHit = Physics2D.BoxCast(componentData.boxCollider.bounds.center, new Vector2(soData.settings.maxDistanceDetection, componentData.boxCollider.size.y), 0.0f, transform.right, soData.settings.maxDistanceDetection, soData.layerSettings.trainLayers.slideDoors);

                if (slideDoorHit.collider == null) { Debug.LogWarning($"{name} did not find a slide door to go to"); return; }

                SlideDoors selectedDoors = slideDoorHit.collider.GetComponent<SlideDoors>();
                statData.targetXPos = selectedDoors.transform.position.x;
                componentData.slideDoors = selectedDoors;

            }
            else if (inputData.move == 0.0f)
            {
                if (componentData.slideDoors.stateData.curState == SlideDoors.State.Unlocked)
                {
                    componentData.slideDoors.OpenDoors();
                }
                else if (componentData.slideDoors.stateData.curState == SlideDoors.State.Opened) // enter train when slide door is opened
                {
                    float zPos = UnityEngine.Random.Range(soData.trainSettings.entityDepthRange.x, soData.trainSettings.entityDepthRange.y);

                    componentData.mpb.SetFloat(materialData.zPosID, zPos);
                    componentData.spriteRenderer.SetPropertyBlock(componentData.mpb);

                    transform.position = new Vector3(transform.position.x, transform.position.y, zPos);
                    componentData.rigidBody.includeLayers = soData.layerSettings.trainMask;                   
                }
            }
        }
        else if (componentData.rigidBody.includeLayers == soData.layerSettings.trainMask)
        {
            if (!NPCManager.boardingNPCQueue.Contains(this) && componentData.carriageChairs == null)
            {
                NPCManager.boardingNPCQueue.Enqueue(this);
            }
        }
    }

    public void FindCarriageChair()
    {
        Vector2 boxCastSize = new Vector2(soData.settings.maxDistanceDetection * 2, componentData.boxCollider.bounds.size.y);
        RaycastHit2D carriageChairsHit = Physics2D.BoxCast(componentData.boxCollider.bounds.center, boxCastSize, 0.0f, transform.right, soData.settings.maxDistanceDetection, soData.layerSettings.trainLayers.carriageChairs);

        CarriageChairs selectedChairs = carriageChairsHit.collider.GetComponent<CarriageChairs>();

        for (int i = 0; i < selectedChairs.chairData.Length; i++)
        {
            if(!selectedChairs.chairData[i].filled)
            {
                statData.targetXPos = selectedChairs.chairData[i].chairXPos;
                selectedChairs.chairData[i].filled = true;

                float zPos = selectedChairs.transform.position.z - 1;
                transform.position = new Vector3(transform.position.x, transform.position.y, zPos);
                componentData.mpb.SetFloat(materialData.zPosID, zPos);
                componentData.spriteRenderer.SetPropertyBlock(componentData.mpb);

                break;
            }
        }
        componentData.carriageChairs = selectedChairs;
    }

    private void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;
        Gizmos.color = Color.magenta;
        Gizmos.DrawLine(componentData.boxCollider.bounds.center, new Vector2(statData.targetXPos, componentData.boxCollider.bounds.center.y));
    }
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(componentData.boxCollider.bounds.center, new Vector2(soData.settings.maxDistanceDetection, componentData.boxCollider.bounds.extents.y));
    }
}
