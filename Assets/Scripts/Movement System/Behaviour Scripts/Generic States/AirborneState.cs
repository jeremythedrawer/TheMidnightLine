using UnityEngine;

public class AirborneState : State
{
    //child states
    public JumpedState jumpedState;
    public FallState fallState;

    [Tooltip("The apex threshold when the antiGravityApexScale is applied")]
    public float antiGravityApexThreshold = 2.0f;

    [Tooltip("Rigid body gravity scale when the anti-gravity apex has reached")]
    public float antiGravityApexScale = 2.0f;

    [Tooltip("Maximum falling speed")]
    public float clampedFallSpeed = 15.0f;

    private bool jumped;
    public override void Enter()
    {
        if (body.linearVelocityY > 0 || inputChecker.yInput != 0) jumped = true;
        //setting states
        if (jumped)
        {
            Set(jumpedState, true);
        }
        else
        {
            Set(fallState, true);
        }
    }
    public override void Do()
    {


        //clamp fall speed
        body.linearVelocityY = Mathf.Max(body.linearVelocityY, clampedFallSpeed * -1); //using max because Y velocity would be negative when falling

        //sticky feet on land
        if (body.linearVelocityY <= 0 && Mathf.Sign(inputChecker.xInput) != Mathf.Sign(body.linearVelocityX))
        {
            body.linearVelocityX = 0;
        }



        //complete state
        if (collisionChecker.grounded)
        {
            jumped = false;

            isComplete = true;
        }


    }

    public override void FixedDo()
    {

    }
    public override void Exit()
    {
    }
}
