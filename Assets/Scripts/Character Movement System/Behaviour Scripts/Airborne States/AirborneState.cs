using UnityEngine;

public class AirborneState : State
{
    [Tooltip("The apex threshold when the antiGravityApexScale is applied")]
    public float antiGravityApexThreshold = 2.0f;

    [Tooltip("Rigid body gravity scale when the anti-gravity apex has reached")]
    public float antiGravityApexScale = 2.0f;

    [Tooltip("Maximum falling speed")]
    public float clampedFallSpeed = 20.0f;

    private bool jumped;
    public bool heavyLanding { get; set; } = false;


    private float jumpAnimationProgress = 0f; // Normalized time for animation
    private float fallAnimationProgress = 0f; // Normalized time for animation
    public override void Enter()
    {

    }
    public override void Do()
    {
        SelectState();
        ClampFallSpeed();
        StickyFeetOnLand();
        JumpAndFallAnimController();


        //complete state
        if (collisionChecker.grounded || CarriageClimbingBounds.Instance != null)
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

    private void SelectState()
    {
        if (body.linearVelocityY > 0 || movementInputs.jumpInput) jumped = true;

            if (jumped)
            {
                Set(core.stateList.jumpedState, true);
            }
            else
            {
                Set(core.stateList.fallState, true);
            }
    }
    private void ClampFallSpeed()
    {
        body.linearVelocityY = Mathf.Max(body.linearVelocityY, clampedFallSpeed * -1); //using max because Y velocity would be negative when falling
        if (body.linearVelocityY <= clampedFallSpeed * -1)
        {
            heavyLanding = true;
            core.stateList.groundState.finishedHeavyLanding = false;
        }
    }

    private void StickyFeetOnLand()
    {
        //sticky feet on land
        if (body.linearVelocityY <= 0 && Mathf.Sign(movementInputs.walkInput) != Mathf.Sign(body.linearVelocityX))
        {
            body.linearVelocityX = 0;
        }
    }

    private void JumpAndFallAnimController()
    {
        if (body.linearVelocityY > 0)
        {
            float velocityY = Mathf.Clamp(body.linearVelocityY, 0, core.characterStats.jumpSpeed);
            float t = Mathf.InverseLerp(core.characterStats.jumpSpeed, 0, velocityY);
            jumpAnimationProgress = Mathf.Lerp(0, 1, t);
            if (jumpAnimationProgress > 0.95) jumpAnimationProgress = 1f;
            animator.Play(animStates.jumpAnimState, 0, jumpAnimationProgress);
        }
        else
        {
            float t = Mathf.InverseLerp(0, clampedFallSpeed, Mathf.Abs(body.linearVelocityY));
            fallAnimationProgress = Mathf.Lerp(0,1, t);
            animator.Play(animStates.fallAnimState, 0, fallAnimationProgress);
        }
    }
}
