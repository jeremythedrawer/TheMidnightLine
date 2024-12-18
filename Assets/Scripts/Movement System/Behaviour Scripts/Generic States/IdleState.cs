using UnityEngine;
using UnityEngine.Windows;

public class IdleState : State
{
    public StateCore stateCore;

    //parent state
    public GroundState groundState;

    private string breathingRightAnimation = "idleBreathingRight";


    //public AnimationClip anim;
    public override void Enter()
    {
        //animator.Play(anim.name);
    }
    public override void Do()
    {
        IdleAnimationController();

        if (!collisionChecker.grounded || movementInputs.walkInput != 0)
        {
            isComplete = true;
        }
    }
    public override void FixedDo()
    {
    }
    public override void Exit()
    {

    }

    private void IdleAnimationController()
    {
        if (stateCore is BystanderMovement) // idle animation is handeled in the behavioural states
        {
            return;
        }
        else if (stateCore.currentAnimStateInfo.normalizedTime >= 1)
        {
            animator.Play(breathingRightAnimation,0,0);
        }
        // TODO implement right and left animation logic and also logic for symetrical animations (agents)
    }

}
