using System.Runtime.CompilerServices;
using UnityEngine;

public class PanicState : State
{
    //parent state
    public BystanderMovement bystanderMovement;

    public float runSpeedMultiplier;
    public float walkSpeedMultiplier;

    public LayerMask playerLayer;
    public float avoidPlayerDistance = 0.5f;
    public override void Enter()
    {
    }
    public override void Do()
    {
        AvoidPlayer();
        if (bystanderMovement.isCalm)
        {
            isComplete = true;
        }

        SelectState();
    }
    public override void FixedDo()
    {
    }
    public override void Exit()
    {
    }
    private void SelectState()
    {
        Set(core.stateList.groundState, true);
    }
    private void AvoidPlayer()
    {
        Vector2 currentPos = transform.position;
        Vector2 currentPlayerPos = bystanderMovement.player.transform.position;
        Vector2 directionToPlayer = (currentPlayerPos - currentPos).normalized;
        Vector2 thresholdPos = currentPos + directionToPlayer * avoidPlayerDistance;

        RaycastHit2D playerHit = Physics2D.Linecast(currentPos, thresholdPos, playerLayer);

        if (playerHit.collider != null)
        {
            if (currentPlayerPos.x > currentPos.x)
            {
                movementInputs.walkInput = -1;
            }
            else
            {
                movementInputs.walkInput = 1;
            }
        }
        else
        {
            movementInputs.walkInput = 0;
        }

        #if UNITY_EDITOR
        Debug.DrawLine(currentPos, thresholdPos, playerHit ? Color.green : Color.red);
        #endif

    }
}
