using System.Runtime.CompilerServices;
using UnityEngine;

public class RunState : State
{
    public GroundState groundState;

    private string runRightAnimation = "runRight";
    private string startRunRightAnimation = "startRunRight";

    private string walkRightAnimation = "walkRight";
    public bool startRunAnimation { get; set; } = false;
    public bool startWalkAnimation { get; set; } = false;
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
                isComplete = true;
                startRunAnimation = false;
                startWalkAnimation = false;
                groundState.pendingState = false;
            }
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
        // TODO implement right and left animation logic and also logic for symetrical animations (agents)

        if (core is BystanderMovement) // run animation is handeled in the behavioural states in bystanders
        {
            return;
        }
        else if (!startRunAnimation)
        {
            animator.Play(startRunRightAnimation, 0, 0);
            startRunAnimation = true;
            startWalkAnimation = false;
        }
        else if (core.currentAnimStateInfo.normalizedTime >= 1)
        {
            animator.Play(runRightAnimation, 0, 0);
        }
    }

    private void WalkAnimationController()
    {
        // TODO implement right and left animation logic and also logic for symetrical animations (agents)

        if (core is BystanderMovement) // run animation is handeled in the behavioural states in bystanders
        {
            return;
        }
        else if (!startWalkAnimation || core.currentAnimStateInfo.normalizedTime >= 1)
        {
            animator.Play(walkRightAnimation, 0, 0);
            startWalkAnimation = true;
            startRunAnimation = false;
        }
    }
}
