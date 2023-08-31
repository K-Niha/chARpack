using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.UI;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEngine;

public class Bond : MonoBehaviour
{

    public ushort atomID1;
    public ushort atomID2;
    [HideInInspector] public float m_bondOrder;  // 1.0 for single bonds; 1.5 for resonant bonds; 2.0 for double bonds; idea is to scale the bond diameter by this value
    [HideInInspector] public float m_bondDistance;
    [HideInInspector] public Molecule m_molecule;
    [HideInInspector] public bool isMarked = false;

    /// <summary>
    /// initialises the bond between two atoms
    /// </summary>
    /// <param name="_atom1">the first atom of the bond</param>
    /// <param name="_atom2">the second atom of the bond</param>
    /// <param name="inputMole">the molecule to which the bond belongs</param>
    public void f_Init(Atom _atom1, Atom _atom2, Molecule inputMole)
    {
        atomID1 = _atom1.m_id;
        atomID2 = _atom2.m_id;
        m_molecule = inputMole;
        m_bondOrder = 1.0f;   // standard
        m_bondDistance = 1.0f;
        gameObject.tag = "Bond";
        gameObject.layer = 7;
        m_molecule.bondList.Add(this);
        transform.position = (_atom1.transform.position + _atom2.transform.position) / 2;
        transform.LookAt(_atom1.transform);
        transform.parent = inputMole.transform;
        float distance = Vector3.Distance(_atom1.transform.position, _atom2.transform.position);
        transform.localScale = new Vector3(m_bondOrder, m_bondOrder, distance);

    }

    /// <summary>
    /// finds the other atom of a bond by entering the ID of the first atom of the bond
    /// </summary>
    /// <param name="num">ID of one atom</param>
    /// <returns></returns>
    public Atom findTheOther(Atom at)
    {
        if (at.m_id == atomID1)
            return m_molecule.atomList.ElementAtOrDefault(atomID2);
        else if (at.m_id == atomID2)
            return m_molecule.atomList.ElementAtOrDefault(atomID1);
        else
            return null;
    }

    /// <summary>
    /// this method marks a bond in a different color if it is selected
    /// </summary>
    /// <param name="mark">true or false if selected</param>
    public void markBond(bool mark, ushort mark_case = 2)
    {
        isMarked = mark;

        if (isMarked)
        {
            colorSwapSelect(mark_case);
        }
        else
        {
            colorSwapSelect(0);
        }
    }

    public void markBondUI(bool mark, bool toolTip = true)
    {
        var bond_id = (ushort)m_molecule.bondList.IndexOf(this);
        EventManager.Singleton.SelectBond(bond_id, m_molecule.m_id, !isMarked);
        markBond(mark);
    }

    /// <summary>
    /// changes color of selected and deselected bonds
    /// </summary>
    /// <param name="isOn">if this bond is selected</param>
    public void colorSwapSelect(int col)
    {
        if (col == 2)
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
            GetComponent<Outline>().OutlineColor = new Color(1.0f, 0.5f, 0.0f); //orange
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
            GetComponentInChildren<Renderer>().material = GlobalCtrl.Singleton.bondMat;
        }
    }

}
