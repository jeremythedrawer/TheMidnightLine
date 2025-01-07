using UnityEngine;
using UnityEngine.Windows;

public class IdleState : State
{
    public override void Enter()
    {
    }
    public override void Do()
    {
        IdleAnimationController();

        if (!collisionChecker.grounded || movementInputs.walkInput != 0)
        {
            Exit();
        }
    }
    public override void FixedDo()
    {
    }
    public override void Exit()
    {
        base.Exit();
    }

    private void IdleAnimationController()
    {
        if (core is not PlayerMovement) // idle animation is handeled in the behavioural states
        {
            return;
        }
        else
        {
            PlayAnimation(animStates.breathingAnimState);
        }
    }

}
