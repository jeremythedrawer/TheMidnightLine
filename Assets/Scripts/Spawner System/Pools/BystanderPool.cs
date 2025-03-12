
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
public class BystanderPool : NPCPool
{
    [SerializeField] private NPCCore bystanderPrefab;

    [System.Serializable]
    public struct CharacterVisuals
    {
        public AnimatorOverrideController overrideController;
        public Material material;

        public CharacterVisuals(AnimatorOverrideController animatorController, Material material)
        {
            this.overrideController = animatorController;
            this.material = material;
        }
    }

    public List<CharacterVisuals> characterVisualsList = new List<CharacterVisuals>();

    private TrainData trainData => GlobalReferenceManager.Instance.trainData;
    private int totalBystandersAmount;
    private int defaultBystandersAmount;

    public List<NPCCore> bystanders { get; set; } = new List<NPCCore>();

    private void Start()
    {
        StartCoroutine(SetUp());
    }

    private void Update()
    {
        SpawnBystanders();
    }
    private IEnumerator SetUp()
    {
        yield return new WaitUntil(() => GlobalReferenceManager.Instance.totalBystanders > 0);
        defaultBystandersAmount = NPCCountOfStation<BystanderBrain>(0);
        totalBystandersAmount = GlobalReferenceManager.Instance.totalBystanders;

        CreatePool(bystanderPrefab, this.transform, totalBystandersAmount, defaultBystandersAmount);
    }

    private void SpawnBystanders()
    {
        if (npcPool == null) return;
        if (trainData.nextStation.bystanderSpawnCount > bystanders.Count)
        {
            int bystandersToSpawn = trainData.nextStation.bystanderSpawnCount - npcPool.CountInactive;
            SpawnNPCs(bystandersToSpawn, bystanders);
        }
    }

}
