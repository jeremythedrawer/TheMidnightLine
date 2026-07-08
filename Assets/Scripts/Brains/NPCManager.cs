using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;
using static NPC;
using static AtlasRendering;
public class NPCManager : MonoBehaviour
{
    public static Dictionary<VisualEffect, Queue<VisualEffect>> GlyphPoolDict;
    public static Dictionary<NPCBrain, Queue<NPCBrain>> NPCPoolDict;

    public TripSO trip;
    public NPCsDataSO npcsData;
    public Texture2D diagonalTexture;
    public AtlasSO glyphAtlas;

    [Header("Generated")]
    public bool npcFindingChair;
    public int totalAgentCount;


    private void Awake()
    {
        GlyphPoolDict = new Dictionary<VisualEffect, Queue<VisualEffect>>();
        NPCPoolDict = new Dictionary<NPCBrain, Queue<NPCBrain>>();
    }
    private void Start()
    {
        npcsData.behaviourContextDict = SetBehaviourContextDictionary();
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
            VisualEffect glyphInstance = queue.Dequeue();
            glyphInstance.gameObject.SetActive(true);
            glyphInstance.Reinit();
            glyphInstance.Stop();
            glyphInstance.gameObject.transform.parent = parent;
            return glyphInstance;
        }

        VisualEffect newVisualEffect = Instantiate(glyphPrefab, parent);
        return newVisualEffect;
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
        NPCBrain newNPC = Instantiate(npcPrefab, spawnPos, Quaternion.identity, parent);

        return newNPC;
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

