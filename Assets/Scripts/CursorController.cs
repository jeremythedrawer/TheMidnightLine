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
    public ColorPicker npcColorPicker;

    [Header("Generated")]
    public float timer;
    public NPCBrain hoveredNPC;

    public static Vector3 CurWorldPos;
    public static bool Active;
    public static AtlasRenderer PrevRenderer;
    public static AtlasRenderer CursorRenderer;
    public static Bounds CursorBounds;
    public static event Action OnMouseEnabled;
    private void Start()
    {
        Cursor.visible = false;
        CursorRenderer = cursorRenderer;

        npcColorPicker.transform.SetParent(null);
    }
    private void Update()
    {
        if (Active)
        {
            cursorRenderer.enabled = true;
            CurWorldPos = playerInputs.mouseWorldPos;
            transform.position = CurWorldPos;

            if (playerInputs.mouseLeftDown)
            {
                ClickNPC();
            }
        }
        else
        {
            cursorRenderer.enabled = false;
        }
        if (hoveredNPC != null)
        {
            npcColorPicker.UpdateOpen(hoveredNPC.atlasRenderer);
        }
        else
        {
            npcColorPicker.UpdateClose();
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
                    OnMouseEnabled?.Invoke();
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
    private void ClickNPC()
    {
        if (spyStats.curLocationState != Spy.LocationState.Station)
        {
            CursorBounds = cursorRenderer.GetBounds();

            bool foundNPC = false;
            for (int i = 0; i < spyStats.curCarriage.curNPCs.Count; i++)
            {
                NPCBrain npc = spyStats.curCarriage.curNPCs[i];

                if (IsInsideBounds(npc.atlasRenderer.bounds))
                {
                    hoveredNPC = npc;
                    foundNPC = true;
                    break;
                }
            }

            if (!foundNPC)
            {
                hoveredNPC = null;
            }            
        }
    }
    public static bool IsInsideBounds(Bounds bounds)
    {
        return CursorBounds.min.x >= bounds.min.x && CursorBounds.max.x <= bounds.max.x && CursorBounds.min.y >= bounds.min.y && CursorBounds.max.y <= bounds.max.y;
    }
    public static bool EnteredBounds(AtlasRenderer renderer)
    {
        if (IsInsideBounds(renderer.bounds))
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
        if (PrevRenderer != null && !IsInsideBounds(PrevRenderer.bounds))
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
