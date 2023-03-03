using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.UI;
using StructClass;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEditor;
using UnityEngine;

[Serializable]
public class Atom : MonoBehaviour, IMixedRealityPointerHandler
{
    // prefabs initialized in GlobalCtrl
    [HideInInspector] public static GameObject myAtomToolTipPrefab;
    [HideInInspector] public static GameObject deleteMeButtonPrefab;
    [HideInInspector] public static GameObject closeMeButtonPrefab;
    [HideInInspector] public static GameObject modifyMeButtonPrefab;

    private Stopwatch stopwatch;
    private GameObject toolTipInstance = null;
    private float toolTipDistanceWeight = 2.5f;

    public void OnPointerDown(MixedRealityPointerEventData eventData)
    {
        // give it a glow halo
        (GetComponent("Halo") as Behaviour).enabled = true;
        stopwatch = Stopwatch.StartNew();
        isGrabbed = true;
        tmp_mass = m_data.m_mass;
        m_data.m_mass = -1.0f;
    }
    public void OnPointerClicked(MixedRealityPointerEventData eventData) 
    {
        // Intentionally empty
    }
    public void OnPointerDragged(MixedRealityPointerEventData eventData)
    {
        // position relative to molecule position
        EventManager.Singleton.MoveAtom(m_molecule.m_id, m_id, transform.localPosition);
    }

    // This function is triggered when a grabbed object is dropped
    public void OnPointerUp(MixedRealityPointerEventData eventData) 
    {
        isGrabbed = false;
        m_data.m_mass = tmp_mass;
        // remove glow
        (GetComponent("Halo") as Behaviour).enabled = false;

        stopwatch?.Stop();
        //UnityEngine.Debug.Log($"[Atom] Interaction stopwatch: {stopwatch.ElapsedMilliseconds} [ms]");
        if (stopwatch?.ElapsedMilliseconds < 200)
        {
            if (m_molecule.isMarked)
            {
                m_molecule.markMolecule(false);
            }
            else
            {
                markAtomUI(!isMarked);
            }
 
        }

        // check for potential merge
        if (GlobalCtrl.Singleton.collision)
        {
            Atom d1 = GlobalCtrl.Singleton.collider1;
            Atom d2 = GlobalCtrl.Singleton.collider2;

            Atom a1 = d1.dummyFindMain();
            Atom a2 = d2.dummyFindMain();

            if (!a1.alreadyConnected(a2))
            {
                EventManager.Singleton.MergeMolecule(GlobalCtrl.Singleton.collider1.m_molecule.m_id, GlobalCtrl.Singleton.collider1.m_id, GlobalCtrl.Singleton.collider2.m_molecule.m_id, GlobalCtrl.Singleton.collider2.m_id);
                GlobalCtrl.Singleton.MergeMolecule(GlobalCtrl.Singleton.collider1, GlobalCtrl.Singleton.collider2);
            }

        }
        //Debug.Log($"[Atom] OnPointerUp: {eventData}");
    }

    //[HideInInspector] public ushort m_id;
    public ushort m_id;
    [HideInInspector] public Molecule m_molecule;
    [HideInInspector] public ElementData m_data; // { get; private set; }
    // we have to clarify the role of m_data: Is this just basic (and constant) data?
    // 0: none; 1: sp1; 2: sp2;  3: sp3;  4: hypervalent trig. bipy; 5: unused;  6: hypervalent octahedral
    [HideInInspector] public Material m_mat;

    [HideInInspector] public Rigidbody m_rigid;
    [HideInInspector] public bool isGrabbed = false;
    [HideInInspector] public List<Vector3> m_posForDummies;

    [HideInInspector] public bool isMarked = false;

    [HideInInspector] public GameObject m_ActiveHand = null;

    private float tmp_mass = -1.0f;


    /// <summary>
    /// initialises the atom with all it's attributes
    /// </summary>
    /// <param name="inputData"></param>
    /// <param name="inputMole"></param>
    /// <param name="pos"></param>
    /// <param name="idInScene"></param>
    public void f_Init(ElementData inputData, Molecule inputMole, Vector3 pos, ushort atom_id)
    {
        m_id = atom_id;
        m_molecule = inputMole;
        m_molecule.atomList.Add(this);
        m_data = inputData;


        gameObject.name = m_data.m_name;
        gameObject.tag = "Atom";
        //gameObject.layer = 6;
        //GetComponent<SphereCollider>().isTrigger = true;
        GetComponent<BoxCollider>().isTrigger = true;

        //I don't want to create the materials for all elements from the beginning,
        //so I only create a material for an element at the first time when I create this element,
        //and then add this material to the dictionary
        //So next time when I need to create this element,
        //I will use the dictionary to get a copy of an existent material.
        if (!GlobalCtrl.Singleton.Dic_AtomMat.ContainsKey(m_data.m_id))
        {
            Material tempMat = Instantiate(GlobalCtrl.Singleton.atomMatPrefab);
            tempMat.color = m_data.m_color;
            GlobalCtrl.Singleton.Dic_AtomMat.Add(m_data.m_id, tempMat);
        }
        GetComponent<MeshRenderer>().material = GlobalCtrl.Singleton.Dic_AtomMat[m_data.m_id];
        m_mat = GetComponent<MeshRenderer>().material;

        transform.parent = inputMole.transform;
        transform.localPosition = pos;    
        transform.localScale = Vector3.one * m_data.m_radius * (GlobalCtrl.Singleton.scale/GlobalCtrl.Singleton.u2pm) * GlobalCtrl.Singleton.atomScale;
        // at this point we have the size of the atom, so we can adjust the size of the halo
        //


        //Debug.Log(string.Format("Added latest {0}:  rad={1}  scale={2}  hyb={3}  nBonds={4}", m_data.m_abbre, m_data.m_radius, GlobalCtrl.Singleton.atomScale, m_data.m_hybridization, m_data.m_bondNum));

        //Initial positions for dummies
        m_posForDummies = new List<Vector3>();
        Vector3 offset = new Vector3(0, 100, 0);
        // TODO: make this dependent on m_nBond and m_hybridization:

        //Debug.Log("Hybrid: " + m_data.m_hybridization.ToString());

        switch (m_data.m_hybridization)
        {
            case (0):
                break;
            case (1): // linear, max 2 bonds
                m_posForDummies.Add(transform.localPosition + Quaternion.Euler(0, 0, 0) * offset * (GlobalCtrl.Singleton.scale / GlobalCtrl.Singleton.u2pm));
                if (m_data.m_bondNum > 1) m_posForDummies.Add(transform.localPosition + Quaternion.Euler(0, 0, 120) * offset * (GlobalCtrl.Singleton.scale / GlobalCtrl.Singleton.u2pm));
                break;
            case (2): // trigonal, max 3 bonds
                m_posForDummies.Add(transform.localPosition + Quaternion.Euler(0, 0, 0) * offset * (GlobalCtrl.Singleton.scale / GlobalCtrl.Singleton.u2pm));
                if (m_data.m_bondNum > 1) m_posForDummies.Add(transform.localPosition + Quaternion.Euler(0, 0, 120) * offset * (GlobalCtrl.Singleton.scale / GlobalCtrl.Singleton.u2pm));
                if (m_data.m_bondNum > 2) m_posForDummies.Add(transform.localPosition + Quaternion.Euler(0, 0, 240) * offset * (GlobalCtrl.Singleton.scale / GlobalCtrl.Singleton.u2pm));
                break;
            case (3): // tetrahedral
                m_posForDummies.Add(transform.localPosition + Quaternion.Euler(0, 0, 0) * offset * (GlobalCtrl.Singleton.scale / GlobalCtrl.Singleton.u2pm));
                if (m_data.m_bondNum > 1) m_posForDummies.Add(transform.localPosition + Quaternion.Euler(70.53f, 60, 180) * offset * (GlobalCtrl.Singleton.scale / GlobalCtrl.Singleton.u2pm));
                if (m_data.m_bondNum > 2) m_posForDummies.Add(transform.localPosition + Quaternion.Euler(-70.53f, 0, 180) * offset * (GlobalCtrl.Singleton.scale / GlobalCtrl.Singleton.u2pm));
                if (m_data.m_bondNum > 3) m_posForDummies.Add(transform.localPosition + Quaternion.Euler(-70.53f, 120, 180) * offset * (GlobalCtrl.Singleton.scale / GlobalCtrl.Singleton.u2pm));
                break;
            case (4): // trigonal bipyramidal
                m_posForDummies.Add(transform.localPosition + Quaternion.Euler(0, 0, 0) * offset * (GlobalCtrl.Singleton.scale / GlobalCtrl.Singleton.u2pm));
                if (m_data.m_bondNum > 1) m_posForDummies.Add(transform.localPosition + Quaternion.Euler(0, 0, 180) * offset * (GlobalCtrl.Singleton.scale / GlobalCtrl.Singleton.u2pm));
                if (m_data.m_bondNum > 2) m_posForDummies.Add(transform.localPosition + Quaternion.Euler(0, 0, 90) * offset * (GlobalCtrl.Singleton.scale / GlobalCtrl.Singleton.u2pm));
                if (m_data.m_bondNum > 3) m_posForDummies.Add(transform.localPosition + Quaternion.Euler(120, 0, 180) * offset * (GlobalCtrl.Singleton.scale / GlobalCtrl.Singleton.u2pm));
                if (m_data.m_bondNum > 4) m_posForDummies.Add(transform.localPosition + Quaternion.Euler(240, 0, 180) * offset * (GlobalCtrl.Singleton.scale / GlobalCtrl.Singleton.u2pm));
                break;
            case (6): // octahedral  (with 4 bonds: quadratic planar)
                m_posForDummies.Add(transform.localPosition + Quaternion.Euler(0, 0, 0) * offset * (GlobalCtrl.Singleton.scale / GlobalCtrl.Singleton.u2pm));
                if (m_data.m_bondNum > 1) m_posForDummies.Add(transform.localPosition + Quaternion.Euler(0, 0, 180) * offset * (GlobalCtrl.Singleton.scale / GlobalCtrl.Singleton.u2pm));
                if (m_data.m_bondNum > 2) m_posForDummies.Add(transform.localPosition + Quaternion.Euler(0, 0, 90) * offset * (GlobalCtrl.Singleton.scale / GlobalCtrl.Singleton.u2pm));
                if (m_data.m_bondNum > 3) m_posForDummies.Add(transform.localPosition + Quaternion.Euler(180, 0, 90) * offset * (GlobalCtrl.Singleton.scale / GlobalCtrl.Singleton.u2pm));
                if (m_data.m_bondNum > 4) m_posForDummies.Add(transform.localPosition + Quaternion.Euler(90, 0, 90) * offset * (GlobalCtrl.Singleton.scale / GlobalCtrl.Singleton.u2pm));
                if (m_data.m_bondNum > 5) m_posForDummies.Add(transform.localPosition + Quaternion.Euler(270, 0, 90) * offset * (GlobalCtrl.Singleton.scale / GlobalCtrl.Singleton.u2pm));
                break;
            default:  // fall-back ... we have to see how to do error handling here
                m_posForDummies.Add(transform.localPosition + Quaternion.Euler(0, 0, 0) * offset * (GlobalCtrl.Singleton.scale / GlobalCtrl.Singleton.u2pm));
                UnityEngine.Debug.Log("[Atom] InitDummies: Landed in Fallback!");
                break;
        }

    }


    /// <summary>
    /// modify the atom using the info on ElementData
    /// </summary>
    /// <param name="newData"></param>
    public void f_Modify(ElementData newData)
    {
        int numConnected = connectedAtoms().Count;
        m_data = newData;
        uint dummyLimit = m_data.m_bondNum;
        gameObject.name = m_data.m_name;
        if (!GlobalCtrl.Singleton.Dic_AtomMat.ContainsKey(m_data.m_id))
        {
            Material tempMat = Instantiate(GlobalCtrl.Singleton.atomMatPrefab);
            tempMat.color = m_data.m_color;
            GlobalCtrl.Singleton.Dic_AtomMat.Add(m_data.m_id, tempMat);
        }
        GetComponent<MeshRenderer>().material = GlobalCtrl.Singleton.Dic_AtomMat[m_data.m_id];
        m_mat = GetComponent<MeshRenderer>().material;

        transform.localScale = Vector3.one * m_data.m_radius * (GlobalCtrl.Singleton.scale / GlobalCtrl.Singleton.u2pm) * GlobalCtrl.Singleton.atomScale;


        foreach(Atom a in connectedDummys())
        {
            if(numConnected > dummyLimit)
            {
                numConnected--;
                a.m_molecule.atomList.Remove(a);
                Destroy(a.gameObject);
                Bond b = a.connectedBonds()[0];
                b.m_molecule.bondList.Remove(b);
                Destroy(b.gameObject);
            }

        }

        while (dummyLimit > numConnected)
        {
            print("before Dummy Limit, numConnected:   " + dummyLimit + "   " + numConnected);
            addDummy(numConnected);
            numConnected++;
        }

        // Debug.Log(string.Format("Modified latest {0}:  rad={1}   scale={2} ", m_data.m_abbre, m_data.m_radius, GlobalCtrl.Singleton.atomScale));
    }


    public void addDummy(int numConnected)
    {
        List<Atom> conAtoms = connectedAtoms();

        Vector3 position = new Vector3();
        Vector3 firstVec = new Vector3();
        Vector3 secondVec = new Vector3();
        Vector3 normalVec = new Vector3();
        switch (numConnected)
        {
            case (0):
                position = transform.localPosition + new Vector3(0,0,0.05f);
                GlobalCtrl.Singleton.CreateDummy(m_molecule.getFreshAtomID(), m_molecule, this, position);
                break;
            case (1):
                firstVec = transform.localPosition - conAtoms[0].transform.localPosition;
                position = transform.localPosition + firstVec;
                GlobalCtrl.Singleton.CreateDummy(m_molecule.getFreshAtomID(), m_molecule, this, position);
                break;
            case (2):
                firstVec = transform.localPosition - conAtoms[0].transform.localPosition;
                secondVec = transform.localPosition - conAtoms[1].transform.localPosition;
                position = transform.localPosition + ((firstVec + secondVec) / 2.0f);
                if (position == transform.localPosition)
                    position = Vector3.Cross(firstVec, secondVec);
                GlobalCtrl.Singleton.CreateDummy(m_molecule.getFreshAtomID(), m_molecule, this, position);
                break;
            case (3):
                firstVec = conAtoms[1].transform.localPosition - conAtoms[0].transform.localPosition;
                secondVec = conAtoms[2].transform.localPosition - conAtoms[0].transform.localPosition;
                normalVec = new Vector3(firstVec.y * secondVec.z - firstVec.z * secondVec.y, firstVec.z * secondVec.x - firstVec.x * secondVec.z, firstVec.x * secondVec.y - firstVec.y * secondVec.x);
                position = transform.localPosition + normalVec;

                float sideCheck1 = normalVec.x * transform.localPosition.x + normalVec.y * transform.localPosition.y + normalVec.z * transform.localPosition.z;
                float sideCheck2 = position.x * transform.localPosition.x + position.y * transform.localPosition.y + position.z * transform.localPosition.z;

                if ((sideCheck1 >= 0 && sideCheck2 >= 0) || (sideCheck1 <= 0 && sideCheck2 <= 0))
                    position = transform.localPosition - normalVec;

                GlobalCtrl.Singleton.CreateDummy(m_molecule.getFreshAtomID(), m_molecule, this, position);
                break;
            default:
                break;
        } 
    }

    /// <summary>
    /// changes color of selected and deselected atoms
    /// </summary>
    /// <param name="isOn">if this atom is selected</param>
    public void colorSwapSelect(int col)
    {
        if (col == 1)
        {
            // merging
            GetComponent<Renderer>().material = GlobalCtrl.Singleton.selectedMat;
        }
        else if (col == 2)
        {
            // single component
            //GetComponent<Renderer>().material = GlobalCtrl.Singleton.markedMat;
            GetComponent<Outline>().enabled = true;
            GetComponent<Outline>().OutlineColor = Color.yellow;
        }
        else if (col == 3)
        {
            // as part of single bond
            GetComponent<Outline>().enabled = true;
            GetComponent<Outline>().OutlineColor = new Color(1.0f,0.5f,0.0f); //orange
        }
        else if (col == 4)
        {
            // as part of angle bond
            GetComponent<Outline>().enabled = true;
            GetComponent<Outline>().OutlineColor = Color.red;
        }
        else if (col == 5)
        {
            // as part of angle bond
            GetComponent<Outline>().enabled = true;
            GetComponent<Outline>().OutlineColor = Color.green;
        }
        else
        {
            // reset or nothing
            GetComponent<Outline>().enabled = false;
            GetComponent<Renderer>().material = GlobalCtrl.Singleton.Dic_AtomMat[m_data.m_id];
        }

    }

    private void OnTriggerEnter(Collider collider)
    {
        // Debug.Log($"[Atom] Collision Detected: {collider.name}");
        if (collider.name.StartsWith("Dummy") && name.StartsWith("Dummy") && GlobalCtrl.Singleton.collision == false)
        {

            GlobalCtrl.Singleton.collision = true;
            GlobalCtrl.Singleton.collider1 = collider.GetComponent<Atom>();
            GlobalCtrl.Singleton.collider2 = GetComponent<Atom>();
            GlobalCtrl.Singleton.collider1.colorSwapSelect(1);
            GlobalCtrl.Singleton.collider2.colorSwapSelect(1);
        }
    }

    private void OnTriggerExit(Collider collider)
    {
        if (collider.name.StartsWith("Dummy") && name.StartsWith("Dummy"))
        {
            if (GlobalCtrl.Singleton.collider1 != null)
            {
                GlobalCtrl.Singleton.collider1.colorSwapSelect(0);
                GlobalCtrl.Singleton.collider1 = null;
            }
            if (GlobalCtrl.Singleton.collider2 != null)
            {
                GlobalCtrl.Singleton.collider2.colorSwapSelect(0);
                GlobalCtrl.Singleton.collider2 = null;
            }
            GlobalCtrl.Singleton.collision = false;
        }
    }

    /// <summary>
    /// this method calculates a list of all connected atoms for a given atom
    /// </summary>
    /// <returns>list of connected atoms</returns>
    public List<Atom> connectedAtoms()
    {
        List<Atom> conAtomList = new List<Atom>();
        foreach(Bond b in m_molecule.bondList)
        {
            if (b.atomID1 == m_id || b.atomID2 == m_id)
            {
                Atom otherAtom = b.findTheOther(this);
                if (!conAtomList.Contains(otherAtom))
                    conAtomList.Add(otherAtom);
            }
        }
        return conAtomList;
    }

    public List<Atom> connectedDummys()
    {
        List<Atom> allConnected = connectedAtoms();
        List<Atom> conDummys = new List<Atom>();
        foreach(Atom at in allConnected)
        {
            if (at.m_data.m_abbre == "Dummy")
                conDummys.Add(at);
        }

        return conDummys;
    }

    /// <summary>
    /// this method calculates a list of all connected bonds for a given atom
    /// </summary>
    /// <returns>list of connected bonds</returns>
    public List<Bond> connectedBonds()
    {
        List<Bond> conBondList = new List<Bond>();
        foreach (Bond b in m_molecule.bondList)
        {
            if (b.atomID1 == m_id || b.atomID2 == m_id)
            {
                conBondList.Add(b);
            }
        }
        return conBondList;
    }

    /// <summary>
    /// this method returns a bond between two atoms
    /// </summary>
    /// <param name="a1">first atom of the bond</param>
    /// <param name="a2">second atom of the bond</param>
    /// <returns>the bond between the two atoms</returns>
    public Bond getBond(Atom a2)
    {
        foreach (Bond b in m_molecule.bondList)
        {
            if (b.atomID1 == m_id && b.atomID2 == a2.m_id)
                return b;
            else if (b.atomID2 == m_id && b.atomID1 == a2.m_id)
                return b;
        }
        return null;
    }


    /// <summary>
    /// this method returns the main atom for a given dummy atom
    /// </summary>
    /// <param name="dummy">the dummy atom</param>
    /// <returns>the main atom of the dummy</returns>
    public Atom dummyFindMain()
    {
        if (m_data.m_name == "Dummy")
        {
            Bond b = m_molecule.bondList.Find(p => p.atomID1 == m_id || p.atomID2 == m_id);
            //Atom atom1 = GlobalCtrl.Singleton.List_curAtoms.Find((x) => x.GetComponent<Atom>() == b.findTheOther(dummy));
            Atom a;
            if (m_id == b.atomID1)
            {
                a = m_molecule.atomList.ElementAtOrDefault(b.atomID2);
            }
            else
            {
                a = m_molecule.atomList.ElementAtOrDefault(b.atomID1);
            }
            if (a == default)
            {
                throw new Exception("[Atom:dummyFindMain] Could not find Atom on the other side of the bond.");
            }
            return a;
        }
        else
        {
            return null;
        }
    }

    /// <summary>
    /// this method tests if two atoms are already connected
    /// </summary>
    /// <param name="a1">the first atom</param>
    /// <param name="a2">the second atom</param>
    /// <returns>true or false depending on if the atoms are connected</returns>
    public bool alreadyConnected(Atom a2)
    {
        foreach(Bond b in m_molecule.bondList)
        {
            if (b.findTheOther(this) == a2)
                return true;
        }

        if (this == a2)
            return true;

        return false;
    }

    private void markConnectedBonds(bool mark)
    {
        foreach (var bond in connectedBonds())
        {
            bond.markBond(mark);
        }
    }

    /// <summary>
    /// this method marks the atom in a different color if selected
    /// </summary>
    /// <param name="mark">true or false if the atom should be marked</param>
    public void markAtom(bool mark, ushort mark_case = 2, bool toolTip = false)
    {

        isMarked = mark;

        if (isMarked)
        {
            colorSwapSelect(mark_case);
            if (!toolTipInstance && toolTip)
            {
                createToolTip();
            }
        }
        else
        {
            if (toolTipInstance)
            {
                Destroy(toolTipInstance);
                toolTipInstance = null;
            }
            colorSwapSelect(0);
            markConnectedBonds(false);
        }
        // destroy tooltip of marked without flag
        if (!toolTip && toolTipInstance)
        {
            Destroy(toolTipInstance);
            toolTipInstance = null;
        }
    }

    private void markConnections(bool toolTip = false)
    {
        // check for connected atom
        var markedList = new List<Atom>();
        foreach (var atom in m_molecule.atomList)
        {
            if (atom.isMarked)
            {
                markedList.Add(atom);
            }
        }
        if (markedList.Count == 1)
        {
            Destroy(m_molecule.toolTipInstance);
            markedList[0].markAtom(true, 2, true);
        }
        else if (markedList.Count == 2)
        {
            foreach (var bond in m_molecule.bondTerms)
            {
                if (bond.Contains(markedList[0].m_id) && bond.Contains(markedList[1].m_id))
                {
                    if (toolTip)
                    {
                        m_molecule.createBondToolTip(bond);
                    }
                    else
                    {
                        m_molecule.markBondTerm(bond, true);
                    }
                }
            }
        }
        else if (markedList.Count == 3)
        {
            var atom1 = markedList[0];
            var atom2 = markedList[1];
            var atom3 = markedList[2];
            foreach (var angle in m_molecule.angleTerms)
            {
                if (angle.Contains(atom1.m_id) && angle.Contains(atom2.m_id) && angle.Contains(atom3.m_id))
                {
                    if (toolTip)
                    {
                        m_molecule.createAngleToolTip(angle);
                    }
                    else
                    {
                        m_molecule.markAngleTerm(angle, true);
                    }
                }
            }

        }
        else if (markedList.Count == 4)
        {
            var atom1 = markedList[0];
            var atom2 = markedList[1];
            var atom3 = markedList[2];
            var atom4 = markedList[3];
            foreach (var torsion in m_molecule.torsionTerms)
            {
                if (torsion.Contains(atom1.m_id) && torsion.Contains(atom2.m_id) && torsion.Contains(atom3.m_id) && torsion.Contains(atom4.m_id))
                {
                    if (toolTip)
                    {
                        m_molecule.createTorsionToolTip(torsion);
                    }
                    else
                    {
                        m_molecule.markTorsionTerm(torsion, true);
                    }
                }
            }
        }
        else
        {

        }
    }

    public void markAtomUI(bool mark, bool toolTip = true)
    {
        EventManager.Singleton.SelectAtom(m_molecule.m_id, m_id, !isMarked);
        advancedMarkAtom(mark, toolTip);
    }

    public void advancedMarkAtom(bool mark, bool toolTip = false)
    {
        markAtom(mark, 2, toolTip);
        markConnections(toolTip);
    }

    private void createToolTip()
    {
        // create tool tip
        toolTipInstance = Instantiate(myAtomToolTipPrefab);
        // calc position for tool tip
        // first: get position in the bounding box and decide if the tool tip spawns left, right, top or bottom of the box
        Vector3 mol_center = m_molecule.getCenter();
        // project to camera coordnates
        Vector2 mol_center_in_cam = new Vector2(Vector3.Dot(mol_center, Camera.main.transform.right), Vector3.Dot(mol_center, Camera.main.transform.up));
        Vector2 atom_pos_in_cam = new Vector2(Vector3.Dot(transform.position, Camera.main.transform.right), Vector3.Dot(transform.position, Camera.main.transform.up));
        // calc diff
        Vector2 diff_mol_atom = atom_pos_in_cam - mol_center_in_cam;
        // enhance diff for final tool tip pos
        Vector3 ttpos = transform.position + toolTipDistanceWeight * diff_mol_atom[0] * Camera.main.transform.right + toolTipDistanceWeight * diff_mol_atom[1] * Camera.main.transform.up;
        toolTipInstance.transform.position = ttpos;
        // add atom as connector
        toolTipInstance.GetComponent<myToolTipConnector>().Target = gameObject;
        string toolTipText = $"Name: {m_data.m_name}\nHybrid.: {m_data.m_hybridization}\nMass: {m_data.m_mass}\nRadius: {m_data.m_radius}\nNumBonds: {m_data.m_bondNum}";
        toolTipInstance.GetComponent<DynamicToolTip>().ToolTipText = toolTipText;
        if (m_data.m_abbre != "Dummy")
        {
            
            if (m_data.m_abbre == "H")
            {
                var modifyButtonInstance = Instantiate(modifyMeButtonPrefab);
                modifyButtonInstance.GetComponent<ButtonConfigHelper>().OnClick.AddListener(delegate { toolTipHelperChangeAtom("Dummy"); });
                modifyButtonInstance.GetComponent<ButtonConfigHelper>().MainLabelText = "To Dummy";
                toolTipInstance.GetComponent<DynamicToolTip>().addContent(modifyButtonInstance);
            }

            var delButtonInstance = Instantiate(deleteMeButtonPrefab);
            delButtonInstance.GetComponent<ButtonConfigHelper>().OnClick.AddListener(delegate { GlobalCtrl.Singleton.deleteAtomUI(this); });
            toolTipInstance.GetComponent<DynamicToolTip>().addContent(delButtonInstance);
        }
        else
        {
            var modifyButtonInstance = Instantiate(modifyMeButtonPrefab);
            modifyButtonInstance.GetComponent<ButtonConfigHelper>().OnClick.AddListener(delegate { toolTipHelperChangeAtom("H"); });
            modifyButtonInstance.GetComponent<ButtonConfigHelper>().MainLabelText = "To Hydrogen";
            toolTipInstance.GetComponent<DynamicToolTip>().addContent(modifyButtonInstance);
        }
        var closeButtonInstance = Instantiate(closeMeButtonPrefab);
        closeButtonInstance.GetComponent<ButtonConfigHelper>().OnClick.AddListener(delegate { markAtomUI(false); });
        toolTipInstance.GetComponent<DynamicToolTip>().addContent(closeButtonInstance);
    }


    private void toolTipHelperChangeAtom(string chemAbbre)
    {
        GlobalCtrl.Singleton.changeAtomUI(m_molecule.m_id, m_id, chemAbbre);
        markAtomUI(false);
    }


    public void OnDestroy()
    {
        if (toolTipInstance)
        {
            Destroy(toolTipInstance);
            toolTipInstance = null;
        }
    }

}