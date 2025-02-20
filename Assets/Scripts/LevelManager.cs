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
    private int trainGroundLayer => LayerMask.NameToLayer("Train Ground");
    void Start()
    {
        parallaxObjects.AddRange(FindObjectsByType<ParallaxController>(FindObjectsSortMode.None));
        spawners.AddRange(FindObjectsByType<Spawner>(FindObjectsSortMode.None));

        foreach (ParallaxController parallaxController in parallaxObjects)
        { 
            parallaxController.enabled = false;
        }
        foreach (Spawner spawner in spawners)
        {
            if (spawner is LoopingTileSpawner)
            {
                if (spawner.startSpawnDistance != 0)
                {
                    spawner.gameObject.SetActive(false);
                }
            }
            spawner.enabled = false;
        }

        playerBrain.boxCollider2D.excludeLayers |= 1 << trainGroundLayer;
        playerBrain.spriteRenderer.sortingOrder = 1;

        StartSequence();
    }

    private async void StartSequence()
    {
        await MoveTrainToStart();
        canvasBounds.SetCanvasData();
        await OpenTrainDoors();
        //TODO NPCS enter train
        //TODO NPCS enable calm path
        //TODO UI to enter ("E")
        //TODO switch sprite depth and collisions
        //TODO Close slide doors
        //TODO Enable parallax objects
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

    private async Task OpenTrainDoors()
    {
        foreach (SlideDoorBounds slideDoor in trainController.slideDoorsList)
        {
            StartCoroutine(slideDoor.OpeningDoors());
            await Task.Yield();
        }
    }
}
