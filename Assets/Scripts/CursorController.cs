using System;
using UnityEngine;
using UnityEngine.InputSystem;

using static NPC;
using static AtlasUI;
public class CursorController : MonoBehaviour
{
    const float VISIBLE_TIMER = 3f;
    const float MOVE_THRESHOLD = 0.01f;

    public static AtlasRenderer PrevRenderer;
    public static AtlasRenderer CursorRenderer;
    
    public static Bounds CursorBounds;
    
    public static Vector3 CurWorldPos;
    
    public static bool Active;

    public static event Action OnMouseEnabled;
    public static event Action OnMouseDisabled;

    public PlayerInputsSO playerInputs;
    public LayerSettingsSO layerSettings;
    public SpyStatsSO spyStats;
    public TripSO trip;

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
        if (Active)
        {
            cursorRenderer.enabled = true;
            CurWorldPos = playerInputs.mouseWorldPos;
            transform.position = CurWorldPos;

            CursorBounds = cursorRenderer.GetBounds();

            if (spyStats.curLocationState != Spy.LocationState.Station)
            {
                if (cursorIsMoving && SceneController.GetNPCPicker().curPickerState == PickerState.Closed) HoverNPC();
                
                if ((trip.curUnlocks & UnlockType.RuleOut) != 0)
                {
                    if (playerInputs.mouseLeftDown)
                    {
                        if (hoveredNPCCount == 1)
                        {
                            NPCBrain selectedNPC = hoveredNPCs[0];
                            if (trip.unlockedClueMarkerCount == 0)
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
                            else
                            {
                                SceneController.GetColorPicker().Open(selectedNPC.atlasRenderer, openAllColors: false);
                            }
                            selectedNPC.ToggleHover(false);
                        }
                        else if (hoveredNPCCount > 1)
                        {
                            QuickSortNPCByXPos(hoveredNPCs, 0, hoveredNPCCount - 1);
                            SceneController.GetNPCPicker().Open(hoveredNPCs, hoveredNPCCount, PickerFunctionType.Color);
                        }
                    }
                    else if (playerInputs.mouseRightDown)
                    {
                        if (hoveredNPCCount == 1)
                        {
                            NPCBrain selectedNPC = hoveredNPCs[0];
                            if ((selectedNPC.atlasRenderer.customBit & ((int)ColorBits.Diagonal)) == 0)
                            {
                                selectedNPC.atlasRenderer.customBit |= (int)ColorBits.Diagonal;
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
            if (spyStats.moveVelocity.x != 0)
            {
                EraseCursorTag();
            }
        }
    }
    private void LateUpdate()
    {
        Vector2 mouseDelta = Mouse.current.delta.ReadValue();
        if (mouseDelta.sqrMagnitude < MOVE_THRESHOLD && !playerInputs.mouseLeftHold)
        {
            cursorIsMoving = false;
            timer += Time.deltaTime;

            if (timer > VISIBLE_TIMER)
            {
                if (Active)
                {
                    Active = false;
                    OnMouseDisabled?.Invoke();
                }
            }
        }
        else
        {
            cursorIsMoving = true;
            if (!Active)
            {
                timer = 0;
                Active = true;
                OnMouseEnabled?.Invoke();
            }

        }
    }
    private void HoverNPC()
    {
        hoveredNPCCount = 0;
        bool hoveringRevealedNPC = false;
        for (int i = 0; i < SpyBrain.CurCarriage.curNPCList.Count; i++)
        {
            NPCBrain npc = SpyBrain.CurCarriage.curNPCList[i];

            if (IsInsideBounds(npc.atlasRenderer.bounds) && hoveredNPCCount < hoveredNPCs.Length)
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
        cursorTag.transform.position = new Vector3(npc.atlasRenderer.bounds.center.x, npc.atlasRenderer.bounds.max.y + cursorTag.background_renderer.bounds.size.y, cursorTag.transform.position.z);
        cursorTag.transform.SetParent(npc.transform, worldPositionStays: true);
    }
    public void EraseCursorTag()
    {
        if(!cursorTag.erasingText && cursorTag.hasText)
        {
            cursorTag.SetText("");
            cursorTag.transform.SetParent(transform, worldPositionStays: true);
            cursorTag.transform.localPosition = new Vector3(0, 0, -0.5f);
        }
    }
    public static bool IsInsideBounds(Bounds bounds)
    {
        return CursorBounds.min.x >= bounds.min.x && CursorBounds.min.x <= bounds.max.x && CursorBounds.max.y >= bounds.min.y && CursorBounds.max.y <= bounds.max.y;
    }
    public static bool EnteredBounds(AtlasRenderer renderer)
    {
        if (IsInsideBounds(renderer.GetBounds()))
        {
            if (PrevRenderer != renderer)
            {
                PrevRenderer = renderer;
                return true;
            }
            else
            {
                return false;
            }
        }
        else
        {
            return false;
        }
    }
    public static bool ExitBounds()
    {
        if (PrevRenderer != null && !IsInsideBounds(PrevRenderer.GetBounds()))
        {
            PrevRenderer = null;
            return true;
        }
        else
        {
            return false;
        }
    }
}
