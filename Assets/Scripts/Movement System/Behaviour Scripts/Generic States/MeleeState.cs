using UnityEngine;

public class MeleeState : State
{
    //parent state
    public StateCore stateCore;
    public GroundState groundState;
    public MeleeColliderData meleeColliderData;

    //child states
    public IdleState idleState;
    public bool landedHit {  get; private set; }

    private bool playingAnimation = false;

    private string groundRightAnimation = "groundMeleeRight";


    public override void Enter()
    {

    }
    public override void Do()
    {
        MeleeAnimationController();
    }

    public override void FixedDo()
    {

    }
    public override void Exit()
    {

    }


    public void DealDamage()
    {
        if (stateCore is PlayerMovement)
        {
            foreach (Collider2D agentCollider in meleeColliderData.agentColliders)
            {
                agentCollider.GetComponentInParent<HealthSystem>().TakeDamage(core.characterStats.meleeStrength);
            }  
        }
        if (stateCore is AgentMovement)
        {
            if (meleeColliderData.hitPlayerCollider)
            {
                meleeColliderData.playerCollider.GetComponentInParent<HealthSystem>().TakeDamage(core.characterStats.meleeStrength);
            }
        }
    }

    private void MeleeAnimationController()
    {

        if (!playingAnimation)
        {
            animator.Play(groundRightAnimation, 0, 0);
            playingAnimation = true;
        }
        if (stateCore.currentAnimStateInfo.normalizedTime >= 1f)
        {
            playingAnimation = false;
            isComplete = true;
            groundState.isAttacking = false;
        }

        // TODO implement right and left animation logic and also logic for symetrical animations (agents)
    }
}
