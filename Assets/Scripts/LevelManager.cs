using UnityEngine;
using System.Collections.Generic;
using System.Threading.Tasks;
public class LevelManager : MonoBehaviour
{
    [Header("References")]
    public PlayerBrain playerBrain;
    public TrainController trainController;

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

        BeginStartSequence();
    }

    private async void BeginStartSequence()
    {
        await MoveTrainToStart();
    }

    private async Task MoveTrainToStart()
    {
        float duration = 20f;
        float elaspedTime = 0f;


        Vector2 initialPos = trainController.transform.position;
        Vector2 targetPos = new Vector2(0, initialPos.y);

        while (elaspedTime < duration)
        {
            if (trainController == null) return;
            float t =  elaspedTime / duration;

            trainController.transform.position = Vector2.Lerp(initialPos, targetPos, startingDecelCurve.Evaluate(t));
            elaspedTime += Time.deltaTime;
            await Task.Yield();
        }

        trainController.transform.position = targetPos;
    }
}
