using UnityEngine;

public class FallState : State
{
    public float coyoteTime = 0.2f;
    [HideInInspector] public float initialCoyoteTime;
    [HideInInspector] public bool coyoteEnabled;

    private void Awake()
    {
        initialCoyoteTime = coyoteTime;
    }
    public override void Enter()
    {
        coyoteEnabled = true;
    }
    public override void Do()
    {
        if (coyoteEnabled)
        {
            coyoteTime -= Time.deltaTime;

            if (coyoteTime <= 0)
            {
                coyoteEnabled = false;
            }
        }

        if (collisionChecker.grounded)
        {
            Exit();
        }
    }

    public override void Exit()
    {
        base.Exit();
        coyoteTime = initialCoyoteTime;
        coyoteEnabled = false;
    }
}
