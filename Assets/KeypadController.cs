using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using TMPro;

public class KeypadController : MonoBehaviour
{
    private List<string> inputServerid = new List<string>();
    [SerializeField] private TMP_InputField codeDisplay;

    public void UserNumberEntry(string selectedChar)
    {
        inputServerid.Add(selectedChar);

        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        codeDisplay.text = null;
        for (int i = 0; i < inputServerid.Count; i++)
        {
            codeDisplay.text += inputServerid[i];
        }
    }
    public void DeleteEntry()
    {
        var listposition = inputServerid.Count - 1;
        inputServerid.RemoveAt(listposition);

        UpdateDisplay();
    }
}
