using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;
using static Atlas;
using static AtlasUI;
public class ColorPicker : MonoBehaviour
{
    public static event Action OnCloseCluePicker;
    public static event Action OnOpenCluePicker;

    public TripData trip;
    public Options options;
    public SpyData spyData;
    public CameraData camStats;
    public NotepadData notepadData;

    public IconButton[] patternButtons;
    public IconButton exitButton;

    public AtlasRenderer paletteRenderer;

    public int colorGridXCount = 4;
    public int colorGridYCount = 4;

    [Header("Generated")]
    public CancellationTokenSource ctsOpen;

    public Vector2[] defaultOpenColorRendPositions;
    public Vector2[] curOpenColorRendPositions;

    public Vector3 defaultCloseColorRendPos;
    public Vector3 curCloseColorRendPos;
    public Vector3 paletteCenterSliceWorldSize;

    public Vector2 colorRendererWorldSize;
    public Vector2 sliceWorldSize;

    public int activeButtonAmount;
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

    public bool canClose;

    private void Start()
    {
        Init();
    }
    private void Init()
    {
        SetOpenPosAndSize();

        void EnterButton(IconButton icon)
        {
            icon.atlasRenderer.custom.w = 0;
        }
        void ExitButton(IconButton icon)
        {
            icon.atlasRenderer.custom.w = 1;
        }

        for (int i = 0; i < patternButtons.Length; i++)
        {
            int index = i;

            void ClickPattern(IconButton icon)
            {
                options.selectedPatternIndex = index;
            }
            patternButtons[i].InitButton(ClickPattern, EnterButton, ExitButton);
        }
        void ClickExit(IconButton icon)
        {
            Close();
        }
        exitButton.InitButton(ClickExit, EnterButton, ExitButton);
    }
    private void Update()
    {
        UpdatePicker();
    }
    private void UpdatePicker()
    {
        for (int i = 0; i < patternButtons.Length; i++)
        {
            patternButtons[i].UpdateButton();
        }
        exitButton.UpdateButton();
    }
    public void SetOpenPosAndSize()
    {
        defaultOpenColorRendPositions = new Vector2[patternButtons.Length];
        curOpenColorRendPositions = new Vector2[patternButtons.Length];

        AtlasRenderer firstColorRend = patternButtons[0].atlasRenderer;
        Vector4 paletteBottomRightWPS = paletteRenderer.worldPivotsAndSizes[5];
        Vector2 firstColorRendPos = new Vector2(paletteBottomRightWPS.x + firstColorRend.worldPivotAndSize.x, paletteBottomRightWPS.y - firstColorRend.worldPivotAndSize.y);

        for (int y = 0; y < colorGridYCount;  y++)
        {
            int rowIndex = y * colorGridXCount;
            float yPos = firstColorRendPos.y + (y * GRID_GAP);

            for (int x = 0; x < colorGridXCount; x++)
            {
                int flatIndex = x + rowIndex;

                AtlasRenderer colorRend = patternButtons[flatIndex].atlasRenderer;

                float xPos = firstColorRendPos.x - (x * GRID_GAP);
                defaultOpenColorRendPositions[flatIndex] = new Vector3(xPos, yPos, -1);

                colorRend.transform.localPosition = defaultOpenColorRendPositions[flatIndex];
            }
        }

        defaultCloseColorRendPos = new Vector3(firstColorRendPos.x, firstColorRendPos.y, -0.1f);
        colorRendererWorldSize = firstColorRend.sprite.worldSize;
        
        Vector4 paletteCenterWPS = paletteRenderer.worldPivotsAndSizes[4];
        paletteCenterSliceWorldSize = new Vector2(paletteCenterWPS.z, paletteCenterWPS.w);

        Vector4 paletteBottomLeftWPS = paletteRenderer.worldPivotsAndSizes[0];
        Vector4 paletteTopRightWPS = paletteRenderer.worldPivotsAndSizes[8];

        sliceWorldSize = new Vector2(paletteBottomLeftWPS.z + paletteTopRightWPS.z, paletteBottomLeftWPS.w + paletteTopRightWPS.w);
    }
    public void TurnOn(AtlasRenderer rend, Direction direction = Direction.Right)
    {
        paletteRenderer.UpdateSliceSpriteInputsSelf();

        for (int i = 0; i < patternButtons.Length; i++)
        {
            AtlasRenderer patternButton = patternButtons[i].atlasRenderer;
            SimpleSprite patterSprite = options.patternAtlas.simpleSprites[i];
            patternButton.custom = patterSprite.uvSizeAndPos;
            patternButton.UpdateSpriteInputsByIndex(COLOR_SQUARE_SPRITE_INDEX);
        }

        OnOpenCluePicker?.Invoke();

        curGridColCount = Mathf.Min(patternButtons.Length, colorGridXCount);
        curGridRowCount = Mathf.CeilToInt((float)patternButtons.Length / (float)colorGridXCount);

        int curXGapCount = curGridColCount - 1;
        int curYGapCount = curGridRowCount - 1;

        float totalGapWidth = curXGapCount * GRID_GAP;
        float totalGapHeight = curYGapCount * GRID_GAP;

        tileWidth = colorRendererWorldSize.x / paletteCenterSliceWorldSize.x;
        tileHeight = colorRendererWorldSize.y / paletteCenterSliceWorldSize.y;

        openSpriteWidth = (tileWidth * curGridColCount) + totalGapWidth;
        openSpriteHeight = (tileHeight * curGridRowCount) + totalGapHeight;

        paletteRenderer.width = tileWidth;
        paletteRenderer.height = tileHeight;
    }
    public void Open(AtlasRenderer rend, Direction direction = Direction.Right)
    {
        ctsOpen?.Cancel();
        ctsOpen = new CancellationTokenSource();

        TurnOn(rend, direction);

        Opening().Forget();
    }
    public void Close()
    {
        ctsOpen?.Cancel();
        ctsOpen = new CancellationTokenSource();
        Closing().Forget();
    }
    public async UniTask Opening()
    {
        try
        {
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

                    for (int i = 0; i < patternButtons.Length; i++)
                    {
                        float posX = Mathf.Lerp(curCloseColorRendPos.x, curOpenColorRendPositions[i].x, easeOutT);
                        patternButtons[i].transform.localPosition = new Vector3(posX, curCloseColorRendPos.y, curCloseColorRendPos.z);
                    }
                }
                else
                {
                    float easOutT = Curves.EaseOutT((t - normColTime) / normRowTime, 5);
                    curSpriteHeight = Mathf.Lerp(tileHeight, openSpriteHeight, easOutT);
                    paletteRenderer.height = curSpriteHeight;
                    paletteRenderer.UpdateSliceSpriteInputsSelf();

                    for (int i = 0; i < patternButtons.Length; i++)
                    {
                        float posY = Mathf.Lerp(curCloseColorRendPos.y, curOpenColorRendPositions[i].y, easOutT);
                        patternButtons[i].transform.localPosition = new Vector3(curOpenColorRendPositions[i].x, posY, curCloseColorRendPos.z);
                    }
                }
                await UniTask.Yield(ctsOpen.Token);
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
                    for (int i = 0; i < patternButtons.Length; i++)
                    {
                        float posX = Mathf.Lerp(curCloseColorRendPos.x, curOpenColorRendPositions[i].x, easeOutT);
                        patternButtons[i].transform.localPosition = new Vector3(posX, curCloseColorRendPos.y, curCloseColorRendPos.z);
                    }
                }
                else
                {
                    float easOutT = Curves.EaseOutT((t - normColTime) / normRowTime, 5);
                    curSpriteHeight = Mathf.Lerp(tileHeight, openSpriteHeight, easOutT);
                    paletteRenderer.height = curSpriteHeight;
                    paletteRenderer.UpdateSliceSpriteInputsSelf();

                    for (int i = 0; i < patternButtons.Length; i++)
                    {
                        float posY = Mathf.Lerp(curCloseColorRendPos.y, curOpenColorRendPositions[i].y, easOutT);
                        patternButtons[i].transform.localPosition = new Vector3(curOpenColorRendPositions[i].x, posY, curCloseColorRendPos.z);
                    }
                }
                await UniTask.Yield(ctsOpen.Token);
            }
        }
        catch (OperationCanceledException)
        {
        }
    }
}
