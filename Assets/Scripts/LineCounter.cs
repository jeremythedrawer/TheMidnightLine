using System.IO;
using UnityEngine;

public class LineCounter : MonoBehaviour
{
    [ContextMenu("Count Lines")]
    public void CountLines()
    {
        string[] files = Directory.GetFiles("Assets", "*.cs", SearchOption.AllDirectories);

        int totalLines = 0;

        foreach (string file in files)
        {
            totalLines += File.ReadAllLines(file).Length;
        }

        Debug.Log("Total lines: " + totalLines);
    }
}