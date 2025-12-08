using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;


public class InsideBounds : InsideOutsideBounds
{
    public static InsideBounds Instance { get; private set; }
    public int bystanderCount { get; private set; } = 0;
    public bool inEmergency { get; private set; } //TODO: find when fighting or when gun is shot
    public List<SeatBounds> setsOfSeats { get; set; } = new List<SeatBounds>();
    public List<SlideDoorBounds> setOfSlideDoors { get; set; } = new List<SlideDoorBounds>();
    public Collider2D boxCollider => GetComponent<Collider2D>();

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

    public override void Start()
    {
        base.Start();
        AddToSetsOfSeats();
        AddToSetsOfSlideDoors();
        SetBounds();
    }

    public void OnTriggerEnter2D(Collider2D collision)
    {
        CollisionChecker collisionChecker = collision.gameObject.GetComponentInParent<CollisionChecker>();
        if (collisionChecker == null) return;
        int activeLayerInt = Helpers.GetLayerInt(collisionChecker.activeGroundLayer.value);
        string activeLayerName = LayerMask.LayerToName(activeLayerInt);
        if (activeLayerName != "Train Ground") return;

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

    public async void SetBounds()
    {
        while (trainData.kmPerHour != 0) { await Task.Yield(); }
        SetUpCarriageBounds();
        AddWallsPosToList();
    }

    public void AddToSetsOfSeats()
    {
        string layerName = "Chairs";
        AddToTrainObjectLists<SeatBounds>(layerName, setsOfSeats);
    }

    private void AddToSetsOfSlideDoors()
    {
        string layerName = "Slide Doors";
        AddToTrainObjectLists<SlideDoorBounds>(layerName, setOfSlideDoors);
    }

    private void AddToTrainObjectLists<T>(string layerName, List<T> trainObjectList)
    {
        ContactFilter2D filter2D = new ContactFilter2D();
        filter2D.SetLayerMask(LayerMask.GetMask(layerName));
        filter2D.useTriggers = true;

        List<Collider2D> results = new List<Collider2D>();

        Collider2D.Overlap(filter2D , results);

        foreach (Collider2D collider in results)
        {
            T bounds = collider.GetComponent<T>();
            if (bounds != null)
            {
                trainObjectList.Add(bounds);
            }
        }
    }

    public void AddWallsPosToList()
    {
        InsertSorted(new StandNpcPosData(objectBounds.min.x, 0));
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
                standingNpcAndWallPosList[index] = new StandNpcPosData(currentPosData.startPos, objectBounds.max.x);
            }
            
        }
    }
}
