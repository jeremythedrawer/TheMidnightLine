using UnityEngine;

public class ClimbState : State
{
    public StateCore stateCore;
    public WallState wallState;
    private string climbPullUpRightAnimation = "climbPullUpRight";
    private string climbPushUpRightAnimation = "climbPushUpRight";
    private string climbGetUpRightAnimation = "climbGetUpRight";

    private int animationClipInt = 0;
    public override void Enter()
    {

    }
    public override void Do()
    {
        AnimationController();
        if (!wallState.isClimbing)
        {
            isComplete = true;
            body.gravityScale = initialGravityScale;

        }
        else
        {
        }

    }

    public override void FixedDo()
    {

    }
    public override void Exit()
    {

    }

    private void AnimationController()
    {
        if (!playingAnimation)
        {
            switch (animationClipInt)
            {
                case 0:

                    PlayAnimation(climbPullUpRightAnimation);
                    break;

                case 1:
                    PlayAnimation(climbPushUpRightAnimation);
                    break;

                case 2:
                    PlayAnimation(climbGetUpRightAnimation);
                    break;

                case 3:
                    wallState.isClimbing = false;
                    break;

            }

        }

        if (playingAnimation && stateCore.currentAnimStateInfo.IsName(GetCurrentAnimationName()))
        {
            if (stateCore.currentAnimStateInfo.normalizedTime >= 1f)
            {
                animationClipInt++;
                playingAnimation = false;
            }
        }
    }

    private string GetCurrentAnimationName()
    {
        return animationClipInt switch
        {
            0 => climbPullUpRightAnimation,
            1 => climbPushUpRightAnimation,
            2 => climbGetUpRightAnimation,
            _ => string.Empty
        };
    }
}
