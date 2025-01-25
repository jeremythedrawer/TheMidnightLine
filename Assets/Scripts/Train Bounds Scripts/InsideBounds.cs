using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

public class InsideBounds : CarriageBounds
{
    public static InsideBounds Instance { get; private set; }

    public int bystanderCount { get; private set; } = 0;

    public Collider2D thisCollider;
    public bool inEmergency { get; private set; } //TODO: find when fighting or when gun is shot

    public List<SeatBounds> setsOfSeats { get; set; } = new List<SeatBounds>();
    public List<float> standingNpcAndWallPosList { get; set; } = new List<float>();
    public List<float> distancesBetweenNpcs { get; set; } = new List<float>();

    private Dictionary<Collider2D, float> npcStandingPosDic = new Dictionary<Collider2D, float>();

    private void Start()
    {
        SetUpCarriageBounds();
        AddToSetsOfSeats();
        AddWallsPosToList();
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

                if (colliderNpcCore.isStanding)
                {
                    if (!standingNpcAndWallPosList.Contains(storedPosition))
                    {
                        InsertSorted(standingNpcAndWallPosList, storedPosition);
                        FindDistances(storedPosition);
                    }
                }
                else
                {
                    if (standingNpcAndWallPosList.Contains(storedPosition))
                    {
                        standingNpcAndWallPosList.Remove(storedPosition);
                    }
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

    private void AddToSetsOfSeats()
    {
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

    private void AddWallsPosToList()
    {
        standingNpcAndWallPosList.Insert(0, Bounds.min.x);
        standingNpcAndWallPosList.Add(Bounds.max.x);
    }

    private void InsertSorted(List<float> list, float value)
    {
        int index = list.BinarySearch(value);
        if (index < 0) index = ~index; // finds the insertion index
        list.Insert(index, value);
    }

    private void FindDistances(float position)
    {
        int index = standingNpcAndWallPosList.IndexOf(position);
        int leftIndex = index - 1;
        int rightIndex = index + 1;

        //Debug.Log("left index: " + leftIndex + " right index: " + rightIndex + " index count " + distancesBetweenNpcs.Length);
        float lefttDistance = standingNpcAndWallPosList[index] - standingNpcAndWallPosList[leftIndex];
        float rightDistance = standingNpcAndWallPosList[rightIndex] - standingNpcAndWallPosList[index];
        if (distancesBetweenNpcs.Count == 0)
        {
            distancesBetweenNpcs.Insert(0, lefttDistance);

            distancesBetweenNpcs.Insert(1, rightDistance);
        }
        else
        {
            float totalDistance = lefttDistance + rightDistance;
            distancesBetweenNpcs.Remove(totalDistance);
            distancesBetweenNpcs.Insert(leftIndex, lefttDistance);
            distancesBetweenNpcs.Insert(index, rightDistance);
        }
    }

    private void UpdateDistancesBeforeRemoval(float position)
    {
       
    }
}
