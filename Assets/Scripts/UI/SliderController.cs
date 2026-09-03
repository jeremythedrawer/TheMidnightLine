using System;
using UnityEngine;
using UnityEngine.Audio;
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
            sliderLocalPos.x = Mathf.Clamp(sliderLocalPos.x, minSlideDist + Mathf.Epsilon, maxSlideDist);
            button.transform.localPosition = sliderLocalPos;

            float t = Mathf.InverseLerp(minSlideDist, maxSlideDist, sliderLocalPos.x);
            t *= t;
            if (sliderType == SliderType.Music)
            {
                options.music.volume = t;
                OnChangeMusicVolume?.Invoke();
            }
            else if (sliderType == SliderType.SoundEffects)
            {
                options.soundEffects.volume = t;
                OnChangeSoundEffectsVolume?.Invoke();
            }
        }
    }
}
