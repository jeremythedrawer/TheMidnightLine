using Cysharp.Threading.Tasks;
using System;
using System.Threading;
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
    public AtlasRenderer pageNumber_renderer;
    public AtlasRenderer mugshot_renderer;

    public AtlasRenderer paperCornerLeft_renderer;
    public AtlasRenderer paperCornerRight_renderer;

    public AtlasRenderer exitButton_renderer;

    [Header("Generated")]
    public AtlasClip paperClip;
    public Behaviours behaviours0;
    public Behaviours behaviours1;
    public int chosenStationIndex;
    public Bounds stationNameBounds;
    public string chosenStationName;
    public bool isLastPage;
    public bool isFirstPage;
    public CancellationTokenSource ctsWrite;
    private void OnDisable()
    {
        ctsWrite?.Cancel();
        ctsWrite = null;
    }
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
        pageNumber_renderer.enabled = toggle;
        
        if(!isFirstPage)
        {
            paperCornerLeft_renderer.enabled = toggle;
        }
        if(!isLastPage)
        {
            paperCornerRight_renderer.enabled = toggle;
        }

        if (chosenStationIndex == -1)
        {
            chosenStation_renderer.enabled = false;
        }
        else
        {
            chosenStation_renderer.enabled = toggle;
        }
    }
    public void TogglePageContentTopHalf(bool toggle)
    {
        name_renderer.enabled = toggle;
        mugshot_renderer.enabled = toggle;
        exitButton_renderer.enabled = toggle;
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
        pageNumber_renderer.UpdateDepthRealtime(contentDepth);
    }
    public void WriteChosenStationText(int stationIndex)
    {
        chosenStationIndex = stationIndex;
        chosenStationName = trip.stationsDataArray[chosenStationIndex].stationName;
        stationNameBounds = chosenStation_renderer.GetTextBounds(chosenStationName);

        ctsWrite?.Cancel();
        ctsWrite = new CancellationTokenSource();
        WritingStationName(chosenStationName).Forget();
    }
    public void EraseChosenStationText()
    {
        ctsWrite?.Cancel();
        ctsWrite = new CancellationTokenSource();
        ErasingStationName().Forget();
    }
    public void SetPreviewStationText(int stationIndex)
    {
        string previewStationText = trip.stationsDataArray[stationIndex].stationName;
        chosenStation_renderer.SetText(previewStationText);
        stationNameBounds = chosenStation_renderer.bounds;
        chosenStation_renderer.enabled = true;
        chosenStation_renderer.AppearPreviewText();
    }
    public void InvertExitButton(bool invert, bool pointDown)
    {
        if (exitButton_renderer.flipY != pointDown)
        {
            exitButton_renderer.custom.x = invert ? 0 : 1;
            exitButton_renderer.FlipV(pointDown, exitButton_renderer.sprite);
        }

    }
    public void InvertLeftArrowButton(bool invert)
    {
        paperCornerLeft_renderer.custom.x = invert ? 0 : 1;
    }
    public void InverRightArrowButton(bool invert)
    {
        paperCornerRight_renderer.custom.x = invert ? 0 : 1;
    }
    public void HideRightArrowButton()
    {
        isLastPage = true;
        paperCornerRight_renderer.enabled = false;
    }
    public void HideLeftArrowButton()
    {
        paperCornerLeft_renderer.enabled = false;
        isFirstPage = true;
    }
    public Bounds GetStationNameBounds()
    {
        return chosenStation_renderer.bounds;
    }
    private async UniTask ErasingStationName()
    {
        string curStationString = chosenStation_renderer.text;
        try
        {
            while (curStationString.Length > 0)
            {
                await UniTask.WaitForSeconds(Notepad.WRITE_LETTER_TIME, cancellationToken: ctsWrite.Token);
                curStationString = curStationString[..^1];
                chosenStation_renderer.SetText(curStationString);
                chosenStation_renderer.AppearConfirmText();
            }
        }
        catch (OperationCanceledException) { }
    }
    private async UniTask WritingStationName(string stationName)
    {
        int stationNameLetterCount = stationName.Length;
        int curLetterIndex = 0;

        string curStationString = "";
        chosenStation_renderer.SetText(curStationString);

        try
        {
            while (curLetterIndex < stationNameLetterCount)
            {
                curStationString += stationName[curLetterIndex];
                await UniTask.WaitForSeconds(Notepad.WRITE_LETTER_TIME, cancellationToken: ctsWrite.Token);
                chosenStation_renderer.SetText(curStationString);
                chosenStation_renderer.AppearConfirmText();
                curLetterIndex++;
            }
        }
        catch (OperationCanceledException) { }
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

}
