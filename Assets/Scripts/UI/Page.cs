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
    public AtlasRenderer chosenStation_renderer;
    
    public AtlasRenderer mugshot_renderer;

    [Header("Generated")]
    public AtlasClip paperClip;
    public Behaviours behaviours0;
    public Behaviours behaviours1;
    public int chosenStationIndex;
    public Bounds stationNameBounds;
    public string chosenStationName;
    public void Init(NPCProfile traitorProfile)
    {
        paperClip = paper_renderer.atlas.clipDict[(int)NotepadMotion.FlipPage];

        behaviours0 = GetBehaviourAtIndex(traitorProfile.behaviours, 0);
        behaviours1 = GetBehaviourAtIndex(traitorProfile.behaviours, 1);

        name_renderer.SetText(traitorProfile.fullName);
        behaviour0_renderer.SetText(npcData.behaviourDescDict[behaviours0]);
        behaviour1_renderer.SetText(npcData.behaviourDescDict[behaviours1]);

        mugshot_renderer.UpdateSpriteInputs(ref mugshot_renderer.atlas.simpleSprites[traitorProfile.coveredMugshotIndex]);

        chosenStationIndex = -1;
        chosenStation_renderer.SetText("");
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
        chosenStation_renderer.enabled = toggle;
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
        chosenStation_renderer.UpdateDepthRealtime(contentDepth);
        name_renderer.UpdateDepthRealtime(contentDepth);
        mugshot_renderer.UpdateDepthRealtime(contentDepth);
    }
    public void TogglePageRenderer(bool toggle)
    {
        paper_renderer.enabled = toggle;
    }
    public void SetChosenStationText(int stationIndex)
    {
        chosenStationIndex = stationIndex;
        chosenStationName = trip.stationsDataArray[chosenStationIndex].stationName;
        stationNameBounds = chosenStation_renderer.GetTextBounds(chosenStationName);
        chosenStation_renderer.AppearConfirmText();
        WritingStationName(chosenStationName).Forget();
    }
    public void EraseChosenStationText()
    {
        ErasingStationName().Forget();
    }
    public void SetPreviewStationText(int stationIndex)
    {
        chosenStation_renderer.SetText(trip.stationsDataArray[stationIndex].stationName);
        chosenStation_renderer.AppearPreviewText();
        stationNameBounds = chosenStation_renderer.bounds;
    }
    private async UniTask ErasingStationName()
    {
        string curStationString = chosenStationName;
        while(curStationString.Length > 0)
        {
            await UniTask.WaitForSeconds(Notepad.WRITE_LETTER_TIME);
            curStationString = curStationString[..^1];
            chosenStation_renderer.SetText(curStationString);
        }
    }
    private async UniTask WritingStationName(string stationName)
    {
        int stationNameLetterCount = stationName.Length;
        int curLetterIndex = 0;

        string curStationString = "";
        chosenStation_renderer.SetText(curStationString);
        while ( curLetterIndex < stationNameLetterCount )
        {
            curStationString += stationName[curLetterIndex];
            await UniTask.WaitForSeconds(Notepad.WRITE_LETTER_TIME);
            chosenStation_renderer.SetText(curStationString);
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
