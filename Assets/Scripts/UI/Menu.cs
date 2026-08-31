using System;
using UnityEngine;
using static AtlasUI;

public class Menu : MonoBehaviour
{
    public static event Action OnClickBegin;
    public static event Action OnClickOptions;
    public static event Action OnClickBackToStartMenu;

    public TextButton[] textButtons;
    public CameraData camData;

    [Header("Generated")]
    public Bounds bounds;

    private void Start()
    {
        InitButtons();
        InitBounds();
    }
    private void InitBounds()
    {
        bounds = new Bounds();
        bounds.center = transform.position;
        bounds.size = camData.bounds.size;
    }
    private void InitButtons()
    {
        void MouseDownText(TextButton icon)
        {
            icon.backgroundRenderer.customBit ^= (int)ColorBits.Invert;
            icon.textRenderer.customBit ^= (int)ColorBits.Invert;
        }
        void EnterButtonText(TextButton icon)
        {
            icon.backgroundRenderer.customBit |= (int)ColorBits.GreenChannel;
        }
        void ExitButtonText(TextButton icon)
        {
            icon.backgroundRenderer.customBit &= ~(int)ColorBits.GreenChannel;
            icon.backgroundRenderer.customBit &= ~(int)ColorBits.Invert;
            icon.textRenderer.customBit |= (int)ColorBits.Invert;
        }
        void MouseUpText(TextButton icon)
        {
            icon.backgroundRenderer.customBit &= ~(int)ColorBits.Invert;
            icon.textRenderer.customBit |= (int)ColorBits.Invert;
        }

        for (int i = 0; i < textButtons.Length; i++)
        {
            TextButton textButton = textButtons[i];

            switch(textButton.buttonFunctionType)
            {
                case ButtonFunctionType.Begin:
                {
                    void Start(TextButton icon)
                    {
                        OnClickBegin?.Invoke();
                        MouseUpText(icon);
                    }
                    textButton.InitButton(Start, MouseDownText, EnterButtonText, ExitButtonText);
                }
                break;

                case ButtonFunctionType.Options:
                {
                    void Options(TextButton icon)
                    {
                        OnClickOptions?.Invoke();
                        MouseUpText(icon);
                    }
                    textButton.InitButton(Options, MouseDownText, EnterButtonText, ExitButtonText);
                }
                break;


                case ButtonFunctionType.Quit:
                {
                    void Quit(TextButton icon)
                    {
                        Application.Quit();
                        MouseUpText(icon);
                    }

                    textButton.InitButton(Quit, MouseDownText, EnterButtonText, ExitButtonText);
                }
                break;

                case ButtonFunctionType.Back:
                {
                    void Back(TextButton icon)
                    {
                        OnClickBackToStartMenu?.Invoke();
                        MouseUpText(icon);
                    }
                    textButton.InitButton(Back, MouseDownText, EnterButtonText, ExitButtonText);
                }
                break;
            }
        }
    }
    public void UpdateMenu()
    {
        for (int i = 0;i < textButtons.Length;i++)
        {
            TextButton button = textButtons[i];
            button.UpdateButton();
        }
    }
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;

        Gizmos.DrawWireCube(transform.position, camData.bounds.size);
    }
}
