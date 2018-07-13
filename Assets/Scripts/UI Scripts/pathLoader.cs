using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;


public class pathLoader : MonoBehaviour {
    public string defaultDir = @"C:\Users\Administrator\Documents\";
    public string defaultFileName = "savedMap";
    // Open panel
    public void openPanel()
    {
        InputField cur = transform.gameObject.GetComponent<InputField>();
        string path = EditorUtility.OpenFilePanel("Open Path", "", "txt");
        if (path.Length != 0)
        {
            cur.text = path;
        }
    }
    // Close panel
    public void savePanel()
    {
        InputField cur = transform.gameObject.GetComponent<InputField>();

        string path = EditorUtility.SaveFilePanel("Save Path", defaultDir, defaultFileName, "txt" );
        if (path.Length != 0)
        {
            cur.text = path;
        }
    }
}
