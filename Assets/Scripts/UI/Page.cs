using System;
using UnityEngine;

using static Atlas;
using static AtlasUI;
using static Notepad;
using static NPC;
public class Page : MonoBehaviour
{
    public const float FLIP_LOCAL_POS_Y = -1.656f;
    public const float PAGE_NUMBER_ICON_BUFFER_X = 0.05f;
    public const float PAGE_NUMBER_ICON_BUFFER_Y = 0.15f;
    public const float SPACE_BUTTON_BUFFER = 0.05f;

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
    public SceneData sceneData;

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
    public IconUIElement carouselLeftButton;
    public IconUIElement carouselRightButton;
    public IconUIElement spaceBarButton;

    [Header("Generated")]
    public Vector3[] startPlayerWritePositions;

    public AtlasTextRenderer activePlayerWriteTextRenderer;
    public AtlasRenderer[] pageNumberIconRenderers;

    public int[] previewPlayerWriteIndices;
    public int playerWriteIndex;
    public int traitorIndex;
    public int activePlayerWriteRowIndex;
    public int pageIndex;

    public string[] playerWriteTexts;

    public Bounds playerWriteTextBounds;
    
    public string activePlayerWriteText;
    public string activePreviewPlayerWriteText;
    public void Init(int pageIndexInput, int totalPages)
    {
        paper_clip = paperRenderer.atlas.clipDict[(int)NotepadMotion.FlipPage];
        
        playerWriteIndex = -1;

        activePlayerWriteText = "";
        activePreviewPlayerWriteText = "";

        if (pageNumberIconRenderers == null || pageNumberIconRenderers.Length == 0)
        {
            pageNumberIconRenderers = new AtlasRenderer[totalPages];
            pageIndex = pageIndexInput;

            Bounds pageNumberIconBounds = pageNumberIconPrefab.bounds;
            float colSize = pageNumberIconBounds.size.x + PAGE_NUMBER_ICON_BUFFER_X;
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
        }

        startPlayerWritePositions = new Vector3[playerWriteTextRenderers.Length];

        switch (pageType)
        {
            case PageType.Prompt:
            {
                exitButton.InitButton(ClickExitButton, EnterExitButton, ExitExitButton);
                rightButton.InitButton(ClickRightButton, EnterRightButton, ExitRightButton);
            }
            break;
            case PageType.Profile:
            {
                exitButton.InitButton(ClickExitButton, EnterExitButton, ExitExitButton);
                rightButton.InitButton(ClickRightButton, EnterRightButton, ExitRightButton);
                leftButton.InitButton(ClickLeftButton, EnterLeftButton, ExitLeftButton);

                carouselLeftButton.InitButton(ClickCarouselLeftButton, EnterCarouselLeftButton, ExitCarouselLeftButton);
                carouselRightButton.InitButton(ClickCarouselRightButton, EnterCarouselRightButton, ExitCarouselRightButton);
                spaceBarButton.InitButton(ClickSpaceBarButton, EnterSpaceBarButton, ExitSpaceBarButton);

                carouselLeftButton.renderer.custom.w = 0;
                carouselRightButton.renderer.custom.w = 0;
                spaceBarButton.renderer.custom.w = 0;

                for (int i = 0; i < playerWriteTextRenderers.Length; i++)
                {
                    playerWriteTextRenderers[i].SetAlignmentType(AtlasRendering.AtlasTextAlignmentType.Center);
                    playerWriteTextRenderers[i].SetText("");
                    startPlayerWritePositions[i] = playerWriteTextRenderers[i].transform.localPosition;
                }
            }
            break;
            case PageType.ColorKey:
            {
                exitButton.InitButton(ClickExitButton, EnterExitButton, ExitExitButton);
                leftButton.InitButton(ClickLeftButton, EnterLeftButton, ExitLeftButton);

                carouselLeftButton.InitButton(ClickCarouselLeftButton, EnterCarouselLeftButton, ExitCarouselLeftButton);
                carouselRightButton.InitButton(ClickCarouselRightButton, EnterCarouselRightButton, ExitCarouselRightButton);
                spaceBarButton.InitButton(ClickSpaceBarButton, EnterSpaceBarButton, ExitSpaceBarButton);

                carouselLeftButton.renderer.custom.w = 0;
                carouselRightButton.renderer.custom.w = 0;
                spaceBarButton.renderer.custom.w = 0;

                for (int i = 0; i < otherButtons.Length; i++)
                {
                    int index = i;
                    
                    void ClickColor(IconUIElement icon)
                    {
                        SceneController.GetClueColorPicker().Open(otherButtons[index].renderer);
                        SceneController.GetNPCColorPicker().Close();
                        SwitchWriteRow(index);
                        notepadData.selectedColorMarkerIndex = activePlayerWriteRowIndex;
                    }

                    otherButtons[i].InitButton(ClickColor, EnterColorButton, ExitColorButton);
                }

                playerWriteTexts = new string[playerWriteTextRenderers.Length];
                Array.Fill(playerWriteTexts, "");
                previewPlayerWriteIndices = new int[playerWriteTextRenderers.Length];

                for (int i = 0; i < proceduralRenderers.Length; i++)
                {
                    proceduralRenderers[i].custom.w = 0;
                }
                for (int i = 0; i < playerWriteTextRenderers.Length; i++)
                {
                    AtlasRenderer behaviourButton = playerWriteRenderers[i];
                    behaviourButton.UpdateSpriteInputsByIndex(LOCK_SPRITE_INDEX);
                    behaviourButton.custom.x = 0;
                    behaviourButton.custom.y = 0;
                    behaviourButton.custom.z = 0;
                    behaviourButton.custom.w = 1;

                    playerWriteTextRenderers[i].SetAlignmentType(AtlasRendering.AtlasTextAlignmentType.Center);
                    playerWriteTextRenderers[i].SetText("");
                    startPlayerWritePositions[i] = playerWriteTextRenderers[i].transform.localPosition;
                }
                activePlayerWriteTextRenderer = null;
                playerWriteIndex = 0;
                activePlayerWriteRowIndex = 0;

                SwitchWriteRow(index: 0);
            }
            break;
        }
    }
    public void InitAgreementPage()
    {
        playerWriteTexts = new string[playerWriteTextRenderers.Length];
        Array.Fill(playerWriteTexts, "");
        activePlayerWriteTextRenderer.SetText("");
        previewPlayerWriteIndices = new int[playerWriteTextRenderers.Length];
    }
    public void InitNextColorRow(int nextIndex)
    {
        AtlasTextRenderer playerWriteTextRend = playerWriteTextRenderers[nextIndex];

        if (playerWriteTextRend.enabled) return;

        int traitorIndex = nextIndex % trip.traitorProfiles.Length;
        Behaviours behaviours = trip.traitorProfiles[traitorIndex].npcProfile.behaviours;

        int behaveIndex = nextIndex % 2;
        Behaviours curBehaviour = GetBehaviourAtIndex(behaviours, behaveIndex);

       string nextText = npcData.behaviourStringDict[curBehaviour];
        playerWriteTextRend.SetText(nextText);
        playerWriteTextRend.SetAppearTextAlpha(0);

        playerWriteTextRend.enabled = true;
    }
    public void ShowUnlockAbilityRenderer(UnlockType unlockType)
    {
        int index = -1;
        switch(unlockType)
        {
            case UnlockType.RuleOut:
            {
                index = 0;
            }
            break;

            case UnlockType.Color:
            {
                index = 1;
            }
            break;

            case UnlockType.MultiColor:
            {
                index = 2;
            }
            break;
        }
        proceduralRenderers[index].ChangeCustom(time: 1, newValue: 1, customChannel: 4);
    }
    public void SetColorMarkerButtonSprite(int index, bool unlocked)
    {
        AtlasRenderer playerWriteRend = playerWriteRenderers[index];
        switch (index)
        {
            case 0:
            {
                if (unlocked)
                {
                    playerWriteRend.UpdateSpriteInputsByIndex(ONE_NUMPAD_SPRITE_INDEX);
                }
                else
                {
                    playerWriteRend.UpdateSpriteInputsByIndex(LOCK_SPRITE_INDEX);
                }
            }
            break;
            case 1:
            {
                if (unlocked)
                {
                    playerWriteRend.UpdateSpriteInputsByIndex(TWO_NUMPAD_SPRITE_INDEX);
                }
                else
                {
                    playerWriteRend.UpdateSpriteInputsByIndex(LOCK_SPRITE_INDEX);
                }
            }
            break;
        }
        proceduralRenderers[index].ChangeCustom(time: 1, newValue: 1, customChannel: 4);
    }
    public void UpdatePage()
    {
        switch (pageType)
        {
            case PageType.Prompt:
            {
                exitButton.UpdateButton(playerInputs);
                rightButton.UpdateButton(playerInputs);
            }
            break;
            case PageType.Profile:
            {
                exitButton.UpdateButton(playerInputs);
                
                rightButton.UpdateButton(playerInputs);
                leftButton.UpdateButton(playerInputs);
                if (sceneData.activeSceneType == Scenes.SceneType.Trip)
                {
                    carouselLeftButton.UpdateButton(playerInputs);
                    carouselRightButton.UpdateButton(playerInputs);
                    if (activePlayerWriteText == "") spaceBarButton.UpdateButton(playerInputs);
                }

            }
            break;
            case PageType.ColorKey:
            {
                if (spyStats.curTutorialState == TutorialState.None)
                {
                    exitButton.UpdateButton(playerInputs);
                }
                
                leftButton.UpdateButton(playerInputs);

                if ((trip.curUnlocks & UnlockType.Color) != 0)
                {

                    carouselLeftButton.UpdateButton(playerInputs);
                    carouselRightButton.UpdateButton(playerInputs);
                    if (activePlayerWriteText == "") spaceBarButton.UpdateButton(playerInputs);

                    if ((trip.curUnlocks & UnlockType.MultiColor) != 0)
                    {
                        if ((spyStats.tutorialsCompleted & TutorialState.MultiColor1) != 0)
                        {
                            otherButtons[1].UpdateButton(playerInputs);
                            otherButtons[0].UpdateButton(playerInputs);
                        }

                        if (spyStats.curTutorialState == TutorialState.None)
                        {
                            bool switchingTo1 = playerInputs.numpad == 1 && activePlayerWriteRowIndex != 0;
                            bool switchingTo2 = playerInputs.numpad == 2 && activePlayerWriteRowIndex != 1;
                            if(switchingTo1 ||  switchingTo2)
                            {
                                SwitchWriteRow(playerInputs.numpad - 1);
                            }
                        }
                    }
                    else
                    {
                        if ((spyStats.tutorialsCompleted & TutorialState.Color1) != 0)
                        {
                            otherButtons[0].UpdateButton(playerInputs);
                        }
                    }
                    
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

        previewPlayerWriteIndices = new int[playerWriteTextRenderers.Length];

        for (int i = 0; i < playerWriteTextRenderers.Length; i++)
        {
            AtlasTextRenderer playerWriteTextRenderer = playerWriteTextRenderers[i];
            playerWriteTextRenderer.SetText(trip.stationsDataArray[0].stationName);
            playerWriteTextRenderer.SetColorText(options.meridiaColor);
        }
        AtlasRenderer coveredMugShot = playerWriteRenderers[0];
        AtlasRenderer uncoveredMugShot = playerWriteRenderers[1];

        int uncoveredMugShotIndex = traitorProfile.mugShotIndex * 2;
        int coveredMugShotIndex = uncoveredMugShotIndex + 1;
        coveredMugShot.UpdateSpriteInputs(coveredMugShot.atlas.simpleSprites[coveredMugShotIndex]);

        coveredMugShot.custom.x = 0;
        coveredMugShot.custom.y = 0;
        coveredMugShot.custom.z = 0;
        coveredMugShot.custom.w = 0;
        coveredMugShot.customBit &= ~(int)ColorBits.Diagonal;

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
    public void CancelPlayerWrite()
    {
        activePlayerWriteTextRenderer.CancelWriting();
    }
    public void WritePlayerWriteText()
    {
        playerWriteIndex = previewPlayerWriteIndices[activePlayerWriteRowIndex];
        activePlayerWriteText = activePlayerWriteTextRenderer.text;
        playerWriteTexts[activePlayerWriteRowIndex] = activePlayerWriteText;
        playerWriteTextBounds = activePlayerWriteTextRenderer.GetBoundsNewText(activePlayerWriteText);

        activePlayerWriteTextRenderer.SetAlignmentType(AtlasRendering.AtlasTextAlignmentType.Left);
        activePlayerWriteTextRenderer.transform.position = new Vector3(playerWriteTextBounds.min.x, playerWriteTextBounds.max.y, activePlayerWriteTextRenderer.transform.position.z);

        ToggleSpacebarButton(false);
        activePlayerWriteTextRenderer.WriteText(activePlayerWriteText, LeftHand.WRITE_LETTER_TIME);
    }

    public void ErasePlayerWriteText()
    {
        playerWriteTexts[activePlayerWriteRowIndex] = "";
        activePlayerWriteTextRenderer.EraseText(LeftHand.WRITE_LETTER_TIME);
    }
    public void SwitchActivePreviewPlayerWriteText(int indexOffset)
    {

        switch (pageType)
        {
            case PageType.Profile:
            {
                switch (promptType)
                {
                    case TripPrompt.Stations:
                    {
                        previewPlayerWriteIndices[activePlayerWriteRowIndex] += indexOffset;
                        previewPlayerWriteIndices[activePlayerWriteRowIndex] = (previewPlayerWriteIndices[activePlayerWriteRowIndex] + trip.stationsDataArray.Length) % trip.stationsDataArray.Length;
                        activePreviewPlayerWriteText = trip.stationsDataArray[previewPlayerWriteIndices[activePlayerWriteRowIndex]].stationName;
                    }
                    break;
                }

                activePlayerWriteTextRenderer.SetText(activePreviewPlayerWriteText, alpha: 0);
                activePlayerWriteTextRenderer.ChangeCustom(time: APPEAR_TEXT_TIME, newValue: 0.5f, customChannel: 4);
                playerWriteTextBounds = activePlayerWriteTextRenderer.GetBoundsNewText(activePreviewPlayerWriteText);
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
                            previewPlayerWriteIndices[activePlayerWriteRowIndex] += indexOffset;

                            previewPlayerWriteIndices[activePlayerWriteRowIndex] = (previewPlayerWriteIndices[activePlayerWriteRowIndex] + BEHAVIOURS_COUNT) % BEHAVIOURS_COUNT;
                            Behaviours allBehaviours = (Behaviours)~((1 << BEHAVIOURS_COUNT) | (int)Behaviours.None);
                            Behaviours activeBehaviour = GetBehaviourAtIndex(allBehaviours, previewPlayerWriteIndices[activePlayerWriteRowIndex]);
                            activePreviewPlayerWriteText = npcData.behaviourStringDict[activeBehaviour];
                            
                            activePlayerWriteTextRenderer.SetText(activePreviewPlayerWriteText, alpha: 0);
                            activePlayerWriteTextRenderer.ChangeCustom(time: APPEAR_TEXT_TIME, newValue: 0.5f, customChannel: 4);
                            
                            playerWriteTextBounds = activePlayerWriteTextRenderer.GetBoundsNewText(activePreviewPlayerWriteText);
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
                if (activePlayerWriteText == "")
                {
                    if (prevNotepadState == NotepadState.Erasing)
                    {
                        activePlayerWriteTextRenderer.SetAlignmentType(AtlasRendering.AtlasTextAlignmentType.Center);
                        activePlayerWriteTextRenderer.transform.localPosition = startPlayerWritePositions[activePlayerWriteRowIndex];
                    }

                    activePreviewPlayerWriteText = trip.stationsDataArray[previewPlayerWriteIndices[activePlayerWriteRowIndex]].stationName;
                    activePlayerWriteTextRenderer.SetText(activePreviewPlayerWriteText, alpha: 0);
                    activePlayerWriteTextRenderer.ChangeCustom(time: APPEAR_TEXT_TIME, newValue: 0.5f, customChannel: 4);

                    playerWriteTextBounds = activePlayerWriteTextRenderer.GetBoundsNewText(activePreviewPlayerWriteText);

                    ToggleSpacebarButton(true);
                }

                ToggleCarouselButtons(true);
            }
            break;

            case PageType.ColorKey:
            {
                if (prevNotepadState == NotepadState.Erasing)
                {
                    activePlayerWriteTextRenderer.SetAlignmentType(AtlasRendering.AtlasTextAlignmentType.Center);
                    activePlayerWriteTextRenderer.transform.localPosition = startPlayerWritePositions[activePlayerWriteRowIndex];
                    
                    SetColorKeyPagePlayerWriteText(activePlayerWriteTextRenderer);
                    ToggleSpacebarButton(true);
                }
                else if (prevNotepadState != NotepadState.Writing)
                {
                    if ((trip.curUnlocks & UnlockType.Color) != 0)
                    {
                        if (playerWriteTexts[0] == "")
                        {
                            SetColorKeyPagePlayerWriteText(playerWriteTextRenderers[0]);
                        }

                        if ((trip.curUnlocks & UnlockType.MultiColor) != 0)
                        {
                            if (playerWriteTexts[1] == "")
                            {
                                SetColorKeyPagePlayerWriteText(playerWriteTextRenderers[1]);
                            }
                        }

                        if (activePlayerWriteTextRenderer == null) activePlayerWriteTextRenderer = playerWriteTextRenderers[activePlayerWriteRowIndex];
                        
                        playerWriteTextBounds = activePlayerWriteTextRenderer.GetBoundsNewText(activePreviewPlayerWriteText);
                        
                        ToggleSpacebarButton(true);
                        ToggleCarouselButtons(true);
                    }
                }

            }
            break;

            case PageType.Agreement:
            {
                activePreviewPlayerWriteText = "{|}~~{";
                activePlayerWriteTextRenderer.SetText(activePreviewPlayerWriteText, alpha: 0);
                activePlayerWriteTextRenderer.ChangeCustom(time: APPEAR_TEXT_TIME, newValue: 0.5f, customChannel: 4);
            }
            break;
        }
    }
    private void SetColorKeyPagePlayerWriteText(AtlasTextRenderer curRend)
    {
        int nextPlayerWriteIndex = (previewPlayerWriteIndices[activePlayerWriteRowIndex] + BEHAVIOURS_COUNT) % BEHAVIOURS_COUNT;
        previewPlayerWriteIndices[activePlayerWriteRowIndex] = nextPlayerWriteIndex;

        Behaviours allBehaviours = (Behaviours)~((1 << BEHAVIOURS_COUNT) | (int)Behaviours.None);
        Behaviours activeBehaviour = GetBehaviourAtIndex(allBehaviours, nextPlayerWriteIndex);

        activePreviewPlayerWriteText = npcData.behaviourStringDict[activeBehaviour];

        curRend.SetText(activePreviewPlayerWriteText, alpha: 0);
        curRend.ChangeCustom(time: APPEAR_TEXT_TIME, newValue: 0.5f, customChannel: 4);
    }
    public void SetActiveRendererText(string text)
    {
        activePlayerWriteTextRenderer?.SetText(text);
    }
    public void SetPlayerWriteTextAlphaBottom(float normAmount)
    {
        for (int i = 0; i < playerWriteTextRenderers.Length; i++)
        {
            if (playerWriteTexts[i] != "") continue;
            
            AtlasTextRenderer renderer = playerWriteTextRenderers[i];

            if (renderer.transform.localPosition.y < FLIP_LOCAL_POS_Y)
            {

                renderer.SetAppearTextAlpha(normAmount);
            }
        }
        if (playerWriteRenderers.Length > 0)
        {
            carouselLeftButton.renderer.custom.w = normAmount;
            carouselRightButton.renderer.custom.w = normAmount;
            spaceBarButton.renderer.custom.w = normAmount;
        }
    }
    public void SetPlayerWriteTextAlphaTop(float normAmount)
    {
        for (int i = 0; i < playerWriteTextRenderers.Length; i++)
        {
            if (playerWriteTexts[i] != "") continue;
            
            AtlasTextRenderer renderer = playerWriteTextRenderers[i];

            if (renderer.transform.localPosition.y >= FLIP_LOCAL_POS_Y)
            {
                renderer.SetAppearTextAlpha(normAmount);
            }
        }

        if (playerWriteRenderers.Length > 0)
        {
            carouselLeftButton.renderer.custom.w = normAmount;
            carouselRightButton.renderer.custom.w = normAmount;
            spaceBarButton.renderer.custom.w = normAmount;
        }
    }
    public void EnterColorButton(IconUIElement icon)
    {
        icon.renderer.custom.w = 1;
    }
    public void ExitColorButton(IconUIElement icon)
    {
        icon.renderer.custom.w = 0;
    }

    public void EnterSpaceBarButton(IconUIElement icon)
    {
        if (activePlayerWriteText == "")
        { 
            icon.renderer.custom.x = 1;
        }
    }
    public void ExitSpaceBarButton(IconUIElement icon)
    {
        icon.renderer.custom.x = 0;
    }
    public void ClickSpaceBarButton(IconUIElement icon)
    {
        if (activePlayerWriteText == "")
        {
            notepadData.subState |= SubState.WriteToggle;
        }
    }

    public void EnterExitButton(IconUIElement icon)
    {
        icon.renderer.custom.x = 1;

        icon.renderer.UpdateSpriteInputsByIndex((int)KeySpriteIndices.F);
        icon.renderer.FlipHSimple(false);
        icon.renderer.FlipVSimple(false);
    }
    public void ExitExitButton(IconUIElement icon)
    {
        icon.renderer.custom.x = 0;
        icon.renderer.UpdateSpriteInputsByIndex((int)KeySpriteIndices.Corner);
        icon.renderer.FlipHSimple(true);
        icon.renderer.FlipVSimple(true);
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

    public void EnterLeftButton(IconUIElement icon)
    {
        icon.renderer.custom.x = 1;
        icon.renderer.UpdateSpriteInputsByIndex((int)KeySpriteIndices.S);
        icon.renderer.FlipHSimple(false);
    }
    public void ExitLeftButton(IconUIElement icon)
    {
        icon.renderer.custom.x = 0;
        icon.renderer.UpdateSpriteInputsByIndex((int)KeySpriteIndices.Corner);
        icon.renderer.FlipHSimple(true);
    }
    public void ClickLeftButton(IconUIElement icon)
    {
        notepadData.subState |= SubState.IsFlippingDown;
    }

    public void EnterRightButton(IconUIElement icon)
    {
        icon.renderer.custom.x = 1;
        icon.renderer.UpdateSpriteInputsByIndex((int)KeySpriteIndices.W);
    }
    public void ExitRightButton(IconUIElement icon)
    {
        icon.renderer.custom.x = 0;
        icon.renderer.UpdateSpriteInputsByIndex((int)KeySpriteIndices.Corner);
    }
    public void ClickRightButton(IconUIElement icon)
    {
        notepadData.subState |= SubState.IsFlippingUp;
    }
    
    public void EnterCarouselLeftButton(IconUIElement icon)
    {
        icon.renderer.custom.x = 1;
        icon.renderer.UpdateSpriteInputsByIndex((int)KeySpriteIndices.Q);
    }
    public void ExitCarouselLeftButton(IconUIElement icon)
    {
        icon.renderer.custom.x = 0;
        icon.renderer.UpdateSpriteInputsByIndex((int)KeySpriteIndices.LeftTriangle);
    }
    public void ClickCarouselLeftButton(IconUIElement icon)
    {
        if (activePlayerWriteText == "")
        {
            SwitchActivePreviewPlayerWriteText(-1);
        }
        else
        {
            notepadData.subState |= SubState.EraseToggle;
        }
    }
    
    public void EnterCarouselRightButton(IconUIElement icon)
    {
        icon.renderer.custom.x = 1;
        icon.renderer.UpdateSpriteInputsByIndex((int)KeySpriteIndices.E);
        icon.renderer.FlipHSimple(false);
    }
    public void ExitCarouselRightButton(IconUIElement icon)
    {
        icon.renderer.custom.x = 0;

        icon.renderer.UpdateSpriteInputsByIndex((int)KeySpriteIndices.LeftTriangle);
        icon.renderer.FlipHSimple(true);
    }
    public void ClickCarouselRightButton(IconUIElement icon)
    {
        if (activePlayerWriteText == "")
        {
            SwitchActivePreviewPlayerWriteText(1);
        }
        else
        {
            notepadData.subState |= SubState.EraseToggle;
        }
    }
    public void UpdateMugShotReveal(float t)
    {
        AtlasRenderer coveredMugShot = playerWriteRenderers[0];
        coveredMugShot.custom.x = Mathf.Clamp01(t);
    }
    public void SwitchWriteRow(int index)
    {
        if (index < playerWriteTextRenderers.Length)
        {
            activePlayerWriteRowIndex = index;
            activePlayerWriteTextRenderer = playerWriteTextRenderers[activePlayerWriteRowIndex];
            activePreviewPlayerWriteText = activePlayerWriteTextRenderer.text;
            activePlayerWriteText = playerWriteTexts[activePlayerWriteRowIndex];
            playerWriteTextBounds = activePlayerWriteTextRenderer.GetBoundsNewText(activePreviewPlayerWriteText);

            bool showSpacebar = activePlayerWriteText == "";

            SetCarouselAndSpaceIconPositions(showSpacebar);
        }
    }
    public void ToggleCarouselButtons(bool toggle)
    {
        int alpha = toggle ? 1 : 0;

        if (carouselLeftButton.renderer.custom.w != alpha)
        {
            carouselLeftButton.renderer.custom.w = 1 - alpha;
            carouselRightButton.renderer.custom.w = 1 - alpha;

            carouselLeftButton.renderer.ChangeCustom(time: APPEAR_TEXT_TIME, newValue: alpha, customChannel: 4);
            carouselRightButton.renderer.ChangeCustom(time: APPEAR_TEXT_TIME, newValue: alpha, customChannel: 4);
        }

    }
    public void ToggleSpacebarButton(bool toggle)
    {
        int alpha = toggle ? 1 : 0;

        if (spaceBarButton.renderer.custom.w != alpha)
        {
            spaceBarButton.renderer.custom.w = 1 -alpha;
            spaceBarButton.renderer.ChangeCustom(time: APPEAR_TEXT_TIME, newValue: alpha, customChannel: 4);
        }
    }
    public void SetCarouselAndSpaceIconPositions(bool showSpacebar)
    {
        carouselLeftButton.renderer.custom.w = 0;
        carouselRightButton.renderer.custom.w = 0;
        ToggleCarouselButtons(toggle: true);
        spaceBarButton.renderer.custom.w = 0;
        ToggleSpacebarButton(showSpacebar);

        carouselLeftButton.renderer.transform.position = new Vector3(carouselLeftButton.renderer.transform.position.x, playerWriteTextBounds.center.y, carouselLeftButton.renderer.transform.position.z);
        carouselRightButton.renderer.transform.position = new Vector3(carouselRightButton.renderer.transform.position.x, playerWriteTextBounds.center.y, carouselRightButton.renderer.transform.position.z);
        spaceBarButton.renderer.transform.position = new Vector3(playerWriteTextBounds.center.x, playerWriteTextBounds.min.y - SPACE_BUTTON_BUFFER - spaceBarButton.renderer.bounds.extents.y, spaceBarButton.renderer.transform.position.z);
        
        carouselLeftButton.InitPos();
        carouselRightButton.InitPos();
        spaceBarButton.InitPos();
    }
    public Bounds GetCurrentWritingBounds()
    {
        return activePlayerWriteTextRenderer.GetBoundsCurrentText();
    }
    public Bounds GetWritingBoundsOfNewText(string newText)
    {
        return activePlayerWriteTextRenderer.GetBoundsNewText(newText);
    }
}
