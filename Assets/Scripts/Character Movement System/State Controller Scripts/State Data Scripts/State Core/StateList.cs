using UnityEngine;

public class StateList : MonoBehaviour
{
    public CharacterType characterType;
    public enum CharacterType
    {
        Player,
        Agent,
        Bystander
    }

    [HideInInspector] public CalmState calmState;
    [HideInInspector] public StalkState stalkState;
    [HideInInspector] public AttackState attackState;

    [HideInInspector] public PanicState panicState;
    [HideInInspector] public LeaveState leaveState;
    [HideInInspector] public FleeState fleeState;

    [HideInInspector] public SacrificeState sacrificeState;

    [HideInInspector] public CheckTicketState checkTicketState;

    [HideInInspector] public GroundState groundState;
    [HideInInspector] public AirborneState airborneState;
    [HideInInspector] public WallState wallState;

    [HideInInspector] public IdleState idleState;
    [HideInInspector] public RunState runState;
    [HideInInspector] public MeleeState meleeState;
    [HideInInspector] public ShootState shootState;

    [HideInInspector] public JumpedState jumpedState;
    [HideInInspector] public FallState fallState;
    [HideInInspector] public AirMeleeState airMeleeState;
    [HideInInspector] public AirShootState airShootState;

    [HideInInspector] public HangState hangState;
    [HideInInspector] public ClimbState climbState;

}

