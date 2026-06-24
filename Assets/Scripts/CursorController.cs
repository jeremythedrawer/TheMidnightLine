using System;
using UnityEngine;
using UnityEngine.InputSystem;

using static NPC;

public class CursorController : MonoBehaviour
{
    const float VISIBLE_TIMER = 3f;
    const float MOVE_THRESHOLD = 0.01f;

    public PlayerInputsSO playerInputs;
    public LayerSettingsSO layerSettings;
    public SpyStatsSO spyStats;
    public AtlasRenderer cursorRenderer;
    public AtlasTextRenderer cursorTag;

    [Header("Generated")]
    public NPCBrain hoveredNPC;
    public ColorPicker colorPicker;

    public float timer;

    public static AtlasRenderer PrevRenderer;
    public static AtlasRenderer CursorRenderer;
    
    public static Bounds CursorBounds;
    
    public static Vector3 CurWorldPos;
    
    public static bool Active;

    public static event Action OnMouseEnabled;
    public static event Action OnMouseDisabled;

    private void Start()
    {
        Cursor.visible = false;
        CursorRenderer = cursorRenderer;
        colorPicker = SceneController.GetColorPicker();
        cursorTag.SetText("");
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
                HoverNPC();
                
                if (playerInputs.mouseLeftDown)
                {
                    if (hoveredNPC != null)
                    {
                        colorPicker.Open(hoveredNPC.atlasRenderer, openAllColors: false);
                        hoveredNPC.ToggleHover(cursorTag, false);
                    }
                }
            }
        }
        else
        {
            cursorRenderer.enabled = false;
        }
    }
    private void LateUpdate()
    {
        Vector2 mouseDelta = Mouse.current.delta.ReadValue();
        if (mouseDelta.sqrMagnitude < MOVE_THRESHOLD && !playerInputs.mouseLeftHold)
        {
            timer += Time.deltaTime;

            if (timer > VISIBLE_TIMER)
            {
                if (Active)
                {
                    hoveredNPC = null;
                    Active = false;
                    OnMouseDisabled?.Invoke();
                }
            }
        }
        else
        {
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
        bool foundNPC = false;
        for (int i = 0; i < SpyBrain.CurCarriage.curNPCs.Count; i++)
        {
            NPCBrain npc = SpyBrain.CurCarriage.curNPCs[i];

            if (IsInsideBounds(npc.atlasRenderer.bounds))
            {
                if (hoveredNPC != npc)
                {
                    hoveredNPC?.ToggleHover(cursorTag, false);
                    hoveredNPC = npc;
                    hoveredNPC.ToggleHover(cursorTag, true);
                }
                foundNPC = true;
                break;
            }
        }

        if (!foundNPC)
        {
            if (hoveredNPC != null)
            {
                hoveredNPC?.ToggleHover(cursorTag, false);
                hoveredNPC = null;
            }
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
