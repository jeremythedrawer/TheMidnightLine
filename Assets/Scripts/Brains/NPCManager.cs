using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;
using static NPC;

public class NPCManager : MonoBehaviour
{
    public TripSO trip;
    public NPCsDataSO npcsData;

    [Header("Generated")]
    public bool npcFindingChair;
    public int totalAgentCount;

    public static Dictionary<VisualEffect, Queue<VisualEffect>> glyph_poolsDict;
    public static StationNameTag stationNameTag;
    private void Awake()
    {

    }
    private void Start()
    {
        glyph_poolsDict = new Dictionary<VisualEffect, Queue<VisualEffect>>();
        npcsData.behaviourContextDict = SetBehaviourContextDictionary();
        stationNameTag = Instantiate(npcsData.stationNameTag_prefab);
        stationNameTag.SetText("");
    }
    public static VisualEffect GetGlyph(VisualEffect glyphPrefab, Transform parent)
    {
        if (!glyph_poolsDict.TryGetValue(glyphPrefab, out Queue<VisualEffect> queue))
        {
            queue = new Queue<VisualEffect>();
            glyph_poolsDict[glyphPrefab] = queue;
        }

        if (queue.Count > 0)
        {
            VisualEffect gylphInstance = queue.Dequeue();
            gylphInstance.gameObject.SetActive(true);
            gylphInstance.Reinit();
            gylphInstance.gameObject.transform.position = parent.position;
            gylphInstance.gameObject.transform.parent = parent;
            return gylphInstance;
        }

        return Instantiate(glyphPrefab, parent.transform.position, parent.transform.rotation, parent);
    }
    public static void ReturnGlyph(VisualEffect glyphPrefab, VisualEffect glyphInstance)
    {
        glyphInstance.Stop();
        glyphInstance.gameObject.transform.parent = null;
        if(!glyph_poolsDict.TryGetValue(glyphPrefab, out Queue<VisualEffect> queue))
        {
            queue = new Queue<VisualEffect>();
            glyph_poolsDict[glyphPrefab] = queue;
        }

        queue.Enqueue(glyphInstance);
    }
    private Dictionary<Behaviours, NPCBehaviourContextSO> SetBehaviourContextDictionary()
    {
        Dictionary<Behaviours, NPCBehaviourContextSO> dict = new Dictionary<Behaviours, NPCBehaviourContextSO>();

        foreach (NPCBehaviourContextSO context in npcsData.behaviourContexts)
        {
            dict[context.behaviour] = context;
        }

        return dict;
    }
}

