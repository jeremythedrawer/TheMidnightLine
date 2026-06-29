using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;

using static AtlasUI;
public class NPCPicker : MonoBehaviour
{
    public const int GRID_X_COUNT = 8;
    public const int GRID_Y_COUNT = 1;

    public AtlasRenderer[] npcIconRenderers;

    public TripSO trip;
    public PlayerInputsSO playerInputs;

    public AtlasRenderer paletteRenderer;

    [Header("Generated")]

    public NPCBrain[] possibleNPCs;

    public AtlasRenderer selectedRenderer;
    public AtlasRenderer prevHoveredNPCIconRenderer;

    public CancellationTokenSource ctsOpen;

    public Vector2[] openNPCIconRendererPositions;

    public Vector3 curWorldPos;
    public Vector3 closeColorRendererPosition;
    public Vector3 paletteCenterSliceWorldSize;

    public Vector2 npcIconRendererWorldSize;
    public Vector2 sliceWorldSize;

    public PickerState curState;
    public PickerState enteredState;

    public int curGridColCount;

    public float openClock;
    public float openSpriteWidth;
    public float curSpriteWidth;
    public float curSpriteHeight;
    public float tileWidth;
    public float tileHeight;

    public bool canClose;

    private void Awake()
    {
        if (Application.isPlaying)
        {
            SceneController.SetNPCPicker(this);
        }
    }
    private void Start()
    {
        curState = PickerState.Closed;
        SetOpenPosAndSize();
    }

    private void Update()
    {
        UpdateState();
    }

    private void SetState(PickerState newState)
    {
        if (curState == newState) return;
        ExitState();
        curState = newState;
        enteredState = newState;
        EnterState();
    }
    private void UpdateState()
    {
        switch (curState)
        {
            case PickerState.Opening:
            case PickerState.Opened:
            {
                for (int i = 0; i < curGridColCount; i++)
                {
                    AtlasRenderer npcIconRend = npcIconRenderers[i];

                    if (CursorController.IsInsideBounds(npcIconRend.bounds))
                    {
                        if (playerInputs.mouseLeftHold)
                        {
                            npcIconRend.custom.w = 0;
                        }
                        else
                        {
                            npcIconRend.custom.w = 1;
                        }

                        if (playerInputs.mouseLeftUp)
                        {
                            SpyBrain.ChooseNPCTicketToCheck(possibleNPCs[i]);
                        }

                        prevHoveredNPCIconRenderer = npcIconRend;
                        return;
                    }
                    else if (npcIconRend == prevHoveredNPCIconRenderer)
                    {
                        npcIconRend.custom.w = 1;
                        prevHoveredNPCIconRenderer = null;
                        return;
                    }
                }

                if (canClose && playerInputs.mouseLeftDown && !CursorController.IsInsideBounds(paletteRenderer.bounds))
                {
                    Close();
                    SpyBrain.CheckingTicket = false;
                    SpyBrain.PickingNPCToTicketCheck = false;
                }
                canClose = true;
            }
            break;
        }
    }
    private void EnterState()
    {
        switch (curState)
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
        openNPCIconRendererPositions = new Vector2[npcIconRenderers.Length];

        AtlasRenderer firstColorRend = npcIconRenderers[0];
        Vector4 paletteBottomRightWPS = paletteRenderer.worldPivotsAndSizes[5];
        Vector2 firstColorRendPos = new Vector2(paletteBottomRightWPS.x + firstColorRend.worldPivotAndSize.x, paletteBottomRightWPS.y - firstColorRend.worldPivotAndSize.y);

        for (int y = 0; y < GRID_Y_COUNT; y++)
        {
            int rowIndex = y * GRID_X_COUNT;
            float yPos = firstColorRendPos.y + (y * GRID_GAP);

            for (int x = 0; x < GRID_X_COUNT; x++)
            {
                int flatIndex = x + rowIndex;

                AtlasRenderer npcIconRend = npcIconRenderers[flatIndex];

                float xPos = firstColorRendPos.x - (x * GRID_GAP);
                openNPCIconRendererPositions[flatIndex] = new Vector3(xPos, yPos, -1);

                npcIconRend.transform.localPosition = openNPCIconRendererPositions[flatIndex];
                npcIconRend.enabled = false;
            }
        }

        closeColorRendererPosition = new Vector3(firstColorRendPos.x, firstColorRendPos.y, -0.1f);
        npcIconRendererWorldSize = firstColorRend.sprite.worldSize;

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
            npcIconRenderers[i].enabled = false;
        }

        selectedRenderer = null;
        transform.SetParent(null);
    }
    public void TurnOn(NPCBrain[] npcs, int npcCount)
    {
        paletteRenderer.enabled = true;

        possibleNPCs = npcs;
        curGridColCount = npcCount;

        for (int i = 0; i < npcs.Length; i++)
        {
            AtlasRenderer iconRend = npcIconRenderers[i];
            NPCBrain npcBrain = npcs[i];

            if (npcBrain == null) break;

            if (i < npcCount)
            {
                iconRend.enabled = true;
                iconRend.UpdateSpriteInputsByIndex(npcBrain.npc.mugShotIndex);
                int npcColorIndex = Convert.ToInt32($"{npcBrain.atlasRenderer.custom.x}", 10);

                if (npcColorIndex < trip.selectedClueMarkerColors.Length)
                {
                    Color iconColor = trip.selectedClueMarkerColors[npcColorIndex];
                    iconRend.custom.x = iconColor.r; 
                    iconRend.custom.y = iconColor.g; 
                    iconRend.custom.z = iconColor.b; 
                }
                else
                {
                    iconRend.custom.x = -1;
                    iconRend.custom.y = 0;
                    iconRend.custom.z = 0;
                }
            }
            else
            {
                iconRend.enabled = false;
            }
        }

        selectedRenderer = npcs[npcCount - 1].atlasRenderer;

        Bounds selectedRendBounds = selectedRenderer.GetBounds();

        curWorldPos.x = selectedRendBounds.min.x;
        curWorldPos.y = selectedRendBounds.max.y;


        int curXGapCount = curGridColCount - 1;

        float totalGapWidth = curXGapCount * GRID_GAP;

        tileWidth = npcIconRendererWorldSize.x / paletteCenterSliceWorldSize.x;
        tileHeight = npcIconRendererWorldSize.y / paletteCenterSliceWorldSize.y;

        openSpriteWidth = (tileWidth * curGridColCount) + totalGapWidth;

        paletteRenderer.transform.position = curWorldPos;
        paletteRenderer.width = tileWidth;
        paletteRenderer.height = tileHeight;
    }

    public void Open(NPCBrain[] npcs, int npcCount)
    {
        if (curState == PickerState.Closed)
        {
            ctsOpen?.Cancel();
            ctsOpen = new CancellationTokenSource();

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
        if (curState == PickerState.Opened || curState == PickerState.Opening)
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

                    float easeOutT = EaseOutT(t, 5);
                    curSpriteWidth = openSpriteWidth * easeOutT;

                    paletteRenderer.width = curSpriteWidth;
                    paletteRenderer.UpdateSliceSpriteInputsSelf();
                    for (int i = 0; i < curGridColCount; i++)
                    {
                        float posX = Mathf.Lerp(closeColorRendererPosition.x, openNPCIconRendererPositions[i].x, easeOutT);
                        npcIconRenderers[i].transform.localPosition = new Vector3(posX, closeColorRendererPosition.y, closeColorRendererPosition.z);
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

                    float easeOutT = EaseOutT(t, 5);

                    curSpriteWidth = openSpriteWidth * easeOutT;

                    paletteRenderer.width = curSpriteWidth;
                    paletteRenderer.UpdateSliceSpriteInputsSelf();

                    for (int i = 0; i < curGridColCount; i++)
                    {
                        float posX = Mathf.Lerp(closeColorRendererPosition.x, openNPCIconRendererPositions[i].x, easeOutT);
                        npcIconRenderers[i].transform.localPosition = new Vector3(posX, closeColorRendererPosition.y, closeColorRendererPosition.z);
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

                float easeOutT = EaseOutT(t, 5);

                curSpriteWidth = openSpriteWidth * easeOutT;

                paletteRenderer.width = curSpriteWidth;
                paletteRenderer.UpdateSliceSpriteInputsSelf();

                for (int i = 0; i < curGridColCount; i++)
                {
                    float posX = Mathf.Lerp(closeColorRendererPosition.x, openNPCIconRendererPositions[i].x, easeOutT);
                    npcIconRenderers[i].transform.localPosition = new Vector3(posX, closeColorRendererPosition.y, closeColorRendererPosition.z);
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

                float easeOutT = EaseOutT(t, 5);
                curSpriteWidth = openSpriteWidth * easeOutT;

                paletteRenderer.width = curSpriteWidth;
                paletteRenderer.UpdateSliceSpriteInputsSelf();
                for (int i = 0; i < curGridColCount; i++)
                {
                    float posX = Mathf.Lerp(closeColorRendererPosition.x, openNPCIconRendererPositions[i].x, easeOutT);
                    npcIconRenderers[i].transform.localPosition = new Vector3(posX, closeColorRendererPosition.y, closeColorRendererPosition.z);
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
