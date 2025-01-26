using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;
using static PathData;

public class InsideBounds : CarriageBounds
{
    public static InsideBounds Instance { get; private set; }

    public int bystanderCount { get; private set; } = 0;

    public Collider2D thisCollider;
    public bool inEmergency { get; private set; } //TODO: find when fighting or when gun is shot

    public List<SeatBounds> setsOfSeats { get; set; } = new List<SeatBounds>();

    [System.Serializable]
    public struct StandNpcPosData
    {
        public float startPos;
        public float endPos;

        public StandNpcPosData(float startPos, float endPos)
        {
            this.startPos = startPos;
            this.endPos = endPos;
        }
    }
    public List<StandNpcPosData> standingNpcAndWallPosList { get; set; } = new List<StandNpcPosData>();

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
                    if (!standingNpcAndWallPosList.Any(posData => posData.startPos == storedPosition))
                    {
                        InsertSorted(new StandNpcPosData(storedPosition, 0));
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
        InsertSorted(new StandNpcPosData(Bounds.min.x, 0));
    }

    private void InsertSorted(StandNpcPosData currentPosData)
    {
        int index = standingNpcAndWallPosList.BinarySearch(currentPosData, Comparer<StandNpcPosData>.Create((x,y) => x.startPos.CompareTo(y.startPos)));
        if (index < 0) index = ~index; // finds the insertion index
        standingNpcAndWallPosList.Insert(index, currentPosData);
        if (index > 0)
        {
            StandNpcPosData prevItem = standingNpcAndWallPosList[index - 1];
            standingNpcAndWallPosList[index - 1] = new StandNpcPosData(prevItem.startPos, currentPosData.startPos);
            if (currentPosData.endPos == 0 && standingNpcAndWallPosList.Count > index + 1) // check if index skips over the previous index
            {
                StandNpcPosData nextItem = standingNpcAndWallPosList[index + 1];
                standingNpcAndWallPosList[index] = new StandNpcPosData(currentPosData.startPos, nextItem.startPos);
            }
            if (index == standingNpcAndWallPosList.Count - 1) // add wall to endpos on the las index
            {
                standingNpcAndWallPosList[index] = new StandNpcPosData(currentPosData.startPos, Bounds.max.x);
            }
            
        }
    }
}
