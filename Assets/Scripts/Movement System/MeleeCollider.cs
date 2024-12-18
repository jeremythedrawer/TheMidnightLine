using UnityEngine;
using System.Collections.Generic;
public class MeleeCollider : MonoBehaviour
{
    List<Collider2D> agentColliders = new();
    public BoxCollider2D meleeCollider;
    public void DealDamage()
    {
        foreach (Collider2D agent in agentColliders)
        {
            Debug.Log("Dealt damage to: " + agent.name);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag("Agent Collider")) return;
        if (agentColliders.Contains(collision)) return;
        agentColliders.Add(collision);
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (!collision.CompareTag("Agent Collider")) return;
        if (agentColliders.Contains(collision))
        {
            agentColliders.Remove(collision);
        }
    }
}
