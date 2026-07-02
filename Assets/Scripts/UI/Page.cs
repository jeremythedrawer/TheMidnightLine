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
    public PlayerInputsSO playerInputs;

    public AtlasRenderer paperRenderer;

    public AtlasTextRenderer[] proceduralTextRenderers;
    public AtlasTextRenderer[] readOnlyTextRenderers;
    public AtlasTextRenderer[] playerWriteTextRenderers;

    public AtlasRenderer[] playerWriteRenderers;
    public AtlasRenderer[] readOnlyRenderers;

    public AtlasTextRenderer pageNumberRenderer;
    public AtlasRenderer paperCornerLeftButtonRenderer;
    public AtlasRenderer paperCornerRightButtonRenderer;
    public AtlasRenderer exitButton_renderer;

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

            case PageType.ColorKey:
            {
                activePlayerWriteTextRenderer = playerWriteTextRenderers[0];
                
                isPlayerWriteTextPreviewSet = new bool[playerWriteTextRenderers.Length];
                playerWriteTexts = new string[playerWriteTextRenderers.Length];
                Array.Fill(playerWriteTexts, "");
            }
            break;
        }
    }
    public void SetClueRows(int oldIndex)
    {
        for (int i = oldIndex; i < trip.unlockedClueMarkerCount; i++)
        {
            AtlasTextRenderer playerWriteTexRend = playerWriteTextRenderers[i];

            int traitorIndex = i % trip.traitorProfiles.Length;
            Behaviours behaviours = trip.traitorProfiles[traitorIndex].npcProfile.behaviours;

            int behaveIndex = i % 2;
            Behaviours curBehaviour = GetBehaviourAtIndex(behaviours, behaveIndex);

            previewPlayerWriteText = npcData.behaviourStringDict[curBehaviour];
            playerWriteTexRend.SetText(previewPlayerWriteText);
            playerWriteTexRend.enabled = true;
            playerWriteTexRend.SetAppearTextAlpha(normAmount: 0.5f);
            playerWriteRenderers[i].custom.x = 0;
            playerWriteRenderers[i].custom.y = 0;
            playerWriteRenderers[i].custom.z = 0;
            playerWriteRenderers[i].custom.w = 0;
            playerWriteRenderers[i].UpdateSpriteInputsByIndex(COLOR_SQUARE_SPRITE_INDEX);
        }
    }
    public void SetNextClueRow()
    {
        int i = trip.unlockedClueMarkerCount - 1;
        AtlasTextRenderer playerWriteTexRend = playerWriteTextRenderers[i];

        int traitorIndex = i % trip.traitorProfiles.Length;
        Behaviours behaviours = trip.traitorProfiles[traitorIndex].npcProfile.behaviours;

        int behaveIndex = i % 2;
        Behaviours curBehaviour = GetBehaviourAtIndex(behaviours, behaveIndex);

        previewPlayerWriteText = npcData.behaviourStringDict[curBehaviour];
        playerWriteTexRend.SetText(previewPlayerWriteText);

        playerWriteTexRend.enabled = true;
        playerWriteRenderers[i].custom = Vector3.one;
        playerWriteRenderers[i].UpdateSpriteInputsByIndex(COLOR_SQUARE_SPRITE_INDEX);
    }
    public void UpdatePage()
    {
        switch (pageType)
        {
            case PageType.ColorKey:
            {
                bool foundColorKeyRend = false;
                for (int i = 0; i < trip.unlockedClueMarkerCount; i++)
                {
                    AtlasRenderer colorKeyRend = playerWriteRenderers[i];
                    if (CursorController.IsInsideBounds(colorKeyRend.GetBounds()) && !foundColorKeyRend)
                    {
                        colorKeyRend.custom.w = 0;

                        if (playerInputs.mouseLeftDown)
                        {
                            ColorPicker colorPicker = SceneController.GetColorPicker();
                            colorPicker.Open(colorKeyRend, openAllColors: true);
                            SwitchActivePLayerWriteTextRenderer(i);
                            trip.selectedClueMarkerIndex = activePlayerWriteRowIndex;

                        }
                        foundColorKeyRend = true;
                    }
                    else
                    {
                        colorKeyRend.custom.w = 1;
                    }
                }
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
        AtlasRenderer coveredMugShot = playerWriteRenderers[0];
        AtlasRenderer uncoveredMugShot = playerWriteRenderers[1];

        int uncoveredMugShotIndex = traitorProfile.mugShotIndex * 2;
        int coveredMugShotIndex = uncoveredMugShotIndex + 1;
        coveredMugShot.UpdateSpriteInputs(coveredMugShot.atlas.simpleSprites[coveredMugShotIndex]);
        uncoveredMugShot.UpdateSpriteInputs(uncoveredMugShot.atlas.simpleSprites[uncoveredMugShotIndex]);
        Init();
    }
    public void TogglePageContentBottomHalf(bool toggle)
    {
        ToggleTextRenderers(playerWriteTextRenderers, toggle, topHalf: false);
        ToggleTextRenderers(readOnlyTextRenderers, toggle, topHalf: false);
        ToggleTextRenderers(proceduralTextRenderers, toggle, topHalf: false);

        ToggleRenderers(readOnlyRenderers, toggle, topHalf: false);
        ToggleRenderers(playerWriteRenderers, toggle, topHalf: false);
    }
    public void TogglePageContentTopHalf(bool toggle)
    {
        ToggleTextRenderers(playerWriteTextRenderers, toggle, topHalf: true);
        ToggleTextRenderers(readOnlyTextRenderers, toggle, topHalf: true);
        ToggleTextRenderers(proceduralTextRenderers, toggle, topHalf: true);

        ToggleRenderers(readOnlyRenderers, toggle, topHalf: true);
        ToggleRenderers(playerWriteRenderers, toggle, topHalf: true);
    }
    public void ToggleTextRenderers(AtlasTextRenderer[] textRenderers, bool toggle, bool topHalf)
    {
        if (textRenderers != null)
        {
            if (topHalf)
            {
                for (int i = 0; i < textRenderers.Length; i++)
                {
                    AtlasTextRenderer renderer = textRenderers[i];
                    if (renderer.transform.localPosition.y >= FLIP_LOCAL_POS_Y)
                    {
                        renderer.enabled = toggle;
                    }
                }
            }
            else
            {
                for (int i = 0; i < textRenderers.Length; i++)
                {
                    AtlasTextRenderer renderer = textRenderers[i];
                    if (renderer.transform.localPosition.y < FLIP_LOCAL_POS_Y)
                    {
                        renderer.enabled = toggle;
                    }
                }
            }
        }
    }
    public void ToggleRenderers(AtlasRenderer[] renderers, bool toggle, bool topHalf)
    {
        if (topHalf)
        {
            for (int i = 0; i < renderers.Length; i++)
            {
                AtlasRenderer rend = renderers[i];

                float localBoundsMinY = transform.InverseTransformPoint(rend.GetBounds().center).y;

                if (localBoundsMinY >= FLIP_LOCAL_POS_Y)
                {
                    rend.enabled = toggle;
                }
            }
        }
        else
        {
            for (int i = 0; i < renderers.Length; i++)
            {
                AtlasRenderer rend = renderers[i];

                float localBoundsMaxY = transform.InverseTransformPoint(rend.GetBounds().center).y;

                if (localBoundsMaxY < FLIP_LOCAL_POS_Y)
                {
                    rend.enabled = toggle;
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

        activePlayerWriteTextRenderer.WriteText(activePlayerWriteText, Notepad.WRITE_LETTER_TIME);
    }
    public void ErasePlayerWriteText()
    {
        playerWriteTexts[activePlayerWriteRowIndex] = "";
        activePlayerWriteTextRenderer.EraseText(Notepad.WRITE_LETTER_TIME);
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
    public void UpdatePreviewPlayerWriteText(bool appear, ref float clock)
    {
        switch (pageType)
        {
            case PageType.Profile:
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
    public void InvertExitButton(bool invert)
    {
        InvertButton(invert, exitButton_renderer);
    }
    public void InvertLeftArrowButton(bool invert)
    {
        if (paperCornerLeftButtonRenderer == null) return;
        InvertButton(invert, paperCornerLeftButtonRenderer);
    }
    public void InvertRightArrowButton(bool invert)
    {
        if (paperCornerRightButtonRenderer == null) return;
        InvertButton(invert, paperCornerRightButtonRenderer);
    }
    public void UpdateMugShotReveal(float t)
    {
        AtlasRenderer coveredMugShot = playerWriteRenderers[0];
        coveredMugShot.custom.x = Mathf.Clamp01(t);
    }
    public void SwitchActivePLayerWriteTextRenderer(int index)
    {
        if (index < playerWriteTextRenderers.Length)
        {
            activePlayerWriteRowIndex = index;
            activePlayerWriteTextRenderer = playerWriteTextRenderers[activePlayerWriteRowIndex];
            previewPlayerWriteText = activePlayerWriteTextRenderer.text;
            activePlayerWriteText = playerWriteTexts[activePlayerWriteRowIndex];
        }
    }
    public Bounds GetWritingBounds()
    {
        return activePlayerWriteTextRenderer.GetBounds(previewPlayerWriteText);
    }
}
