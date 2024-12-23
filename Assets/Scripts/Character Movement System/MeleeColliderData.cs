using UnityEngine;
using System.Collections.Generic;
public class MeleeColliderData : MonoBehaviour
{
    public bool meleeColliderIsPlayer;
    public Collider2D playerCollider; //TODO edit inspector GUI;
    public List<Collider2D> agentColliders { get; private set; } = new();

    public bool hitPlayerCollider {  get; private set; } = false;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (meleeColliderIsPlayer)
        {
            AddAgents(collision);
        }
        else
        {
            FindPlayerCollider(collision);
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (meleeColliderIsPlayer)
        {
            RemoveAgents(collision);
        }
        else
        {
            LostPlayerCollider(collision);
        }
    }

    private void AddAgents(Collider2D collision)
    {
        if (!collision.CompareTag("Agent Collider")) return;
        if (agentColliders.Contains(collision)) return;
        agentColliders.Add(collision);
    }

    private void RemoveAgents(Collider2D collision)
    {
        if (!collision.CompareTag("Agent Collider")) return;
        if (agentColliders.Contains(collision))
        {
            agentColliders.Remove(collision);
        }
    }


    private void FindPlayerCollider(Collider2D collision)
    {
        if (collision == playerCollider)
        {
            hitPlayerCollider = true;
        }
    }

    private void LostPlayerCollider(Collider2D collision)
    {
        if (collision == playerCollider)
        {
            hitPlayerCollider = false;
        }
    }
}
