using UnityEngine;

public class MeleeState : State
{
    //parent state
    public StateCore stateCore;
    public GroundState groundState;
    public MeleeColliderData meleeColliderData;

    public bool landedHit {  get; private set; }

    private int hitCombos = 0;

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
                animator.Play(animStates.groundMeleeAnimState, 0, 0);
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

        // TODO: randomly choose when to start at primary secondary or final attack //index 2 in Animation Event 
    }

    private void PlayComboAnimation()
    {
        if (!playingAnimation)
        {
            switch (hitCombos)
            {
                case 0:

                    PlayAnimation(animStates.stanceToPrimaryAnimState);

                    if (stateCore.currentAnimStateInfo.IsName(animStates.stanceToPrimaryAnimState) &&
                        stateCore.currentAnimStateInfo.normalizedTime >= 1f && hitCombos == 0) // missed hit
                    {
                        PlayAnimation(animStates.primaryHitToStanceAnimState);
                        hitCombos = -1;
                    }

                    break;

                case 1:
                    PlayAnimation(animStates.primaryHitToSecondaryAnimState);

                    if (stateCore.currentAnimStateInfo.IsName(animStates.primaryHitToSecondaryAnimState) && 
                        stateCore.currentAnimStateInfo.normalizedTime >= 1f && hitCombos == 1) // missed hit
                    {
                        PlayAnimation(animStates.secondaryHitToStanceAnimState);
                        hitCombos= -1;
                    }
                    break;

                case 2:
                    PlayAnimation(animStates.secondaryHitToFinalAnimState);
                    if (stateCore.currentAnimStateInfo.normalizedTime >= 1f)
                    {
                        hitCombos++;
                    }
                    break;

                case 3:
                    PlayAnimation(animStates.finalHitToStanceAnimState);
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
}
