using UnityEngine;

public class MeleeState : State
{
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
        if (core is PlayerMovement)
        {
            foreach (Collider2D agentCollider in meleeColliderData.agentColliders)
            {
                agentCollider.GetComponentInParent<HealthSystem>().TakeDamage(core.characterStats.meleeStrength);
            }  
        }
        if (core is AgentMovement)
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
        if (core is PlayerMovement)
        {
            if (!playingAnimation)
            {
                animator.Play(animStates.groundMeleeAnimState, 0, 0);
                movementInputs.canMove = false;
                playingAnimation = true;
            }
            if (core.currentAnimStateInfo.normalizedTime >= 1f)
            {
                playingAnimation = false;
                isComplete = true;
                core.stateList.groundState.isAttacking = false;
                movementInputs.canMove = true;
            }
            // TODO implement right and left animation logic and also logic for symetrical animations (agents)
        }
        else if (core is AgentMovement)
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

                    if (core.currentAnimStateInfo.IsName(animStates.stanceToPrimaryAnimState) &&
                        core.currentAnimStateInfo.normalizedTime >= 1f && hitCombos == 0) // missed hit
                    {
                        PlayAnimation(animStates.primaryHitToStanceAnimState);
                        hitCombos = -1;
                    }

                    break;

                case 1:
                    PlayAnimation(animStates.primaryHitToSecondaryAnimState);

                    if (core.currentAnimStateInfo.IsName(animStates.primaryHitToSecondaryAnimState) && 
                        core.currentAnimStateInfo.normalizedTime >= 1f && hitCombos == 1) // missed hit
                    {
                        PlayAnimation(animStates.secondaryHitToStanceAnimState);
                        hitCombos= -1;
                    }
                    break;

                case 2:
                    PlayAnimation(animStates.secondaryHitToFinalAnimState);
                    if (core.currentAnimStateInfo.normalizedTime >= 1f)
                    {
                        hitCombos++;
                    }
                    break;

                case 3:
                    PlayAnimation(animStates.finalHitToStanceAnimState);
                        hitCombos = -1;
                    break;

                case -1:
                    if (core.currentAnimStateInfo.normalizedTime >= 1f) // wait to get back to stance before leaving
                    {
                        playingAnimation = false;
                        core.stateList.groundState.isAttacking = false;
                        hitCombos = 0;
                        isComplete = true;
                    }

                    break;
            }

        }

        if (core.currentAnimStateInfo.normalizedTime >= 1f)
        {
            playingAnimation = false;
        }
    }
}
