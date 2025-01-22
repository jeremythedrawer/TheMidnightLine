using System.Collections;
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

    protected float initialGravityScale => core.initialGravityScale;


    public StateMachine machine;
    public StateMachine parent;

    public State state => machine.state;

    protected void Set(State newState)
    {
        machine.Set(newState);
    }

    protected void PlayAnimation(string animState)
    {
        if (!playingAnimation)
        {
            playingAnimation = true;
            StartCoroutine(PlayAnimationCoroutine(animState));
        }
    }
    protected IEnumerator PlayAnimationCoroutine(string animState)
    {
        animator.Play(animState, 0, 0);
        while (core.currentAnimStateInfo.normalizedTime < 1)
        {
            yield return null;
        }
        playingAnimation = false;
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
        machine.forceState = true;
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
