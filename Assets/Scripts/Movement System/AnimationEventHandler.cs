using UnityEngine;

public class AnimationEventHandler : MonoBehaviour
{
    public MeleeCollider meleeCollider;
    public void InvokeDealDamage()
    {
        meleeCollider.DealDamage();
    }
}

