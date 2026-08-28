using System;
using UnityEngine;
using UnityEngine.InputSystem;

using static NPC;
using static AtlasUI;
public class CursorController : MonoBehaviour
{
    const int CURSOR_SPRITE_INDEX = 2;
    const int POINTER_SPRITE_INDEX = 6;

    const float VISIBLE_TIMER = 3f;
    const float MOVE_THRESHOLD = 0.01f;

    public static AtlasRenderer PrevRenderer;
    public static AtlasRenderer CursorRenderer;
    
    public static event Action OnMouseEnabled;
    public static event Action OnMouseDisabled;

    public InputData inputData;
    public LayerData layerSettings;
    public SpyData spyData;
    public CameraData camData;
    public TripData trip;
    public CursorData cursorData;
    public SceneData sceneData;
    public Options options;

    public AtlasRenderer cursorRenderer;
    public AtlasTextRenderer cursorTag;
    public AudioSource audioSource;

    [Header("Generated")]

    public NPCBrain[] hoveredNPCs;

    public int hoveredNPCCount;

    public float timer;

    public bool cursorIsMoving;
    public bool active;
    
    private void Start()
    {
        Cursor.visible = false;
        CursorRenderer = cursorRenderer;
        cursorTag.SetText("");
        hoveredNPCs = new NPCBrain[8];
    }

    private void OnEnable()
    {
        SliderController.OnChangeSoundEffectsVolume += UpdateVolume;
    }
    private void OnDisable()
    {
        SliderController.OnChangeSoundEffectsVolume -= UpdateVolume;
    }
    private void Update()
    {
        if (active)
        {
            if (inputData.mouseLeftUp && cursorData.isHovering)
            {
                audioSource.PlayOneShot(options.soundEffects.cursorClick);
            }
            cursorRenderer.enabled = true;
            transform.position = inputData.mouseWorldPos;

            cursorData.cursorBounds = cursorRenderer.GetBounds();
        }
    }
    private void LateUpdate()
    {
        cursorData.CheckButtonResults();

        Vector2 mouseDelta = Mouse.current.delta.ReadValue();
        if (mouseDelta.sqrMagnitude < MOVE_THRESHOLD && !inputData.mouseLeftHold && !cursorData.isHovering)
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
        if (cursorData.changeButton)
        {
            cursorRenderer.UpdateSpriteInputsByIndex(POINTER_SPRITE_INDEX);
            audioSource.PlayOneShot(options.soundEffects.cursorHover);
            cursorData.changeButton = false;
        }
        else if (!cursorData.isHovering)
        {
            if (cursorRenderer.spriteIndex == POINTER_SPRITE_INDEX)
            {
                cursorRenderer.UpdateSpriteInputsByIndex(CURSOR_SPRITE_INDEX);
            }
        }
    }

    private void UpdateVolume()
    {
        audioSource.volume = options.soundEffects.volume;
    }
}
