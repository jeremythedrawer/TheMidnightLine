using UnityEngine;

public class OutsideBounds : CarriageBounds
{
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Player Collider"))
        {
            playerInActiveArea = true;
        }

        if (collision.gameObject.CompareTag("Agent Collider") || collision.gameObject.CompareTag("Bystander Collider"))
        {
            var pathData = collision.gameObject.GetComponentInParent<PathData>();
            if (pathData != null)
            {
                pathData.currentOutsideBounds = this;
            }
            else
            {
                Debug.LogWarning("No PathData was found in " + this.name + " for " + collision.gameObject.name);
            }
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Player Collider"))
        {
            playerInActiveArea = false;
        }

        if (collision.gameObject.CompareTag("Agent Collider") || collision.gameObject.CompareTag("Bystander Collider"))
        {
            var pathData = collision.gameObject.GetComponentInParent<PathData>();
            if (pathData != null)
            {
                pathData.currentOutsideBounds = null;
            }
        }
    }
}
