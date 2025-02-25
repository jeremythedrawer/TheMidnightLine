using System.Collections.Generic;
using UnityEngine;

public abstract class StateCore : MonoBehaviour
{
    [Header("State Core Data")]
    public Rigidbody2D body;
    public SpriteRenderer spriteRenderer;
    public Animator animator;
    public BoxCollider2D boxCollider2D;
    public AnimatorStateInfo currentAnimStateInfo;

    [Header ("State Core References")]
    public CollisionChecker collisionChecker;
    public MovementInputs movementInputs;
    public CharacterStats characterStats;
    public HealthSystem healthSystem;
    public StateList stateList;
    public AnimStateStrings animStates;
    public StateMachine machine;
    public CharacterMaterial characterMaterial;

    public bool showStateList;

    public State state => machine.state;

    internal float initialGravityScale;

    public ClimbingBounds currentClimbBounds { get; set; }

    protected float normalizedAnimTime;
    public bool isDropping {  get; set; }
    private int trainGroundLayer => LayerMask.NameToLayer("Train Ground");
    public bool onTrain => collisionChecker.activeGroundLayer == 1 << trainGroundLayer;
    protected void Set(State newState)
    {
        machine.Set(newState);
    }

    public void SetupInstances()
    {
        machine = new StateMachine();
        animStates = new AnimStateStrings();

        State[] allChildStates = GetComponentsInChildren<State>();

        if (allChildStates.Length == 0)
        {
            Debug.LogError("No child states found under " + gameObject.name);
        }
        foreach (State state in allChildStates)
        {
            state.SetCore(this);
        }
    }

    private void OnDrawGizmos()
    {
        #if UNITY_EDITOR
        if (Application.isPlaying && state != null)
        {
            if (showStateList)
            {
                List<State> states = machine.GetActiveStateBranch();
                UnityEditor.Handles.Label(transform.position, "Active States: " + string.Join(" > ", states));
            }
        }
        #endif
    }
}
