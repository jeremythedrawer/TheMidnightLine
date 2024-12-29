using UnityEngine;

public abstract class State : MonoBehaviour
{
    public bool isComplete { get; protected set; }

    protected float startTime;

    public float time => Time.time - startTime;

    protected StateCore core;
    protected NPCCore npcCore;

    protected AnimStateStrings animStates => core.animStates;
    protected Rigidbody2D body => core.body;

    protected BoxCollider2D boxCollider2D => core.boxCollider2D;

    protected float initialGravityScale => core.initialGravityScale;

    protected Animator animator => core.animator;
    protected bool playingAnimation = false;

    protected CollisionChecker collisionChecker => core.collisionChecker;

    protected MovementInputs movementInputs => core.movementInputs;

    protected BehaviourParams navigationSystem => npcCore.behaviourParams;

    public StateMachine machine;
    public StateMachine parent;

    public State state => machine.state;

    protected void Set(State newState, bool forceReset = false)
    {
        machine.Set(newState, forceReset);
    }
    protected void PlayAnimation(string animationName)
    {
        animator.Play(animationName, 0, 0);
        playingAnimation = true;
    }

    public void SetCore(StateCore _core)
    {
        machine = new StateMachine();
        core = _core;
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
    }

    public void DoBranch()
    {
        Do();
        state?.DoBranch(); // finds leaf state by calling Do() on every branch
    }
    
    public void FixedDoBranch()
    {
        FixedDo();
        state?.FixedDoBranch(); // finds leaf state by calling Do() on every branch
    }
    public void Initialise(StateMachine _parent)
    {
        parent = _parent;
        isComplete = false;
        startTime = Time.time;
    }
}
