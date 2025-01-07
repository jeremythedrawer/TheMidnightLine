using UnityEngine;

public abstract class State : MonoBehaviour
{
    public bool isComplete { get; protected set; } = true;
    public bool playingAnimation { get; set; } = false;

    protected StateCore core;
    protected NPCCore npcCore;

    protected Rigidbody2D body => core.body;
    protected BoxCollider2D boxCollider2D => core.boxCollider2D;
    protected Animator animator => core.animator;
    protected CollisionChecker collisionChecker => core.collisionChecker;
    protected MovementInputs movementInputs => core.movementInputs;
    protected StateList stateList => core.stateList;
    protected AnimStateStrings animStates => core.animStates;

    protected BehaviourParams behaviourParams => npcCore.behaviourParams;
    protected NavigationSystem navigationSystem => npcCore.navigationSystem;

    protected float initialGravityScale => core.initialGravityScale;


    public StateMachine machine;
    public StateMachine parent;

    public State state => machine.state;

    protected void Set(State newState, bool forceReset = false)
    {
        machine.Set(newState, forceReset);
    }
    protected void PlayAnimation(string animationName)
    {
        if (!playingAnimation)
        {
            animator.Play(animationName, 0, 0);
            playingAnimation = true;
        }
        if (core.currentAnimStateInfo.normalizedTime >= 1)
        {
            playingAnimation = false;
        }
    }

    public void SetCore(StateCore _core)
    {
        machine = new StateMachine();
        core = _core;
    }

    public void SetNpcCore(NPCCore _npcCore)
    {
        npcCore = _npcCore;
    }


    public virtual void Enter()
    { 
    }
    public virtual void Do()
    {

    }
    public virtual void FixedDo()
    {
    }
    public virtual void Exit()
    {
        isComplete = true;
        playingAnimation = false;
    }

    public void DoBranch()
    {
        if (!isComplete)
        {
            Do();
            state?.DoBranch(); // finds leaf state by calling Do() on every branch
        }
    }
    
    public void FixedDoBranch()
    {
        if (!isComplete)
        {
            FixedDo();
            state?.FixedDoBranch(); // finds leaf state by calling Do() on every branch
        }
    }
    public void Initialise(StateMachine _parent)
    {
        parent = _parent;
        isComplete = false;
    }
}
