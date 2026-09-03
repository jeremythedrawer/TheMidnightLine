using Proselyte.Sigils;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

using static Passenger;
public class PassengerManager : MonoBehaviour
{
    public GameEvent onBeginTrip;
    public PassengersData passengerData;
    public Texture2D diagonalTexture;
    public AtlasSO glyphAtlas;

    [Header("Generated")]
    public bool npcFindingChair;
    public int totalAgentCount;


    private void OnEnable()
    {
        onBeginTrip.RegisterListener(InitPoolsDict);
    }
    private void OnDisable()
    {
        onBeginTrip.UnregisterListener(InitPoolsDict);
    }
    private void Start()
    {
        GraffitiPool = new Graffiti[MAX_GRAFFITI_RENDERERS];
        graffitiActiveCount = -1;
        passengerData.habitDataDict = SetBehaviourContextDictionary(passengerData.habitDataArray);
    }
    public static Graffiti GetGraffitiRenderer(Graffiti graffitiPrefab)
    {
        Graffiti graffitInstance;

        if (graffitiActiveCount < 0)
        {
            graffitInstance = Instantiate(graffitiPrefab);

        }
        else
        {
            graffitInstance = GraffitiPool[graffitiActiveCount];
            graffitiActiveCount--;
        }

        return graffitInstance;
    }
    public static void ReturnGraffiti(Graffiti graffiti)
    {
        if (graffitiActiveCount == MAX_GRAFFITI_RENDERERS - 1) return;

        graffitiActiveCount++;
        GraffitiPool[graffitiActiveCount] = graffiti;
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
        newVisualEffect.Reinit();
        newVisualEffect.Stop();
        return newVisualEffect;
    }
    public static void ReturnGlyph(VisualEffect glyphPrefab, VisualEffect glyphInstance)
    {
        glyphInstance.Stop();
        glyphInstance.gameObject.transform.parent = null;
        if (!GlyphPoolDict.TryGetValue(glyphPrefab, out Queue<VisualEffect> queue))
        {
            queue = new Queue<VisualEffect>();
            GlyphPoolDict[glyphPrefab] = queue;
        }

        queue.Enqueue(glyphInstance);
    }
    public static PassengerBrain GetNPC(PassengerBrain npcPrefab, Vector3 localPos, Transform parent)
    {
        if (!PassengerPoolDict.TryGetValue(npcPrefab, out Queue<PassengerBrain> queue))
        {
            queue = new Queue<PassengerBrain>();
            PassengerPoolDict.Add(npcPrefab, queue);
        }

        if (queue.Count > 0)
        {
            PassengerBrain npc = queue.Dequeue();
            npc.gameObject.SetActive(true);
            npc.gameObject.transform.parent = parent;
            npc.transform.localPosition = localPos;
            return npc;
        }
        PassengerBrain newNPC = Instantiate(npcPrefab, parent);
        newNPC.transform.localPosition = localPos;

        return newNPC;
    }
    public static void ReturnNPC(PassengerBrain npcPrefab, PassengerBrain npcInstance)
    {
        npcInstance.gameObject.transform.parent = null;
        if (!PassengerPoolDict.TryGetValue(npcPrefab, out Queue<PassengerBrain> queue))
        {
            queue = new Queue<PassengerBrain>();
            PassengerPoolDict.Add(npcPrefab, queue);
        }
        queue.Enqueue(npcInstance);
        npcInstance.gameObject.SetActive(false);
    }
    private void InitPoolsDict()
    {
        if (PassengerPoolDict != null)
        {
            PassengerPoolDict.Clear();
        }
        else
        {
            PassengerPoolDict = new Dictionary<PassengerBrain, Queue<PassengerBrain>>();
        }

        if (GlyphPoolDict != null)
        {
            GlyphPoolDict.Clear();
        }
        else
        {
            GlyphPoolDict = new Dictionary<VisualEffect, Queue<VisualEffect>>();
        }
    }
}

