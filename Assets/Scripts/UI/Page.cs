using System;
using System.Reflection;
using System.Threading;
using UnityEngine;
using static Atlas;
using static AtlasUI;
using static NPC;
using static Spy;
public class Page : MonoBehaviour
{
    public const float FLIP_LOCAL_POS_Y = -1.656f;

    public static AtlasClip paper_clip;
    public PageType pageType;
    public TripPrompt promptType;
    public TripClue clueType;

    public TripSO trip;
    public NPCsDataSO npcData;
    public SpyStatsSO spyStats;
    public ColorsSO colors;

    public AtlasRenderer paperRenderer;

    public AtlasTextRenderer[] proceduralTextRenderers;
    public AtlasTextRenderer[] readOnlyTextRenderers;
    public AtlasTextRenderer[] playerWriteTextRenderers;

    public AtlasRenderer[] pictureRenderers;

    public AtlasTextRenderer pageNumberRenderer;
    public AtlasRenderer paperCornerLeftButtonRenderer;
    public AtlasRenderer paperCornerRightButtonRenderer;
    public AtlasRenderer exitButton_renderer;

    public AtlasRenderer[] bottomRenderers;
    public AtlasRenderer[] topRenderers;
    
    public ColorPicker colorPicker;

    [Header("Generated")]
    public AtlasTextRenderer activePlayerWriteTextRenderer;

    public int playerWriteIndex;
    public int traitorIndex;
    public int previewPlayerWriteIndex;
    public int activePlayerWriteRowIndex;

    public bool[] isPlayerWriteTextPreviewSet;
    public string[] playerWriteTexts;

    public Bounds playerWriteTextBounds;
    
    public string activePlayerWriteText;
    public string previewPlayerWriteText;

    //public string playerSignature; //TODO: Put this in a player profile scriptable object

    public CancellationTokenSource ctsWrite;
    private void OnEnable()
    {
        CursorController.OnMouseEnabled += ToggleCursorControlIcons;
    }
    private void OnDisable()
    {
        CursorController.OnMouseEnabled -= ToggleCursorControlIcons;
        ctsWrite?.Cancel();
        ctsWrite = null;
    }
    public void Init()
    {
        paper_clip = paperRenderer.atlas.clipDict[(int)NotepadMotion.FlipPage];
        
        playerWriteIndex = -1;

        switch (pageType)
        {
            case PageType.Prompt:
            {
                string promptText = AtlasUI.PromptStringDict[trip.prompt];
                proceduralTextRenderers[0].SetText(promptText);
            }
            break;

            case PageType.Confirm:
            {
                activePlayerWriteTextRenderer = playerWriteTextRenderers[0];
                isPlayerWriteTextPreviewSet = new bool[playerWriteTextRenderers.Length];
                playerWriteTexts = new string[playerWriteTextRenderers.Length];
                Array.Fill(playerWriteTexts, "");
            }
            break;

            case PageType.ColorKey:
            {
                activePlayerWriteTextRenderer = playerWriteTextRenderers[0];
                
                isPlayerWriteTextPreviewSet = new bool[playerWriteTextRenderers.Length];
                playerWriteTexts = new string[playerWriteTextRenderers.Length];
                Array.Fill(playerWriteTexts, "");

                int traitorProfileLoops = 0;
                for (int i = 0; i < playerWriteTextRenderers.Length; i++)
                {
                    int traitorProfileIndex = i;
                    if (i == trip.traitorProfiles.Length)
                    {
                        traitorProfileLoops++;
                        traitorProfileIndex = 0;
                    }
                    Behaviours behaviours = trip.traitorProfiles[traitorProfileIndex].npcProfile.behaviours;
                    Behaviours curBehaviour = GetBehaviourAtIndex(behaviours, traitorProfileLoops);

                    previewPlayerWriteText = npcData.behaviourStringDict[curBehaviour];
                    playerWriteTextRenderers[i].SetText(previewPlayerWriteText);
                }

                colors.behaviourColors = new Color[playerWriteTextRenderers.Length];
            }
            break;
        }
    }
    public void InitProfile(TraitorProfile traitorProfile)
    {
        for (int i = 0; i < proceduralTextRenderers.Length; i++)
        {
            Behaviours behaviour = GetBehaviourAtIndex(traitorProfile.npcProfile.behaviours, i);
            proceduralTextRenderers[i].SetText(npcData.behaviourStringDict[behaviour]);
        }
        
        activePlayerWriteTextRenderer = playerWriteTextRenderers[0];
        
        isPlayerWriteTextPreviewSet = new bool[playerWriteTextRenderers.Length];
        playerWriteTexts = new string[playerWriteTextRenderers.Length];
        Array.Fill(playerWriteTexts, "");

        for (int i = 0; i < playerWriteTextRenderers.Length; i++)
        {
            playerWriteTextRenderers[i].SetText(trip.stationsDataArray[previewPlayerWriteIndex].stationName);
        }
        AtlasRenderer coveredMugShot = pictureRenderers[0];
        AtlasRenderer uncoveredMugShot = pictureRenderers[1];

        int coveredMugShotIndex = traitorProfile.coveredMugshotIndex;
        int uncoveredMugShotIndex = traitorProfile.uncoveredMugshotIndex;
        coveredMugShot.UpdateSpriteInputs(coveredMugShot.atlas.simpleSprites[coveredMugShotIndex]);
        uncoveredMugShot.UpdateSpriteInputs(uncoveredMugShot.atlas.simpleSprites[uncoveredMugShotIndex]);
        Init();
    }
    public void TogglePageContentBottomHalf(bool toggle)
    {
        ToggleTextRenderers(playerWriteTextRenderers, toggle, topHalf: false);
        ToggleTextRenderers(readOnlyTextRenderers, toggle, topHalf: false);
        ToggleTextRenderers(proceduralTextRenderers, toggle, topHalf: false);

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
        ToggleTextRenderers(playerWriteTextRenderers, toggle, topHalf: true);
        ToggleTextRenderers(readOnlyTextRenderers, toggle, topHalf: true);
        ToggleTextRenderers(proceduralTextRenderers, toggle, topHalf: true);

        if (topRenderers != null)
        {
            for (int i = 0; i < topRenderers.Length; i++)
            {
                topRenderers[i].enabled = toggle;
            }
        }
    }

    public void ToggleTextRenderers(AtlasTextRenderer[] textRenderers, bool toggle, bool topHalf)
    {
        if (textRenderers != null)
        {
            for (int i = 0; i < textRenderers.Length; i++)
            {
                AtlasTextRenderer renderer = textRenderers[i];

                bool condition = topHalf ? renderer.transform.localPosition.y >= FLIP_LOCAL_POS_Y : renderer.transform.localPosition.y < FLIP_LOCAL_POS_Y;
                if (condition)
                {
                    renderer.enabled = toggle;
                }
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
    public void WritePlayerWriteText()
    {
        playerWriteIndex = previewPlayerWriteIndex;
        activePlayerWriteText = activePlayerWriteTextRenderer.text;
        playerWriteTexts[activePlayerWriteRowIndex] = activePlayerWriteText;
        playerWriteTextBounds = activePlayerWriteTextRenderer.GetBounds(activePlayerWriteText);

        switch (pageType)
        { 
            case PageType.ColorKey:
            {
                //colors
            }
            break;
        }


        WriteText(activePlayerWriteTextRenderer, activePlayerWriteText, ctsWrite, Notepad.WRITE_LETTER_TIME);
    }
    public void ErasePlayerWriteText()
    {
        playerWriteTexts[activePlayerWriteRowIndex] = "";
        EraseText(activePlayerWriteText, activePlayerWriteTextRenderer, ctsWrite, Notepad.WRITE_LETTER_TIME);
    }
    public void SwitchActivePreviewPlayerWriteText(int indexOffset)
    {
        previewPlayerWriteIndex += indexOffset;

        switch (pageType)
        {
            case PageType.Profile:
            {
                switch (promptType)
                {
                    case TripPrompt.Stations:
                    {
                        previewPlayerWriteIndex = (previewPlayerWriteIndex + trip.stationsDataArray.Length) % trip.stationsDataArray.Length;
                        previewPlayerWriteText = trip.stationsDataArray[previewPlayerWriteIndex].stationName;
                    }
                    break;
                }

                activePlayerWriteTextRenderer.SetText(previewPlayerWriteText);
                playerWriteTextBounds = activePlayerWriteTextRenderer.GetBounds(previewPlayerWriteText);
                isPlayerWriteTextPreviewSet[0] = false;
            }
            break;

            case PageType.ColorKey:
            {
                switch (trip.clue)
                {
                    case TripClue.Behaviours:
                    {
                        if (activePlayerWriteText == "")
                        {
                            int behaviourLength = (int)Mathf.Log((int)Behaviours.Count, 2) - 1;
                            previewPlayerWriteIndex = ((previewPlayerWriteIndex + behaviourLength) % behaviourLength);
                            Behaviours allBehaviours = (Behaviours)~(1 << behaviourLength);
                            Behaviours activeBehaviour = GetBehaviourAtIndex(allBehaviours, previewPlayerWriteIndex);
                            previewPlayerWriteText = npcData.behaviourStringDict[activeBehaviour];
                            activePlayerWriteTextRenderer.SetText(previewPlayerWriteText);

                            isPlayerWriteTextPreviewSet[activePlayerWriteRowIndex] = false;

                            playerWriteTextBounds = activePlayerWriteTextRenderer.GetBounds(previewPlayerWriteText);
                        }
                    }
                    break;
                }
            }
            break;
        }

    }
    public void SetPreviewPlayerWriteTexts(NotepadState prevNotepadState)
    {
        switch(pageType)
        {
            case PageType.Profile:
            {
                switch (promptType)
                {
                    case TripPrompt.Stations:
                    {
                        previewPlayerWriteText = trip.stationsDataArray[previewPlayerWriteIndex].stationName;
                        activePlayerWriteTextRenderer.SetText(previewPlayerWriteText);
                    }
                    break;
                }
            }
            break;

            case PageType.Confirm:
            {
                if (spyStats.curLocationState != LocationState.Station)
                {
                    previewPlayerWriteText = "J.E";
                    activePlayerWriteTextRenderer.SetText(previewPlayerWriteText);
                }
            }
            break;

            case PageType.ColorKey:
            {
                if (prevNotepadState == NotepadState.Erasing)
                {
                    int behaviourLength = (int)Behaviours.Count - 1;
                    Behaviours allBehaviours = (Behaviours)~(1 << behaviourLength);
                    Behaviours activeBehaviour = GetBehaviourAtIndex(allBehaviours, previewPlayerWriteIndex);
                    previewPlayerWriteText = npcData.behaviourStringDict[activeBehaviour];
                    activePlayerWriteTextRenderer.SetText(previewPlayerWriteText);
                }
            }
            break;
        }

        if (playerWriteTextRenderers.Length > 0)
        {
            if (prevNotepadState == NotepadState.Erasing)
            {
                isPlayerWriteTextPreviewSet[activePlayerWriteRowIndex] = false;
            }
            else
            {
                for (int i = 0; i < isPlayerWriteTextPreviewSet.Length; i++)
                {
                    if (playerWriteTexts[i] == "")
                    {
                        isPlayerWriteTextPreviewSet[i] = false;
                    }
                }
            }
            playerWriteTextBounds = activePlayerWriteTextRenderer.GetBounds(previewPlayerWriteText);
        }

    }
    public void SetPlayerWriteTextAlphaBottom(float normAmount)
    {
        for (int i = 0; i < playerWriteTextRenderers.Length; i++)
        {
            AtlasTextRenderer renderer = playerWriteTextRenderers[i];

            if (renderer.transform.localPosition.y < FLIP_LOCAL_POS_Y)
            {
                if (playerWriteTexts[i] != "") continue;

                renderer.SetAppearTextAlpha(normAmount);
            }
        }
    }
    public void SetPlayerWriteTextAlphaTop(float normAmount)
    {
        for (int i = 0; i < playerWriteTextRenderers.Length; i++)
        {
            AtlasTextRenderer renderer = playerWriteTextRenderers[i];

            if (renderer.transform.localPosition.y >= FLIP_LOCAL_POS_Y)
            {
                if (playerWriteTexts[i] != "") continue;

                renderer.SetAppearTextAlpha(normAmount);
            }
        }
    }
    public void UpdatePage()
    {
        switch (pageType)
        {
            case PageType.ColorKey:
            {
                for (int i = 0; i < pictureRenderers.Length; i++)
                {
                    AtlasRenderer picRenderer = pictureRenderers[i];
                    colorPicker.OpenColorPicker(picRenderer);
                    if (ColorPicker.IsHoveringColorPicker) break;
                }

                colorPicker.CloseColorPicker();
                colorPicker.SetNewColor();
            }
            break;
        }
    }
    public void UpdatePreviewPlayerWriteText(bool appear, ref float clock)
    {
        switch (pageType)
        {
            case PageType.Profile:
            case PageType.Confirm:
            {
                if (isPlayerWriteTextPreviewSet[0]) return;
                activePlayerWriteTextRenderer.UpdateAppearTextAlpha(normAmount: 0.5f, appear, ref clock);
                if (clock <= 0) isPlayerWriteTextPreviewSet[0] = true;
            }
            break;
            case PageType.ColorKey:
            {
                for(int i = 0; i < playerWriteTextRenderers.Length; i++)
                {
                    if (isPlayerWriteTextPreviewSet[i]) continue;
                    AtlasTextRenderer behaviourTextRend = playerWriteTextRenderers[i];
                    behaviourTextRend.UpdateAppearTextAlpha(normAmount: 0.5f, appear, ref clock);
                    if (clock <= 0) isPlayerWriteTextPreviewSet[i] = true;
                }
            }
            break;
        }
    }
    public void InvertExitButton(bool invert, bool pointDown)
    {
        if (exitButton_renderer.flipY != pointDown)
        {
            InvertButton(invert, exitButton_renderer);
            if (CursorController.active)
            {
                exitButton_renderer.FlipV(pointDown);
            }
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
    public void UpdateMugShotReveal(float t)
    {
        AtlasRenderer coveredMugShot = pictureRenderers[0];
        coveredMugShot.custom.x = Mathf.Clamp01(t);
    }
    public void SwitchActivePLayerWriteTextRenderer(int numpad)
    {
        if (numpad > playerWriteTextRenderers.Length) return;

        activePlayerWriteRowIndex = numpad - 1;
        activePlayerWriteTextRenderer = playerWriteTextRenderers[activePlayerWriteRowIndex];
        previewPlayerWriteText = activePlayerWriteTextRenderer.text;
        activePlayerWriteText = playerWriteTexts[activePlayerWriteRowIndex];
    }
    public void ToggleCursorControlIcons()
    {
        if (CursorController.active)
        {
            exitButton_renderer.UpdateSpriteInputsByIndex(3);
            paperCornerLeftButtonRenderer?.UpdateSpriteInputsByIndex(4);
            paperCornerRightButtonRenderer?.UpdateSpriteInputsByIndex(4);
        }
        else
        {
            exitButton_renderer.UpdateSpriteInputsByIndex(7);
            paperCornerLeftButtonRenderer?.UpdateSpriteInputsByIndex(10);
            paperCornerRightButtonRenderer?.UpdateSpriteInputsByIndex(8);

        }
        paperCornerLeftButtonRenderer?.FlipH(CursorController.active);
        exitButton_renderer.FlipV(CursorController.active);
    }
    public Bounds GetWritingBounds()
    {
        return activePlayerWriteTextRenderer.GetBounds(previewPlayerWriteText);
    }
}
