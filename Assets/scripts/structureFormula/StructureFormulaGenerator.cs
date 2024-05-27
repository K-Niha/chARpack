using System;
using System.IO;
using UnityEngine;
using Python.Runtime;
using System.Collections.Generic;


public class StructureFormulaGenerator : MonoBehaviour
{

    private static StructureFormulaGenerator _singleton;

    public static StructureFormulaGenerator Singleton
    {
        get => _singleton;
        private set
        {
            if (_singleton == null)
            {
                _singleton = value;
            }
            else if (_singleton != value)
            {
                Debug.Log($"[{nameof(StructureFormulaGenerator)}] Instance already exists, destroying duplicate!");
                Destroy(value);
            }

        }
    }

    private void Awake()
    {
        Singleton = this;
    }

    private string pythonHome;
    private string pythonPath;
    private string pythonScriptPath;

    void Start()
    {
        //// Set the path to the embedded Python environment
        pythonHome = Path.Combine(Application.dataPath, "PythonEnv");
        pythonPath = Path.Combine(pythonHome, "Lib");

        //Environment.SetEnvironmentVariable("PYTHONHOME", pythonHome);
        //Environment.SetEnvironmentVariable("PYTHONPATH", pythonPath + ";" + Path.Combine(pythonPath, "site-packages"));

        // Initialize the Python runtime
        Runtime.PythonDLL = Application.dataPath + "/PythonEnv/python312.dll";

        // Initialize the Python engine with the embedded Python environment
        PythonEngine.PythonHome = pythonHome;
        PythonEngine.PythonPath = pythonPath + ";" + Path.Combine(pythonPath, "site-packages");
        PythonEngine.Initialize();
    }

    public void requestStructureFormula(Molecule mol)
    {

        // Prepare lists
        float[] posList = new float[mol.atomList.Count * 3];
        for (int i = 0; i < mol.atomList.Count; i++)
        {
            var atom = mol.atomList[i];
            var pos = atom.transform.localPosition * GlobalCtrl.u2aa / GlobalCtrl.scale;
            posList[3 * i + 0] = pos.x;
            posList[3 * i + 1] = pos.y;
            posList[3 * i + 2] = pos.z;
        }

        string[] symbolList = new string[mol.atomList.Count];
        for (int i = 0; i < mol.atomList.Count; i++)
        {
            var atom = mol.atomList[i];
            if (atom.m_data.m_abbre.ToLower() == "dummy")
            {
                symbolList[i] = "H";
            }
            else
            {
                symbolList[i] = atom.m_data.m_abbre;
            }
        }

        // define outputs
        string svgContent = "";
        var coordsArray = new List<Vector2>();

        // Acquire the GIL before using any Python APIs
        using (Py.GIL())
        {
            // Convert the C# float array to a Python list
            var pyPosList = new PyList();
            foreach (var f in posList)
            {
                pyPosList.Append(new PyFloat(f));
            }

            var pySymbolList = new PyList();
            foreach (var s in symbolList)
            {
                pySymbolList.Append(new PyString(s));
            }

            // Import and run the Python script
            dynamic sys = Py.Import("sys");
            sys.path.append(Application.dataPath + "/scripts/structureFormula/");

            // Import the built-in module
            dynamic builtins = Py.Import("builtins");

            // Import your Python script
            dynamic script = Py.Import("StructureFormulaPythonBackend");

            // Print the attributes of the imported module
            Debug.Log("Attributes of the imported module:");
            foreach (string key in builtins.dir(script))
            {
                Debug.Log(key);
            }

            // Call the function from the Python script
            dynamic myClass = script.MyClass("hallo");
            string greeting = myClass.greet();
            Debug.Log(greeting);

            //// Extract values from the returned tuple
            //svgContent = result.Item1.ToString();
            //dynamic coordsList = result.Item2;

            //// Convert the Python list of coordinates to a C# array
            //for (int i = 0; i < coordsList.Length(); i++)
            //{
            //    var coord = coordsList[i];
            //    coordsArray.Add(new Vector2(coord[0].As<float>(), coord[1].As<float>()));
            //}
        }

        //// push content
        //for (int i = 0; i < coordsArray.Count; i++)
        //{
        //    mol.atomList[i].structure_coords = coordsArray[i];
        //}

        //if (StructureFormulaManager.Singleton)
        //{
        //    StructureFormulaManager.Singleton.pushContent(mol.m_id, svgContent);
        //}
        //else
        //{
        //    Debug.LogError("[structureReceiveComplete] Could not find StructureFormulaManager");
        //    return;
        //}

        ////write svg to file
        //var file_path = Path.Combine(Application.streamingAssetsPath, $"{svgContent.Length}.svg");
        //if (File.Exists(file_path))
        //{
        //    Debug.Log(file_path + " already exists.");
        //    return;
        //}
        //var sr = File.CreateText(file_path);
        //sr.Write(svgContent);
        //sr.Close();

    }

    private void OnDestroy()
    {
        // Shutdown the Python engine
        PythonEngine.Shutdown();
    }
}