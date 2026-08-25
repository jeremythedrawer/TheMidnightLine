using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;

using static AtlasUI;
using static NPC;
public class NPCPicker : MonoBehaviour
{
    public const int GRID_X_COUNT = 8;
    public const int GRID_Y_COUNT = 1;

    public IconButton[] icons;

    public TripData trip;
    public InputData inputData;
    public Options colorData;
    public CameraData camData;
    public CursorData cursorData;

    public AtlasRenderer paletteRenderer;

    [Header("Generated")]

    public NPCBrain[] possibleNPCs;

    public AtlasRenderer selectedRenderer;

    public CancellationTokenSource ctsOpen;

    public Vector2[] openIconRendererPositions;

    public Vector3 curWorldPos;
    public Vector3 closeIconRendererPosition;
    public Vector3 paletteCenterSliceWorldSize;

    public Vector2 iconRendererWorldSize;
    public Vector2 sliceWorldSize;

    public PickerFunctionType functionType;

    public int curGridColCount;

    public float openClock;
    public float openSpriteWidth;
    public float curSpriteWidth;
    public float curSpriteHeight;
    public float tileWidth;
    public float tileHeight;

    private void OnEnable()
    {
        Scenes.OnLoadTrip0 += Init;
    }
    private void OnDisable()
    {
        Scenes.OnLoadTrip0 -= Init;
    }
    private void Update()
    {
        for (int i = 0; i < curGridColCount; i++)
        {
            icons[i].UpdateButton();
        }

        if ((inputData.mouseLeftUp || inputData.mouseRightUp || camData.curLocationState != Spy.LocationState.Carriage) && !cursorData.IsInsideBounds(paletteRenderer.bounds, isClickable: false))
        {
            Close();
        }
    }
    private void Init()
    {
        SceneController.SetNPCPicker(this);
        SetOpenPosAndSize();

        void EnterColorIcon(IconButton icon)
        {
            icon.atlasRenderer.customBit |= (int)ColorBits.Invert;
        }
        void ExitColorIcon(IconButton icon)
        {
            icon.atlasRenderer.customBit &= ~(int)ColorBits.Invert;
        }

        for (int i = 0; i < icons.Length; i++)
        {
            int index = i;

            void ClickIcon(IconButton icon)
            {
                switch (functionType)
                {
                    case PickerFunctionType.TicketCheck:
                    {
                        SceneController.GetSpy().ChooseNPCTicketToCheck(possibleNPCs[index]);
                    }
                    break;

                    case PickerFunctionType.Color:
                    {
                        NPCBrain selectedNPC = possibleNPCs[index];
                        if ((trip.curUnlocks & UnlockType.Color) == 0)
                        {
                            if ((selectedNPC.atlasRenderer.customBit & ((int)ColorBits.Diagonal)) == 0)
                            {
                                selectedNPC.atlasRenderer.customBit |= (int)ColorBits.Diagonal;
                            }
                            else
                            {
                                selectedNPC.atlasRenderer.customBit &= ~((int)ColorBits.Diagonal);
                            }
                        }
                        else
                        {
                            SceneController.GetNPCColorPicker().Open(selectedNPC.atlasRenderer);
                        }
                    }
                    break;

                    case PickerFunctionType.RuleOut:
                    {
                        NPCBrain selectedNPC = possibleNPCs[index];
                        if ((selectedNPC.atlasRenderer.customBit & ((int)ColorBits.Diagonal)) == 0)
                        {
                            selectedNPC.atlasRenderer.customBit |= (int)ColorBits.Diagonal;
                        }
                        else
                        {
                            selectedNPC.atlasRenderer.customBit &= ~((int)ColorBits.Diagonal);
                        }
                    }
                    break;
                }
                Close();
            }

            icons[i].InitButton(ClickIcon, EnterColorIcon, ExitColorIcon);
        }
    }

    public void SetOpenPosAndSize()
    {
        openIconRendererPositions = new Vector2[icons.Length];

        AtlasRenderer firstColorRend = icons[0].atlasRenderer;
        Vector4 paletteBottomRightWPS = paletteRenderer.worldPivotsAndSizes[5];
        Vector2 firstColorRendPos = new Vector2(paletteBottomRightWPS.x + firstColorRend.worldPivotAndSize.x, paletteBottomRightWPS.y - firstColorRend.worldPivotAndSize.y);

        for (int y = 0; y < GRID_Y_COUNT; y++)
        {
            int rowIndex = y * GRID_X_COUNT;
            float yPos = firstColorRendPos.y + (y * GRID_GAP);

            for (int x = 0; x < GRID_X_COUNT; x++)
            {
                int flatIndex = x + rowIndex;

                AtlasRenderer npcIconRend = icons[flatIndex].atlasRenderer;

                float xPos = firstColorRendPos.x - (x * GRID_GAP);
                openIconRendererPositions[flatIndex] = new Vector3(xPos, yPos, -1);

                npcIconRend.transform.localPosition = openIconRendererPositions[flatIndex];
                npcIconRend.enabled = false;
            }
        }

        closeIconRendererPosition = new Vector3(firstColorRendPos.x, firstColorRendPos.y, -0.1f);
        iconRendererWorldSize = firstColorRend.sprite.worldSize;

        curWorldPos.z = paletteRenderer.transform.position.z;

        Vector4 paletteCenterWPS = paletteRenderer.worldPivotsAndSizes[4];
        paletteCenterSliceWorldSize = new Vector2(paletteCenterWPS.z, paletteCenterWPS.w);

        Vector4 paletteBottomLeftWPS = paletteRenderer.worldPivotsAndSizes[0];
        Vector4 paletteTopRightWPS = paletteRenderer.worldPivotsAndSizes[8];

        sliceWorldSize = new Vector2(paletteBottomLeftWPS.z + paletteTopRightWPS.z, paletteBottomLeftWPS.w + paletteTopRightWPS.w);
    }

    public void Open(NPCBrain[] npcs, int npcCount, PickerFunctionType funcType)
    {
        functionType = funcType;

        paletteRenderer.enabled = true;

        possibleNPCs = npcs;
        curGridColCount = npcCount;

        for (int i = 0; i < possibleNPCs.Length; i++)
        {
            AtlasRenderer iconRend = icons[i].atlasRenderer;
            NPCBrain npcBrain = possibleNPCs[i];

            if (npcBrain == null) break;

            if (i < npcCount)
            {
                iconRend.enabled = true;
                int npcIconIndex = (npcBrain.npc.mugShotIndex * 2);
                if (npcBrain.ticketHasBeenChecked) npcIconIndex += 1;

                iconRend.UpdateSpriteInputsByIndex(npcIconIndex);
                iconRend.customBit = npcBrain.atlasRenderer.customBit;
                iconRend.customBit &= ~(int)ColorBits.Invert;
            }
            else
            {
                iconRend.customBit = 0;
                iconRend.enabled = false;
            }
        }

        selectedRenderer = npcs[npcCount - 1].atlasRenderer;

        Bounds selectedRendBounds = selectedRenderer.GetBounds();

        curWorldPos.x = selectedRendBounds.min.x;
        curWorldPos.y = selectedRendBounds.max.y;


        int curXGapCount = curGridColCount - 1;

        float totalGapWidth = curXGapCount * GRID_GAP;

        tileWidth = iconRendererWorldSize.x / paletteCenterSliceWorldSize.x;
        tileHeight = iconRendererWorldSize.y / paletteCenterSliceWorldSize.y;

        openSpriteWidth = (tileWidth * curGridColCount) + totalGapWidth;

        paletteRenderer.transform.position = curWorldPos;
        paletteRenderer.width = tileWidth;
        paletteRenderer.height = tileHeight;

        ctsOpen?.Cancel();
        ctsOpen = new CancellationTokenSource();
        
        Opening().Forget();
    }
    public void Close()
    {
        ctsOpen?.Cancel();
        ctsOpen = new CancellationTokenSource();

        transform.SetParent(selectedRenderer.transform);
        Closing().Forget();
    }
    public async UniTask Opening()
    {
        try
        {
            float totalTime = curGridColCount * OPEN_TIME_ROW_COL;
            openClock = Mathf.Max(openClock, 0);

            while (openClock < totalTime)
            {
                openClock += Time.deltaTime;
                float t = openClock / totalTime;

                float easeOutT = Curves.EaseOutT(t, 5);

                curSpriteWidth = openSpriteWidth * easeOutT;

                paletteRenderer.width = curSpriteWidth;
                paletteRenderer.UpdateSliceSpriteInputsSelf();

                for (int i = 0; i < curGridColCount; i++)
                {
                    float posX = Mathf.Lerp(closeIconRendererPosition.x, openIconRendererPositions[i].x, easeOutT);
                    icons[i].atlasRenderer.transform.localPosition = new Vector3(posX, closeIconRendererPosition.y, closeIconRendererPosition.z);
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
            float totalTime =  curGridColCount * OPEN_TIME_ROW_COL;
            openClock = Mathf.Min(openClock, totalTime);

            while (openClock > 0)
            {
                openClock -= Time.deltaTime;

                float t = openClock / totalTime;

                float easeOutT = Curves.EaseOutT(t, 5);
                curSpriteWidth = openSpriteWidth * easeOutT;

                paletteRenderer.width = curSpriteWidth;
                paletteRenderer.UpdateSliceSpriteInputsSelf();
                for (int i = 0; i < curGridColCount; i++)
                {
                    float posX = Mathf.Lerp(closeIconRendererPosition.x, openIconRendererPositions[i].x, easeOutT);
                    icons[i].atlasRenderer.transform.localPosition = new Vector3(posX, closeIconRendererPosition.y, closeIconRendererPosition.z);
                }
                await UniTask.Yield(ctsOpen.Token);
            }

            paletteRenderer.enabled = false;
            selectedRenderer = null;

        }
        catch (OperationCanceledException)
        {

        }
    }
}
