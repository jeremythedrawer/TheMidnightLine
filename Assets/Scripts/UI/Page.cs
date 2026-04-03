using System;
using UnityEngine;
using static Atlas;
using static NPC;
public class Page : MonoBehaviour
{
    public NPCsDataSO npcData;
    public AtlasRenderer paperRenderer;

    public AtlasRenderer behavioursRenderer0;
    public AtlasRenderer behaviourRenderer1;
    public AtlasRenderer nameRenderer;
    public AtlasRenderer appearenceRenderer;
    public AtlasRenderer departureStationRenderer;
    
    public AtlasRenderer mugshotRenderer;

    [Header("Generated")]
    public AtlasClip paperClip;
    public Behaviours behaviours0;
    public Behaviours behaviours1;
    public Appearance appearance;
    public int departureStationIndex;

    public void Init(NPCProfile traitorProfile)
    {
        paperClip = paperRenderer.atlas.clipDict[(int)NotepadMotion.Page];

        behaviours0 = GetBehaviourAtIndex(traitorProfile.behaviours, 0);
        behaviours1 = GetBehaviourAtIndex(traitorProfile.behaviours, 1);
        int appearencesCount = GetFlagAmount((int)traitorProfile.appearence);
        int randAppearenceIndex = UnityEngine.Random.Range(0, appearencesCount);
        appearance = GetAppearanceAtIndex(traitorProfile.appearence, randAppearenceIndex);

        nameRenderer.SetText(traitorProfile.fullName);
        behavioursRenderer0.SetText(npcData.behaviourDescDict[behaviours0]);
        behaviourRenderer1.SetText(npcData.behaviourDescDict[behaviours1]);
        appearenceRenderer.SetText(npcData.appearanceDescDict[appearance]);
        mugshotRenderer.UpdateSpriteInputs(ref mugshotRenderer.atlas.simpleSprites[traitorProfile.npcPrefabIndex]);
    }
    public void PlayPaperClip()
    {
        paperRenderer.PlayClipOneShot(paperClip);
    }
    public void PlayPaperClipReverse()
    {
        paperRenderer.PlayClipOneShotReverse(paperClip);
    }
    public void TogglePageContentBottomHalf(bool toggle)
    {
        behavioursRenderer0.enabled = toggle;
        behaviourRenderer1.enabled = toggle;
        appearenceRenderer.enabled = toggle;
        departureStationRenderer.enabled = toggle;
    }
    public void TogglePageContentTopHalf(bool toggle)
    {
        nameRenderer.enabled = toggle;
        mugshotRenderer.enabled = toggle;
    }
    public void UpdatePageDepth(int depth)
    {
        paperRenderer.UpdateDepthRealtime(depth);
        int contentDepth = depth - 1;
        behavioursRenderer0.UpdateDepthRealtime(contentDepth);
        behaviourRenderer1.UpdateDepthRealtime(contentDepth);
        appearenceRenderer.UpdateDepthRealtime(contentDepth);
        departureStationRenderer.UpdateDepthRealtime(contentDepth);
        nameRenderer.UpdateDepthRealtime(contentDepth);
        mugshotRenderer.UpdateDepthRealtime(contentDepth);
    }
    public void TogglePageRenderer(bool toggle)
    {
        paperRenderer.enabled = toggle;
    }
    private Behaviours GetBehaviourAtIndex(Behaviours behaviours, int index)
    {
        int count = 0;
        foreach(Behaviours flag in Enum.GetValues(typeof(Behaviours)))
        {
            if (flag == Behaviours.None) continue;

            if ((behaviours & flag) != 0)
            {
                if (count == index) return flag;
                count++;
            }
        }
        return Behaviours.None;
    }
    private Appearance GetAppearanceAtIndex(Appearance appearance, int index)
    {
        int count = 0;
        foreach (Appearance flag in Enum.GetValues(typeof(Appearance)))
        {
            if (flag == Appearance.None) continue;

            if ((appearance & flag) != 0)
            {
                if (count == index) return flag;
                count++;
            }
        }
        return Appearance.None;
    }
    private int GetFlagAmount(int value)
    {
        int count = 0;

        while (value != 0)
        {
            count += value & 1;
            value >>= 1;
        }
        return count;
    }
}
