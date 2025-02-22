using UnityEngine;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Collections;
public class LevelManager : MonoBehaviour
{
    [Header("References")]
    public PlayerBrain playerBrain;
    public TrainController trainController;
    public CanvasBounds canvasBounds;

    [Header("Parameters")]
    [SerializeField] private AnimationCurve startingDecelCurve;

    private List<ParallaxController> parallaxObjects = new List<ParallaxController>();
    private List<Spawner> spawners = new List<Spawner>();
    public List<NPCCore> npcsInFirstStation => trainController.currentStation.npcs;
    private int trainGroundLayer => LayerMask.NameToLayer("Train Ground");
    void Start()
    {
        StartSequence();
    }

    private async void StartSequence()
    {
        parallaxObjects.AddRange(FindObjectsByType<ParallaxController>(FindObjectsSortMode.None));
        spawners.AddRange(FindObjectsByType<Spawner>(FindObjectsSortMode.None));

        EnableParallaxAndSpawners(false);

        playerBrain.boxCollider2D.excludeLayers |= 1 << trainGroundLayer;
        playerBrain.spriteRenderer.sortingOrder = 1;
        while (trainController.currentStation == null) await Task.Yield();

        foreach (NPCCore npc in npcsInFirstStation)
        {
            npc.spriteRenderer.sortingOrder = 1;
            npc.boxCollider2D.excludeLayers |= 1 << trainGroundLayer;
        }


        await MoveTrainToStart();
        canvasBounds.SetCanvasData();
        await SetAllSeatData();
        EnableParallaxAndSpawners(true);
        //Player Enters onto train
    }

    private async Task MoveTrainToStart()
    {
        float duration = 5f;
        float elaspedTime = 0f;


        Vector2 initialPos = trainController.transform.position; 
        Vector2 targetPos = new Vector2(0, initialPos.y);
        float stoppingDistance = targetPos.x - initialPos.x;
        float startSpeedInMPS = (2 * stoppingDistance) / duration; // from the equation of motion v=u+at

        float startSpeed = startSpeedInMPS * 3.6f;
        while (elaspedTime < duration)
        {
            if (trainController == null) return;
            float t =  elaspedTime / duration;

            trainController.transform.position = Vector2.Lerp(initialPos, targetPos, startingDecelCurve.Evaluate(t));
            elaspedTime += Time.deltaTime;

            trainController.kmPerHour = Mathf.Lerp(startSpeed, 0, startingDecelCurve.Evaluate(t));
            await Task.Yield();
        }
        trainController.kmPerHour = 0;
        trainController.transform.position = targetPos;
    }

    private async Task SetAllSeatData()
    {
        while (trainController.kmPerHour != 0) await Task.Yield();
        foreach (SeatBounds setsOfSeats in trainController.seatBoundsList)
        {
            setsOfSeats.SetSeatData();
        }
    }

    private void EnableParallaxAndSpawners(bool isEnabled)
    {
        foreach (ParallaxController parallaxController in parallaxObjects)
        {
            parallaxController.enabled = isEnabled;
        }
        foreach (Spawner spawner in spawners)
        {
            if (spawner is LoopingTileSpawner)
            {
                if (spawner.startSpawnDistance != 0)
                {
                    spawner.gameObject.SetActive(isEnabled);
                }
            }
            spawner.enabled = false;
        }
    }
}
