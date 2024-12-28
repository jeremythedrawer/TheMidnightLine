using UnityEngine;

public class ClimbState : State
{
    public WallState wallState;

    private string climbPullUpRightAnimation = "climbPullUpRight";
    private string climbPushUpRightAnimation = "climbPushUpRight";
    private string climbGetUpRightAnimation = "climbGetUpRight";

    private string climbRightAnimation = "climbRight";

    private float cachePosX;
    private float cachePosY;

    private float newPosX;
    private float newPosY;

    private Vector2 currentPos;
    private bool checkPos = true;
    public override void Enter()
    {

    }
    public override void Do()
    {

        AnimationController();
        UpdatePos();

    }

    public override void FixedDo()
    {

    }
    public override void Exit()
    {

    }

    private void UpdatePos()
    {
        if (checkPos)
        {
            cachePosX = core.transform.position.x;
            cachePosY = core.transform.position.y;

            newPosX = cachePosX + boxCollider2D.bounds.size.x;
            newPosY = cachePosY + boxCollider2D.bounds.size.y + CarriageClimbingBounds.Instance.boxHeight; 
            checkPos = false;
        }

        if (core.currentAnimStateInfo.normalizedTime < 1f && !checkPos)
        {
            float xPos = Mathf.Lerp(cachePosX, newPosX, core.currentAnimStateInfo.normalizedTime);
            float yPos = Mathf.Lerp(cachePosY, newPosY, core.currentAnimStateInfo.normalizedTime);
            currentPos = new Vector2(xPos, yPos);
            core.transform.position = currentPos;
        }
    }

    private void AnimationController()
    {
        if (!playingAnimation)
        {
            PlayAnimation(climbRightAnimation);
        }
        else if (core.currentAnimStateInfo.normalizedTime >= 1f)
        {
            isComplete = true;
            body.gravityScale = initialGravityScale;
            wallState.isClimbing = false;
        }
    }



}
