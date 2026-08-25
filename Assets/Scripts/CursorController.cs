using System;
using UnityEngine;
using UnityEngine.InputSystem;

using static NPC;
using static AtlasUI;
public class CursorController : MonoBehaviour
{
    const int CURSOR_SPRITE_INDEX = 2;
    const int POINTER_SPRITE_INDEX = 7;

    const float VISIBLE_TIMER = 3f;
    const float MOVE_THRESHOLD = 0.01f;

    public static AtlasRenderer PrevRenderer;
    public static AtlasRenderer CursorRenderer;
    
    public bool active;
    public bool canClick;

    public static event Action OnMouseEnabled;
    public static event Action OnMouseDisabled;

    public InputData inputData;
    public LayerData layerSettings;
    public SpyData spyData;
    public CameraData camData;
    public TripData trip;
    public CursorData cursorData;

    public SceneData sceneData;

    public AtlasRenderer cursorRenderer;
    public AtlasTextRenderer cursorTag;

    [Header("Generated")]
    public NPCBrain[] hoveredNPCs;

    public int hoveredNPCCount;

    public float timer;

    public bool cursorIsMoving;
    
    private void Start()
    {
        Cursor.visible = false;
        CursorRenderer = cursorRenderer;
        cursorTag.SetText("");
        hoveredNPCs = new NPCBrain[8];
    }
    
    private void Update()
    {
        if (active)
        {
            cursorRenderer.enabled = true;
            transform.position = inputData.mouseWorldPos;

            cursorData.bounds = cursorRenderer.GetBounds();

            if (sceneData.activeSceneType == Scenes.SceneType.Trip && camData.curLocationState != Spy.LocationState.Station)
            {
                if (cursorIsMoving) HoverNPC();
                
                if ((trip.curUnlocks & UnlockType.RuleOut) != 0)
                {
                    if (inputData.mouseLeftDown)
                    {
                        NPCBrain selectedNPC = hoveredNPCs[0];

                        if (hoveredNPCCount == 1)
                        {
                            if ((trip.curUnlocks & UnlockType.Color) != 0)
                            {
                                SceneController.GetNPCColorPicker().Open(selectedNPC.atlasRenderer);
                                SceneController.GetClueColorPicker().Close();
                            }
                            else
                            {
                                if ((selectedNPC.atlasRenderer.customBit & (int)ColorBits.Diagonal) == 0)
                                {
                                    selectedNPC.atlasRenderer.customBit |= (int)ColorBits.Diagonal;
                                }
                                else
                                {
                                    selectedNPC.atlasRenderer.customBit &= ~((int)ColorBits.Diagonal);
                                }
                            }
                            selectedNPC.ToggleHover(false);
                        }
                        else if (hoveredNPCCount > 1)
                        {
                            QuickSortNPCByXPos(hoveredNPCs, 0, hoveredNPCCount - 1);
                            SceneController.GetNPCPicker().Open(hoveredNPCs, hoveredNPCCount, PickerFunctionType.Color);
                        }
                    }
                    else if (inputData.mouseRightDown)
                    {
                        NPCBrain selectedNPC = hoveredNPCs[0];
                        if (hoveredNPCCount == 1)
                        {
                            if ((selectedNPC.atlasRenderer.customBit & ((int)ColorBits.Diagonal)) == 0)
                            {
                                selectedNPC.atlasRenderer.customBit |= (int)ColorBits.Diagonal;
                                if ((trip.curUnlocks & UnlockType.MultiColor) == 0)
                                {
                                    selectedNPC.atlasRenderer.customBit &= ~((int)ColorBits.Color1);
                                }
                            }
                            else
                            {
                                selectedNPC.atlasRenderer.customBit &= ~((int)ColorBits.Diagonal);
                            }
                            selectedNPC.ToggleHover(false);
                        }
                        else if (hoveredNPCCount > 1)
                        {
                            QuickSortNPCByXPos(hoveredNPCs, 0, hoveredNPCCount - 1);
                            SceneController.GetNPCPicker().Open(hoveredNPCs, hoveredNPCCount, PickerFunctionType.RuleOut);
                        }
                    }
                }
            }
        }
        else
        {
            cursorRenderer.enabled = false;
            if (spyData.moveVelocity.x != 0)
            {
                EraseCursorTag();
            }
        }
    }
    private void LateUpdate()
    {
        Vector2 mouseDelta = Mouse.current.delta.ReadValue();
        if (mouseDelta.sqrMagnitude < MOVE_THRESHOLD && !inputData.mouseLeftHold)
        {
            cursorIsMoving = false;
            timer += Time.deltaTime;

            if (timer > VISIBLE_TIMER)
            {
                if (active)
                {
                    active = false;
                    OnMouseDisabled?.Invoke();
                }
            }
        }
        else
        {
            cursorIsMoving = true;
            if (!active)
            {
                timer = 0;
                active = true;
                OnMouseEnabled?.Invoke();
            }
        }
        if (canClick)
        {
            if (cursorRenderer.spriteIndex == CURSOR_SPRITE_INDEX)
            {
                cursorRenderer.UpdateSpriteInputsByIndex(POINTER_SPRITE_INDEX);
            }
            canClick = false;
        }
        else
        {
            if (cursorRenderer.spriteIndex == POINTER_SPRITE_INDEX)
            {
                cursorRenderer.UpdateSpriteInputsByIndex(CURSOR_SPRITE_INDEX);
            }
        }
    }
    private void HoverNPC()
    {
        hoveredNPCCount = 0;
        bool hoveringRevealedNPC = false;
        bool canClick = (trip.curUnlocks & UnlockType.RuleOut) != 0; // TODO : Find a way to not have the cursor flicker between sprites

        Carriage curCarriage = SpyBrain.CurCarriage;
        for (int i = 0; i < curCarriage.curNPCList.Count; i++)
        {
            NPCBrain npc = curCarriage.curNPCList[i];

            if (spyData.curState == Spy.SpyState.Notepad && npc.transform.position.x > curCarriage.insideBoundsCollider.bounds.center.x) return;

            if (cursorData.IsInsideBounds(npc.atlasRenderer.bounds, isClickable: false) && hoveredNPCCount < hoveredNPCs.Length)
            {
                hoveredNPCs[hoveredNPCCount] = npc;
                hoveredNPCCount++;
                npc.ToggleHover(true);
                if (npc.ticketHasBeenChecked) hoveringRevealedNPC = true;
            }
            else
            {
                npc.ToggleHover(false);
            }
        }

        if (hoveredNPCCount == 1 && hoveringRevealedNPC)
        {
            NPCBrain selectedNPC = hoveredNPCs[0];
            WriteCursorTag(selectedNPC);
        }
        else if (!hoveringRevealedNPC || hoveredNPCCount > 1)
        {
            EraseCursorTag();
        }
    }
    public void WriteCursorTag(NPCBrain npc)
    {
        cursorTag.SetText(trip.stationsDataArray[npc.profile.disembarkingStationIndex].name);
        cursorTag.transform.position = new Vector3(npc.atlasRenderer.bounds.center.x, npc.atlasRenderer.bounds.max.y + cursorTag.backgroundRenderer.bounds.size.y, cursorTag.transform.position.z);
        cursorTag.transform.SetParent(npc.transform, worldPositionStays: true);
    }
    public void EraseCursorTag()
    {
        if(!cursorTag.erasingText && cursorTag.hasText)
        {
            cursorTag.SetText("");
            cursorTag.transform.SetParent(transform, worldPositionStays: true);
            cursorTag.transform.localPosition = new Vector3(0, 0, cursorTag.transform.localPosition.z);
        }
    }
}
