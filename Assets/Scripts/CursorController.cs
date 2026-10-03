using System;
using UnityEngine;
using UnityEngine.InputSystem;
using static AtlasUI;
using static Passenger;
using static UnityEngine.InputSystem.InputAction;
public class CursorController : MonoBehaviour
{


    const float VISIBLE_TIMER = 3f;
    const float MOVE_THRESHOLD = 0.01f;

    public static AtlasRenderer PrevRenderer;
    public static AtlasRenderer CursorRenderer;
    
    public InputData inputData;
    public LayerData layerSettings;
    public SpyData spyData;
    public CameraData camData;
    public TripData trip;
    public CursorData cursorData;
    public AudioData audioData;
    public Options options;

    public AtlasRenderer cursorRenderer;
    public AudioSource audioSource;

    [Header("Generated")]

    public PassengerBrain[] hoveredNPCs;

    public int hoveredNPCCount;

    public float timer;

    public bool cursorIsMoving;
    public bool active;
    
    private void Start()
    {
        Init();
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
                audioSource.PlayOneShot(audioData.cursorClick);
            }
            cursorRenderer.enabled = true;
            transform.position = inputData.mouseWorldPos;

            cursorData.cursorBounds = cursorRenderer.GetBounds();
        }
    }
    private void LateUpdate()
    {
        cursorData.CheckButtonResults();

        if (inputData.mouseDelta.sqrMagnitude < MOVE_THRESHOLD && !inputData.mouseLeftHold && !cursorData.isHovering)
        {
            cursorIsMoving = false;
            timer += Time.deltaTime;

            if (timer > VISIBLE_TIMER)
            {
                if (active)
                {
                    cursorRenderer.enabled = false;
                    active = false;
                }
            }
        }
        else
        {
            cursorIsMoving = true;
            if (!active)
            {
                cursorRenderer.enabled = true;
                timer = 0;
                active = true;
            }
        }
        if (cursorData.changeButton)
        {
            cursorRenderer.UpdateSpriteInputsByIndex((int)cursorData.curCursorSpriteType);
            audioSource.PlayOneShot(audioData.cursorHover);

            cursorData.changeButton = false;
        }
        else if (!cursorData.isHovering)
        {
            if (cursorRenderer.spriteIndex != (int)CursorData.CursorSpriteType.Arrow)
            {
                cursorRenderer.UpdateSpriteInputsByIndex((int)CursorData.CursorSpriteType.Arrow);
            }
        }
    }
    
    private void PlayAudioOnClick(CallbackContext ctx)
    {
        audioSource.PlayOneShot(audioData.cursorClick);
    }
    private void Init()
    {
        Cursor.visible = false;
        CursorRenderer = cursorRenderer;
        hoveredNPCs = new PassengerBrain[8];

        cursorData.curCursorSpriteType = CursorData.CursorSpriteType.Point;
        cursorData.curPassengerSelectionMode = CursorData.PassengerSelectionMode.Unmasking;
    }
    private void UpdateVolume()
    {
        audioSource.volume = audioData.soundEffectsVolume;
    }
}
