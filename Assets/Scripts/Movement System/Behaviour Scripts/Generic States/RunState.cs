using UnityEngine;

public class RunState : State
{
    public GroundState groundState;
    //public AnimationClip anim;

    //public float maxXSpeed;
    public override void Enter()
    {
        //animator.Play(anim.name);
    }
    public override void Do()
    {

        //animator.speed = Helpers.Remap(maxXSpeed, 0, 1, 0, 1.6f, true);

        if (!collisionChecker.grounded || inputChecker.walkInput == 0)
        {
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
