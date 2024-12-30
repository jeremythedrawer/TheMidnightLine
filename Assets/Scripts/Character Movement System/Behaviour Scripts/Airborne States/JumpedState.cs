using UnityEngine;

public class JumpedState : State
{
    private int bhcFrameSmoothing = 2;
    private int frameSmoothingCurrent;


    public override void Enter()
    {
    }
    public override void Do()
    {

        //anti gravity apex
        if (Mathf.Abs(body.linearVelocityY) < core.stateList.airborneState.antiGravityApexThreshold && body.linearVelocityY != 0 && Input.GetButton("Jump"))
        {
            float lerpFactor = Mathf.InverseLerp(0, core.stateList.airborneState.antiGravityApexThreshold, Mathf.Abs(body.linearVelocityY));
            body.gravityScale = core.stateList.airborneState.antiGravityApexScale;
        }
        else
        {
            body.gravityScale = initialGravityScale;
        }

    }
    public override void FixedDo()
    {
        //bumped head correction
        if (collisionChecker.bhcEnabled)
        {
            frameSmoothingCurrent = 0;
            float currentOffsetX = collisionChecker.bhcRightEnabled ? -collisionChecker.characterBoxCollider.size.x :
                                   collisionChecker.bhcleftEnabled ? collisionChecker.characterBoxCollider.size.x :
                                   collisionChecker.characterBoxCollider.offset.x;

            collisionChecker.characterBoxCollider.offset = new Vector2(currentOffsetX, collisionChecker.characterBoxCollider.offset.y);
            Debug.Log("bhc enabled");
        }

        if (Mathf.Abs(collisionChecker.characterBoxCollider.offset.x) > 0.0001)
        {
            frameSmoothingCurrent++;
            float currentOffsetX = Mathf.Lerp(collisionChecker.characterBoxCollider.offset.x, 0, frameSmoothingCurrent / bhcFrameSmoothing);
            collisionChecker.characterBoxCollider.offset = new Vector2(currentOffsetX, 0.0f);
        }
    }
    public override void Exit()
    {
    }
}
