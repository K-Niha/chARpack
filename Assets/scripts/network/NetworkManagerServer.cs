using RiptideNetworking;
using RiptideNetworking.Utils;
using StructClass;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class NetworkManagerServer : MonoBehaviour
{
    private static NetworkManagerServer _singleton;

    public static NetworkManagerServer Singleton
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
                Debug.Log($"[{nameof(NetworkManagerServer)}] Instance already exists, destroying duplicate!");
                Destroy(value);
            }

        }
    }

    public Server Server { get; private set; }
 

    private bool ServerStarted = false;

    private static byte[] cmlTotalBytes;
    private static List<cmlData> cmlWorld;
    private static ushort chunkSize = 255;
    private static bool receiveComplete = false;

    private GameObject _userWorld;
    public GameObject UserWorld { get => _userWorld; private set => _userWorld = value; }

    private void Awake()
    {
        Singleton = this;
        // create user world
        UserWorld = new GameObject("UserWorld");
    }

    private void Start()
    {
        Application.targetFrameRate = 60;

        RiptideLogger.Initialize(Debug.Log, Debug.Log, Debug.LogWarning, Debug.LogError, false);

        StartServer();

        EventManager.Singleton.OnCmlReceiveCompleted += flagReceiveComplete;
        EventManager.Singleton.OnMoveAtom += bcastMoveAtom;
        EventManager.Singleton.OnMergeMolecule += bcastMergeMolecule;
        EventManager.Singleton.OnSelectAtom += bcastSelectAtom;
        EventManager.Singleton.OnCreateAtom += bcastCreateAtom;
    }


    /// <summary>
    /// Starts the actual riptide server
    /// </summary>
    public static void StartServer()
    {
        Singleton.Server = new Server();
        Singleton.Server.Start(LoginData.port, LoginData.maxConnections);
        Singleton.Server.ClientDisconnected += Singleton.ClientDisconnected;
        Singleton.Server.ClientConnected += Singleton.ClientConnected; // client invokes sendName

        Debug.Log("[NetworkManagerServer] Server started.");

        Singleton.ServerStarted = true;

    }

    private void flagReceiveComplete()
    {
        receiveComplete = true;
    }

    private void FixedUpdate()
    {
        if (ServerStarted)
        {
            Server.Tick();
        }
    }

    private void OnApplicationQuit()
    {
        if (ServerStarted)
        {
            Server.Stop();
        }
    }

    /// <summary>
    /// Callback on client disconnection
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void ClientDisconnected(object sender, ClientDisconnectedEventArgs e)
    {
        Debug.Log($"[NetworkManagerServer] Client {e.Id} disconnected. Cleaning up.");
        // destroy user gameObject and pannel entry
        Destroy(UserServer.list[e.Id].gameObject);
    }

    private void ClientConnected(object sender, ServerClientConnectedEventArgs e)
    {
        // send current atom world
        Debug.Log($"[NetworkManagerServer] Client {e.Client.Id} connected. Sending current world.");
        var atomWorld = GlobalCtrl.Singleton.saveAtomWorld();
        sendAtomWorld(atomWorld, e.Client.Id);
    }

    #region Messages
    public void pushLoadMolecule(List<cmlData> molecule)
    {
        NetworkUtils.serializeCmlData((ushort)ServerToClientID.bcastMoleculeLoad, molecule, chunkSize, false);
    }
    public void bcastMoveAtom(ushort mol_id, ushort atom_id, Vector3 pos)
    {
        // Broadcast to other clients
        Message message = Message.Create(MessageSendMode.unreliable, ServerToClientID.bcastAtomMoved);
        message.AddUShort(0);
        message.AddUShort(mol_id);
        message.AddUShort(atom_id);
        message.AddVector3(pos);
        Server.SendToAll(message);
    }

    public void bcastMergeMolecule(ushort mol1ID, ushort atom1ID, ushort mol2ID, ushort atom2ID)
    {
        Message message = Message.Create(MessageSendMode.reliable, ServerToClientID.bcastMoleculeMerged);
        message.AddUShort(0);
        message.AddUShort(mol1ID);
        message.AddUShort(atom1ID);
        message.AddUShort(mol2ID);
        message.AddUShort(atom2ID);
        Server.SendToAll(message);
    }

    public void bcastSelectAtom(ushort mol_id, ushort atom_id, bool selected)
    {
        Message message = Message.Create(MessageSendMode.reliable, ServerToClientID.bcastSelectAtom);
        message.AddUShort(0);
        message.AddUShort(mol_id);
        message.AddUShort(atom_id);
        message.AddBool(selected);
        Server.SendToAll(message);
    }

    public void bcastCreateAtom(ushort id, string abbre, Vector3 pos, ushort hyb)
    {
        Message message = Message.Create(MessageSendMode.reliable, ServerToClientID.bcastAtomCreated);
        message.AddUShort(0);
        message.AddUShort(id);
        message.AddString(abbre);
        message.AddVector3(pos);
        message.AddUShort(hyb);
        Server.SendToAll(message);
    }

    #endregion

    #region MessageHandler

    [MessageHandler((ushort)ClientToServerID.atomCreated)]
    private static void getAtomCreated(ushort fromClientId, Message message)
    {
        var atom_id = message.GetUShort();
        var abbre = message.GetString();
        var pos = message.GetVector3();
        var hyb = message.GetUShort();
        // do the create on the server
        GlobalCtrl.Singleton.CreateAtom(atom_id, abbre, pos, hyb, true);

        // Broadcast to other clients
        Message outMessage = Message.Create(MessageSendMode.reliable, ServerToClientID.bcastAtomCreated);
        outMessage.AddUShort(fromClientId);
        outMessage.AddUShort(atom_id);
        outMessage.AddString(abbre);
        outMessage.AddVector3(pos);
        outMessage.AddUShort(hyb);
        NetworkManagerServer.Singleton.Server.SendToAll(outMessage);
    }

    [MessageHandler((ushort)ClientToServerID.moleculeMoved)]
    private static void getMoleculeMoved(ushort fromClientId, Message message)
    {
        var molecule_id = message.GetUShort();
        var pos = message.GetVector3();
        var quat = message.GetQuaternion();
        // do the move on the server
        if (!GlobalCtrl.Singleton.moveMolecule(molecule_id, pos, quat))
        {
            Debug.LogError($"[NetworkManagerServer:getMoleculeMoved] Molecule with id {molecule_id} does not exist.\nSynchronizing world with client {fromClientId}.");
            NetworkManagerServer.Singleton.sendAtomWorld(GlobalCtrl.Singleton.saveAtomWorld(), fromClientId);
            return;
        }

        // Broadcast to other clients
        Message outMessage = Message.Create(MessageSendMode.unreliable, ServerToClientID.bcastMoleculeMoved);
        outMessage.AddUShort(fromClientId);
        outMessage.AddUShort(molecule_id);
        outMessage.AddVector3(pos);
        outMessage.AddQuaternion(quat);
        NetworkManagerServer.Singleton.Server.SendToAll(outMessage);
    }

    [MessageHandler((ushort)ClientToServerID.atomMoved)]
    private static void getAtomMoved(ushort fromClientId, Message message)
    {
        var mol_id = message.GetUShort();
        var atom_id = message.GetUShort();
        var pos = message.GetVector3();
        // do the move on the server
        if (!GlobalCtrl.Singleton.moveAtom(mol_id, atom_id, pos))
        {
            Debug.LogError($"[NetworkManagerServer:getAtomMoved] Atom with id {atom_id} of Molecule {mol_id} does not exist.\nSynchronizing world with client {fromClientId}.");
            NetworkManagerServer.Singleton.sendAtomWorld(GlobalCtrl.Singleton.saveAtomWorld(), fromClientId);
            return;
        }

        // Broadcast to other clients
        Message outMessage = Message.Create(MessageSendMode.unreliable, ServerToClientID.bcastAtomMoved);
        outMessage.AddUShort(fromClientId);
        outMessage.AddUShort(mol_id);
        outMessage.AddUShort(atom_id);
        outMessage.AddVector3(pos);
        NetworkManagerServer.Singleton.Server.SendToAll(outMessage);
    }

    [MessageHandler((ushort)ClientToServerID.moleculeMerged)]
    private static void getMoleculeMerged(ushort fromClientId, Message message)
    {
        var mol1ID = message.GetUShort();
        var atom1ID = message.GetUShort();
        var mol2ID = message.GetUShort();
        var atom2ID = message.GetUShort();

        // do the merge on the server
        // fist check the existence of atoms with the correspoinding ids
        var atom1 = GlobalCtrl.Singleton.List_curMolecules.ElementAtOrDefault(mol1ID).atomList.ElementAtOrDefault(atom1ID);
        var atom2 = GlobalCtrl.Singleton.List_curMolecules.ElementAtOrDefault(mol2ID).atomList.ElementAtOrDefault(atom2ID);
        if (atom1 == default || atom2 == default)
        {
            Debug.LogError($"[NetworkManagerServer:getMoleculeMerged] Merging operation cannot be executed. Atom IDs do not exist (Atom1 {mol1ID}:{atom1ID}, Atom2 {mol2ID}:{atom2ID}).\nSynchronizing world with client {fromClientId}.");
            NetworkManagerServer.Singleton.sendAtomWorld(GlobalCtrl.Singleton.saveAtomWorld(), fromClientId);
            return;
        }
        GlobalCtrl.Singleton.MergeMolecule(mol1ID, atom1ID, mol2ID, atom2ID);

        // Broadcast to other clients
        Message outMessage = Message.Create(MessageSendMode.reliable, ServerToClientID.bcastMoleculeMerged);
        outMessage.AddUShort(fromClientId);
        outMessage.AddUShort(mol1ID);
        outMessage.AddUShort(atom1ID);
        outMessage.AddUShort(mol2ID);
        outMessage.AddUShort(atom2ID);
        NetworkManagerServer.Singleton.Server.SendToAll(outMessage);
    }


    public void sendAtomWorld(List<cmlData> world, int toClientID = -1)
    {
        if (world.Count < 1) return;
        NetworkUtils.serializeCmlData((ushort)ServerToClientID.sendAtomWorld, world, chunkSize, false, toClientID);
    }

    // message not in use yet
    [MessageHandler((ushort)ClientToServerID.sendAtomWorld)]
    private static void listenForAtomWorld(ushort fromClientId, Message message)
    {
        NetworkUtils.deserializeCmlData(message, ref cmlTotalBytes, ref cmlWorld, chunkSize);

        // do bcast?
    }

    [MessageHandler((ushort)ClientToServerID.moleculeLoaded)]
    private static void bcastMoleculeLoad(ushort fromClientId, Message message)
    {
        receiveComplete = false;
        NetworkUtils.deserializeCmlData(message, ref cmlTotalBytes, ref cmlWorld, chunkSize, false);

        // do the bcast
        if (receiveComplete)
        {
            foreach (var client in UserServer.list.Values)
            {
                if (client.ID != fromClientId)
                {
                    NetworkUtils.serializeCmlData((ushort)ServerToClientID.bcastMoleculeLoad, cmlWorld, chunkSize, false, client.ID);
                }
            }
        }
    }

    [MessageHandler((ushort)ClientToServerID.deleteEverything)]
    private static void bcastDeleteEverything(ushort fromClientId, Message message)
    {
        GlobalCtrl.Singleton.DeleteAll();
        GlobalCtrl.Singleton.SaveMolecule(true); // for working undo state
        Message outMessage = Message.Create(MessageSendMode.reliable, ServerToClientID.bcastDeleteEverything);
        outMessage.AddUShort(fromClientId);
        NetworkManagerServer.Singleton.Server.SendToAll(outMessage);
    }

    [MessageHandler((ushort)ClientToServerID.selectAtom)]
    private static void getAtomSelected(ushort fromClientId, Message message)
    {
        var mol_id = message.GetUShort();
        var atom_id = message.GetUShort();
        var selected = message.GetBool();
        // do the select on the server
        // don't show the tooltip - may change later
        var atom = GlobalCtrl.Singleton.List_curMolecules.ElementAtOrDefault(mol_id).atomList.ElementAtOrDefault(atom_id);
        if (atom == default)
        {
            Debug.LogError($"[NetworkManagerServer:getAtomSelected] Atom with id {atom_id} does not exist.\nSynchronizing world with client {fromClientId}.");
            NetworkManagerServer.Singleton.sendAtomWorld(GlobalCtrl.Singleton.saveAtomWorld(), fromClientId);
            return;
        }
        if (atom.m_molecule.isMarked)
        {
            atom.m_molecule.markMolecule(false);
        }
        atom.advancedMarkAtom(selected);

        // Broadcast to other clients
        Message outMessage = Message.Create(MessageSendMode.reliable, ServerToClientID.bcastSelectAtom);
        outMessage.AddUShort(fromClientId);
        outMessage.AddUShort(mol_id);
        outMessage.AddUShort(atom_id);
        outMessage.AddBool(selected);
        NetworkManagerServer.Singleton.Server.SendToAll(outMessage);
    }

    [MessageHandler((ushort)ClientToServerID.selectMolecule)]
    private static void getMoleculeSelected(ushort fromClientId, Message message)
    {
        var mol_id = message.GetUShort();
        var selected = message.GetBool();
        // do the select on the server
        // don't show the tooltip - may change later
        var mol = GlobalCtrl.Singleton.List_curMolecules.ElementAtOrDefault(mol_id);
        if (mol == default)
        {
            Debug.LogError($"[NetworkManagerClient:getMoleculeSelected] Molecule with id {mol_id} does not exist.\nSynchronizing world with client {fromClientId}.");
            NetworkManagerServer.Singleton.sendAtomWorld(GlobalCtrl.Singleton.saveAtomWorld(), fromClientId);
            return;
        }
        mol.markMolecule(selected);

        // Broadcast to other clients
        Message outMessage = Message.Create(MessageSendMode.reliable, ServerToClientID.bcastSelectMolecule);
        outMessage.AddUShort(fromClientId);
        outMessage.AddUShort(mol_id);
        outMessage.AddBool(selected);
        NetworkManagerServer.Singleton.Server.SendToAll(outMessage);
    }

    [MessageHandler((ushort)ClientToServerID.selectBond)]
    private static void getBondSelected(ushort fromClientId, Message message)
    {
        var bond_id = message.GetUShort();
        var mol_id = message.GetUShort();
        var selected = message.GetBool();
        // do the select on the server
        // don't show the tooltip - may change later
        var mol = GlobalCtrl.Singleton.List_curMolecules.ElementAtOrDefault(mol_id);
        var bond = mol.bondList.ElementAtOrDefault(bond_id);
        if (mol == default || bond == default)
        {
            Debug.LogError($"[NetworkManagerClient:getBondSelected] Bond with id {bond_id} or molecule with id {mol_id} does not exist.\nSynchronizing world with client {fromClientId}.");
            NetworkManagerServer.Singleton.sendAtomWorld(GlobalCtrl.Singleton.saveAtomWorld(), fromClientId);
            return;
        }
        bond.markBond(selected);

        // Broadcast to other clients
        Message outMessage = Message.Create(MessageSendMode.reliable, ServerToClientID.bcastSelectBond);
        outMessage.AddUShort(fromClientId);
        outMessage.AddUShort(bond_id);
        outMessage.AddUShort(mol_id);
        outMessage.AddBool(selected);
        NetworkManagerServer.Singleton.Server.SendToAll(outMessage);
    }

    [MessageHandler((ushort)ClientToServerID.deleteAtom)]
    private static void getAtomDeleted(ushort fromClientId, Message message)
    {
        Debug.Log("[NetworkManagerServer] Received delete atom");
        var mol_id = message.GetUShort();
        var atom_id = message.GetUShort();
        // do the delete on the server
        var atom = GlobalCtrl.Singleton.List_curMolecules.ElementAtOrDefault(mol_id).atomList.ElementAtOrDefault(atom_id);
        if (atom == default)
        {
            Debug.LogError($"[NetworkManagerServer:getAtomDeleted] Atom with id {atom_id} does not exist.\nSynchronizing world with client {fromClientId}.");
            NetworkManagerServer.Singleton.sendAtomWorld(GlobalCtrl.Singleton.saveAtomWorld(), fromClientId);
            return;
        }
        GlobalCtrl.Singleton.deleteAtom(atom);

        // Broadcast to other clients
        Message outMessage = Message.Create(MessageSendMode.reliable, ServerToClientID.bcastDeleteAtom);
        outMessage.AddUShort(fromClientId);
        outMessage.AddUShort(mol_id);
        outMessage.AddUShort(atom_id);
        NetworkManagerServer.Singleton.Server.SendToAll(outMessage);
    }

    [MessageHandler((ushort)ClientToServerID.deleteMolecule)]
    private static void getMoleculeDeleted(ushort fromClientId, Message message)
    {
        Debug.Log("[NetworkManagerServer] Received delete molecule");
        var mol_id = message.GetUShort();
        // do the delete on the server
        var mol = GlobalCtrl.Singleton.List_curMolecules.ElementAtOrDefault(mol_id);
        if (mol == default)
        {
            Debug.LogError($"[NetworkManagerServer:getMoleculeDeleted] Molecule with id {mol_id} does not exist.\nSynchronizing world with client {fromClientId}.");
            NetworkManagerServer.Singleton.sendAtomWorld(GlobalCtrl.Singleton.saveAtomWorld(), fromClientId);
            return;
        }
        GlobalCtrl.Singleton.deleteMolecule(mol);

        // Broadcast to other clients
        Message outMessage = Message.Create(MessageSendMode.reliable, ServerToClientID.bcastDeleteMolecule);
        outMessage.AddUShort(fromClientId);
        outMessage.AddUShort(mol_id);
        NetworkManagerServer.Singleton.Server.SendToAll(outMessage);
    }

    [MessageHandler((ushort)ClientToServerID.deleteBond)]
    private static void getBondDeleted(ushort fromClientId, Message message)
    {
        Debug.Log("[NetworkManagerServer] Received delete bond");
        var bond_id = message.GetUShort();
        var mol_id = message.GetUShort();
        // do the delete on the server
        var mol = GlobalCtrl.Singleton.List_curMolecules.ElementAtOrDefault(mol_id);
        var bond = mol.bondList.ElementAtOrDefault(bond_id);
        if (mol == default || bond == default)
        {
            Debug.LogError($"[NetworkManagerServer:getBondDeleted] Bond with id {bond_id} or molecule with id {mol_id} does not exist.\nSynchronizing world with client {fromClientId}.");
            NetworkManagerServer.Singleton.sendAtomWorld(GlobalCtrl.Singleton.saveAtomWorld(), fromClientId);
            return;
        }
        GlobalCtrl.Singleton.deleteBond(bond);

        // Broadcast to other clients
        Message outMessage = Message.Create(MessageSendMode.reliable, ServerToClientID.bcastDeleteBond);
        outMessage.AddUShort(fromClientId);
        outMessage.AddUShort(bond_id);
        outMessage.AddUShort(mol_id);
        NetworkManagerServer.Singleton.Server.SendToAll(outMessage);
    }

    [MessageHandler((ushort)ClientToServerID.syncMe)]
    private static void getSyncRequest(ushort fromClientId, Message message)
    {
        NetworkManagerServer.Singleton.sendAtomWorld(GlobalCtrl.Singleton.saveAtomWorld(), fromClientId);
    }

    [MessageHandler((ushort)ClientToServerID.changeAtom)]
    private static void getAtomChanged(ushort fromClientId, Message message)
    {
        var mol_id = message.GetUShort();
        var atom_id = message.GetUShort();
        var chemAbbre = message.GetString();
        // do the change on the server
        if (!GlobalCtrl.Singleton.changeAtom(mol_id, atom_id, chemAbbre))
        {
            Debug.LogError($"[NetworkManagerServer:getAtomChanged] Atom with id {atom_id} of Molecule {mol_id} does not exist.\nSynchronizing world with client {fromClientId}.");
            NetworkManagerServer.Singleton.sendAtomWorld(GlobalCtrl.Singleton.saveAtomWorld(), fromClientId);
            return;
        }

        // Broadcast to other clients
        Message outMessage = Message.Create(MessageSendMode.unreliable, ServerToClientID.bcastChangeAtom);
        outMessage.AddUShort(fromClientId);
        outMessage.AddUShort(mol_id);
        outMessage.AddUShort(atom_id);
        outMessage.AddString(chemAbbre);
        NetworkManagerServer.Singleton.Server.SendToAll(outMessage);
    }

    [MessageHandler((ushort)ClientToServerID.undo)]
    private static void getUndo(ushort fromClientId, Message message)
    {
        // do the undo
        GlobalCtrl.Singleton.undo();
        // Broadcast undone world to other clients
        NetworkManagerServer.Singleton.sendAtomWorld(GlobalCtrl.Singleton.saveAtomWorld());
    }

    [MessageHandler((ushort)ClientToServerID.enableForceField)]
    private static void getEnableForceField(ushort fromClientId, Message message)
    {
        // process message
        var ffEnabled = message.GetBool();

        // do the enable/disable
        ForceField.Singleton.enableForceFieldMethod(ffEnabled);

        // Broadcast
        Message outMessage = Message.Create(MessageSendMode.reliable, ServerToClientID.bcastEnableForceField);
        outMessage.AddUShort(fromClientId);
        outMessage.AddBool(ffEnabled);
        NetworkManagerServer.Singleton.Server.SendToAll(outMessage);
    }

    [MessageHandler((ushort)ClientToServerID.changeBondTerm)]
    private static void getChangeBondTerm(ushort fromClientId, Message message)
    {
        // process message
        var mol_id = message.GetUShort();
        var term_id = message.GetUShort();
        var term = message.GetBondTerm();

        // do the change
        if (!GlobalCtrl.Singleton.changeBondTerm(mol_id, term_id, term))
        {
            Debug.LogError($"[NetworkManagerServer:getChangeBondTerm] Molecule with id {mol_id} or bond term with id {term_id} does not exist.\nSynchronizing world with client {fromClientId}.");
            NetworkManagerServer.Singleton.sendAtomWorld(GlobalCtrl.Singleton.saveAtomWorld(), fromClientId);
            return;
        }

        // Broadcast
        Message outMessage = Message.Create(MessageSendMode.reliable, ServerToClientID.bcastChangeBondTerm);
        outMessage.AddUShort(fromClientId);
        outMessage.AddUShort(mol_id);
        outMessage.AddUShort(term_id);
        outMessage.AddBondTerm(term);
        NetworkManagerServer.Singleton.Server.SendToAll(outMessage);
    }

    [MessageHandler((ushort)ClientToServerID.changeAngleTerm)]
    private static void getChangeAngleTerm(ushort fromClientId, Message message)
    {
        // process message
        var mol_id = message.GetUShort();
        var term_id = message.GetUShort();
        var term = message.GetAngleTerm();

        // do the change
        if (!GlobalCtrl.Singleton.changeAngleTerm(mol_id, term_id, term))
            {
            Debug.LogError($"[NetworkManagerServer:changeAngleTerm] Molecule with id {mol_id} or angle term with id {term_id} does not exist.\nSynchronizing world with client {fromClientId}.");
            NetworkManagerServer.Singleton.sendAtomWorld(GlobalCtrl.Singleton.saveAtomWorld(), fromClientId);
            return;
        }

        // Broadcast
        Message outMessage = Message.Create(MessageSendMode.reliable, ServerToClientID.bcastChangeAngleTerm);
        outMessage.AddUShort(fromClientId);
        outMessage.AddUShort(mol_id);
        outMessage.AddUShort(term_id);
        outMessage.AddAngleTerm(term);
        NetworkManagerServer.Singleton.Server.SendToAll(outMessage);
    }

    [MessageHandler((ushort)ClientToServerID.changeTorsionTerm)]
    private static void getChangeTorsionTerm(ushort fromClientId, Message message)
    {
        // process message
        var mol_id = message.GetUShort();
        var term_id = message.GetUShort();
        var term = message.GetTorsionTerm();

        // do the change
        if (!GlobalCtrl.Singleton.changeTorsionTerm(mol_id, term_id, term))
        {
            Debug.LogError($"[NetworkManagerServer:getChangeTorsionTerm] Molecule with id {mol_id} or torsion term with id {term_id} does not exist.\nSynchronizing world with client {fromClientId}.");
            NetworkManagerServer.Singleton.sendAtomWorld(GlobalCtrl.Singleton.saveAtomWorld(), fromClientId);
            return;
        }

        // Broadcast
        Message outMessage = Message.Create(MessageSendMode.reliable, ServerToClientID.bcastChangeTorsionTerm);
        outMessage.AddUShort(fromClientId);
        outMessage.AddUShort(mol_id);
        outMessage.AddUShort(term_id);
        outMessage.AddTorsionTerm(term);
        NetworkManagerServer.Singleton.Server.SendToAll(outMessage);
    }

    [MessageHandler((ushort)ClientToServerID.markTerm)]
    private static void getMarkTerm(ushort fromClientId, Message message)
    {
        // process message
        var term_type = message.GetUShort();
        var mol_id = message.GetUShort();
        var term_id = message.GetUShort();
        var marked = message.GetBool();

        // do the change
        var mol = GlobalCtrl.Singleton.List_curMolecules.ElementAtOrDefault(mol_id);
        if (mol == default)
        {
            Debug.LogError($"[NetworkManagerServer:getMarkTerm] Molecule with id {mol_id} does not exist.\nSynchronizing world with client {fromClientId}.");
            NetworkManagerServer.Singleton.sendAtomWorld(GlobalCtrl.Singleton.saveAtomWorld(), fromClientId);
            return;
        }

        if (term_type == 0)
        {
            var term = mol.bondTerms.ElementAtOrDefault(term_id);
            mol.markBondTerm(term, marked);
        }
        else if (term_type == 1)
        {
            var term = mol.angleTerms.ElementAtOrDefault(term_id);
            mol.markAngleTerm(term, marked);
        }
        else if (term_type == 2)
        {
            var term = mol.torsionTerms.ElementAtOrDefault(term_id);
            mol.markTorsionTerm(term, marked);
        }

        // Broadcast
        Message outMessage = Message.Create(MessageSendMode.reliable, ServerToClientID.bcastMarkTerm);
        outMessage.AddUShort(fromClientId);
        outMessage.AddUShort(term_type);
        outMessage.AddUShort(mol_id);
        outMessage.AddUShort(term_id);
        outMessage.AddBool(marked);
        NetworkManagerServer.Singleton.Server.SendToAll(outMessage);
    }

    [MessageHandler((ushort)ClientToServerID.modifyHyb)]
    private static void getModifyHyb(ushort fromClientId, Message message)
    {
        var mol_id = message.GetUShort();
        var atom_id = message.GetUShort();
        var hyb = message.GetUShort();
        // do the move on the server
        if (!GlobalCtrl.Singleton.modifyHybrid(mol_id, atom_id, hyb))
        {
            Debug.LogError($"[NetworkManagerServer:getModifyHyb] Atom with id {atom_id} of Molecule {mol_id} does not exist.\nSynchronizing world with client {fromClientId}.");
            NetworkManagerServer.Singleton.sendAtomWorld(GlobalCtrl.Singleton.saveAtomWorld(), fromClientId);
            return;
        }

        // Broadcast to other clients
        Message outMessage = Message.Create(MessageSendMode.unreliable, ServerToClientID.bcastModifyHyb);
        outMessage.AddUShort(fromClientId);
        outMessage.AddUShort(mol_id);
        outMessage.AddUShort(atom_id);
        outMessage.AddUShort(hyb);
        NetworkManagerServer.Singleton.Server.SendToAll(outMessage);
    }

    [MessageHandler((ushort)ClientToServerID.keepConfig)]
    private static void getKeepConfig(ushort fromClientId, Message message)
    {
        var mol_id = message.GetUShort();
        var keep_config = message.GetBool();

        // do the move on the server
        if (!GlobalCtrl.Singleton.setKeepConfig(mol_id, keep_config))
        {
            Debug.LogError($"[NetworkManagerServer:getModifyHyb] Molecule {mol_id} does not exist.\nSynchronizing world with client {fromClientId}.");
            NetworkManagerServer.Singleton.sendAtomWorld(GlobalCtrl.Singleton.saveAtomWorld(), fromClientId);
            return;
        }

        // Broadcast to other clients
        Message outMessage = Message.Create(MessageSendMode.unreliable, ServerToClientID.bcastKeepConfig);
        outMessage.AddUShort(fromClientId);
        outMessage.AddUShort(mol_id);
        outMessage.AddBool(keep_config);
        NetworkManagerServer.Singleton.Server.SendToAll(outMessage);
    }

    #endregion
}
