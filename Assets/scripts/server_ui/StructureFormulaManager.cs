using chARpackColorPalette;
using chARpackTypes;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.VectorGraphics;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class StructureFormulaManager : MonoBehaviour
{
    private static StructureFormulaManager _singleton;

    public static StructureFormulaManager Singleton
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
                Debug.Log($"[{nameof(StructureFormulaManager)}] Instance already exists, destroying duplicate!");
                Destroy(value);
            }
        }
    }

    public List<ushort> getMolIDs()
    {
        List<ushort> mol_ids = new List<ushort>();
        foreach (var id in svg_instances.Keys)
        {
            mol_ids.Add(id);
        }
        return mol_ids;
    }

    private void Awake()
    {
        Singleton = this;
    }

    private Dictionary<ushort, Triple<GameObject, string, List<GameObject>>> svg_instances;
    private GameObject interactiblePrefab;
    private GameObject structureFormulaPrefab;
    public GameObject UICanvas;
    System.Diagnostics.Process python_process = null;
    private List<Texture2D> heatMapTextures;
    private List<string> heatMapNames = new List<string> { "HeatTexture_Cool", "HeatTexture_Inferno", "HeatTexture_Magma", "HeatTexture_Plasma", "HeatTexture_Viridis", "HeatTexture_Warm" };
    [HideInInspector]
    public GameObject secondaryStructureDialogPrefab;

    private void Start()
    {
        svg_instances = new Dictionary<ushort, Triple<GameObject, string, List<GameObject>>>();
        structureFormulaPrefab = (GameObject)Resources.Load("prefabs/StructureFormulaPrefab");
        interactiblePrefab = (GameObject)Resources.Load("prefabs/2DAtom");
        selectionBoxPrefab = (GameObject)Resources.Load("prefabs/2DSelectionBox");
        secondaryStructureDialogPrefab = (GameObject)Resources.Load("prefabs/SecondaryStructureFormulaDialog");
        UICanvas = GameObject.Find("UICanvas");

        heatMapTextures = new List<Texture2D>();
        foreach (var name in heatMapNames)
        {
            heatMapTextures.Add((Texture2D)Resources.Load($"materials/{name}"));
        }

        // Startup structure provider in python
        StartCoroutine(waitAndInitialize());
    }

    public void setColorMap(int id)
    {
        if (id >= heatMapNames.Count) return;

        foreach (var svg in svg_instances.Values)
        {
            svg.Item1.GetComponentInChildren<HeatMap2D>().UpdateTexture(heatMapTextures[id]);
        }
    }


    private IEnumerator waitAndInitialize()
    {
        yield return new WaitForSeconds(1f);
        var pythonArgs = Path.Combine(Application.dataPath, "scripts/network/PytideInterface/chARpack_run_structrure_provider.py");
        var psi = new System.Diagnostics.ProcessStartInfo("python", pythonArgs);
        psi.WindowStyle = System.Diagnostics.ProcessWindowStyle.Minimized;
        python_process = System.Diagnostics.Process.Start(psi);
    }

    public void pushSecondaryContent(ushort mol_id, int focus_id)
    {
        if (!svg_instances.ContainsKey(mol_id))
        {
            Debug.LogError($"[StructureFormulaManager] Not able to generate secondary content for Molecule {mol_id}.");
            return;
        }
        var sf = svg_instances[mol_id].Item1.GetComponentInParent<StructureFormula>();
        var new_go = Instantiate(sf.gameObject, UICanvas.transform);
        new_go.transform.localScale = 0.8f * new_go.transform.localScale;
        var new_sf = new_go.GetComponent<StructureFormula>();
        new_sf.label.text = $"{sf.label.text} Focus: {focus_id}";
        new_sf.onlyUser = focus_id;
        // Deactivate interactibles
        var interactibles = new_go.GetComponentInChildren<SVGImage>().gameObject.GetComponentsInChildren<Atom2D>();
        foreach (var inter in interactibles)
        {
            inter.GetComponent<Button>().interactable = false;
        }

        svg_instances[mol_id].Item3.Add(new_go.GetComponentInChildren<SVGImage>().gameObject);
    }

    public void updateSecondaryContent(ushort mol_id, GameObject old_go)
    {
        svg_instances[mol_id].Item3.Remove(old_go);
        var old_sf = old_go.GetComponentInParent<StructureFormula>();
        var sf = svg_instances[mol_id].Item1.GetComponentInParent<StructureFormula>();
        var new_go = Instantiate(sf.gameObject, UICanvas.transform);
        new_go.transform.localScale = old_sf.transform.localScale;
        new_go.transform.localPosition = old_sf.transform.localPosition;
        var new_sf = new_go.GetComponentInChildren<StructureFormula>();
        new_sf.label.text = old_sf.label.text;
        new_sf.onlyUser = old_sf.onlyUser;
        // Deactivate interactibles
        var interactibles = new_go.GetComponentInChildren<SVGImage>().gameObject.GetComponentsInChildren<Atom2D>();
        foreach (var inter in interactibles)
        {
            inter.GetComponent<Button>().interactable = false;
        }

        Destroy(old_sf.gameObject);
        svg_instances[mol_id].Item3.Add(new_go.GetComponentInChildren<SVGImage>().gameObject);
    }

    public void pushContent(ushort mol_id, string svg_content)
    {
        if (svg_instances.ContainsKey(mol_id))
        {
            var sceneInfo = SVGParser.ImportSVG(new StringReader(svg_content));
            var rect = svg_instances[mol_id].Item1.transform as RectTransform;
            var ui_rect = UICanvas.transform as RectTransform;
            float scaling_factor_w = (ui_rect.sizeDelta.x * 0.3f) / sceneInfo.SceneViewport.width;
            float scaling_factor_h = (ui_rect.sizeDelta.y * 0.5f) / sceneInfo.SceneViewport.height;
            var scaling_factor = scaling_factor_w < scaling_factor_h ? scaling_factor_w : scaling_factor_h;
            rect.sizeDelta = scaling_factor * new Vector2(sceneInfo.SceneViewport.width, sceneInfo.SceneViewport.height);

            var svg_component = svg_instances[mol_id].Item1.GetComponent<SVGImage>();
            // Tessellate
            var geometries = VectorUtils.TessellateScene(sceneInfo.Scene, new VectorUtils.TessellationOptions
            {
                StepDistance = 0.1f,
                SamplingStepSize = 50,
                MaxCordDeviation = 0.5f,
                MaxTanAngleDeviation = 0.1f
            });
            // Build a sprite
            var sprite = VectorUtils.BuildSprite(geometries, 100, VectorUtils.Alignment.Center, Vector2.zero, 128, true);

            // push image
            svg_component.sprite = sprite;
            var sf = svg_component.GetComponentInParent<StructureFormula>();
            sf.originalSize = new Vector2(sceneInfo.SceneViewport.width, sceneInfo.SceneViewport.height);
            sf.scaleFactor = scaling_factor;
            sf.newImageResize();

            svg_instances[mol_id] = new Triple<GameObject, string, List<GameObject>>(svg_instances[mol_id].Item1, svg_content, new List<GameObject>());

            removeInteractibles(mol_id);
            createInteractibles(mol_id);
            if (svg_instances[mol_id].Item3.Count > 0)
            {
                foreach (var secondary_sf in svg_instances[mol_id].Item3)
                {
                    updateSecondaryContent(mol_id, secondary_sf);
                }
            }
        }
        else
        {
            GameObject sf_object = Instantiate(structureFormulaPrefab, UICanvas.transform);
            sf_object.transform.localScale = Vector3.one;
            var sf = sf_object.GetComponent<StructureFormula>();

            sf.label.text = $"StructureFormula_{mol_id}";
            var rect = sf.image.transform as RectTransform;
            rect.transform.localScale = Vector2.one;

            var sceneInfo = SVGParser.ImportSVG(new StringReader(svg_content));

            var ui_rect = UICanvas.transform as RectTransform;
            float scaling_factor_w = (ui_rect.sizeDelta.x * 0.3f) / sceneInfo.SceneViewport.width;
            float scaling_factor_h = (ui_rect.sizeDelta.y * 0.5f) / sceneInfo.SceneViewport.height;
            var scaling_factor = scaling_factor_w < scaling_factor_h ? scaling_factor_w : scaling_factor_h;

            rect.anchorMin = new Vector2(1f, 0.5f);
            rect.anchorMax = new Vector2(1f, 0.5f);
            rect.pivot = new Vector2(1f, 0.5f);
            rect.anchoredPosition = new Vector2(-0.5f * sceneInfo.SceneViewport.width, 0f);
            rect.sizeDelta = scaling_factor * new Vector2(sceneInfo.SceneViewport.width, sceneInfo.SceneViewport.height);


            // Tessellate
            var geometries = VectorUtils.TessellateScene(sceneInfo.Scene, new VectorUtils.TessellationOptions
            {
                StepDistance = 0.1f,
                SamplingStepSize = 50,
                MaxCordDeviation = 0.5f,
                MaxTanAngleDeviation = 0.1f
            });

            // Build a sprite
            var sprite = VectorUtils.BuildSprite(geometries, 100, VectorUtils.Alignment.Center, Vector2.zero, 128, true);
            sf.image.sprite = sprite;
            sf.originalSize = new Vector2(sceneInfo.SceneViewport.width, sceneInfo.SceneViewport.height);
            sf.scaleFactor = scaling_factor;
            sf.newImageResize();

            svg_instances[mol_id] = new Triple<GameObject, string, List<GameObject>>(sf.image.gameObject, svg_content, new List<GameObject>());

            createInteractibles(mol_id);
        }
    }

    public void removeContent(ushort mol_id)
    {
        if (!svg_instances.ContainsKey(mol_id))
        {
            return;
        }

        Destroy(svg_instances[mol_id].Item1.transform.parent.gameObject);
        svg_instances.Remove(mol_id);
    }

    private void removeInteractibles(ushort mol_id)
    {
        var mol = GlobalCtrl.Singleton.List_curMolecules.ElementAtOrNull(mol_id, null);
        if (!mol)
        {
            Debug.LogError("[removeInteractibles] Invalid Molecule ID.");
            return;
        }

        if (!svg_instances.ContainsKey(mol_id))
        {
            Debug.LogError("[removeInteractibles] No structure formula found.");
            return;
        }

        var interactible_instances = svg_instances[mol_id].Item1.GetComponentsInChildren<Atom2D>();

        foreach (var inter in interactible_instances)
        {
            inter.atom.structure_interactible = null;
            Destroy(inter.gameObject);
        }
    }

    public void createInteractibles(ushort mol_id)
    {
        var mol = GlobalCtrl.Singleton.List_curMolecules.ElementAtOrNull(mol_id, null);
        if (!mol)
        {
            Debug.LogError("[createInteractibles] Invalid Molecule ID.");
            return;
        }

        if (!svg_instances.ContainsKey(mol_id))
        {
            Debug.LogError("[createInteractibles] No structure formula found.");
            return;
        }

        var sf_go = svg_instances[mol_id].Item1;
        var sf = sf_go.GetComponentInParent<StructureFormula>();


        foreach (var atom in mol.atomList)
        {
            if (!atom.structure_interactible)
            {
                var inter = Instantiate(interactiblePrefab);
                inter.transform.SetParent(sf_go.transform, true);
                inter.transform.localScale = Vector3.one;
                atom.structure_interactible = inter;
                inter.GetComponent<Atom2D>().atom = atom;
            }


            var rect = sf_go.transform as RectTransform;
            var atom_rect = atom.structure_interactible.transform as RectTransform;
            atom_rect.sizeDelta = 15f * sf.scaleFactor * Vector2.one;

            var offset = new Vector2(-rect.sizeDelta.x, 0.5f * rect.sizeDelta.y) + sf.scaleFactor * new Vector2(atom.structure_coords.x, -atom.structure_coords.y) + 0.5f * new Vector2(-atom_rect.sizeDelta.x, atom_rect.sizeDelta.y);
            atom_rect.localPosition = offset;
        }
    }


    //public void addHighlight(ushort mol_id, Vector2 coord, bool value)
    //{
    //    if (svg_instances.ContainsKey(mol_id))
    //    {
    //        var radius = 6f;
    //        var fill = "blue";
    //        var alpha = 0.6f;

    //        var svg_content = svg_instances[mol_id].Item2;
    //        var circle = $"<circle cx=\"{coord.x}\" cy=\"{coord.y}\" r=\"{radius}\" fill=\"{fill}\" fill-opacity=\"{alpha}\"/>";
    //        var test = $"<circle cx=\"{coord.x}\" cy=\"{coord.y}\"";

    //        if (value)
    //        {
    //            if (!svg_content.Contains(test))
    //            {
    //                svg_content = svg_content.Replace("</svg>", $"\n{circle}\n</svg>");
    //                pushContent(mol_id, svg_content);
    //                //Debug.Log(svg_content);
    //            }
    //        }
    //        else
    //        {
    //            if (svg_content.Contains(test))
    //            {
    //                svg_content = svg_content.Replace($"{circle}\n", "");
    //                pushContent(mol_id, svg_content);
    //            }
    //        }
    //    }
    //    else
    //    {
    //        Debug.LogError($"[StructureFormulaManager] Tying to add content to non existent structure formula.");
    //        return;
    //    }
    //}

    public void addFocusHighlight(ushort mol_id, Atom atom, bool[] values, Color[] cols)
    {
        if (atom.isMarked) return;
        if (svg_instances.ContainsKey(mol_id))
        {
            List<StructureFormula> sf_list = new List<StructureFormula>();
            sf_list.Add(svg_instances[mol_id].Item1.GetComponentInParent<StructureFormula>());
            if (svg_instances[mol_id].Item3.Count > 0)
            {
                foreach (var item in svg_instances[mol_id].Item3)
                {
                    sf_list.Add(item.GetComponentInParent<StructureFormula>());
                }
            }
            foreach (var sf in sf_list)
            {
                if (sf.current_highlight_choice == 0)
                {
                    //var atom2d = atom.structure_interactible.GetComponent<Atom2D>();
                    Atom2D atom2d = null;
                    foreach (var inter in sf.gameObject.GetComponentsInChildren<Atom2D>())
                    {
                        if (inter.atom == atom)
                        {
                            atom2d = inter;
                            break;
                        }
                    }
                    if (sf.onlyUser >= 0)
                    {
                        for (int i = 0; i < FocusManager.currentNumOutlines; i++)
                        {
                            cols[i] = cols[sf.onlyUser];
                        }
                    }
                    atom2d.FociColors = cols; // set full array to trigger set function
                }
                else if (sf.current_highlight_choice == 1)
                {
                    var heat = sf.gameObject.GetComponentInChildren<HeatMap2D>();
                    if (sf.onlyUser >= 0)
                    {
                        heat.SetAtomFocus(atom, values[sf.onlyUser]);
                    }
                    else
                    {
                        if (values.AnyTrue())
                        {
                            heat.SetAtomFocus(atom, true);
                        }
                        else
                        {
                            heat.SetAtomFocus(atom, false);
                        }
                    }
                }
            }
        }
        else
        {
            Debug.LogError($"[StructureFormulaManager] Tying to add content to non existent structure formula.");
            return;
        }
    }

    public void addServerFocusHighlight(ushort mol_id, Atom atom, Color[] col)
    {
        if (svg_instances.ContainsKey(mol_id))
        {
            var atom2d = atom.structure_interactible.GetComponent<Atom2D>();
            atom2d.FociColors = col; // set full array to trigger set function
        }
    }

    public void addSelectHighlight(ushort mol_id, Atom atom, Color[] selCol)
    {

        if (svg_instances.ContainsKey(mol_id))
        {
            var atom2d = atom.structure_interactible.GetComponent<Atom2D>();
            atom2d.FociColors = selCol; // set full array to trigger set function
        }
        else
        {
            Debug.LogError($"[StructureFormulaManager] Tying to add content to non existent structure formula.");
            return;
        }
    }

    Vector2 selectionStartPos = Vector2.zero;
    bool isSelecting = false;
    bool needsInitialization = false;
    int currentMol = -1;
    GameObject currentStructureFormula;
    GameObject selectionBoxPrefab;
    GameObject selectionBoxInstance;


    private void Update()
    {
        var rayCastResults = GetEventSystemRaycastResults();

        if (Input.GetMouseButtonDown(0))
        {
            currentMol = getMolIDFromRaycastResult(rayCastResults);
            if (currentMol >= 0)
            {
                currentStructureFormula = svg_instances[(ushort)currentMol].Item1;
                selectionStartPos = currentStructureFormula.transform.InverseTransformPoint(Mouse.current.position.ReadValue());
                isSelecting = true;
                needsInitialization = true;
            }
        }

        if (Input.GetMouseButtonUp(0))
        {
            isSelecting = false;
            currentMol = -1;
            if (selectionBoxInstance)
            {
                Destroy(selectionBoxInstance);
            }
        }

        if (isSelecting && Input.GetMouseButton(0))
        {
            var currentSelectionPos = currentStructureFormula.transform.InverseTransformPoint(Mouse.current.position.ReadValue());

            float width = currentSelectionPos.x - selectionStartPos.x;
            float height = currentSelectionPos.y - selectionStartPos.y;
            var dist = new Vector2(width, height);

            if (needsInitialization && dist.magnitude > 2f)
            {
                
                selectionBoxInstance = Instantiate(selectionBoxPrefab);
                selectionBoxInstance.transform.SetParent(currentStructureFormula.transform);
                selectionBoxInstance.transform.localScale = Vector3.one;
                needsInitialization = false;
            }
            
            if (selectionBoxInstance)
            {
                var scaleFactor = UICanvas.GetComponent<Canvas>().scaleFactor;

                var selBox = selectionBoxInstance.transform as RectTransform;
                selBox.sizeDelta = new Vector2(Mathf.Abs(width), Mathf.Abs(height)); // / scaleFactor;
                selBox.localPosition = (selectionStartPos + new Vector2(width / 2, height / 2)); // / scaleFactor;

                var left = selBox.localPosition.x - 0.5f * selBox.sizeDelta.x - 30;
                var right = selBox.localPosition.x + 0.5f * selBox.sizeDelta.x;
                var bot = selBox.localPosition.y - 0.5f * selBox.sizeDelta.y;
                var top = selBox.localPosition.y + 0.5f * selBox.sizeDelta.y + 30;


                foreach (var atom in GlobalCtrl.Singleton.List_curMolecules[currentMol].atomList)
                {
                    var atom_rect = atom.structure_interactible.transform as RectTransform;
                    if (atom_rect.localPosition.x >= left &&
                        atom_rect.localPosition.x <= right &&
                        atom_rect.localPosition.y >= bot &&
                        atom_rect.localPosition.y <= top)
                    {
                        if (Input.GetKey(KeyCode.LeftControl))
                        {
                            if (!atom.isMarked) atom.markAtomUI(true);
                        }
                        else
                        {
                            if (!atom.serverFocus) atom.serverFocusHighlightUI(true);
                        }
                    }
                    else
                    {
                        if (Input.GetKey(KeyCode.LeftControl))
                        {
                            if (atom.isMarked) atom.markAtomUI(false);
                        }
                        else
                        {
                            if (atom.serverFocus) atom.serverFocusHighlightUI(false);
                        }
                    }
                }

            }
        }

    }

    ///Gets all event systen raycast results of current mouse or touch position.
    private static List<RaycastResult> GetEventSystemRaycastResults()
    {
        PointerEventData eventData = new PointerEventData(EventSystem.current);
        eventData.position = Input.mousePosition;

        List<RaycastResult> raysastResults = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, raysastResults);

        return raysastResults;
    }

    private int getMolIDFromRaycastResult(List<RaycastResult> eventSystemRaysastResults)
    {
        foreach (var sf in svg_instances)
        {
            // only if its in front of the list (or blocked by interactivble)
            // TODO does not work for overlayed heatmap anymore
            if (eventSystemRaysastResults.Count < 1) return -1;
            if (eventSystemRaysastResults[0].gameObject == sf.Value.Item1)
            {
                return sf.Key;
            }
            if (eventSystemRaysastResults.Count < 2) return -1;
            if (eventSystemRaysastResults[1].gameObject == sf.Value.Item1)
            {
                return sf.Key;
            }
        }

        //foreach (var sf in svg_instances)
        //{
        //    foreach (var rr in eventSystemRaysastResults)
        //    {
        //        if (sf.Value.Item1 == rr.gameObject)
        //        {
        //            return sf.Key;
        //        }
        //    }
        //}
        return -1;
    }

    private Atom2D getInteractibleFromRaycastResult(List<RaycastResult> eventSystemRaysastResults)
    {
        var mol_id = getMolIDFromRaycastResult(eventSystemRaysastResults);
        if (mol_id >= 0)
        {
            foreach (var rr in eventSystemRaysastResults)
            {
                if (rr.gameObject.GetComponent<Atom2D>() != null)
                {
                    return rr.gameObject.GetComponent<Atom2D>();
                }
            }
        }
        return null;
    }

    private void OnDestroy()
    {
        if (python_process != null)
        {
            python_process.Kill();
            python_process.WaitForExit();
            python_process.Dispose();
        }
    }

}

