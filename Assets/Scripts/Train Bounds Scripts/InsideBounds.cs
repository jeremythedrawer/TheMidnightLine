using UnityEngine;

public class InsideBounds : CarriageBounds
{
    public static InsideBounds Instance { get; private set; }

    public int bystanderCount { get; private set; } = 0;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Player Collider"))
        {
            Instance = this;
            playerInActiveArea = true;
        }

        if (collision.gameObject.CompareTag("Agent Collider") || collision.gameObject.CompareTag("Bystander Collider"))
        {
            var pathData = collision.gameObject.GetComponentInParent<PathData>();
            if (pathData != null)
            {
                    pathData.currentInsideBounds = this;
            }
            else
            {
                Debug.LogWarning("No PathData was found in " + this.name + " for " + collision.gameObject.name);
            }
        }

        if (collision.gameObject.CompareTag("Bystander Collider"))
        {
            bystanderCount++;
        }

    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Player Collider"))
        {
            Instance = null;
            playerInActiveArea = true;
        }

        if (collision.gameObject.CompareTag("Agent Collider") || collision.gameObject.CompareTag("Bystander Collider"))
        {
            var pathData = collision.gameObject.GetComponentInParent<PathData>();
            if (pathData != null)
            {
                pathData.currentInsideBounds = null;
            }
        }
        if (collision.gameObject.CompareTag("Bystander Collider"))
        {
            bystanderCount--;
        }
    }
}
