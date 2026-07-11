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

    public AtlasRenderer[] iconRenderers;

    public TripSO trip;
    public PlayerInputsSO playerInputs;
    public ColorsSO colorData;

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

    public PickerState curPickerState;
    public PickerState enteredPickerState;
    public PickerFunctionType functionType;

    public int curGridColCount;

    public float openClock;
    public float openSpriteWidth;
    public float curSpriteWidth;
    public float curSpriteHeight;
    public float tileWidth;
    public float tileHeight;

    public bool canClose;

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
        UpdateState();
    }
    private void Init()
    {
        SceneController.SetNPCPicker(this);
        curPickerState = PickerState.Closed;
        SetOpenPosAndSize();
    }
    private void SetState(PickerState newState)
    {
        if (curPickerState == newState) return;
        ExitState();
        curPickerState = newState;
        enteredPickerState = newState;
        EnterState();
    }
    private void UpdateState()
    {
        switch (curPickerState)
        {
            case PickerState.Opening:
            case PickerState.Opened:
            {
                for (int i = 0; i < curGridColCount; i++)
                {
                    AtlasRenderer npcIconRend = iconRenderers[i];

                    if (CursorController.IsInsideBounds(npcIconRend.bounds))
                    {
                        npcIconRend.custom.w = 1;

                        if ((playerInputs.mouseLeftUp || playerInputs.mouseRightUp) && canClose)
                        {
                            switch(functionType)
                            {
                                case PickerFunctionType.TicketCheck:
                                {
                                    SpyBrain.ChooseNPCTicketToCheck(possibleNPCs[i]);
                                }
                                break;

                                case PickerFunctionType.Color:
                                {
                                    NPCBrain selectedNPC = possibleNPCs[i];
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
                                        SceneController.GetColorPicker().Open(selectedNPC.atlasRenderer, openAllColors: false);
                                    }
                                }
                                break;

                                case PickerFunctionType.RuleOut:
                                {
                                    NPCBrain selectedNPC = possibleNPCs[i];
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
                        return;
                    }
                    else
                    {
                        npcIconRend.custom.w = 0;
                    }
                }

                if (canClose && (playerInputs.mouseLeftUp || playerInputs.mouseRightUp) && !CursorController.IsInsideBounds(paletteRenderer.bounds))
                {
                    Close();
                }
                if (playerInputs.mouseLeftUp || playerInputs.ticketCheckKeyUp || playerInputs.mouseRightUp)
                {
                    canClose = true;
                }
            }
            break;
        }
    }
    private void EnterState()
    {
        switch (curPickerState)
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
    public void SetOpenPosAndSize()
    {
        openIconRendererPositions = new Vector2[iconRenderers.Length];

        AtlasRenderer firstColorRend = iconRenderers[0];
        Vector4 paletteBottomRightWPS = paletteRenderer.worldPivotsAndSizes[5];
        Vector2 firstColorRendPos = new Vector2(paletteBottomRightWPS.x + firstColorRend.worldPivotAndSize.x, paletteBottomRightWPS.y - firstColorRend.worldPivotAndSize.y);

        for (int y = 0; y < GRID_Y_COUNT; y++)
        {
            int rowIndex = y * GRID_X_COUNT;
            float yPos = firstColorRendPos.y + (y * GRID_GAP);

            for (int x = 0; x < GRID_X_COUNT; x++)
            {
                int flatIndex = x + rowIndex;

                AtlasRenderer npcIconRend = iconRenderers[flatIndex];

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
    public void TurnOff()
    {
        paletteRenderer.enabled = false;

        for (int i = 0; i < curGridColCount; i++)
        {
            iconRenderers[i].enabled = false;
        }

        selectedRenderer = null;
        transform.SetParent(null);
    }
    public void TurnOn(NPCBrain[] npcs, int npcCount)
    {
        paletteRenderer.enabled = true;

        possibleNPCs = npcs;
        curGridColCount = npcCount;

        for (int i = 0; i < possibleNPCs.Length; i++)
        {
            AtlasRenderer iconRend = iconRenderers[i];
            NPCBrain npcBrain = possibleNPCs[i];

            if (npcBrain == null) break;

            if (i < npcCount)
            {
                iconRend.enabled = true;
                int npcIconIndex = (npcBrain.npc.mugShotIndex * 2);
                if (npcBrain.ticketHasBeenChecked) npcIconIndex += 1;

                iconRend.UpdateSpriteInputsByIndex(npcIconIndex);

                if (npcBrain.role == Role.Accomplice)
                {
                    iconRend.customBit |= (int)ColorBits.Meridia;

                    if ((npcBrain.atlasRenderer.customBit & (int)ColorBits.Diagonal) != 0)
                    {
                        iconRend.customBit |= (int)ColorBits.Diagonal;
                    }
                    else
                    {
                        iconRend.customBit &= ~((int)ColorBits.Diagonal);
                    }
                }
                else
                {
                    int npcColorIndex = Convert.ToInt32($"{npcBrain.atlasRenderer.customBit}", 10) - 1;
                    if (npcColorIndex >= 0) 
                    {
                        if (npcColorIndex < trip.selectedClueMarkerColors.Length)
                        {
                            Color iconColor = trip.selectedClueMarkerColors[npcColorIndex].linear;
                            iconRend.custom.x = iconColor.r; 
                            iconRend.custom.y = iconColor.g; 
                            iconRend.custom.z = iconColor.b;
                            iconRend.customBit = 0;
                        }
                        else
                        {
                            iconRend.custom.x = 0;
                            iconRend.custom.y = 0;
                            iconRend.custom.z = 0;
                            iconRend.customBit = (int)ColorBits.Diagonal;
                        }
                    }
                    else
                    {
                        iconRend.custom.x = 0;
                        iconRend.custom.y = 0;
                        iconRend.custom.z = 0;
                        iconRend.customBit = 0;
                    }

                }
                iconRend.custom.w = 0;
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
    }
    public void Open(NPCBrain[] npcs, int npcCount, PickerFunctionType funcType)
    {
        if (curPickerState == PickerState.Closed)
        {
            ctsOpen?.Cancel();
            ctsOpen = new CancellationTokenSource();

            functionType = funcType;

            TurnOn(npcs, npcCount);
            Opening().Forget();
        }
    }
    public void Adjust(NPCBrain[] npcs, int npcCount)
    {
        ctsOpen?.Cancel();
        ctsOpen = new CancellationTokenSource();

        TurnOn(npcs, npcCount);
        Adjusting().Forget();
    }
    public void Close()
    {
        if (curPickerState == PickerState.Opened || curPickerState == PickerState.Opening)
        {
            ctsOpen?.Cancel();
            ctsOpen = new CancellationTokenSource();

            transform.SetParent(selectedRenderer.transform);
            Closing().Forget();
        }
    }
    public async UniTask Adjusting()
    {
        try
        {
            SetState(PickerState.Adjusting);

            float totalTime = curGridColCount * OPEN_TIME_ROW_COL;

            if (openClock > totalTime)
            {
                while (openClock > totalTime)
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
                        iconRenderers[i].transform.localPosition = new Vector3(posX, closeIconRendererPosition.y, closeIconRendererPosition.z);
                    }
                    await UniTask.Yield(ctsOpen.Token);
                }
            }
            else
            {
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
                        iconRenderers[i].transform.localPosition = new Vector3(posX, closeIconRendererPosition.y, closeIconRendererPosition.z);
                    }
                    await UniTask.Yield(ctsOpen.Token);
                }
            }

            SetState(PickerState.Opened);
        }
        catch
        {

        }
    }
    public async UniTask Opening()
    {
        try
        {
            SetState(PickerState.Opening);

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
                    iconRenderers[i].transform.localPosition = new Vector3(posX, closeIconRendererPosition.y, closeIconRendererPosition.z);
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
                    iconRenderers[i].transform.localPosition = new Vector3(posX, closeIconRendererPosition.y, closeIconRendererPosition.z);
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
