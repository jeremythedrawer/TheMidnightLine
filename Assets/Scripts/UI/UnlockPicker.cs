using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;

using static AtlasUI;
using static NPC;
public class UnlockPicker : MonoBehaviour
{
    public const int GRID_X_COUNT = 3;
    public const int GRID_Y_COUNT = 1;

    public const int RULE_OUT_ICON_SPRITE_INDEX = 19;
    public const int COLOR_ICON_SPRITE_INDEX = 20;
    public const int MULTI_COLOR_ICON_SPRITE_INDEX = 23;

    public static event Action<UnlockType> OnNewAbilityUnlocked;
    public static event Action OnNewColorUnlocked;


    public AtlasRenderer[] iconRenderers;

    public TripSO trip;
    public PlayerInputsSO playerInputs;

    public AtlasRenderer paletteRenderer;

    [Header("Generated")]

    public NPCBrain selectedNPC;

    public CancellationTokenSource ctsOpen;

    public Vector2[] openIconRendererPositions;

    public Vector3 curWorldPos;
    public Vector3 closeIconRendererPosition;
    public Vector3 paletteCenterSliceWorldSize;

    public Vector2 iconRendererWorldSize;
    public Vector2 sliceWorldSize;

    public PickerState curPickerState;
    public PickerState enteredPickerState;
    public UnlockType curUnlockSelectioMask;

    public int curGridColCount;

    public float openClock;
    public float openSpriteWidth;
    public float curSpriteWidth;
    public float curSpriteHeight;
    public float tileWidth;
    public float tileHeight;

    private void Start()
    {
        SceneController.SetUnlockPicker(this);
        curPickerState = PickerState.Closed;
        SetOpenPosAndSize();

        trip.curUnlocks = UnlockType.None;
        paletteRenderer.customBit = (int)ColorBits.Meridia;

        for (int i = 0; i < iconRenderers.Length; i++)
        {
            iconRenderers[i].customBit = (int)ColorBits.Meridia;
            iconRenderers[i].enabled = false;
        }
    }

    private void Update()
    {
        UpdateState();
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
                    AtlasRenderer iconRend = iconRenderers[i];

                    if (CursorController.IsInsideBounds(iconRend.bounds))
                    {
                        iconRend.custom.w = 1;

                        if (playerInputs.mouseLeftDown)
                        {
                            int validIndex = 0;
                            for (int j = 0; j < 32; j++)
                            {
                                if (((int)curUnlockSelectioMask & (1 << j)) == 0) continue;
                                
                                if (validIndex != i)
                                {
                                    validIndex++;
                                    continue;
                                }
                                else
                                {
                                    UnlockType selectedUnlockType = (UnlockType)(1 << j);
                                    if ((selectedUnlockType & UnlockType.RuleOut) != 0)
                                    {
                                        trip.curUnlocks |= UnlockType.RuleOut;
                                        OnNewAbilityUnlocked?.Invoke(UnlockType.RuleOut);
                                    }
                                    else if ((selectedUnlockType & UnlockType.Color) != 0)
                                    {
                                        if (trip.unlockedColorMarkerCount == 0)
                                        {
                                            trip.curUnlocks |= UnlockType.Color;
                                            OnNewAbilityUnlocked.Invoke(UnlockType.Color);
                                        }
                                        trip.unlockedColorMarkerCount++;
                                        OnNewColorUnlocked?.Invoke();
                                    }
                                    else if ((selectedUnlockType & UnlockType.MultiColor) != 0)
                                    {
                                        trip.curUnlocks |= UnlockType.MultiColor;
                                        OnNewAbilityUnlocked?.Invoke(UnlockType.MultiColor);
                                        OnNewColorUnlocked?.Invoke();
                                        trip.unlockedColorMarkerCount++;
                                    }

                                    break;
                                }
                            }
                            
                            selectedNPC.atlasRenderer.customBit &= ~((int)ColorBits.Meridia);
                            selectedNPC.atlasRenderer.custom.z = 1;
                            selectedNPC.ticketHasBeenChecked = true;
                        }
                        return;
                    }
                    else
                    {
                        iconRend.custom.w = 0;
                    }
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

        AtlasRenderer firstIconRend = iconRenderers[0];
        Vector4 paletteBottomRightWPS = paletteRenderer.worldPivotsAndSizes[5];
        Vector2 firstIconRendPos = new Vector2(paletteBottomRightWPS.x + firstIconRend.worldPivotAndSize.x, paletteBottomRightWPS.y - firstIconRend.worldPivotAndSize.y);

        for (int y = 0; y < GRID_Y_COUNT; y++)
        {
            int rowIndex = y * GRID_X_COUNT;
            float yPos = firstIconRendPos.y + (y * GRID_GAP);

            for (int x = 0; x < GRID_X_COUNT; x++)
            {
                int flatIndex = x + rowIndex;

                AtlasRenderer npcIconRend = iconRenderers[flatIndex];

                float xPos = firstIconRendPos.x - (x * GRID_GAP);
                openIconRendererPositions[flatIndex] = new Vector3(xPos, yPos, -1);

                npcIconRend.transform.localPosition = openIconRendererPositions[flatIndex];
                npcIconRend.enabled = false;
            }
        }

        closeIconRendererPosition = new Vector3(firstIconRendPos.x, firstIconRendPos.y, -0.1f);
        iconRendererWorldSize = firstIconRend.sprite.worldSize;

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

        selectedNPC = null;
        transform.SetParent(null);
    }
    public void TurnOn(int unlockSelectionAmount, UnlockType unlockType,  NPCBrain npc)
    {
        paletteRenderer.enabled = true;

        curGridColCount = unlockSelectionAmount;

        curUnlockSelectioMask = unlockType;

        int iconIndex = 0;

        if ((unlockType & UnlockType.RuleOut) != 0)
        {
            AtlasRenderer iconRend = iconRenderers[iconIndex];
            iconRend.enabled = true;
            iconRend.UpdateSpriteInputsByIndex(RULE_OUT_ICON_SPRITE_INDEX);
            iconRend.custom.x = 0;
            iconRend.custom.y = 0;
            iconRend.custom.z = 0;
            iconRend.custom.w = 1;
            iconIndex++;
        }
        
        if ((unlockType & UnlockType.Color) != 0)
        {
            AtlasRenderer iconRend = iconRenderers[iconIndex];
            iconRend.enabled = true;
            iconRend.custom.x = 0;
            iconRend.custom.y = 0;
            iconRend.custom.z = 0;
            iconRend.custom.w = 1;
            iconRend.UpdateSpriteInputsByIndex(COLOR_ICON_SPRITE_INDEX);
            iconIndex++;
        }

        if ((unlockType & UnlockType.MultiColor) != 0)
        {
            AtlasRenderer iconRend = iconRenderers[iconIndex];
            iconRend.enabled = true;
            iconRend.custom.x = 0;
            iconRend.custom.y = 0;
            iconRend.custom.z = 0;
            iconRend.custom.w = 1;
            iconRend.UpdateSpriteInputsByIndex(MULTI_COLOR_ICON_SPRITE_INDEX);
            iconIndex++;
        }

        selectedNPC = npc;

        Bounds selectedRendBounds = selectedNPC.atlasRenderer.GetBounds();

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
    public void Open(int unlockSelectionAmount, UnlockType unlockType, NPCBrain npc)
    {
        if (curPickerState == PickerState.Closed)
        {
            ctsOpen?.Cancel();
            ctsOpen = new CancellationTokenSource();


            TurnOn(unlockSelectionAmount, unlockType, npc);
            Opening().Forget();
        }
    }
    public void Close()
    {
        if (curPickerState == PickerState.Opened || curPickerState == PickerState.Opening)
        {
            ctsOpen?.Cancel();
            ctsOpen = new CancellationTokenSource();

            transform.SetParent(selectedNPC.transform);
            Closing().Forget();
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

            float totalTime = curGridColCount * OPEN_TIME_ROW_COL;
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
