using System;
using UnityEngine;
using System.Collections.Generic;

using static Atlas;
using static AtlasUI;
using static Notepad;
using static NPC;
using System.Linq;
public class Page : MonoBehaviour
{
    public const float FLIP_LOCAL_POS_Y = -1.656f;
    public const float PAGE_NUMBER_ICON_BUFFER_X = 0.05f;
    public const float PAGE_NUMBER_ICON_BUFFER_Y = 0.15f;
    public static AtlasClip paper_clip;

    public PageType pageType;
    public TripPrompt promptType;
    public TripClue clueType;

    public TripSO trip;
    public NPCsDataSO npcData;
    public SpyStatsSO spyStats;
    public PlayerInputsSO playerInputs;
    public NotepadData notepadData;
    public OptionsSO options;

    public AtlasRenderer pageNumberIconPrefab;

    public AtlasRenderer paperRenderer;

    public AtlasTextRenderer[] proceduralTextRenderers;
    public AtlasTextRenderer[] readOnlyTextRenderers;
    public AtlasTextRenderer[] playerWriteTextRenderers;

    public AtlasRenderer[] proceduralRenderers;
    public AtlasRenderer[] readOnlyRenderers;
    public AtlasRenderer[] playerWriteRenderers;

    public IconUIElement[] otherButtons;

    public IconUIElement leftButton;
    public IconUIElement rightButton;
    public IconUIElement exitButton;

    [Header("Generated")]
    public AtlasTextRenderer activePlayerWriteTextRenderer;
    public AtlasRenderer[] pageNumberIconRenderers;

    public int playerWriteIndex;
    public int traitorIndex;
    public int previewPlayerWriteIndex;
    public int activePlayerWriteRowIndex;
    public int pageIndex;

    public string[] playerWriteTexts;

    public Bounds playerWriteTextBounds;
    
    public string activePlayerWriteText;
    public string previewPlayerWriteText;

    public void Init(int pageIndexInput, int totalPages)
    {
        paper_clip = paperRenderer.atlas.clipDict[(int)NotepadMotion.FlipPage];
        
        playerWriteIndex = -1;
        
        exitButton.InitButton(ClickExitButton, EnterButton, ExitButton);

        Bounds pageNumberIconBounds = pageNumberIconPrefab.bounds;
        float colSize = pageNumberIconBounds.size.x + PAGE_NUMBER_ICON_BUFFER_X;

        pageNumberIconRenderers = new AtlasRenderer[totalPages];
        pageIndex = pageIndexInput;

        Vector3 startPos = new Vector3();
        startPos.x = (float)totalPages * colSize * -0.5f;
        startPos.y = -paperRenderer.bounds.size.y + PAGE_NUMBER_ICON_BUFFER_Y;
        startPos.z = exitButton.renderer.transform.localPosition.z;

        for (int i = 0; i < totalPages; i++)
        {
            AtlasRenderer pageNumberIcon = Instantiate(pageNumberIconPrefab, transform);
            Vector3 localPos = startPos;
            
            localPos.x += i * colSize;
            pageNumberIcon.transform.localPosition = localPos;

            if (i == pageIndex)
            {
                pageNumberIcon.custom.x = 1f;
            }
            pageNumberIconRenderers[i] = pageNumberIcon;
        }

        switch (pageType)
        {
            case PageType.Prompt:
            {
                rightButton.InitButton(ClickRightButton, EnterButton, ExitButton);
            }
            break;
            case PageType.Profile:
            {
                rightButton.InitButton(ClickRightButton, EnterButton, ExitButton);
                leftButton.InitButton(ClickLeftButton, EnterButton, ExitButton);
            }
            break;
            case PageType.ColorKey:
            {
                leftButton.InitButton(ClickLeftButton, EnterButton, ExitButton);

                for (int i = 0; i < otherButtons.Length; i++)
                {
                    int index = i;
                    
                    void ClickColor(IconUIElement icon)
                    {
                        SceneController.GetClueColorPicker().Open(otherButtons[index].renderer, ColorPicker.SelectType.Clue);
                        SwitchActivePLayerWriteTextRenderer(index);
                        trip.selectedColorMarkerIndex = activePlayerWriteRowIndex;
                    }

                    otherButtons[i].InitButton(ClickColor, EnterButton, ExitButton);
                }

                playerWriteTexts = new string[playerWriteTextRenderers.Length];
                Array.Fill(playerWriteTexts, "");

                for (int i = 0; i < proceduralRenderers.Length; i++)
                {
                    proceduralRenderers[i].custom.w = 0;
                }
            }
            break;
        }
    }
    public void InitNextColorRow(int nextIndex)
    {
        AtlasTextRenderer playerWriteTextRend = playerWriteTextRenderers[nextIndex];

        int traitorIndex = nextIndex % trip.traitorProfiles.Length;
        Behaviours behaviours = trip.traitorProfiles[traitorIndex].npcProfile.behaviours;

        int behaveIndex = nextIndex % 2;
        Behaviours curBehaviour = GetBehaviourAtIndex(behaviours, behaveIndex);

        previewPlayerWriteText = npcData.behaviourStringDict[curBehaviour];
        playerWriteTextRend.SetText(previewPlayerWriteText);
        playerWriteTextRend.SetAppearTextAlpha(0);

        playerWriteTextRend.enabled = true;
    }
    public void ShowRuleOutRenderer()
    {
        proceduralRenderers[0].ChangeCustom(time: 1, newValue: 1, customChannel: 4);
    }
    public void SetColorMarkerButtonSprite(int index)
    {
        AtlasRenderer playerWriteRend = playerWriteRenderers[index];

        switch (index)
        {
            case 0:
            {
                playerWriteRend.UpdateSpriteInputsByIndex(ONE_NUMPAD_SPRITE_INDEX);
            }
            break;
            case 1:
            {
                playerWriteRend.UpdateSpriteInputsByIndex(TWO_NUMPAD_SPRITE_INDEX);
            }
            break;
        }
        proceduralRenderers[index].ChangeCustom(time: 1, newValue: 1, customChannel: 4);
    }
    public void UpdatePage()
    {
        exitButton.UpdateButton(playerInputs);

        switch (pageType)
        {
            case PageType.Prompt:
            {
                rightButton.UpdateButton(playerInputs);
            }
            break;
            case PageType.Profile:
            {
                rightButton.UpdateButton(playerInputs);
                leftButton.UpdateButton(playerInputs);

            }
            break;
            case PageType.ColorKey:
            {
                leftButton.UpdateButton(playerInputs);
                for (int i = 0; i < trip.unlockedColorMarkerCount; i++)
                {
                    otherButtons[i].UpdateButton(playerInputs);
                }

                if (playerInputs.shiftDown && activePlayerWriteRowIndex > 0)
                {
                    SceneController.GetClueColorPicker().Open(playerWriteRenderers[activePlayerWriteRowIndex], ColorPicker.SelectType.Clue);
                }
            }
            break;
        }
    }
    public void InitProfile(TraitorProfile traitorProfile, int pageIndex, int totalPages)
    {
        for (int i = 0; i < proceduralTextRenderers.Length; i++)
        {
            Behaviours behaviour = GetBehaviourAtIndex(traitorProfile.npcProfile.behaviours, i);
            proceduralTextRenderers[i].SetText(npcData.behaviourStringDict[behaviour]);
        }
        
        activePlayerWriteTextRenderer = playerWriteTextRenderers[0];

        playerWriteTexts = new string[playerWriteTextRenderers.Length];
        Array.Fill(playerWriteTexts, "");

        for (int i = 0; i < playerWriteTextRenderers.Length; i++)
        {
            AtlasTextRenderer playerWriteTextRenderer = playerWriteTextRenderers[i];
            playerWriteTextRenderer.SetText(trip.stationsDataArray[previewPlayerWriteIndex].stationName);
            playerWriteTextRenderer.SetColorText(options.meridiaColor);
        }
        AtlasRenderer coveredMugShot = playerWriteRenderers[0];
        AtlasRenderer uncoveredMugShot = playerWriteRenderers[1];

        int uncoveredMugShotIndex = traitorProfile.mugShotIndex * 2;
        int coveredMugShotIndex = uncoveredMugShotIndex + 1;
        coveredMugShot.UpdateSpriteInputs(coveredMugShot.atlas.simpleSprites[coveredMugShotIndex]);
        uncoveredMugShot.UpdateSpriteInputs(uncoveredMugShot.atlas.simpleSprites[uncoveredMugShotIndex]);
        Init(pageIndex, totalPages);
    }
    public void TogglePageContentBottomHalf(bool toggle)
    {
        ToggleTextRenderers(playerWriteTextRenderers, toggle, topHalf: false);
        ToggleTextRenderers(readOnlyTextRenderers, toggle, topHalf: false);
        ToggleTextRenderers(proceduralTextRenderers, toggle, topHalf: false);

        ToggleRenderers(readOnlyRenderers, toggle, topHalf: false);
        ToggleRenderers(playerWriteRenderers, toggle, topHalf: false);
        ToggleRenderers(pageNumberIconRenderers, toggle, topHalf: false);
    }
    public void TogglePageContentTopHalf(bool toggle)
    {
        ToggleTextRenderers(playerWriteTextRenderers, toggle, topHalf: true);
        ToggleTextRenderers(readOnlyTextRenderers, toggle, topHalf: true);
        ToggleTextRenderers(proceduralTextRenderers, toggle, topHalf: true);

        ToggleRenderers(readOnlyRenderers, toggle, topHalf: true);
        ToggleRenderers(playerWriteRenderers, toggle, topHalf: true);
        ToggleRenderers(pageNumberIconRenderers, toggle, topHalf: true);
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
    public void SetPageDepth(float localDepth)
    {
        transform.localPosition = new Vector3(transform.localPosition.x, transform.localPosition.y, localDepth);
    }
    public void WritePlayerWriteText()
    {
        playerWriteIndex = previewPlayerWriteIndex;
        activePlayerWriteText = activePlayerWriteTextRenderer.text;
        playerWriteTexts[activePlayerWriteRowIndex] = activePlayerWriteText;
        playerWriteTextBounds = activePlayerWriteTextRenderer.GetBoundsNewText(activePlayerWriteText);

        activePlayerWriteTextRenderer.WriteText(activePlayerWriteText, Notepad.WRITE_LETTER_TIME);
    }
    public void WriteForPlayerWriteText(string text)
    {
        playerWriteIndex = previewPlayerWriteIndex;
        activePlayerWriteText = text;
        playerWriteTexts[activePlayerWriteRowIndex] = activePlayerWriteText;
        playerWriteTextBounds = activePlayerWriteTextRenderer.GetBoundsNewText(activePlayerWriteText);

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

                activePlayerWriteTextRenderer.SetText(previewPlayerWriteText, alpha: 0);
                activePlayerWriteTextRenderer.ChangeCustom(time: APPEAR_TEXT_TIME, newValue: 0.5f, customChannel: 4);
                playerWriteTextBounds = activePlayerWriteTextRenderer.GetBoundsNewText(previewPlayerWriteText);
            }
            break;

            case PageType.ColorKey:
            {
                switch (trip.clue)
                {
                    case TripClue.Behaviours:
                    {
                        if (activePlayerWriteTextRenderer != null)
                        {
                            int behaviourLength = (int)Behaviours.Count;
                            previewPlayerWriteIndex = (previewPlayerWriteIndex + behaviourLength) % behaviourLength;
                            Behaviours allBehaviours = (Behaviours)~((1 << behaviourLength) | (int)Behaviours.None);
                            Behaviours activeBehaviour = GetBehaviourAtIndex(allBehaviours, previewPlayerWriteIndex);
                            previewPlayerWriteText = npcData.behaviourStringDict[activeBehaviour];
                            
                            activePlayerWriteTextRenderer.SetText(previewPlayerWriteText, alpha: 0);
                            activePlayerWriteTextRenderer.ChangeCustom(time: APPEAR_TEXT_TIME, newValue: 0.5f, customChannel: 4);
                            
                            playerWriteTextBounds = activePlayerWriteTextRenderer.GetBoundsNewText(previewPlayerWriteText);
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
                        if (playerWriteTexts[activePlayerWriteRowIndex] == "")
                        {
                            previewPlayerWriteText = trip.stationsDataArray[previewPlayerWriteIndex].stationName;
                            activePlayerWriteTextRenderer.SetText(previewPlayerWriteText, alpha: 0);
                        }
                    }
                    break;
                }
                
                if (playerWriteTextRenderers.Length > 0)
                {
                    playerWriteTextBounds = activePlayerWriteTextRenderer.GetBoundsNewText(previewPlayerWriteText);
                }
            }
            break;

            case PageType.ColorKey:
            {
                if (prevNotepadState == NotepadState.Erasing)
                {
                    int behaviourLength = (int)Behaviours.Count;
                    previewPlayerWriteIndex = (previewPlayerWriteIndex + behaviourLength) % behaviourLength;
                    Behaviours allBehaviours = (Behaviours)~((1 << behaviourLength) | (int)Behaviours.None);
                    Behaviours activeBehaviour = GetBehaviourAtIndex(allBehaviours, previewPlayerWriteIndex);
                    previewPlayerWriteText = npcData.behaviourStringDict[activeBehaviour];
                    activePlayerWriteTextRenderer.SetText(previewPlayerWriteText, alpha: 0);
                }
                else
                {
                    if ((trip.curUnlocks & UnlockType.Color) != 0)
                    {
                        for (int i = 0; i < trip.unlockedColorMarkerCount; i++)
                        {
                            if (playerWriteTexts[i] != "") continue;

                            AtlasTextRenderer curRend = playerWriteTextRenderers[i];
                            int behaviourLength = (int)Behaviours.Count;
                            previewPlayerWriteIndex = (previewPlayerWriteIndex + behaviourLength) % behaviourLength;
                            Behaviours allBehaviours = (Behaviours)~((1 << behaviourLength) | (int)Behaviours.None);
                            Behaviours activeBehaviour = GetBehaviourAtIndex(allBehaviours, previewPlayerWriteIndex);
                            previewPlayerWriteText = npcData.behaviourStringDict[activeBehaviour];
                            curRend.SetText(previewPlayerWriteText, alpha: 0);
                        }

                        if (playerWriteTextRenderers.Length > 0)
                        {
                            if (activePlayerWriteTextRenderer == null) activePlayerWriteTextRenderer = playerWriteTextRenderers[activePlayerWriteRowIndex];
                            playerWriteTextBounds = activePlayerWriteTextRenderer.GetBoundsNewText(previewPlayerWriteText);
                        }
                    }
                }
            }
            break;
        }
        UpdatePreviewPlayerWriteText();
    }
    public void SetActiveRendererText(string text)
    {
        activePlayerWriteTextRenderer?.SetText(text);
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
    public void UpdatePreviewPlayerWriteText()
    {
        switch (pageType)
        {
            case PageType.Profile:
            {
                if (activePlayerWriteText == "")
                {
                    activePlayerWriteTextRenderer.ChangeCustom(time: APPEAR_TEXT_TIME, newValue: 0.5f, customChannel: 4);
                }
                else
                {
                    activePlayerWriteTextRenderer.ChangeCustom(time: APPEAR_TEXT_TIME, newValue: 1, customChannel: 4);
                }
            }
            break;
            case PageType.ColorKey:
            {
                if ((trip.curUnlocks & UnlockType.RuleOut) != 0)
                {
                    for (int i = 0; i < trip.unlockedColorMarkerCount; i++)
                    {
                        AtlasTextRenderer playerWriteTextRend = playerWriteTextRenderers[i];
                        if (playerWriteTextRend.text == "") continue;
                        if (playerWriteTexts[i] == "")
                        {
                            playerWriteTextRend.ChangeCustom(time: APPEAR_TEXT_TIME, newValue: 0.5f, customChannel: 4);
                        }
                        else
                        {
                            playerWriteTextRend.ChangeCustom(time: APPEAR_TEXT_TIME, newValue: 1, customChannel: 4);
                        }
                    }
                }
            }
            break;
        }
    }
    public void UpdateButtons()
    {
        exitButton.UpdateButton(playerInputs);

        switch (pageType)
        {
            case PageType.Prompt:
            {
                rightButton.UpdateButton(playerInputs);
            }
            break;
            case PageType.Profile:
            {
                rightButton.UpdateButton(playerInputs);
                leftButton.UpdateButton(playerInputs);
            }
            break;
            case PageType.ColorKey:
            {
                leftButton.UpdateButton(playerInputs);

                for (int i = 0; i < otherButtons.Length; i++)
                {
                    otherButtons[i].UpdateButton(playerInputs);
                }
            }
            break;
        }
    }
    public void EnterButton(IconUIElement icon)
    {
        icon.renderer.custom.x = 1;
    }
    public void ExitButton(IconUIElement icon)
    {
        icon.renderer.custom.x = 0;
    }
    public void ClickExitButton(IconUIElement icon)
    {
        if (spyStats.checkingNotepad)
        {
            spyStats.checkingNotepad = false;
        }
        else
        {
            spyStats.checkingNotepad = true;
        }
    }
    public void ClickLeftButton(IconUIElement icon)
    {
        notepadData.subState |= SubState.IsFlippingDown;
    }
    public void ClickRightButton(IconUIElement icon)
    {
        notepadData.subState |= SubState.IsFlippingUp;
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
        return activePlayerWriteTextRenderer.GetBoundsNewText(previewPlayerWriteText);
    }
}
