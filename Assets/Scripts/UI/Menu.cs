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


        for (int i = 0; i < textButtons.Length; i++)
        {
            TextButton textButton = textButtons[i];

            switch(textButton.buttonFunctionType)
            {
                case ButtonFunctionType.Begin:
                {
                    void Start()
                    {
                        OnClickBegin?.Invoke();
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
