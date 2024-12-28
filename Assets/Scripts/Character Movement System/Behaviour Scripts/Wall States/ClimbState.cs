using UnityEngine;

public class ClimbState : State
{
    public WallState wallState;

    public BoxCollider2D boxCollider;

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
            newPosY = cachePosY + boxCollider2D.bounds.size.y + (CarriageClimbingBounds.Instance.boxHeight - CarriageClimbingBounds.Instance.hangActivationThreshold); 
            checkPos = false;
        }

        if (core.currentAnimStateInfo.normalizedTime < 1f && !checkPos)
        {
            currentPos = new Vector2(newPosX, newPosY);
            core.transform.position = currentPos;
        }
    }

    private void UpdateBoxCollider()
    {

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
            playingAnimation = false;

            wallState.isClimbing = false;
            wallState.isDropping = true;
        }
    }



}
