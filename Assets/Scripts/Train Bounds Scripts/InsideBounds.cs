using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class InsideBounds : CarriageBounds
{
    public static InsideBounds Instance { get; private set; }

    public int bystanderCount { get; private set; } = 0;

    public Collider2D thisCollider;
    public List<SeatBounds> setsOfSeats { get; set; } = new List<SeatBounds>();
    public List<float> npcStandingPosList { get; set; } = new List<float>();
    private Dictionary<Collider2D, float> npcStandingPosDic = new Dictionary<Collider2D, float>();


    public bool inEmergency; //TODO: find when fighting or when gun is shot
    private void Start()
    {
        SetUpCarriageBounds();

        thisCollider = GetComponent<Collider2D>();

        ContactFilter2D filter2D = new ContactFilter2D();
        filter2D.SetLayerMask(LayerMask.GetMask("Chairs"));
        filter2D.useTriggers = true;

        List<Collider2D> results = new List<Collider2D>();

        thisCollider.Overlap(filter2D, results);

        foreach (var collider in results)
        {
            SeatBounds chairBounds = collider.GetComponent<SeatBounds>();
            if (chairBounds != null)
            {
                setsOfSeats.Add(chairBounds);
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Player Collider"))
        {
            Instance = this;
            playerInActiveArea = true;
        }

        if (collision.gameObject.CompareTag("Agent Collider") || collision.gameObject.CompareTag("Bystander Collider"))
        {
            PathData pathData = collision.gameObject.GetComponentInParent<PathData>();

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


    private void OnTriggerStay2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Bystander Collider") || collision.gameObject.CompareTag("Agent Collider"))
        {
            NPCCore colliderNpcCore = collision.gameObject.GetComponentInParent<NPCCore>();

            if (!npcStandingPosDic.ContainsKey(collision) && colliderNpcCore.isStanding)
            {
                npcStandingPosDic[collision] = collision.transform.position.x; //Storing standing pos x
            }

            if (npcStandingPosDic.ContainsKey(collision))
            {
                float storedPosition = npcStandingPosDic[collision];

                if (colliderNpcCore.isStanding && !npcStandingPosList.Contains(storedPosition))
                {
                    npcStandingPosList.Add(storedPosition);
                }
                else if (!colliderNpcCore.isStanding && npcStandingPosList.Contains(storedPosition))
                {
                    npcStandingPosList.Remove(storedPosition);
                }
            }
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
            PathData pathData = collision.gameObject.GetComponentInParent<PathData>();
            if (pathData != null)
            {
                pathData.currentInsideBounds = null;
            };
        }
        if (collision.gameObject.CompareTag("Bystander Collider"))
        {
            bystanderCount--;
        }
    }
}
