using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;

public class ColorPicker : MonoBehaviour
{
    public enum State
    {
        Opening,
        Opened,
        Closing,
        Closed,
    }
    public const float OPEN_TIME = 1;
    public const float COLOR_GRID_GAP = 0.272f;
    public const int COLOR_GRID_X_COUNT = 4;
    public const int COLOR_GRID_Y_COUNT = 4;

    public TripSO trip;
    public ColorsSO colorsData;
    public PlayerInputsSO playerInputs;
    
    public AtlasRenderer[] colorRenderers;
    public AtlasRenderer paletteRenderer;
    
    [Header("Generated")]
    public AtlasRenderer selectedRenderer;
    public AtlasRenderer prevHoveredColorRenderer;

    public CancellationTokenSource ctsOpen;

    public Vector2[] openColorRendererPositions;

    public State curState;

    public Vector3 curWorldPos;
    public Vector3 closeColorRendererPosition;
    public Vector3 paletteCenterSliceWorldSize;

    public Vector2 colorRendererWorldSize;
    public Vector2 sliceWorldSize;

    public int activeColorAmount;

    public float openClock;
    public float openSpriteWidth;
    public float openSpriteHeight;
    public float curSpriteWidth;
    public float curSpriteHeight;
    public float tileWidth;
    public float tileHeight;

    public bool openedFully;

    private void Awake()
    {
        trip.unlockedClueMarkerCount = 1;

        if (Application.isPlaying)
        {
            SceneController.KeepColorPicker(this);
        }
    }
    private void Start()
    {
        curState = State.Closed;

        SetSelectableColors();
        SetOpenPosAndSize();
        if (Application.isPlaying)
        {
            TurnOff();
        }
        Shader.SetGlobalColor("_BlackColor", colorsData.blackColor);
        Shader.SetGlobalColor("_WhiteColor", colorsData.whiteColor);
        Shader.SetGlobalFloat("_DayNight", colorsData.dayNight);
        Shader.SetGlobalFloat("_DayNightFactor", colorsData.dayNightFactor);
    }
    private void Update()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            SetSelectableColors();
        }
        Shader.SetGlobalColor("_BlackColor", colorsData.blackColor);
        Shader.SetGlobalColor("_WhiteColor", colorsData.whiteColor);
        Shader.SetGlobalFloat("_DayNightFactor", colorsData.dayNightFactor);
#endif
        Shader.SetGlobalFloat("_DayNight", colorsData.dayNight);

        UpdateState();
    }
    private void UpdateState()
    {
        switch(curState)
        {
            case State.Opened:
            case State.Opening:
            {
                for (int i = 0; i < activeColorAmount; i++)
                {
                    AtlasRenderer colorRend = colorRenderers[i];

                    if (CursorController.IsInsideBounds(colorRend.bounds))
                    {
                        if (playerInputs.mouseLeftHold)
                        {
                            colorRend.custom.w = 0;

                            if (openedFully)
                            {
                                SetNewColor(i);
                            }
                            else
                            {
                                SetNPCColor(i);
                            }
                        }
                        else
                        {
                            colorRend.custom.w = 1;   

                        }

                        prevHoveredColorRenderer = colorRend;
                        return;
                    }
                    else if (colorRend == prevHoveredColorRenderer)
                    {
                        colorRend.custom.w = 1;
                        prevHoveredColorRenderer = null;
                        return;
                    }
                }
                if (playerInputs.mouseLeftDown && !CursorController.IsInsideBounds(paletteRenderer.bounds))
                {
                    Close();
                }
            }
            break;
        }
    }
    private void SetSelectableColors()
    {
        for (int i = 0; i < colorRenderers.Length; i++)
        {
            AtlasRenderer colorRenderer = colorRenderers[i];
            colorRenderer.enabled = false;
        }
        trip.selectedClueMarkerColors = new Color[]
        {
            Color.white,
            Color.black,
            Color.black,
            Color.black,
        };
        for (int i = 0; i < trip.selectedClueMarkerColors.Length - 1; i++)
        {
            Shader.SetGlobalColor("_ColorKey" + i, Color.black);
        }

    }
    public void SetOpenPosAndSize()
    {
        openColorRendererPositions = new Vector2[colorRenderers.Length];

        AtlasRenderer firstColorRend = colorRenderers[0];
        Vector4 paletteBottomRightWPS = paletteRenderer.worldPivotsAndSizes[5];
        Vector2 firstColorRendPos = new Vector2(paletteBottomRightWPS.x + firstColorRend.worldPivotAndSize.x, paletteBottomRightWPS.y - firstColorRend.worldPivotAndSize.y);

        for (int y = 0; y < COLOR_GRID_Y_COUNT;  y++)
        {
            int rowIndex = y * COLOR_GRID_X_COUNT;
            float yPos = firstColorRendPos.y + (y * COLOR_GRID_GAP);

            for (int x = 0; x < COLOR_GRID_X_COUNT; x++)
            {
                int flatIndex = x + rowIndex;

                AtlasRenderer colorRend = colorRenderers[flatIndex];

                float xPos = firstColorRendPos.x - (x * COLOR_GRID_GAP);
                openColorRendererPositions[flatIndex] = new Vector3(xPos, yPos, -1);

                colorRend.transform.localPosition = openColorRendererPositions[flatIndex];
            }
        }

        closeColorRendererPosition = new Vector3(firstColorRendPos.x, firstColorRendPos.y, -0.1f);
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
            colorRenderers[i].enabled = false;
        }

        selectedRenderer = null;
        transform.SetParent(null);
    }
    public void TurnOn(bool openFully, AtlasRenderer rend)
    {
        paletteRenderer.enabled = true;

        openedFully = openFully;

        activeColorAmount = openedFully ? colorRenderers.Length : trip.selectedClueMarkerColors.Length;
        Color[] colorsToUse = openedFully ? colorsData.selectableClueColors : trip.selectedClueMarkerColors;

        for (int i = 0; i < activeColorAmount; i++)
        {
            AtlasRenderer colorRend = colorRenderers[i];
            colorRend.custom = colorsToUse[i].linear;
            colorRenderers[i].enabled = true;
        }
        
        selectedRenderer = rend;

        Bounds selectedRendBounds = selectedRenderer.GetBounds();

        curWorldPos.x = selectedRendBounds.min.x;
        curWorldPos.y = selectedRendBounds.max.y;
        
        int curGridColCount = Mathf.Min(activeColorAmount, COLOR_GRID_X_COUNT);
        int curGridRowCount = Mathf.CeilToInt((float)activeColorAmount / (float)COLOR_GRID_X_COUNT);

        int curXGapCount = curGridColCount - 1;
        int curYGapCount = curGridRowCount - 1;

        float totalGapWidth = curXGapCount * COLOR_GRID_GAP;
        float totalGapHeight = curYGapCount * COLOR_GRID_GAP;

        tileWidth = colorRendererWorldSize.x / paletteCenterSliceWorldSize.x;
        tileHeight = colorRendererWorldSize.y / paletteCenterSliceWorldSize.y;

        openSpriteWidth = (tileWidth * curGridColCount) + totalGapWidth;
        openSpriteHeight = (tileHeight * curGridRowCount) + totalGapHeight;

        paletteRenderer.transform.position = curWorldPos;
        paletteRenderer.width = tileWidth;
        paletteRenderer.height = tileHeight;
    }
    public void SetNewColor(int index)
    {
        Color selectedColor = colorsData.selectableClueColors[index];
        selectedRenderer.custom = selectedColor.linear;
        trip.selectedClueMarkerColors[trip.selectedClueMarkerIndex + 1] = selectedColor;

        Shader.SetGlobalColor("_ColorKey" + trip.selectedClueMarkerIndex, selectedColor.linear);

    }
    public void SetNPCColor(int index)
    {
        selectedRenderer.custom.x = 1 << (index - 1);
    }
    public void Open(AtlasRenderer rend, bool openAllColors)
    {
        if (curState == State.Closed)
        {
            ctsOpen?.Cancel();
            ctsOpen = new CancellationTokenSource();

            TurnOn(openAllColors, rend);
            Opening().Forget();
        }
    }
    public void Close()
    {
        if (curState == State.Opened || curState == State.Opening)
        {
            ctsOpen?.Cancel();
            ctsOpen = new CancellationTokenSource();

            transform.SetParent(selectedRenderer.transform);
            Closing().Forget();
        }
    }
    public async UniTask Opening()
    {
        try
        {
            curState = State.Opening;

            while (openClock < OPEN_TIME)
            {
                openClock += Time.deltaTime;
                float t = openClock / OPEN_TIME;
     
                if (t < 0.5)
                {
                    float easeOutT = 1 - Mathf.Pow(1 - t * 2, 5);
                    curSpriteWidth = openSpriteWidth * easeOutT;

                    paletteRenderer.width = curSpriteWidth;
                    paletteRenderer.UpdateSliceSpriteInputsSelf();

                    for (int i = 0; i < activeColorAmount; i++)
                    {
                        float posX = Mathf.Lerp(closeColorRendererPosition.x, openColorRendererPositions[i].x, easeOutT);
                        colorRenderers[i].transform.localPosition = new Vector3(posX, closeColorRendererPosition.y, closeColorRendererPosition.z);
                    }
                }
                else
                {
                    float easOutT = 1 - Mathf.Pow(1 - (t - 0.5f) * 2, 5);
                    curSpriteHeight = Mathf.Lerp(tileHeight, openSpriteHeight, easOutT);
                    paletteRenderer.height = curSpriteHeight;
                    paletteRenderer.UpdateSliceSpriteInputsSelf();

                    for (int i = 0; i < activeColorAmount; i++)
                    {
                        float posY = Mathf.Lerp(closeColorRendererPosition.y, openColorRendererPositions[i].y, easOutT);
                        colorRenderers[i].transform.localPosition = new Vector3(openColorRendererPositions[i].x, posY, closeColorRendererPosition.z);
                    }
                }
                await UniTask.Yield(ctsOpen.Token);
            }
            curState = State.Opened;

        }
        catch (OperationCanceledException)
        {

        }
    }
    public async UniTask Closing()
    {
        try
        {
            curState = State.Closing;

            while(openClock > 0)
            {
                openClock -= Time.deltaTime;
                float t = openClock / OPEN_TIME;

                if (t < 0.5)
                {
                    float easeOutT = Mathf.Max(1 - Mathf.Pow(1 - t * 2, 5), 0);
                    curSpriteWidth = openSpriteWidth * easeOutT;

                    paletteRenderer.width = curSpriteWidth;
                    paletteRenderer.UpdateSliceSpriteInputsSelf();
                    for (int i = 0; i < activeColorAmount; i++)
                    {
                        float posX = Mathf.Lerp(closeColorRendererPosition.x, openColorRendererPositions[i].x, easeOutT);
                        colorRenderers[i].transform.localPosition = new Vector3(posX, closeColorRendererPosition.y, closeColorRendererPosition.z);
                    }
                }
                else
                {
                    float easOutT = Mathf.Max(1 - Mathf.Pow(1 - (t - 0.5f) * 2, 5), 0);
                    curSpriteHeight = Mathf.Lerp(tileHeight, openSpriteHeight, easOutT);
                    paletteRenderer.height = curSpriteHeight;
                    paletteRenderer.UpdateSliceSpriteInputsSelf();

                    for (int i = 0; i < activeColorAmount; i++)
                    {
                        float posY = Mathf.Lerp(closeColorRendererPosition.y, openColorRendererPositions[i].y, easOutT);
                        colorRenderers[i].transform.localPosition = new Vector3(openColorRendererPositions[i].x, posY, closeColorRendererPosition.z);
                    }
                }
                await UniTask.Yield(ctsOpen.Token);
            }

            curState = State.Closed;

            TurnOff();

        }
        catch (OperationCanceledException)
        {

        }
    }
}
