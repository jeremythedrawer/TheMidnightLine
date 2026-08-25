using System;
using UnityEngine;

using static Atlas;
using static AtlasUI;
using static Notepad;
using static NPC;
public class Page : MonoBehaviour
{
    public const float CONTENTS_LOCAL_POS_Z = -0.1f;
    public const float FLIP_LOCAL_POS_Y = -1.656f;
    public const float PAGE_NUMBER_ICON_BUFFER_X = 0.05f;
    public const float PAGE_NUMBER_ICON_BUFFER_Y = 0.15f;
    public const float SPACE_BUTTON_BUFFER = 0.05f;
    public const float COLOR_KEY_TEXT_POS_X = 0.137f;

    public static AtlasClip paperClip;

    public PageType pageType;
    public TripPrompt promptType;
    public TripClue clueType;

    public TripData trip;
    public PassengerData npcData;
    public SpyData spyData;
    public InputData playerInputs;
    public NotepadData notepadData;
    public Options options;
    public SceneData sceneData;

    public AtlasRenderer pageNumberIconPrefab;

    public AtlasRenderer paperRenderer;

    public AtlasTextRenderer[] proceduralTextRenderers;
    public AtlasTextRenderer[] readOnlyTextRenderers;

    public AtlasRenderer[] proceduralRenderers;
    public AtlasRenderer[] readOnlyRenderers;
    public AtlasRenderer[] playerWriteRenderers;

    public IconButton leftButton;
    public IconButton rightButton;
    public IconButton exitButton;

    [Header("Generated")]

    public AtlasRenderer[] pageNumberIconRenderers;

    public int traitorIndex;
    public int pageIndex;
    public void Init(int pageIndexInput, int totalPages)
    {
        paperClip = paperRenderer.atlas.clipDict[(int)NotepadMotion.FlipPage];

        if (pageNumberIconRenderers == null || pageNumberIconRenderers.Length == 0)
        {
            pageNumberIconRenderers = new AtlasRenderer[totalPages];
            pageIndex = pageIndexInput;

            Bounds pageNumberIconBounds = pageNumberIconPrefab.bounds;
            float colSize = pageNumberIconBounds.size.x + PAGE_NUMBER_ICON_BUFFER_X;
            Vector3 startPos = new Vector3();
            startPos.x = (float)totalPages * colSize * -0.5f;
            startPos.y = -paperRenderer.bounds.size.y + PAGE_NUMBER_ICON_BUFFER_Y;
            startPos.z = CONTENTS_LOCAL_POS_Z;

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

        void EnterButton(IconButton icon)
        {
            icon.atlasRenderer.custom.x = 1;
        }
        void ExitButton(IconButton icon)
        {
            icon.atlasRenderer.custom.x = 0;
        }
        void ClickExitButton(IconButton icon)
        {
            if (spyData.checkingNotepad)
            {
                spyData.checkingNotepad = false;
            }
            else
            {
                spyData.checkingNotepad = true;
            }
        }
        void ClickLeftButton(IconButton icon)
        {
            notepadData.subState |= SubState.IsFlippingDown;
        }

        void ClickRightButton(IconButton icon)
        {
            notepadData.subState |= SubState.IsFlippingUp;
        }

        exitButton.InitButton(ClickExitButton, EnterButton, ExitButton);
        rightButton.InitButton(ClickRightButton, EnterButton, ExitButton);
        leftButton.InitButton(ClickLeftButton, EnterButton, ExitButton);
    }
    public void UpdatePage()
    {
        switch (pageType)
        {
            case PageType.Prompt:
            {
                exitButton.UpdateButton();
                rightButton.UpdateButton();
            }
            break;
            case PageType.Profile:
            {
                exitButton.UpdateButton();
                
                rightButton.UpdateButton();
                leftButton.UpdateButton();
            }
            break;
        }
    }
    public void InitProfile(TraitorProfile traitorProfile, int pageIndex, int totalPages)
    {
        for (int i = 0; i < proceduralTextRenderers.Length; i++)
        {
            Habits behaviour = GetBehaviourAtIndex(traitorProfile.npcProfile.behaviours, i);
            proceduralTextRenderers[i].SetText(npcData.habitStringDict[behaviour]);
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
        ToggleTextRenderers(readOnlyTextRenderers, toggle, topHalf: false);
        ToggleTextRenderers(proceduralTextRenderers, toggle, topHalf: false);

        ToggleRenderers(readOnlyRenderers, toggle, topHalf: false);
        ToggleRenderers(playerWriteRenderers, toggle, topHalf: false);
        ToggleRenderers(pageNumberIconRenderers, toggle, topHalf: false);
    }
    public void TogglePageContentTopHalf(bool toggle)
    {
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
        paperRenderer.PlayClipOneShot(paperClip);
    }
    public void PlayPaperClipReverse()
    {
        paperRenderer.PlayClipOneShotReverse(paperClip);
    }
    public void SetPageDepth(float localDepth)
    {
        transform.localPosition = new Vector3(transform.localPosition.x, transform.localPosition.y, localDepth);
    }
    public void UpdateMugShotReveal(float t)
    {
        AtlasRenderer coveredMugShot = playerWriteRenderers[0];
        coveredMugShot.custom.x = Mathf.Clamp01(t);
    }
}
