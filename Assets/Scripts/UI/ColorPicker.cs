using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;

using static AtlasUI;
public class ColorPicker : MonoBehaviour
{
    public enum ColorPickerType
    { 
        Clue,
        Main,
    }

    public enum SelectType
    {
        NPC,
        Clue,
        Dark,
        Light
    }

    public enum Direction
    {
        BottomLeft,
        BottomRight,
        TopLeft,
        TopRight,
    }

    public TripSO trip;
    public OptionsSO colorsData;
    public PlayerInputsSO playerInputs;
    
    public IconUIElement[] colorIcons;
    public AtlasRenderer paletteRenderer;
    
    public ColorPickerType colorPickerType;
    public Direction direction;

    public int colorGridXCount = 4;
    public int colorGridYCount = 4;

    [Header("Generated")]
    public AtlasRenderer selectedRenderer;

    public CancellationTokenSource ctsOpen;

    public Vector2[] defaultOpenColorRendPositions;
    public Vector2[] curOpenColorRendPositions;

    public Vector3 curWorldPos;
    public Vector3 defaultCloseColorRendPos;
    public Vector3 curCloseColorRendPos;
    public Vector3 paletteCenterSliceWorldSize;

    public Vector2 colorRendererWorldSize;
    public Vector2 sliceWorldSize;

    public int activeColorAmount;
    public int curGridRowCount;
    public int curGridColCount;
    public int selectedDarkColorIndex;
    public int selectedLightColorIndex;

    public float openClock;
    public float openSpriteWidth;
    public float openSpriteHeight;
    public float curSpriteWidth;
    public float curSpriteHeight;
    public float tileWidth;
    public float tileHeight;

    public SelectType selectType;
    public bool canClose;

    private void OnEnable()
    {
        Scenes.OnLoadStart += Init;
        Scenes.OnLoadTrip0 += Init;
    }
    private void OnDisable()
    {
        Scenes.OnLoadStart -= Init;
        Scenes.OnLoadTrip0 -= Init;
        
    }
    private void Start()
    {
        if (Application.isPlaying)
        {
            TurnOff();
        }
        Shader.SetGlobalColor("_BlackColor", colorsData.blackColor.linear);
        Shader.SetGlobalColor("_WhiteColor", colorsData.whiteColor.linear);
        Shader.SetGlobalColor("_MeridiaColor", colorsData.meridiaColor.linear);

        Shader.SetGlobalFloat("_DayNightFactor", colorsData.dayNightFactor);
        Shader.SetGlobalTexture("_DiagonalTexture", colorsData.diagonalTexture);
        Shader.SetGlobalTexture("_StripesTexture", colorsData.stripesTexture);
    }

    private void Init()
    {
        trip.unlockedColorMarkerCount = 0;
        colorsData.curState = PickerState.Closed;

        SetSelectableColors();
        SetOpenPosAndSize();

        for (int i = 0; i < colorIcons.Length; i++)
        {
            int index = i;

            void ClickIcon(IconUIElement icon)
            {
                switch (selectType)
                {
                    case SelectType.Dark:
                    {
                        selectedDarkColorIndex = index;

                        for (int j = 0; j < activeColorAmount; j++)
                        {
                            colorIcons[j].renderer.UpdateSpriteInputsByIndex(COLOR_SQUARE_SPRITE_INDEX);
                            colorIcons[j].renderer.custom.w = 1;
                        }
                        icon.renderer.UpdateSpriteInputsByIndex(TICK_SPRITE_INDEX);

                        colorsData.blackColor = colorsData.selectableDarkColors[index];

                        Shader.SetGlobalColor("_BlackColor", colorsData.blackColor.linear);
                    }
                    break;
                    case SelectType.Light:
                    {
                        selectedLightColorIndex = index;
                        for (int i = 0; i < activeColorAmount; i++)
                        {
                            colorIcons[i].renderer.UpdateSpriteInputsByIndex(COLOR_SQUARE_SPRITE_INDEX);
                            colorIcons[i].renderer.custom.w = 1;
                        }
                        icon.renderer.UpdateSpriteInputsByIndex(TICK_SPRITE_INDEX);

                        colorsData.whiteColor = colorsData.selectableLightColors[index];

                        Shader.SetGlobalColor("_WhiteColor", colorsData.whiteColor.linear);
                    }
                    break;
                    case SelectType.Clue:
                    {
                        Color selectedColor = colorsData.selectableClueColors[index];
                        trip.selectedClueMarkerColors[trip.selectedColorMarkerIndex] = selectedColor;
                        Shader.SetGlobalColor("_ColorKey" + trip.selectedColorMarkerIndex, selectedColor.linear);
                        selectedRenderer.custom.x = selectedColor.r;
                        selectedRenderer.custom.y = selectedColor.g;
                        selectedRenderer.custom.z = selectedColor.b;
                    }
                    break;
                    case SelectType.NPC:
                    {
                        int colorIndex = index - 1;

                        if (colorIndex >= 0)
                        {
                            if ((selectedRenderer.customBit & (1 << colorIndex)) != 0)
                            {
                                if ((trip.curUnlocks & UnlockType.MultiColor) != 0)
                                {
                                    selectedRenderer.customBit &= ~(1 << colorIndex);
                                }
                                else
                                {
                                    selectedRenderer.customBit = 0;
                                }
                            }
                            else
                            {
                                if ((trip.curUnlocks & UnlockType.MultiColor) != 0)
                                {
                                    selectedRenderer.customBit |= 1 << colorIndex;
                                }
                                else
                                {
                                    selectedRenderer.customBit = 1 << colorIndex;
                                }
                            }
                        }
                        else
                        {
                            if ((selectedRenderer.customBit & (int)ColorBits.Diagonal) != 0)
                            {
                                if ((trip.curUnlocks & UnlockType.MultiColor) != 0)
                                {
                                    selectedRenderer.customBit &= ~((int)ColorBits.Diagonal);
                                }
                                else
                                {
                                    selectedRenderer.customBit = 0;
                                }
                            }
                            else
                            {
                                if ((trip.curUnlocks & UnlockType.MultiColor) != 0)
                                {
                                    selectedRenderer.customBit |= (int)ColorBits.Diagonal;
                                }
                                else
                                {
                                    selectedRenderer.customBit = (int)ColorBits.Diagonal;
                                }
                            }
                        }

                        if (icon.renderer.spriteIndex == TICK_SPRITE_INDEX)
                        {
                            icon.renderer.UpdateSpriteInputsByIndex(COLOR_SQUARE_SPRITE_INDEX);
                        }
                        else
                        {
                            if ((trip.curUnlocks & UnlockType.MultiColor) != 0)
                            {
                                icon.renderer.UpdateSpriteInputsByIndex(TICK_SPRITE_INDEX);
                            }
                            else
                            {
                                for (int j = 0; j < activeColorAmount; j++)
                                {
                                    AtlasRenderer otherColorRend = colorIcons[j].renderer;
                                    if (otherColorRend != icon.renderer && otherColorRend.spriteIndex == TICK_SPRITE_INDEX)
                                    {
                                        otherColorRend.UpdateSpriteInputsByIndex(COLOR_SQUARE_SPRITE_INDEX);
                                    }
                                }

                                icon.renderer.UpdateSpriteInputsByIndex(TICK_SPRITE_INDEX);
                            }
                        }
                    }
                    break;
                }
            }
            colorIcons[i].Init(ClickIcon, EnterColorIcon, ExitColorIcon);
        }
    }
    private void Update()
    {
        UpdateState();
    }
    private void SetState(PickerState newState)
    {
        if (colorsData.curState == newState) return;
        ExitState();
        colorsData.curState = newState;
        colorsData.enteredState = newState;
        EnterState();
    }
    private void EnterColorIcon(IconUIElement icon)
    {
        icon.renderer.custom.w = 0;
    }
    private void ExitColorIcon(IconUIElement icon)
    {
        icon.renderer.custom.w = 1;
    }

    private void UpdateState()
    {
        switch(colorsData.curState)
        {
            case PickerState.Opening:
            case PickerState.Opened:
            {
                for (int i = 0; i < activeColorAmount; i++)
                {
                    colorIcons[i].UpdateButton(playerInputs);
                }

                if (canClose && (playerInputs.mouseLeftDown || playerInputs.shiftDown) && !CursorController.IsInsideBounds(paletteRenderer.bounds, isClickable: false))
                {
                    Close();
                }
                canClose = true;
            }
            break;
        }
    }
    private void EnterState()
    {
        switch(colorsData.curState)
        {
            case PickerState.Opening:
            {
                canClose = false;
            }
            break;
            case PickerState.Opened:
            {

            }
            break;
            case PickerState.Closed:
            {

            }
            break;
        }
    }
    private void ExitState()
    {

    }
    private void SetSelectableColors()
    {
        for (int i = 0; i < colorIcons.Length; i++)
        {
            AtlasRenderer colorRenderer = colorIcons[i].renderer;
            colorRenderer.enabled = false;
        }

        if (colorPickerType == ColorPickerType.Clue)
        {
            trip.selectedClueMarkerColors = new Color[]
            {
                Color.black,
                Color.black,
                Color.black,
            };
            for (int i = 0; i < trip.selectedClueMarkerColors.Length - 1; i++)
            {
                Shader.SetGlobalColor("_ColorKey" + i, Color.black);
            }
            SceneController.SetClueColorPicker(this);
        }
        else
        {
            SceneController.SetMainColorPicker(this);
        }
    }
    public void SetOpenPosAndSize()
    {
        defaultOpenColorRendPositions = new Vector2[colorIcons.Length];
        curOpenColorRendPositions = new Vector2[colorIcons.Length];

        AtlasRenderer firstColorRend = colorIcons[0].renderer;
        Vector4 paletteBottomRightWPS = paletteRenderer.worldPivotsAndSizes[5];
        Vector2 firstColorRendPos = new Vector2(paletteBottomRightWPS.x + firstColorRend.worldPivotAndSize.x, paletteBottomRightWPS.y - firstColorRend.worldPivotAndSize.y);

        for (int y = 0; y < colorGridYCount;  y++)
        {
            int rowIndex = y * colorGridXCount;
            float yPos = firstColorRendPos.y + (y * GRID_GAP);

            for (int x = 0; x < colorGridXCount; x++)
            {
                int flatIndex = x + rowIndex;

                AtlasRenderer colorRend = colorIcons[flatIndex].renderer;

                float xPos = firstColorRendPos.x - (x * GRID_GAP);
                defaultOpenColorRendPositions[flatIndex] = new Vector3(xPos, yPos, -1);

                colorRend.transform.localPosition = defaultOpenColorRendPositions[flatIndex];
            }
        }

        defaultCloseColorRendPos = new Vector3(firstColorRendPos.x, firstColorRendPos.y, -0.1f);
        colorRendererWorldSize = firstColorRend.sprite.worldSize;

        curWorldPos.z = paletteRenderer.transform.position.z;
        
        Vector4 paletteCenterWPS = paletteRenderer.worldPivotsAndSizes[4];
        paletteCenterSliceWorldSize = new Vector2(paletteCenterWPS.z, paletteCenterWPS.w);

        Vector4 paletteBottomLeftWPS = paletteRenderer.worldPivotsAndSizes[0];
        Vector4 paletteTopRightWPS = paletteRenderer.worldPivotsAndSizes[8];

        sliceWorldSize = new Vector2(paletteBottomLeftWPS.z + paletteTopRightWPS.z, paletteBottomLeftWPS.w + paletteTopRightWPS.w);
    }
    public void TurnOff()
    {
        paletteRenderer.enabled = false;

        for(int i = 0; i < activeColorAmount; i++)
        {
            colorIcons[i].renderer.enabled = false;
        }

        selectedRenderer = null;
        transform.SetParent(null);
    }
    public void TurnOn(SelectType selectedType, AtlasRenderer rend, Direction direction = Direction.TopLeft)
    {
        paletteRenderer.enabled = true;

        selectType = selectedType;
        selectedRenderer = rend;
        Bounds selectedRendBounds = selectedRenderer.GetBounds();
        curWorldPos.y = selectedRendBounds.max.y;

        switch (direction)
        {
            case Direction.TopLeft:
            {
                paletteRenderer.flipX = false;
                paletteRenderer.flipY = false;

                for (int i = 0; i < curOpenColorRendPositions.Length; i ++)
                {
                    Vector2 defPos = defaultOpenColorRendPositions[i];
                    curOpenColorRendPositions[i] = defPos;
                }
                Vector3 newClosePos = defaultCloseColorRendPos;
                curCloseColorRendPos = newClosePos;

                curWorldPos.x = selectedRendBounds.min.x;
            }
            break;

            case Direction.TopRight:
            {
                paletteRenderer.flipX = true;
                paletteRenderer.flipY = false;

                for (int i = 0; i < curOpenColorRendPositions.Length; i++)
                {
                    Vector2 newPos = defaultOpenColorRendPositions[i];
                    newPos.x *= -1;
                    curOpenColorRendPositions[i] = newPos;
                }
                Vector3 newClosePos = defaultCloseColorRendPos;
                newClosePos.x *= -1;

                curCloseColorRendPos = newClosePos;

                curWorldPos.x = selectedRendBounds.max.x;
            }
            break;

            case Direction.BottomLeft:
            {
                paletteRenderer.flipX = false;
                paletteRenderer.flipY = true;

                for (int i = 0; i < curOpenColorRendPositions.Length; i++)
                {
                    Vector2 defPos = defaultOpenColorRendPositions[i];
                    defPos.y *= -1;
                    curOpenColorRendPositions[i] = defPos;
                }

                Vector3 newClosePos = defaultCloseColorRendPos;
                newClosePos.y *= -1;

                curCloseColorRendPos = newClosePos;

                curWorldPos.x = selectedRendBounds.min.x;
            }
            break;

            case Direction.BottomRight:
            {
                paletteRenderer.flipX = true;
                paletteRenderer.flipY = true;

                for (int i = 0; i < curOpenColorRendPositions.Length; i++)
                {
                    Vector2 defPos = defaultOpenColorRendPositions[i];
                    defPos.x *= -1;
                    defPos.y *= -1;
                    curOpenColorRendPositions[i] = defPos;
                }
                Vector3 newClosePos = defaultCloseColorRendPos;
                newClosePos.x *= -1;
                newClosePos.y *= -1;
                curCloseColorRendPos = newClosePos;
                curWorldPos.x = selectedRendBounds.max.x;
            }
            break;
        }
        paletteRenderer.UpdateSliceSpriteInputsSelf();

        switch(selectedType)
        {
            case SelectType.NPC:
            {
                activeColorAmount = trip.selectedClueMarkerColors.Length + 1;

                for (int i = 0; i < activeColorAmount; i++)
                {
                    AtlasRenderer colorRend = colorIcons[i].renderer;
                    colorRend.enabled = true;

                    int colorIndex = i - 1;

                    if (i == 0)
                    {
                        if ((selectedRenderer.customBit & ((int)ColorBits.Diagonal)) != 0)
                        {
                            colorRend.UpdateSpriteInputsByIndex(TICK_SPRITE_INDEX);
                        }
                        else
                        {
                            colorRend.UpdateSpriteInputsByIndex(COLOR_SQUARE_SPRITE_INDEX);
                        }

                        colorRend.customBit |= (int)ColorBits.Diagonal;

                        colorRend.custom.x = 0;
                        colorRend.custom.y = 0;
                        colorRend.custom.z = 0;
                    }
                    else if (colorIndex < trip.unlockedColorMarkerCount)
                    {
                        if ((selectedRenderer.customBit & (1 << colorIndex)) != 0)
                        {
                            colorRend.UpdateSpriteInputsByIndex(TICK_SPRITE_INDEX);
                        }
                        else
                        {

                            colorRend.UpdateSpriteInputsByIndex(COLOR_SQUARE_SPRITE_INDEX);
                        }

                        Color color = trip.selectedClueMarkerColors[colorIndex].linear;

                        colorRend.customBit = 0;

                        colorRend.custom.x = color.r;
                        colorRend.custom.y = color.g;
                        colorRend.custom.z = color.b;
                    }
                    else
                    {
                        colorRend.UpdateSpriteInputsByIndex(LOCK_SPRITE_INDEX);

                        colorRend.customBit = 0;

                        colorRend.custom.x = 0;
                        colorRend.custom.y = 0;
                        colorRend.custom.z = 0;
                    }
                    colorRend.custom.w = 1;
                }
            }
            break;

            case SelectType.Clue:
            {
                activeColorAmount = colorIcons.Length;

                Color[] colorsToUse = colorPickerType == ColorPickerType.Clue ? colorsData.selectableClueColors : colorsData.selectableDarkColors;

                for (int i = 0; i < activeColorAmount; i++)
                {
                    AtlasRenderer colorRend = colorIcons[i].renderer;
                    colorRend.custom = colorsToUse[i].linear;
                    colorRend.customBit = 0;
                    colorRend.UpdateSpriteInputsByIndex(COLOR_SQUARE_SPRITE_INDEX);
                    colorRend.enabled = true;
                }
            }
            break;

            case SelectType.Light:
            {
                activeColorAmount = colorIcons.Length;

                Color[] colorsToUse = colorPickerType == ColorPickerType.Clue ? colorsData.selectableClueColors : colorsData.selectableLightColors;

                for (int i = 0; i < activeColorAmount; i++)
                {
                    AtlasRenderer colorRend = colorIcons[i].renderer;
                    colorRend.custom = colorsToUse[i].linear;
                    colorRend.customBit = 0;
                    colorRend.enabled = true;
                    if (i == selectedLightColorIndex)
                    {
                        colorRend.UpdateSpriteInputsByIndex(TICK_SPRITE_INDEX);
                    }
                    else
                    {
                        colorRend.UpdateSpriteInputsByIndex(COLOR_SQUARE_SPRITE_INDEX);
                    }
                }
            }
            break;
            case SelectType.Dark:
            {
                activeColorAmount = colorIcons.Length;

                Color[] colorsToUse = colorPickerType == ColorPickerType.Clue ? colorsData.selectableClueColors : colorsData.selectableDarkColors;

                for (int i = 0; i < activeColorAmount; i++)
                {
                    AtlasRenderer colorRend = colorIcons[i].renderer;
                    colorRend.custom = colorsToUse[i].linear;
                    colorRend.customBit = 0;
                    colorRend.enabled = true;
                    if (i == selectedDarkColorIndex)
                    {
                        colorRend.UpdateSpriteInputsByIndex(TICK_SPRITE_INDEX);
                    }
                    else
                    {
                        colorRend.UpdateSpriteInputsByIndex(COLOR_SQUARE_SPRITE_INDEX);
                    }
                }
            }
            break;
        }


        curGridColCount = Mathf.Min(activeColorAmount, colorGridXCount);
        curGridRowCount = Mathf.CeilToInt((float)activeColorAmount / (float)colorGridXCount);

        int curXGapCount = curGridColCount - 1;
        int curYGapCount = curGridRowCount - 1;

        float totalGapWidth = curXGapCount * GRID_GAP;
        float totalGapHeight = curYGapCount * GRID_GAP;

        tileWidth = colorRendererWorldSize.x / paletteCenterSliceWorldSize.x;
        tileHeight = colorRendererWorldSize.y / paletteCenterSliceWorldSize.y;

        openSpriteWidth = (tileWidth * curGridColCount) + totalGapWidth;
        openSpriteHeight = (tileHeight * curGridRowCount) + totalGapHeight;

        paletteRenderer.transform.position = curWorldPos;
        paletteRenderer.width = tileWidth;
        paletteRenderer.height = tileHeight;
    }
    public void Open(AtlasRenderer rend, SelectType selectedType, Direction direction = Direction.TopLeft)
    {
        if (colorsData.curState == PickerState.Closed)
        {
            ctsOpen?.Cancel();
            ctsOpen = new CancellationTokenSource();

            TurnOn(selectedType, rend, direction);
            Opening().Forget();
        }
    }
    public void Close()
    {
        if (colorsData.curState == PickerState.Opened || colorsData.curState == PickerState.Opening)
        {
            ctsOpen?.Cancel();
            ctsOpen = new CancellationTokenSource();
            Closing().Forget();
        }
    }
    public async UniTask Opening()
    {
        try
        {
            SetState(PickerState.Opening);

            float totalTime = curGridRowCount * curGridColCount * OPEN_TIME_ROW_COL;
            openClock = Mathf.Max(openClock, 0);

            float rowsToClose = (float)(curGridRowCount - 1);
            float colsToClose = (float)(curGridColCount - 1);

            float normRowTime = rowsToClose / (rowsToClose + colsToClose);
            float normColTime = 1 - normRowTime;
            
            while (openClock < totalTime)
            {
                openClock += Time.deltaTime;
                float t = openClock / totalTime;
     
                if (t < normColTime)
                {
                    float easeOutT = Curves.EaseOutT(t / normColTime, 5);

                    curSpriteWidth = openSpriteWidth * easeOutT;

                    paletteRenderer.width = curSpriteWidth;
                    paletteRenderer.UpdateSliceSpriteInputsSelf();

                    for (int i = 0; i < activeColorAmount; i++)
                    {
                        float posX = Mathf.Lerp(curCloseColorRendPos.x, curOpenColorRendPositions[i].x, easeOutT);
                        colorIcons[i].renderer.transform.localPosition = new Vector3(posX, curCloseColorRendPos.y, curCloseColorRendPos.z);
                    }
                }
                else
                {
                    float easOutT = Curves.EaseOutT((t - normColTime) / normRowTime, 5);
                    curSpriteHeight = Mathf.Lerp(tileHeight, openSpriteHeight, easOutT);
                    paletteRenderer.height = curSpriteHeight;
                    paletteRenderer.UpdateSliceSpriteInputsSelf();

                    for (int i = 0; i < activeColorAmount; i++)
                    {
                        float posY = Mathf.Lerp(curCloseColorRendPos.y, curOpenColorRendPositions[i].y, easOutT);
                        colorIcons[i].renderer.transform.localPosition = new Vector3(curOpenColorRendPositions[i].x, posY, curCloseColorRendPos.z);
                    }
                }
                await UniTask.Yield(ctsOpen.Token);
            }
            SetState(PickerState.Opened);
        }
        catch (OperationCanceledException)
        {
            SetState(PickerState.Opened);
        }
    }
    public async UniTask Closing()
    {
        try
        {
            SetState(PickerState.Closing);

            float totalTime = curGridRowCount * curGridColCount * OPEN_TIME_ROW_COL;
            openClock = Mathf.Min(openClock, totalTime);

            float rowsToClose = (float)(curGridRowCount - 1);
            float colsToClose = (float)(curGridColCount - 1);

            float normRowTime = rowsToClose / (rowsToClose + colsToClose);
            float normColTime = 1 - normRowTime;

            while (openClock > 0)
            {
                openClock -= Time.deltaTime;

                float t = openClock / totalTime; 

                if (t < normColTime)
                {
                    float easeOutT = Curves.EaseOutT(t / normColTime, 5);
                    curSpriteWidth = openSpriteWidth * easeOutT;

                    paletteRenderer.width = curSpriteWidth;
                    paletteRenderer.UpdateSliceSpriteInputsSelf();
                    for (int i = 0; i < activeColorAmount; i++)
                    {
                        float posX = Mathf.Lerp(curCloseColorRendPos.x, curOpenColorRendPositions[i].x, easeOutT);
                        colorIcons[i].renderer.transform.localPosition = new Vector3(posX, curCloseColorRendPos.y, curCloseColorRendPos.z);
                    }
                }
                else
                {
                    float easOutT = Curves.EaseOutT((t - normColTime) / normRowTime, 5);
                    curSpriteHeight = Mathf.Lerp(tileHeight, openSpriteHeight, easOutT);
                    paletteRenderer.height = curSpriteHeight;
                    paletteRenderer.UpdateSliceSpriteInputsSelf();

                    for (int i = 0; i < activeColorAmount; i++)
                    {
                        float posY = Mathf.Lerp(curCloseColorRendPos.y, curOpenColorRendPositions[i].y, easOutT);
                        colorIcons[i].renderer.transform.localPosition = new Vector3(curOpenColorRendPositions[i].x, posY, curCloseColorRendPos.z);
                    }
                }
                await UniTask.Yield(ctsOpen.Token);
            }
            SetState(PickerState.Closed);
            TurnOff();

        }
        catch (OperationCanceledException)
        {
            SetState(PickerState.Closed);
        }
    }
}
