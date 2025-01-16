using System.Collections.Generic;
using UnityEngine;

public class StateMachine
{
    public State state;
    public bool forceState { private get; set; } = true;

    public void Set(State newState)
    {
        if (state != newState || forceState == true)
        {
            state?.Exit();
            //Debug.Log("left state of " + state + " and entered into " + newState);
            state = newState;
            state.Initialise(this);
            state.Enter();

            forceState = false;
        }
    }

    public List<State> GetActiveStateBranch(List<State> list = null)
    {
        if (list == null) { list = new List<State>(); }

        if (state == null || state.machine == null)
        {
            return list;
        }
        else
        {
            list.Add(state);
            return state.machine.GetActiveStateBranch(list);
        }
    }
}
