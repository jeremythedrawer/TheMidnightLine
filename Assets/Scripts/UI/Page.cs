using System;
using UnityEngine;

using static Atlas;
using static AtlasUI;
using static Notepad;
using static Passenger;
public class Page : MonoBehaviour
{
    public static event Action OnMouseUpExitButton;

    public const float CONTENTS_LOCAL_POS_Z = -0.1f;
    public const float FLIP_LOCAL_POS_Y = -1.656f;
    public const float PAGE_NUMBER_ICON_BUFFER_X = 0.05f;
    public const float PAGE_NUMBER_ICON_BUFFER_Y = 0.15f;
    public const float SPACE_BUTTON_BUFFER = 0.05f;
    public const float COLOR_KEY_TEXT_POS_X = 0.137f;

    public PassengersData npcData;
    public SpyData spyData;
    public InputData inputData;
    public NotepadData notepadData;
    public Options options;

    public AtlasTextRenderer[] textRenderers;
    public AtlasRenderer[] iconRenderers;
    
    public AtlasRenderer paperRenderer;

    public IconButton leftButton;
    public IconButton rightButton;
    public IconButton exitButton;

    [Header("Generated")]

    public IconButton[] pageNumberIconButtons;
    public int pageIndex;
    public void Init(int pageIndexInput)
    {
        pageIndex = pageIndexInput;

        InitPageNumberIcons();
        InitPageButtons();
    }
    private void InitPageButtons()
    {
        void MouseUpLeftButton()
        {
            leftButton.MouseUp();
            notepadData.subState |= SubState.IsFlippingDown;
        }
        void MouseUpRightButton()
        {
            rightButton.MouseUp();
            notepadData.subState |= SubState.IsFlippingUp;
        }
        void MouseUpExitButton()
        {
            exitButton.MouseUp();
            OnMouseUpExitButton?.Invoke();
        }

        leftButton?.InitButton(MouseUpLeftButton);
        rightButton?.InitButton(MouseUpRightButton);
        exitButton?.InitButton(MouseUpExitButton);
    }
    private void InitPageNumberIcons()
    {
        if (pageNumberIconButtons == null || pageNumberIconButtons.Length == 0)
        {
            pageNumberIconButtons = new IconButton[notepadData.pageCount];

            Bounds pageNumberIconBounds = notepadData.pageNumberIconButtonPrefab.atlasRenderer.bounds;
            float colSize = pageNumberIconBounds.size.x + PAGE_NUMBER_ICON_BUFFER_X;

            Vector3 startPos = new Vector3();
            startPos.x = (float)notepadData.pageCount * colSize * -0.5f;
            startPos.y = -paperRenderer.bounds.size.y + PAGE_NUMBER_ICON_BUFFER_Y;
            startPos.z = CONTENTS_LOCAL_POS_Z;

            for (int i = 0; i < notepadData.pageCount; i++)
            {
                IconButton pageNumberIcon = Instantiate(notepadData.pageNumberIconButtonPrefab, transform);
                Vector3 localPos = startPos;

                localPos.x += i * colSize;
                pageNumberIcon.transform.localPosition = localPos;

                if (i == pageIndex)
                {
                    pageNumberIcon.atlasRenderer.customBit |= (int)ColorBits.RedChannel;
                }

                int index = i;
                void MouseUp()
                {
                    pageNumberIcon.MouseUp();
                    //TODO: Set active page to the selected
                }
                pageNumberIcon.InitButton(onMouseUp: MouseUp);

                pageNumberIconButtons[i] = pageNumberIcon;
            }
        }
    }
    public void UpdatePage()
    {
        exitButton?.UpdateButton();
        rightButton?.UpdateButton();
        leftButton?.UpdateButton();
    }
    public void SetPageDepth(float localDepth)
    {
        transform.localPosition = new Vector3(transform.localPosition.x, transform.localPosition.y, localDepth);
    }
    public void SetInvertNotepadMaskBit(bool invert)
    {
        if (invert)
        {
            for (int i = 0; i < textRenderers.Length; i++)
            {
                textRenderers[i].customBit |= (int)ColorBits.InvertNotepadFlip;
            }

            for (int i = 0; i < iconRenderers.Length; i++)
            {
                iconRenderers[i].customBit |= (int)ColorBits.InvertNotepadFlip;
            }
            paperRenderer.customBit |= (int)ColorBits.InvertNotepadFlip;
            if (leftButton) leftButton.atlasRenderer.customBit |= (int)ColorBits.InvertNotepadFlip;
            if (rightButton) rightButton.atlasRenderer.customBit |= (int)ColorBits.InvertNotepadFlip;
            exitButton.atlasRenderer.customBit |= (int)ColorBits.InvertNotepadFlip;

            for (int i = 0; i < pageNumberIconButtons.Length; i++)
            {
                pageNumberIconButtons[i].atlasRenderer.customBit |= (int)ColorBits.InvertNotepadFlip;
            }
        }
        else
        {
            for (int i = 0; i < textRenderers.Length; i++)
            {
                textRenderers[i].customBit &= ~(int)ColorBits.InvertNotepadFlip;
            }

            for (int i = 0; i < iconRenderers.Length; i++)
            {
                iconRenderers[i].customBit &= ~(int)ColorBits.InvertNotepadFlip;
            }
            paperRenderer.customBit &= ~(int)ColorBits.InvertNotepadFlip;
            if (leftButton) leftButton.atlasRenderer.customBit &= ~(int)ColorBits.InvertNotepadFlip;
            if (rightButton) rightButton.atlasRenderer.customBit &= ~(int)ColorBits.InvertNotepadFlip;
            exitButton.atlasRenderer.customBit &= ~(int)ColorBits.InvertNotepadFlip;

            for (int i = 0; i < pageNumberIconButtons.Length; i++)
            {
                pageNumberIconButtons[i].atlasRenderer.customBit &= ~(int)ColorBits.InvertNotepadFlip;
            }
        }
    }
}
