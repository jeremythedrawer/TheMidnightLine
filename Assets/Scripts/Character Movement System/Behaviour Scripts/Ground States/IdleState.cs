using UnityEngine;
using UnityEngine.Windows;

public class IdleState : State
{
    public bool startAnimation { get; set; } = false;


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
            startAnimation = false;
            core.stateList.groundState.pendingState = false;
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
        if (core is not PlayerMovement) // idle animation is handeled in the behavioural states
        {
            return;
        }
        else if (core.currentAnimStateInfo.normalizedTime >= 1 || !startAnimation)
        {
            startAnimation = true;
            animator.Play(animStates.breathingAnimState,0,0);
        }
        // TODO implement right and left animation logic and also logic for symetrical animations (agents)
    }

}
