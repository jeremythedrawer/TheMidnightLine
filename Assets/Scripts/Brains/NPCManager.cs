using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;
using static NPC;

public class NPCManager : MonoBehaviour
{
    public TripSO trip;
    public NPCsDataSO npcsData;
    public Texture2D diagonalTexture;

    [Header("Generated")]
    public bool npcFindingChair;
    public int totalAgentCount;

    public static Dictionary<VisualEffect, Queue<VisualEffect>> GlyphPoolDict;
    public static Dictionary<NPCBrain, Queue<NPCBrain>> NPCPoolDict;

    private void Awake()
    {
        GlyphPoolDict = new Dictionary<VisualEffect, Queue<VisualEffect>>();
        NPCPoolDict = new Dictionary<NPCBrain, Queue<NPCBrain>>();
    }
    private void Start()
    {

        npcsData.behaviourContextDict = SetBehaviourContextDictionary();
        Shader.SetGlobalTexture("_DiagonalTexture", diagonalTexture);
    }
    public static VisualEffect GetGlyph(VisualEffect glyphPrefab, Transform parent)
    {
        if (!GlyphPoolDict.TryGetValue(glyphPrefab, out Queue<VisualEffect> queue))
        {
            queue = new Queue<VisualEffect>();
            GlyphPoolDict[glyphPrefab] = queue;
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
        if(!GlyphPoolDict.TryGetValue(glyphPrefab, out Queue<VisualEffect> queue))
        {
            queue = new Queue<VisualEffect>();
            GlyphPoolDict[glyphPrefab] = queue;
        }

        queue.Enqueue(glyphInstance);
    }

    public static NPCBrain GetNPC(NPCBrain npcPrefab, Vector3 spawnPos, Transform parent)
    {
        if (!NPCPoolDict.TryGetValue(npcPrefab, out Queue<NPCBrain> queue))
        {
            queue = new Queue<NPCBrain>();
            NPCPoolDict.Add(npcPrefab, queue);
        }

        if (queue.Count > 0)
        {
            NPCBrain npc = queue.Dequeue();
            npc.gameObject.SetActive(true);
            npc.transform.position = spawnPos;
            npc.gameObject.transform.parent = parent;
            return npc;
        }
        return Instantiate(npcPrefab, spawnPos, Quaternion.identity, parent);
    }

    public static void ReturnNPC(NPCBrain npcPrefab, NPCBrain npcInstance)
    {
        npcInstance.gameObject.transform.parent = null;
        if (!NPCPoolDict.TryGetValue(npcPrefab, out Queue<NPCBrain> queue))
        {
            queue = new Queue<NPCBrain>();
            NPCPoolDict.Add(npcPrefab, queue);
        }
        queue.Enqueue(npcInstance);
        npcInstance.gameObject.SetActive(false);
    }

    private Dictionary<Behaviours, NPCBehaviourContextSO> SetBehaviourContextDictionary()
    {
        Dictionary<Behaviours, NPCBehaviourContextSO> dict = new Dictionary<Behaviours, NPCBehaviourContextSO>();

        foreach (NPCBehaviourContextSO context in npcsData.behaviourContexts)
        {
            dict[context.behaviours] = context;
        }

        return dict;
    }
}

