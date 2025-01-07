using System.Runtime.CompilerServices;
using UnityEngine;

public class RunState : State
{
    private bool startRunAnimation;
    public override void Enter()
    {
    }
    public override void Do()
    {
        if (movementInputs.runInput)
        {
            RunAnimationController();
        }
        else
        {
            WalkAnimationController();
        }

        if (!collisionChecker.grounded || Mathf.Abs(movementInputs.walkInput) < 1)
        {
            if (!movementInputs.adjustingCollider)
            {
                Exit();
            }
        }
    }

    public override void FixedDo()
    {

    }
    public override void Exit()
    {
        base.Exit();
        startRunAnimation = false;
    }

    private void RunAnimationController()
    {
        if (core is BystanderMovement) return;

        if (!startRunAnimation)
        {
            Debug.Log("starting to run");
            startRunAnimation = true;
            animator.Play(animStates.startRunAnimState, 0, 0);
        }
        else
        {
            Debug.Log("now in run cycle");
            PlayAnimation(animStates.runAnimState);
        }
    }

    private void WalkAnimationController()
    {
        if (core is BystanderMovement) return;
            PlayAnimation(animStates.walkAnimState);
    }
}
