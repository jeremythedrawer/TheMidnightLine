using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;

using static AtlasUI;
public class ColorPicker : MonoBehaviour
{
    public enum PickerType
    { 
        DarkColor,
        LightColor,
    }

    const float OPEN_TIME = 0.5f;
    const float BUTTON_PADDING = 0.05f;
    const float BUTTON_DEPTH = -0.1f;
    const float SELECTED_BUTTON_DEPTH = -0.2f;


    public PickerType pickerType;

    public TripData trip;
    public Options options;
    public SpyData spyData;
    public CameraData camStats;
    public NotepadData notepadData;

    public TextButton textButton;
    
    [Header("Generated")]
    public IconButton[] colorButtons;

    public Color[] selectableColors;

    public Vector3[] openColorButtonPositions;
    public Vector3[] curColorButtonPositions;
    
    public Vector3 closeButtonPosition;
    public Vector3 closeBackgroundPosition;
    public Vector3 openBackgroundPosition;

    public int globalShaderID;
    public int selectedIndex;

    public float openWidth;
    public float closeWidth;
    public float openClock;

    public bool isOpen;

    public CancellationTokenSource ctsOpen;

    private void Start()
    {
        SetType();
        Init();
    }
    private void SetType()
    {
        switch(pickerType)
        {
            case PickerType.DarkColor:
            {
                globalShaderID = options.darkColorID;
                selectableColors = options.selectableDarkColors;
            }
            break;

            case PickerType.LightColor:
            {
                globalShaderID = options.lightColorID;
                selectableColors = options.selectableLightColors;
            }
            break;
        }
        
    }
    private void Init()
    {
        curColorButtonPositions = new Vector3[selectableColors.Length];
        colorButtons = new IconButton[selectableColors.Length];
        openColorButtonPositions = new Vector3[selectableColors.Length];


        Vector3 colorButtonSize = options.colorButtonPrefab.atlasRenderer.sprite.worldSize;
        Vector4 middlePivSize = textButton.backgroundRenderer.worldPivotsAndSizes[4];

        float colorButtonCellWidth = colorButtonSize.x + BUTTON_PADDING;

        float initBackgroundWorldWidth = textButton.backgroundRenderer.bounds.size.x;

        closeWidth = textButton.backgroundRenderer.width + (colorButtonCellWidth / middlePivSize.z);
        textButton.backgroundRenderer.width = closeWidth;
        textButton.backgroundRenderer.UpdateSliceSpriteInputsSelf();

        float newBackgroundWorldWidth = textButton.backgroundRenderer.bounds.size.x;
        float backgroundWorldWidthDiff = newBackgroundWorldWidth - initBackgroundWorldWidth;
        closeBackgroundPosition.x = textButton.backgroundRenderer.transform.localPosition.x + (backgroundWorldWidthDiff * 0.5f);
        closeBackgroundPosition.y = textButton.backgroundRenderer.transform.localPosition.y;
        closeBackgroundPosition.z = textButton.backgroundRenderer.transform.localPosition.z;

        openBackgroundPosition.x = textButton.backgroundRenderer.transform.localPosition.x + (backgroundWorldWidthDiff * 0.5f * selectableColors.Length);
        openBackgroundPosition.y = textButton.backgroundRenderer.transform.localPosition.y; 
        openBackgroundPosition.z = textButton.backgroundRenderer.transform.localPosition.z; 
          
        textButton.backgroundRenderer.transform.localPosition = closeBackgroundPosition;


        float totalWidth = colorButtonCellWidth * (selectableColors.Length - 1);
        openWidth = closeWidth + (totalWidth / middlePivSize.z);


        closeButtonPosition.x = textButton.textRenderer.bounds.size.x + (colorButtonSize.x * 0.5f);
        closeButtonPosition.y = - (colorButtonSize.y * 0.5f);
        closeButtonPosition.z = 0;


        if (pickerType == PickerType.DarkColor)
        {
            for (int i = 0; i < selectableColors.Length; i++)
            {
                IconButton colorButton = Instantiate(options.colorButtonPrefab, textButton.transform);
                colorButton.transform.localPosition = closeButtonPosition;

                Color selectableColor = selectableColors[i];
                colorButton.atlasRenderer.custom = selectableColor.linear;

                if (i != selectedIndex)
                {
                    colorButton.atlasRenderer.enabled = false;
                }
                else
                {
                    colorButton.atlasRenderer.customBit |= (int)ColorBits.RedChannel;
                    colorButton.atlasRenderer.customBit &= ~(int)ColorBits.Invert;
                }

                int index = i;
                void MouseUpColor()
                {
                    if (isOpen)
                    {
                        colorButton.atlasRenderer.customBit |= (int)ColorBits.RedChannel;
                        colorButton.atlasRenderer.customBit ^= (int)ColorBits.Invert;

                        Shader.SetGlobalColor(globalShaderID, selectableColor.linear);
                        options.darkColor = selectableColor;
                        selectedIndex = index;
                        curColorButtonPositions[index].z = SELECTED_BUTTON_DEPTH;

                        for (int j = 0; j < colorButtons.Length; j++)
                        {
                            if (j == selectedIndex) continue;
                            IconButton colorButton = colorButtons[j];

                            colorButton.atlasRenderer.customBit &= ~(int)ColorBits.RedChannel;
                            curColorButtonPositions[j].z = BUTTON_DEPTH;
                        }
                    }
                    else
                    {
                        Open();
                    }
                }
                void EnterButton()
                {
                    if (pickerType == PickerType.DarkColor)
                    {
                        colorButton.atlasRenderer.customBit |= (int)ColorBits.GreenChannel;
                    }
                    else
                    {
                        colorButton.atlasRenderer.customBit |= (int)ColorBits.RedChannel;
                    }
                }
                void ExitButton()
                {
                    if (pickerType == PickerType.DarkColor)
                    {
                        colorButton.atlasRenderer.customBit &= ~(int)ColorBits.GreenChannel;
                    }
                    else
                    {
                        colorButton.atlasRenderer.customBit &= ~(int)ColorBits.RedChannel;
                    }
                    colorButton.atlasRenderer.customBit &= ~(int)ColorBits.Invert;
                }
                colorButton.InitButton(MouseUpColor, onEnter: EnterButton, onExit: ExitButton);

                colorButtons[i] = colorButton;
                curColorButtonPositions[i] = closeButtonPosition;

                Vector3 openPos = new Vector3();
                openPos.x = closeButtonPosition.x + ((colorButton.atlasRenderer.sprite.worldSize.x + BUTTON_PADDING) * i);
                openPos.y = closeButtonPosition.y;
                openPos.z = closeButtonPosition.z;

                openColorButtonPositions[i] = openPos;
            }
        }
        else
        {
            for (int i = 0; i < selectableColors.Length; i++)
            {
                IconButton colorButton = Instantiate(options.colorButtonPrefab, textButton.transform);
                colorButton.transform.localPosition = closeButtonPosition;

                Color selectableColor = selectableColors[i];
                colorButton.atlasRenderer.custom = selectableColor.linear;

                if (i != selectedIndex)
                {
                    colorButton.atlasRenderer.enabled = false;
                }
                else
                {
                    colorButton.atlasRenderer.customBit |= (int)ColorBits.GreenChannel;
                    colorButton.atlasRenderer.customBit &= ~(int)ColorBits.Invert;
                }

                int index = i;

                void MouseUpColor()
                {
                    if (isOpen)
                    {
                        colorButton.atlasRenderer.customBit |= (int)ColorBits.GreenChannel;
                        colorButton.atlasRenderer.customBit ^= (int)ColorBits.Invert;

                        Shader.SetGlobalColor(globalShaderID, selectableColor.linear);
                        options.lightColor = selectableColor;
                        selectedIndex = index;
                        curColorButtonPositions[index].z = SELECTED_BUTTON_DEPTH;

                        for (int j = 0; j < colorButtons.Length; j++)
                        {
                            if (j == selectedIndex) continue;
                            IconButton colorButton = colorButtons[j];

                            colorButton.atlasRenderer.customBit &= ~(int)ColorBits.GreenChannel;
                            curColorButtonPositions[j].z = BUTTON_DEPTH;
                        }
                    }
                    else
                    {
                        Open();
                    }
                }
                void EnterButton()
                {
                    if (pickerType == PickerType.DarkColor)
                    {
                        colorButton.atlasRenderer.customBit |= (int)ColorBits.GreenChannel;
                    }
                    else
                    {
                        colorButton.atlasRenderer.customBit |= (int)ColorBits.RedChannel;
                    }
                }
                void ExitButton()
                {
                    if (pickerType == PickerType.DarkColor)
                    {
                        colorButton.atlasRenderer.customBit &= ~(int)ColorBits.GreenChannel;
                    }
                    else
                    {
                        colorButton.atlasRenderer.customBit &= ~(int)ColorBits.RedChannel;
                    }
                    colorButton.atlasRenderer.customBit &= ~(int)ColorBits.Invert;
                }
                colorButton.InitButton(MouseUpColor, onEnter: EnterButton, onExit: ExitButton);

                colorButtons[i] = colorButton;
                curColorButtonPositions[i] = closeButtonPosition;

                Vector3 openPos = new Vector3();
                openPos.x = closeButtonPosition.x + ((colorButton.atlasRenderer.sprite.worldSize.x + BUTTON_PADDING) * i);
                openPos.y = closeButtonPosition.y;
                openPos.z = closeButtonPosition.z;

                openColorButtonPositions[i] = openPos;
            }
        }

        void MouseUpText()
        {
            if (isOpen)
            {
                for (int i = 0; i < colorButtons.Length; i++)
                {
                    IconButton colorButton = colorButtons[i];
                    if (colorButton.curState == ButtonState.Hovered || colorButton.curState == ButtonState.Clicked) return;
                }
                Close();
            }
            else
            {
                Open();
            }

            textButton.backgroundRenderer.customBit &= ~(int)ColorBits.Invert;
            textButton.textRenderer.customBit |= (int)ColorBits.Invert;
        }
        void MouseDownText()
        {
            if (isOpen)
            {
                for (int i = 0; i < colorButtons.Length; i++)
                {
                    IconButton colorButton = colorButtons[i];
                    if (colorButton.curState == ButtonState.Hovered || colorButton.curState == ButtonState.Clicked) return;
                }
            }

            textButton.backgroundRenderer.customBit ^= (int)ColorBits.Invert;
            textButton.textRenderer.customBit ^= (int)ColorBits.Invert;
        }
        void EnterButtonText()
        {
            if (isOpen)
            {
                for (int i = 0; i < colorButtons.Length; i++)
                {
                    IconButton colorButton = colorButtons[i];
                    if (colorButton.curState == ButtonState.Hovered || colorButton.curState == ButtonState.Clicked) return;
                }
            }
            textButton.backgroundRenderer.customBit |= (int)ColorBits.GreenChannel;
        }
        void ExitButtonText()
        {
            if (isOpen)
            {
                for (int i = 0; i < colorButtons.Length; i++)
                {
                    IconButton colorButton = colorButtons[i];
                    if (colorButton.curState == ButtonState.Hovered || colorButton.curState == ButtonState.Clicked) return;
                }
            }
            textButton.backgroundRenderer.customBit &= ~(int)ColorBits.GreenChannel;
            textButton.backgroundRenderer.customBit &= ~(int)ColorBits.Invert;
            textButton.textRenderer.customBit |= (int)ColorBits.Invert;
        }

        textButton.InitButton(MouseUpText, MouseDownText, EnterButtonText, ExitButtonText);
    }
    private void Update()
    {
        UpdatePicker();
    }
    private void UpdatePicker()
    {
        textButton.UpdateButton();
        if (isOpen)
        {
            for (int i = 0; i < colorButtons.Length; i++)
            {
                colorButtons[i].UpdateButton();
            }
        }
    }
    public void Open()
    {
        isOpen = true;
        for (int i = 0; i < colorButtons.Length; i++)
        {
            if (i == selectedIndex) continue;
            colorButtons[i].atlasRenderer.enabled = true;
        }

        ctsOpen?.Cancel();
        ctsOpen = new CancellationTokenSource();

        Opening().Forget();
    }
    public void Close()
    {
        isOpen = false;

        ctsOpen?.Cancel();
        ctsOpen = new CancellationTokenSource();
        Closing().Forget();
    }
    public async UniTask Opening()
    {
        try
        {
            while (openClock <= OPEN_TIME)
            {
                openClock += Time.deltaTime;
                float t = openClock / OPEN_TIME;
                t = Curves.EaseInOutCubic(t);
                textButton.backgroundRenderer.width = Mathf.Lerp(closeWidth, openWidth, t);
                textButton.backgroundRenderer.UpdateSliceSpriteInputsSelf();
                textButton.backgroundRenderer.transform.localPosition = Vector3.Lerp(closeBackgroundPosition, openBackgroundPosition, t);
                for (int i = 0; i < colorButtons.Length; i++)
                {
                    IconButton patternButton = colorButtons[i];
                    Vector3 openPos = openColorButtonPositions[i];
                    curColorButtonPositions[i].x = Mathf.Lerp(closeButtonPosition.x, openPos.x, t);
                    patternButton.transform.localPosition = curColorButtonPositions[i];
                }

                await UniTask.Yield();
            }
        }
        catch (OperationCanceledException)
        {

        }
    }
    public async UniTask Closing() 
    {
        try
        {
            while (openClock >= 0)
            {
                openClock -= Time.deltaTime;
                float t = openClock / OPEN_TIME;
                t = Curves.EaseInOutCubic(t);
                textButton.backgroundRenderer.width = Mathf.Lerp(closeWidth, openWidth, t);
                textButton.backgroundRenderer.UpdateSliceSpriteInputsSelf();
                textButton.backgroundRenderer.transform.localPosition = Vector3.Lerp(closeBackgroundPosition, openBackgroundPosition, t);

                for (int i = 0; i < colorButtons.Length; i++)
                {
                    IconButton patternButton = colorButtons[i];
                    Vector3 openPos = openColorButtonPositions[i];
                    curColorButtonPositions[i].x = Mathf.Lerp(closeButtonPosition.x, openPos.x, t);
                    patternButton.transform.localPosition = curColorButtonPositions[i];
                }

                await UniTask.Yield();
            }
            for (int i = 0; i < colorButtons.Length; i++)
            {
                if (i == selectedIndex) continue;

                IconButton colorButton = colorButtons[i];
                colorButton.atlasRenderer.enabled = false;
            }
        }
        catch (OperationCanceledException)
        {

        }
    }
}
