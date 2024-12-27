using UnityEngine;

public class ClimbState : State
{
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

        ClimbingController();

    }

    public override void FixedDo()
    {

    }
    public override void Exit()
    {

    }

    private void ClimbingController()
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
                    isComplete = true;
                    body.gravityScale = initialGravityScale;
                    wallState.isClimbing = false;
                    animationClipInt = 0;
                    break;

            }

        }

        if (playingAnimation && core.currentAnimStateInfo.IsName(GetCurrentAnimationName()))
        {
            if (core.currentAnimStateInfo.normalizedTime >= 1f)
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
