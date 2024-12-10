using System.Collections.Generic;
using UnityEngine;

public abstract class StateCore : MonoBehaviour
{
    public Rigidbody2D body;

    public SpriteRenderer spriteRenderer;

    public Animator animator;

    public CollisionChecker collisionChecker;

    public InputChecker inputChecker;

    public CharacterStats characterStats;

    public StateMachine machine;

    public State state => machine.state;

    internal float initialGravityScale;

    protected void Set(State newState, bool forceReset = false)
    {
        machine.Set(newState, forceReset);
    }

    public void SetupInstances()
    {
        machine = new StateMachine();

        State[] allChildStates = GetComponentsInChildren<State>();

        if (allChildStates.Length == 0)
        {
            Debug.LogError("No child states found under " + gameObject.name);
        }
        foreach (State state in allChildStates)
        {
            state.SetCore(this);
        }
    }

    private void OnDrawGizmos()
    {
#if UNITY_EDITOR
        if (Application.isPlaying && state != null)
        {
            List<State> states = machine.GetActiveStateBranch();
            UnityEditor.Handles.Label(transform.position, "Active States: " + string.Join(" > ", states));
        }
#endif
    }
}
