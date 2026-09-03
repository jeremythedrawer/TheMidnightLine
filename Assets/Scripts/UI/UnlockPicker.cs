using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;

using static AtlasUI;
using static Passenger;
public class UnlockPicker : MonoBehaviour
{
    public const int GRID_X_COUNT = 3;
    public const int GRID_Y_COUNT = 1;

    public const int RULE_OUT_ICON_SPRITE_INDEX = 19;
    public const int COLOR_ICON_SPRITE_INDEX = 20;
    public const int MULTI_COLOR_ICON_SPRITE_INDEX = 23;

    public static event Action<IconButton> OnRuleOutAbilityUnlock;
    public static event Action<IconButton> OnColorAbilityUnlock;
    public static event Action<IconButton> OnMutliColorAbilityUnlock;

    public IconButton[] icons;

    public TripData trip;
    public InputData playerInputs;
    public CameraData camStats;
    public SpyData spyStats;

    public AtlasRenderer paletteRenderer;

    [Header("Generated")]

    public PassengerBrain selectedNPC;

    public CancellationTokenSource ctsOpen;

    public Vector2[] openIconRendererPositions;

    public Vector3 curWorldPos;
    public Vector3 closeIconRendererPosition;
    public Vector3 paletteCenterSliceWorldSize;

    public Vector2 iconRendererWorldSize;
    public Vector2 sliceWorldSize;

    public UnlockType curUnlockSelectionMask;

    public int curGridColCount;

    public float openClock;
    public float openSpriteWidth;
    public float curSpriteWidth;
    public float curSpriteHeight;
    public float tileWidth;
    public float tileHeight;

    public bool tutorialInUse;
    private void Start()
    {
        Init();
    }

    private void Update()
    {
        UpdateState();
    }
    private void Init()
    {
        SetOpenPosAndSize();

        paletteRenderer.customBit = (int)ColorBits.Meridia;

        void EnterColorIcon(IconButton icon)
        {
            icon.atlasRenderer.custom.x = 1;
        }
        void ExitColorIcon(IconButton icon)
        {
            icon.atlasRenderer.custom.x = 0;
        }
        for (int i = 0; i < icons.Length; i++)
        {
            icons[i].atlasRenderer.customBit = (int)ColorBits.Meridia;
            icons[i].atlasRenderer.enabled = false;

            int index = i;

            void ClickIcon(IconButton icon)
            {
                int validIndex = 0;
                for (int j = 0; j < 32; j++)
                {
                    if (((int)curUnlockSelectionMask & (1 << j)) == 0) continue;

                    if (validIndex != index)
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
                            tutorialInUse = true;

                            OnRuleOutAbilityUnlock?.Invoke(icon);
                        }
                        else if ((selectedUnlockType & UnlockType.Color) != 0)
                        {
                            trip.curUnlocks |= UnlockType.Color;
                            tutorialInUse = true;
                            
                            OnColorAbilityUnlock?.Invoke(icon);

                        }
                        else if ((selectedUnlockType & UnlockType.MultiColor) != 0)
                        {
                            trip.curUnlocks |= UnlockType.MultiColor;
                            tutorialInUse = true;
                            
                            OnMutliColorAbilityUnlock?.Invoke(icon);
                        }
                        break;
                    }
                }

                selectedNPC.atlasRenderer.customBit &= ~((int)ColorBits.Meridia);
                selectedNPC.atlasRenderer.custom.z = 1;
                selectedNPC.ticketHasBeenChecked = true;
            }
            //icons[i].InitButton(ClickIcon, EnterColorIcon, ExitColorIcon);
        }

    }

    private void UpdateState()
    {
        for (int i = 0; i < curGridColCount; i++)
        {
            icons[i].UpdateButton();
        }
        if (camStats.curLocationState != Spy.LocationState.Carriage)
        {
            Close();
        }
    }
    public void SetOpenPosAndSize()
    {
        openIconRendererPositions = new Vector2[icons.Length];

        AtlasRenderer firstIconRend = icons[0].atlasRenderer;
        Vector4 paletteBottomRightWPS = paletteRenderer.worldPivotsAndSizes[5];
        Vector2 firstIconRendPos = new Vector2(paletteBottomRightWPS.x + firstIconRend.worldPivotAndSize.x, paletteBottomRightWPS.y - firstIconRend.worldPivotAndSize.y);

        for (int y = 0; y < GRID_Y_COUNT; y++)
        {
            int rowIndex = y * GRID_X_COUNT;
            float yPos = firstIconRendPos.y + (y * GRID_GAP);

            for (int x = 0; x < GRID_X_COUNT; x++)
            {
                int flatIndex = x + rowIndex;

                AtlasRenderer npcIconRend = icons[flatIndex].atlasRenderer;

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

    public void Open(int unlockSelectionAmount, UnlockType unlockType, PassengerBrain npc)
    {

        tutorialInUse = false;
        paletteRenderer.enabled = true;

        curGridColCount = unlockSelectionAmount;

        curUnlockSelectionMask = unlockType;

        int iconIndex = 0;

        if ((unlockType & UnlockType.RuleOut) != 0)
        {
            AtlasRenderer iconRend = icons[iconIndex].atlasRenderer;
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
            AtlasRenderer iconRend = icons[iconIndex].atlasRenderer;
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
            AtlasRenderer iconRend = icons[iconIndex].atlasRenderer;
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

        ctsOpen?.Cancel();
        ctsOpen = new CancellationTokenSource();

        //Opening().Forget();
    }
    public void Close()
    {
        ctsOpen?.Cancel();
        ctsOpen = new CancellationTokenSource();

        transform.SetParent(selectedNPC.transform);
       // Closing().Forget();
    }
    //public async UniTask Opening()
    //{
    //    try
    //    {
    //        float totalTime = curGridColCount * OPEN_TIME_ROW_COL;
    //        openClock = Mathf.Max(openClock, 0);

    //        while (openClock < totalTime)
    //        {
    //            openClock += Time.deltaTime;
    //            float t = openClock / totalTime;

    //            float easeOutT = Curves.EaseOutT(t, 5);

    //            curSpriteWidth = openSpriteWidth * easeOutT;

    //            paletteRenderer.width = curSpriteWidth;
    //            paletteRenderer.UpdateSliceSpriteInputsSelf();

    //            for (int i = 0; i < curGridColCount; i++)
    //            {
    //                float posX = Mathf.Lerp(closeIconRendererPosition.x, openIconRendererPositions[i].x, easeOutT);
    //                icons[i].atlasRenderer.transform.localPosition = new Vector3(posX, closeIconRendererPosition.y, closeIconRendererPosition.z);
    //            }
    //            await UniTask.Yield(ctsOpen.Token);
    //        }
    //    }
    //    catch (OperationCanceledException)
    //    {
    //    }
    //}
    //public async UniTask Closing()
    //{
    //    try
    //    {
    //        float totalTime = curGridColCount * OPEN_TIME_ROW_COL;
    //        openClock = Mathf.Min(openClock, totalTime);

    //        while (openClock > 0)
    //        {
    //            openClock -= Time.deltaTime;

    //            float t = openClock / totalTime;

    //            float easeOutT = Curves.EaseOutT(t, 5);
    //            curSpriteWidth = openSpriteWidth * easeOutT;

    //            paletteRenderer.width = curSpriteWidth;
    //            paletteRenderer.UpdateSliceSpriteInputsSelf();
    //            for (int i = 0; i < curGridColCount; i++)
    //            {
    //                float posX = Mathf.Lerp(closeIconRendererPosition.x, openIconRendererPositions[i].x, easeOutT);
    //                icons[i].atlasRenderer.transform.localPosition = new Vector3(posX, closeIconRendererPosition.y, closeIconRendererPosition.z);
    //            }
    //            await UniTask.Yield(ctsOpen.Token);
    //        }

    //        paletteRenderer.enabled = false;
    //        selectedNPC = null;
    //    }
    //    catch (OperationCanceledException)
    //    {
    //    }
    //}
}
