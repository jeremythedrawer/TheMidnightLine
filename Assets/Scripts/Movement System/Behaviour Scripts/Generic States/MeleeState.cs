using UnityEngine;

public class MeleeState : State
{
    //parent state
    public StateCore stateCore;
    public GroundState groundState;

    //child states
    public IdleState idleState;
    public bool landedHit {  get; private set; }

    private bool playingAnimation = false;

    private string groundRightAnimation = "groundMeleeRight";


    public override void Enter()
    {
    }
    public override void Do()
    {
        SuspendInputs();
        MeleeAnimationController();
        GivenDamage();
    }

    public override void FixedDo()
    {

    }
    public override void Exit()
    {

    }


    private void GivenDamage()
    {


        if (landedHit == false)
        {
            landedHit = true;
        }
        else
        {
            landedHit = false;
        }
        //TODO damage logic
    }

    private void MeleeAnimationController()
    {

        if (!playingAnimation)
        {
            animator.Play(groundRightAnimation, 0, 0);
            playingAnimation = true;
        }
        if (stateCore.currentAnimStateInfo.normalizedTime >= 1f)
        {
            playingAnimation = false;
            isComplete = true;
            groundState.isAttacking = false;
        }

        // TODO implement right and left animation logic and also logic for symetrical animations (agents)
    }

    private void SuspendInputs()
    {
        movementInputs.walkInput = 0;
        movementInputs.jumpInput = false;
        movementInputs.runInput = false;
    }
}
