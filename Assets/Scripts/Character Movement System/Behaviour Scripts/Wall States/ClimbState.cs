using UnityEditor.ShaderGraph.Internal;
using UnityEngine;

public class ClimbState : State
{
    private float cachePosX;
    private float cachePosY;

    private float newPosX;
    private float newPosY;

    private float offsetX;
    private float offsetY;

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
        base.Exit();
        boxCollider2D.offset = Vector2.zero;
        currentPos = new Vector2(cachePosX, cachePosY);
        checkPos = true;
        body.linearVelocityY = 0f;
        offsetX = 0f; offsetY = 0f;

    }

    private void UpdatePos()
    {
        if (core.currentClimbBounds == null) return;

        if (checkPos) //find next position
        {
            Debug.Log("checking new pos");
            cachePosX = core.transform.position.x;
            cachePosY = core.transform.position.y;
            if (core.currentClimbBounds.isLeftEdge)
            {
                offsetX = boxCollider2D.bounds.size.x;
            }
            else
            {
                offsetX = -boxCollider2D.bounds.size.x;
            }

            newPosX = cachePosX + offsetX;
            offsetY = core.currentClimbBounds.boundsMaxY - transform.position.y;

            newPosY = cachePosY + offsetY; 
            checkPos = false;
        }
        else //move to next position
        {
            if (movementInputs.crouchInput)
            {
                Exit();
            }
            else
            {
                UpdateBoxCollider(-offsetX,-offsetY);
                currentPos = new Vector2(newPosX, newPosY);
            }
            core.transform.position = currentPos;

            if (core.currentAnimStateInfo.normalizedTime >= 1f)
            {
                Exit();
            }
        }
    }

    private void UpdateBoxCollider(float offsetX, float offsetY)
    {
        Vector2 offset = new Vector2(offsetX, offsetY);

        boxCollider2D.offset = Vector2.Lerp(offset, Vector2.zero, core.currentAnimStateInfo.normalizedTime);
    }

    private void AnimationController()
    {
        if (!playingAnimation)
        {
            PlayAnimation(animStates.climbAnimState);
        }
    }
}
