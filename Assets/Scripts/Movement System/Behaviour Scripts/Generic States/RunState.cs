using UnityEngine;

public class RunState : State
{
    public StateCore stateCore;
    public GroundState groundState;

    private string breathingRightAnimation = "idleBreathingRight";
    public override void Enter()
    {
    }
    public override void Do()
    {
        RunAnimationController();

        if (!collisionChecker.grounded || movementInputs.walkInput == 0)
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

    private void RunAnimationController()
    {
        //TODO Exclude bystanders
        // TODO implement right and left animation logic and also logic for symetrical animations (agents)
        if (stateCore.currentAnimStateInfo.normalizedTime >= 1)
        {
            animator.Play(breathingRightAnimation, 0, 0); //TODO replace with run animation
        }
    }
}
