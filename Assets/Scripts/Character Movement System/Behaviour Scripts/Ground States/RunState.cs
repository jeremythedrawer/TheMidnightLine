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
        AnimationController();

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

    private void AnimationController()
    {
        if (core.stateList.airborneState != null && core.stateList.airborneState.isHeavyLanding) return;

        if (movementInputs.runInput)
        {
            RunAnimationController();
        }
        else
        {
            WalkAnimationController();
        }
    }
    private void RunAnimationController()
    {
        if (!startRunAnimation)
        {
            startRunAnimation = true;
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
