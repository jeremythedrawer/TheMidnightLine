using System;
using System.Threading;
using UnityEngine;

using static Atlas;
using static NPC;
using static AtlasUI;

public class Page : MonoBehaviour
{
    public static AtlasClip paper_clip;
    public PageType pageType;
    public TripPrompt promptType;
    public TripClue clueType;

    public TripSO trip;
    public NPCsDataSO npcData;

    public AtlasRenderer paperRenderer;

    public AtlasTextRenderer[] clueTextRenderers;
    public AtlasRenderer[] pictureRenderers;
    public AtlasTextRenderer[] answerTextRenderers;

    public AtlasTextRenderer[] contentTextRenderers;
    

    public AtlasTextRenderer pageNumberRenderer;
    public AtlasRenderer paperCornerLeftButtonRenderer;
    public AtlasRenderer paperCornerRightButtonRenderer;
    public AtlasRenderer exitButton_renderer;

    public AtlasTextRenderer[] bottomTextRenderers;
    public AtlasTextRenderer[] topTextRenderers;

    public AtlasRenderer[] bottomRenderers;
    public AtlasRenderer[] topRenderers;
    
    [Header("Generated")]
    public AtlasTextRenderer activeAnswerTextRenderer;
    public int answerIndex;
    public Bounds answerTextBounds;
    
    public string answerText;
    public string previewAnswerText;

    //public string playerSignature; //TODO: Put this in a player profile scriptable object

    public CancellationTokenSource ctsWrite;
    private void OnDisable()
    {
        ctsWrite?.Cancel();
        ctsWrite = null;
    }
    public void Init()
    {
        paper_clip = paperRenderer.atlas.clipDict[(int)NotepadMotion.FlipPage];
        
        answerIndex = -1;

        switch (pageType)
        {
            case PageType.Prompt:
            {
                string promptText = AtlasUI.promptDict[trip.prompt];
                contentTextRenderers[0].SetText(promptText);
            }
            break;

            case PageType.Confirm:
            {
                activeAnswerTextRenderer = answerTextRenderers[0];
            }
            break;

            case PageType.Profile:
            {
                activeAnswerTextRenderer = answerTextRenderers[0];

                for (int i = 0; i < answerTextRenderers.Length; i++)
                {
                    answerTextRenderers[i].SetText("");
                }
            }
            break;
        }
    }
    public void InitBehaviourClueText(NPCProfile traitorProfile)
    {
        for (int i = 0; i < clueTextRenderers.Length; i++)
        {
            Behaviours behaviour = GetBehaviourAtIndex(traitorProfile.behaviours, i);
            clueTextRenderers[i].SetText(npcData.behaviourDescDict[behaviour]);
        }
        Init();
    }
    public void TogglePageContentBottomHalf(bool toggle)
    {
        if (bottomTextRenderers != null)
        {
            for (int i = 0; i < bottomTextRenderers.Length; i++)
            {
                bottomTextRenderers[i].enabled = toggle;
            }
        }
        if (bottomRenderers != null)
        {
            for (int i = 0; i < bottomRenderers.Length; i++)
            {
                bottomRenderers[i].enabled = toggle;
            }
        }
    }
    public void TogglePageContentTopHalf(bool toggle)
    {
        if (topTextRenderers != null)
        {
            for (int i = 0; i < topTextRenderers.Length; i++)
            {
                topTextRenderers[i].enabled = toggle;
            }
        }

        if (topRenderers != null)
        {
            for (int i = 0; i < topRenderers.Length; i++)
            {
                topRenderers[i].enabled = toggle;
            }
        }
    }
    public void PlayPaperClip()
    {
        paperRenderer.PlayClipOneShot(paper_clip);
    }
    public void PlayPaperClipReverse()
    {
        paperRenderer.PlayClipOneShotReverse(paper_clip);
    }
    public void SetPageDepth(float depth)
    {
        transform.position = new Vector3(transform.position.x, transform.position.y, depth);
    }
    public void WriteSignature()
    {
        answerText = "J.E";
        answerTextBounds = activeAnswerTextRenderer.GetBounds(answerText);
        WriteText(activeAnswerTextRenderer, answerText, ctsWrite, Notepad.WRITE_LETTER_TIME);
    }
    public void WriteAnswerText(int answerIndex)
    {
        this.answerIndex = answerIndex;

        switch(promptType)
        {
            case TripPrompt.Stations:
            {
                answerText = trip.stationsDataArray[this.answerIndex].stationName;
            }
            break;
            case TripPrompt.Sports_Teams:
            {

            }
            break;
        }
        answerTextBounds = activeAnswerTextRenderer.GetBounds(answerText);
        WriteText(activeAnswerTextRenderer, answerText, ctsWrite, Notepad.WRITE_LETTER_TIME);
    }
    public void EraseChosenStationText()
    {
        EraseText(answerText, activeAnswerTextRenderer, ctsWrite, Notepad.WRITE_LETTER_TIME);
    }
    public void SetPreviewAnswerText(int answerIndex)
    {
        switch (promptType)
        {
            case TripPrompt.Stations:
            {
                previewAnswerText = trip.stationsDataArray[answerIndex].stationName;
                activeAnswerTextRenderer.SetText(previewAnswerText);
                answerTextBounds = activeAnswerTextRenderer.GetBounds(previewAnswerText);
            }
            break;
        }
    }
    public void SetPreviewSignatureText()
    {
        previewAnswerText = "J.E";
        activeAnswerTextRenderer.SetText(previewAnswerText);
        answerTextBounds = activeAnswerTextRenderer.GetBounds(previewAnswerText);
    }
    public void SetAnswerTextAlpha(float normAmount)
    {
        for (int i = 0; i < answerTextRenderers.Length; i++)
        {
            AtlasTextRenderer renderer = answerTextRenderers[i];
            renderer.SetAppearTextAlpha(normAmount);
        }
    }
    public void UpdatePreviewAnswerText(bool appear,  ref float clock)
    {
        switch (pageType)
        {
            case PageType.Profile:
            case PageType.Confirm:
            {
                activeAnswerTextRenderer.UpdateAppearTextAlpha(normAmount: 0.5f, appear, ref clock);
            }
            break;
        }

    }
    public void InvertExitButton(bool invert, bool pointDown)
    {
        if (exitButton_renderer.flipY != pointDown)
        {
            InvertButton(invert, exitButton_renderer);
            exitButton_renderer.FlipV(pointDown);
        }
    }
    public void InvertLeftArrowButton(bool invert)
    {
        if (paperCornerLeftButtonRenderer != null)
        {
            InvertButton(invert, paperCornerLeftButtonRenderer);
        }
    }
    public void InvertRightArrowButton(bool invert)
    {
        if (paperCornerRightButtonRenderer != null)
        {
            InvertButton(invert, paperCornerRightButtonRenderer);
        }
    }
    public Bounds GetWritingBounds()
    {
        return activeAnswerTextRenderer.GetBounds(previewAnswerText);
    }
}
