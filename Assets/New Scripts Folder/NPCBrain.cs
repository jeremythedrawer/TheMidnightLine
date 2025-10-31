using Proselyte.Sigils;
using System;
using UnityEngine;

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
        internal RuntimeAnimatorController animController;
        internal SlideDoors slideDoors;
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
    }
    [SerializeField] StatData statData;

    [Serializable] public struct InputData
    {
        internal bool jump;
        internal int move;
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

    private void Awake()
    {
        componentData.animController = componentData.animator.runtimeAnimatorController;

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

        SetAnimationEvent(animClipData.startRunClip, nameof(PlayRunningClip));
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
        componentData.rigidBody.includeLayers = soData.layerSettings.stationGround;
        statData.curRunSpeed = 1.0f;
    }

    private void Update()
    {
        SelectingStates();
        UpdateStates();
    }

    private void FixedUpdate()
    {
        FixedUpdateStates();

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
                componentData.animator.Play(animClipData.calmBreathingHash);
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
    private void SetAnimationEvent(AnimationClip clip, string inputFunction, float? inputTime = null)
    {
        AnimationEvent animationEvent = new AnimationEvent
        {
            time = inputTime ?? clip.length,
            functionName = inputFunction
        };
        clip.AddEvent(animationEvent);
    }

    private void PlayRunningClip()
    {
        componentData.animator.Play(animClipData.runHash);
    }

    private void OpenSlideDoor()
    {
        if (componentData.rigidBody.includeLayers == soData.layerSettings.stationGround && statData.canBoardTrain)
        {
            RaycastHit2D slideDoorHit = Physics2D.BoxCast(componentData.boxCollider.bounds.center, componentData.boxCollider.bounds.extents, 0.0f, Vector2.zero, 0.0f, soData.layerSettings.slideDoors);

            if (slideDoorHit.collider != null)
            {
                componentData.slideDoors = slideDoorHit.collider.GetComponent<SlideDoors>();
                if (componentData.slideDoors != null && componentData.slideDoors.stateData.curState == SlideDoors.State.Unlocked)
                {
                    componentData.slideDoors.OpenDoors();
                }
            }
        }
    }

    private void EnterTrain()
    {
        if (componentData.rigidBody.includeLayers == soData.layerSettings.stationGround && statData.canBoardTrain)
        {
            RaycastHit2D slideDoorHit = Physics2D.BoxCast(componentData.boxCollider.bounds.center, componentData.boxCollider.bounds.extents, 0.0f, Vector2.zero, 0.0f, soData.layerSettings.slideDoors);

            if (slideDoorHit.collider != null)
            {
                componentData.slideDoors = slideDoorHit.collider.GetComponent<SlideDoors>();
                if (componentData.slideDoors != null && componentData.slideDoors.stateData.curState == SlideDoors.State.Opened)
                {
                    componentData.rigidBody.includeLayers = soData.layerSettings.trainGround;
                    transform.position = new Vector3(transform.position.x, transform.position.y, soData.trainSettings.entityDepthRange.x - soData.settings.depthPositionInTrain);
                }
            }
        }
    }
}
