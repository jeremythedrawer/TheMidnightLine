using UnityEngine;

public class MeleeState : State
{
    //parent state
    public StateCore stateCore;
    public GroundState groundState;
    public MeleeColliderData meleeColliderData;

    public bool landedHit {  get; private set; }

    private bool playingAnimation = false;
    private int hitCombos = 0;

    //player animation state names
    private string groundRightAnimation = "groundMeleeRight";

    //agent animation state names
    private string stanceToPrimaryHitAnimation = "stanceToPrimaryHit"; //index 1 in Animation Event
    private string primaryHitToSecondaryHitAnimation = "primaryHitToSecondaryHit"; //index 2 in Animation Event
    private string secondaryHitToFinalHitAnimation = "secondaryHitToFinalHit";
    private string finalHitToStanceAnimation = "finalHitToStance"; //index 3 in Animation Event

    private string primaryHitToStanceAnimation = "primaryHitToStance";
    private string secondaryHitToStanceAnimation = "secondaryHitToStance";
    private string stanceToSecondaryHitAnimation = "stanceToSecondaryHit"; // TODO: randomly choose when to start at primary secondary or final attack //index 2 in Animation Event 


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


    public void DealDamage(int attackIndex)
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
                hitCombos++;
                meleeColliderData.playerCollider.GetComponentInParent<HealthSystem>().TakeDamage(core.characterStats.meleeStrength);
            }
            else
            {
            }
        }
    }

    private void MeleeAnimationController()
    {
        if (stateCore is PlayerMovement)
        {
            if (!playingAnimation)
            {
                animator.Play(groundRightAnimation, 0, 0);
                movementInputs.canMove = false;
                playingAnimation = true;
            }
            if (stateCore.currentAnimStateInfo.normalizedTime >= 1f)
            {
                playingAnimation = false;
                isComplete = true;
                groundState.isAttacking = false;
                movementInputs.canMove = true;
            }
            // TODO implement right and left animation logic and also logic for symetrical animations (agents)
        }
        else if (stateCore is AgentMovement)
        {
            PlayComboAnimation();
        }
    }

    private void PlayComboAnimation()
    {
        if (!playingAnimation)
        {
            switch (hitCombos)
            {
                case 0:

                    PlayAnimation(stanceToPrimaryHitAnimation);

                    if (stateCore.currentAnimStateInfo.IsName(stanceToPrimaryHitAnimation) &&
                        stateCore.currentAnimStateInfo.normalizedTime >= 1f && hitCombos == 0) // missed hit
                    {
                        PlayAnimation(primaryHitToStanceAnimation);
                        hitCombos = -1;
                    }

                    break;

                case 1:
                    PlayAnimation(primaryHitToSecondaryHitAnimation);

                    if (stateCore.currentAnimStateInfo.IsName(primaryHitToSecondaryHitAnimation) && 
                        stateCore.currentAnimStateInfo.normalizedTime >= 1f && hitCombos == 1) // missed hit
                    {
                        PlayAnimation(secondaryHitToStanceAnimation);
                        hitCombos= -1;
                    }
                    break;

                case 2:
                    PlayAnimation(secondaryHitToFinalHitAnimation);
                    if (stateCore.currentAnimStateInfo.normalizedTime >= 1f)
                    {
                        hitCombos++;
                    }
                    break;

                case 3:
                    PlayAnimation(finalHitToStanceAnimation);
                        hitCombos = -1;
                    break;

                case -1:
                    if (stateCore.currentAnimStateInfo.normalizedTime >= 1f) // wait to get back to stance before leaving
                    {
                        playingAnimation = false;
                        groundState.isAttacking = false;
                        hitCombos = 0;
                        isComplete = true;
                    }

                    break;
            }

        }

        if (stateCore.currentAnimStateInfo.normalizedTime >= 1f)
        {
            playingAnimation = false;
        }
    }

    private void PlayAnimation(string animationName)
    {
        animator.Play(animationName, 0, 0);
        playingAnimation = true;
    }
}
