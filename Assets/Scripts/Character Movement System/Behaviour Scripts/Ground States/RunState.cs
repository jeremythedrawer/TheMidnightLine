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
        if (!startRunAnimation)
        {
            startRunAnimation = true;
            animator.Play(animStates.startRunAnimState, 0, 0);
        }
        else
        {
            PlayAnimation(animStates.runAnimState);
        }
    }

    private void WalkAnimationController()
    {
            PlayAnimation(animStates.walkAnimState);
    }
}
