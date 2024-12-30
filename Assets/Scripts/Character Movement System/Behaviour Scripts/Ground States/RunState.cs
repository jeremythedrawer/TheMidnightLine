using System.Runtime.CompilerServices;
using UnityEngine;

public class RunState : State
{
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
                core.stateList.groundState.pendingState = false;
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
            animator.Play(animStates.startRunAnimState, 0, 0);
            startRunAnimation = true;
            startWalkAnimation = false;
        }
        else if (core.currentAnimStateInfo.normalizedTime >= 1)
        {
            animator.Play(animStates.runAnimState, 0, 0);
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
            animator.Play(animStates.walkAnimState, 0, 0);
            startWalkAnimation = true;
            startRunAnimation = false;
        }
    }
}
