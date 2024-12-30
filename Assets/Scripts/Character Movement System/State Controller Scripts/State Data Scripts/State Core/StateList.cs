using UnityEngine;

public class StateList : MonoBehaviour
{
    [Header("States to use")]
    public bool airborne;
     public bool jumped;
     public bool fall;
     public bool airMelee;
     public bool airShoot;
           
     public bool ground;
     public bool idle;
     public bool run;
     public bool melee;
     public bool shoot;
           
     public bool wall;
     public bool hang;
     public bool climb;
           
     public bool attack;
     public bool hiding;
     public bool stalk;
           
     public bool calm;
     public bool flee;
     public bool leave;
     public bool panic;
           
     public bool sacrifice;
           
     public bool checkTicket;

    [HideInInspector] public AirborneState airborneState;
    [HideInInspector] public JumpedState jumpedState;
    [HideInInspector] public FallState fallState;
    [HideInInspector] public AirMeleeState airMeleeState;
    [HideInInspector] public AirShootState airShootState;

    [HideInInspector] public GroundState groundState;
    [HideInInspector] public IdleState idleState;
    [HideInInspector] public RunState runState;
    [HideInInspector] public MeleeState meleeState;
    [HideInInspector] public ShootState shootState;

    [HideInInspector] public WallState wallState;
    [HideInInspector] public HangState hangState;
    [HideInInspector] public ClimbState climbState;

    [HideInInspector] public AttackState attackState;
    [HideInInspector] public HidingState hidingState;
    [HideInInspector] public StalkState stalkState;

    [HideInInspector] public CalmState calmState;
    [HideInInspector] public FleeState fleeState;
    [HideInInspector] public LeaveState leaveState;
    [HideInInspector] public PanicState panicState;

    [HideInInspector] public SacrificeState sacrificeState;

    [HideInInspector] public CheckTicketState checkTicketState;
}

