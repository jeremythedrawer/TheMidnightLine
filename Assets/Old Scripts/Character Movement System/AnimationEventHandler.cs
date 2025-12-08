using UnityEngine;

public class AnimationEventHandler : MonoBehaviour
{
    public MeleeState meleeState;
    public void InvokeDealDamage(int attackIndex)
    {
        meleeState.DealDamage(attackIndex);
    }
}

