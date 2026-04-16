using Cysharp.Threading.Tasks;
using System;
using UnityEngine;
using static Atlas;
using static NPC;
public class Page : MonoBehaviour
{
    public TripSO trip;
    public NPCsDataSO npcData;
    public AtlasRenderer paper_renderer;

    public AtlasRenderer behaviour0_renderer;
    public AtlasRenderer behaviour1_renderer;
    public AtlasRenderer name_renderer;
    public AtlasRenderer disembarkingStation_renderer;
    
    public AtlasRenderer mugshot_renderer;

    [Header("Generated")]
    public AtlasClip paperClip;
    public Behaviours behaviours0;
    public Behaviours behaviours1;
    public int disembarkingStationIndex;
    public Bounds stationNameBounds;
    public string stationName;
    public void Init(NPCProfile traitorProfile)
    {
        paperClip = paper_renderer.atlas.clipDict[(int)NotepadMotion.FlipPage];

        behaviours0 = GetBehaviourAtIndex(traitorProfile.behaviours, 0);
        behaviours1 = GetBehaviourAtIndex(traitorProfile.behaviours, 1);

        name_renderer.SetText(traitorProfile.fullName);
        behaviour0_renderer.SetText(npcData.behaviourDescDict[behaviours0]);
        behaviour1_renderer.SetText(npcData.behaviourDescDict[behaviours1]);

        mugshot_renderer.UpdateSpriteInputs(ref mugshot_renderer.atlas.simpleSprites[traitorProfile.coveredMugshotIndex]);

        disembarkingStationIndex = -1;
    }
    public void PlayPaperClip()
    {
        paper_renderer.PlayClipOneShot(paperClip);
    }
    public void PlayPaperClipReverse()
    {
        paper_renderer.PlayClipOneShotReverse(paperClip);
    }
    public void TogglePageContentBottomHalf(bool toggle)
    {
        behaviour0_renderer.enabled = toggle;
        behaviour1_renderer.enabled = toggle;
        disembarkingStation_renderer.enabled = toggle;
    }
    public void TogglePageContentTopHalf(bool toggle)
    {
        name_renderer.enabled = toggle;
        mugshot_renderer.enabled = toggle;
    }
    public void UpdatePageDepth(int depth)
    {
        paper_renderer.UpdateDepthRealtime(depth);
        int contentDepth = depth - 1;
        behaviour0_renderer.UpdateDepthRealtime(contentDepth);
        behaviour1_renderer.UpdateDepthRealtime(contentDepth);
        disembarkingStation_renderer.UpdateDepthRealtime(contentDepth);
        name_renderer.UpdateDepthRealtime(contentDepth);
        mugshot_renderer.UpdateDepthRealtime(contentDepth);
    }
    public void TogglePageRenderer(bool toggle)
    {
        paper_renderer.enabled = toggle;
    }
    public void SetDisembarkingStationText(int chosenStationIndex)
    {
        disembarkingStationIndex = chosenStationIndex;
        stationName = trip.stationsDataArray[disembarkingStationIndex].stationName;
        stationNameBounds = disembarkingStation_renderer.GetTextBounds(stationName);
        WriteStationName(stationName).Forget();
    }
    private async UniTask WriteStationName(string stationName)
    {
        int stationNameLetterCount = stationName.Length;
        int curLetterIndex = 0;

        string curStationString = "";

        while( curLetterIndex < stationNameLetterCount )
        {
            await UniTask.WaitForSeconds(Notepad.WRITE_LETTER_TIME);
            curStationString += stationName[curLetterIndex];
            disembarkingStation_renderer.SetText(curStationString);
            curLetterIndex++;
        }
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
