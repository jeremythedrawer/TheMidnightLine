using UnityEngine;
using UnityEngine.Windows;

public class IdleState : State
{
    //parent state
    public GroundState groundState;

    [Tooltip("How sticky the floor is")]
    [Range(0f, 1f)]
    public float groundDrag;


    //public AnimationClip anim;
    public override void Enter()
    {
        //animator.Play(anim.name);
    }
    public override void Do()
    {
    }
    public override void FixedDo()
    {
        if (collisionChecker.grounded && inputChecker.xInput == 0 && body.linearVelocityY <= 0)
        {
            body.linearVelocity *= groundDrag;
        }
        float velX = body.linearVelocityX;

        if (!collisionChecker.grounded || inputChecker.xInput != 0)
        {
            isComplete = true;
        }
    }
    public override void Exit()
    {

    }
}
