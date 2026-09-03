using Proselyte.Sigils;
using System;
using UnityEngine;
using static AtlasUI;

public class Menu : MonoBehaviour
{
    public static event Action OnClickToMap;
    public static event Action OnClickOptions;
    public static event Action OnClickBackToStartMenu;
    public static event Action OnClickContinueMenu;

    public TextButton[] textButtons;
    public AtlasTextRenderer[] texts;

    public CameraData camData;
    public Options options;

    public GameEvent onBeginTrip;
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
        for (int i = 0; i < textButtons.Length; i++)
        {
            TextButton textButton = textButtons[i];

            switch(textButton.buttonFunctionType)
            {
                case ButtonFunctionType.Begin:
                {
                    void Start()
                    {
                        if (options.thirdPointRegion.trips[0].completed)
                        {
                            OnClickToMap?.Invoke();
                        }
                        else
                        {
                            options.curRegion = options.thirdPointRegion;
                            options.curTrip = options.thirdPointRegion.trips[0];
                            onBeginTrip?.Raise();
                        }
                        textButton.MouseUpText();
                    }
                    textButton.InitButton(onMouseUp: Start);
                }
                break;

                case ButtonFunctionType.Options:
                {
                    void Options()
                    {
                        OnClickOptions?.Invoke();
                        textButton.MouseUpText();
                    }
                    textButton.InitButton(Options);
                }
                break;


                case ButtonFunctionType.Quit:
                {
                    void Quit()
                    {
                        Application.Quit();
                        textButton.MouseUpText();
                    }

                    textButton.InitButton(Quit);
                }
                break;

                case ButtonFunctionType.Back:
                {
                    void Back()
                    {
                        OnClickBackToStartMenu?.Invoke();
                        textButton.MouseUpText();
                    }
                    textButton.InitButton(Back);
                }
                break;

                case ButtonFunctionType.Continue:
                {
                    void Continue()
                    {
                        OnClickContinueMenu?.Invoke();
                        textButton.MouseUpText();
                    }
                    textButton.InitButton(Continue);
                }
                break;
            }
        }
    }
    public void ShowButtons(bool toggle)
    {
        for (int i = 0; i < textButtons.Length; i++)
        {
            textButtons[i].enabled = toggle;
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
