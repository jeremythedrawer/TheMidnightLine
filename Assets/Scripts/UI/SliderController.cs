using System;
using UnityEngine;
using static AtlasUI;
using static ColorPicker;

public class SliderController : MonoBehaviour
{
    public static event Action OnChangeMusicVolume;
    public static event Action OnChangeSoundEffectsVolume;
    public enum SliderType
    { 
        Music,
        SoundEffects,
    }

    public SliderType sliderType;

    public Options options;
    public InputData inputData;

    public IconButton button;

    public AtlasRenderer rangeRenderer;


    [Header("Generated")]
    public Vector3 sliderLocalPos;
    public float minSlideDist;
    public float maxSlideDist;
    public float startDragDelta;
    private void Start()
    {
        Init();
    }
    private void Update()
    {
        UpdateSlider();
    }
    private void Init()
    {
        void ButtonUp(IconButton icon)
        {
            icon.atlasRenderer.customBit ^= (int)ColorBits.Invert;

            if (sliderType == SliderType.Music)
            {
                options.music.volume = Mathf.InverseLerp(minSlideDist, maxSlideDist, sliderLocalPos.x);
                OnChangeMusicVolume?.Invoke();
            }
            else if (sliderType == SliderType.SoundEffects)
            {
                options.soundEffects.volume = Mathf.InverseLerp(minSlideDist, maxSlideDist, sliderLocalPos.x);
                OnChangeSoundEffectsVolume?.Invoke();
            }
        }
        void ButtonDown(IconButton icon)
        {
            icon.atlasRenderer.customBit ^= (int)ColorBits.Invert;
            startDragDelta = button.transform.position.x - inputData.mouseWorldPos.x;
        }
        void EnterButton(IconButton icon)
        {
            icon.atlasRenderer.customBit |= (int)ColorBits.GreenChannel;
        }
        void ExitButton(IconButton icon)
        {
            icon.atlasRenderer.customBit &= ~(int)ColorBits.GreenChannel;
            icon.atlasRenderer.customBit &= ~(int)ColorBits.Invert;
        }
        button.InitButton(ButtonUp, ButtonDown, EnterButton, ExitButton);
        
        Vector4[] worldPivAndSizes = rangeRenderer.worldPivotsAndSizes;
        minSlideDist = worldPivAndSizes[1].x;
        float endPos = worldPivAndSizes[2].x;
        maxSlideDist = endPos - minSlideDist;

        sliderLocalPos = button.transform.localPosition;
    }
    private void UpdateSlider()
    {
        button.UpdateButton();
        if (button.curState == ButtonState.Clicked)
        {
            sliderLocalPos.x = button.transform.parent.InverseTransformPoint(inputData.mouseWorldPos).x + startDragDelta;
            sliderLocalPos.x = Mathf.Clamp(sliderLocalPos.x, minSlideDist, maxSlideDist);
            button.transform.localPosition = sliderLocalPos;
        }
    }
}
