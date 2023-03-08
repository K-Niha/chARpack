using RiptideNetworking;
using RiptideNetworking.Utils;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class UserServer : MonoBehaviour
{
    public static Dictionary<ushort, UserServer> list = new Dictionary<ushort, UserServer>();
    public static Dictionary<ushort, GameObject> pannel = new Dictionary<ushort, GameObject>();
    public ushort ID;
    public string deviceName;
    public myDeviceType deviceType;
    private GameObject head;
    private Vector3 offsetPos;
    private Quaternion offsetRot;

    public static void spawn(ushort id_, string deviceName_, myDeviceType deviceType_, Vector3 offset_pos, Quaternion offset_rot)
    {
        foreach (UserServer otherUser in list.Values)
        {
            otherUser.sendSpawned(id_);
        }

        var anchorPrefab = (GameObject)Resources.Load("prefabs/QR/QRAnchorNoScript");
        var anchor = Instantiate(anchorPrefab);
        //anchor.transform.position = offset_pos;
        //anchor.transform.rotation = offset_rot;
        anchor.transform.parent = NetworkManagerServer.Singleton.UserWorld.transform;
        anchor.transform.localPosition = Vector3.zero;
        anchor.transform.localRotation = Quaternion.identity;

        UserServer user = anchor.AddComponent<UserServer>();

        user.deviceName = string.IsNullOrEmpty(deviceName_) ? $"Unknown{id_}" : deviceName_;
        user.ID = id_;
        user.deviceType = deviceType_;
        user.offsetPos = offset_pos;
        user.offsetRot = offset_rot;

        anchor.name = user.deviceName;

        // add user to pannel
        var userPannelEntryPrefab = (GameObject)Resources.Load("prefabs/UserPannelEntryPrefab");
        var userPannelEntryInstace = Instantiate(userPannelEntryPrefab, UserPannel.Singleton.transform);
        userPannelEntryInstace.GetComponentInChildren<TextMeshProUGUI>().text = user.deviceName;
        pannel.Add(id_, userPannelEntryInstace);

        // head
        var cubeUser = GameObject.CreatePrimitive(PrimitiveType.Cube);
        CameraSwitcher.Singleton.addCamera(id_ , cubeUser.AddComponent<Camera>());
        cubeUser.transform.localScale = Vector3.one * 0.2f;

        // view ray
        var lineRenderer = cubeUser.AddComponent<LineRenderer>();
        lineRenderer.positionCount = 2;
        lineRenderer.startWidth = 0.005f;
        lineRenderer.endWidth = 0.005f;
        var line_material = (Material)Resources.Load("prefabs/QR/yellow");
        lineRenderer.material = line_material;

        cubeUser.transform.parent = anchor.transform;
        user.head = cubeUser;

        user.sendSpawned();
        list.Add(id_, user);

        // TODO: Probably not necessary
        if (list.Count == 1)
        {
            GlobalCtrl.Singleton.atomWorld.transform.position = offset_pos;
            GlobalCtrl.Singleton.atomWorld.transform.rotation = offset_rot;
            NetworkManagerServer.Singleton.UserWorld.transform.position = offset_pos;
            NetworkManagerServer.Singleton.UserWorld.transform.rotation = offset_rot;
        }
    }

    private void OnDestroy()
    {
        CameraSwitcher.Singleton.removeCamera(ID);
        list.Remove(ID);
        pannel.Remove(ID);
    }

    private void applyPositionAndRotation(Vector3 pos, Quaternion quat)
    {
        var new_pos = GlobalCtrl.Singleton.atomWorld.transform.TransformPoint(pos);
        head.transform.position = new_pos;
        var new_quat = GlobalCtrl.Singleton.atomWorld.transform.rotation * quat;
        head.transform.rotation = new_quat;
        head.GetComponent<LineRenderer>().SetPosition(0, head.transform.position);
        head.GetComponent<LineRenderer>().SetPosition(1, head.transform.forward * 0.8f + head.transform.position);
    }

    #region Messages

    [MessageHandler((ushort)ClientToServerID.deviceNameAndType)]
    private static void getName(ushort fromClientId, Message message)
    {
        var name = message.GetString();
        myDeviceType type = (myDeviceType)message.GetUShort();
        var offset_pos = message.GetVector3();
        var offset_rot = message.GetQuaternion();
        Debug.Log($"[UserServer] Got name {name}, and device type {type} from client {fromClientId}");
        spawn(fromClientId, name, type, offset_pos, offset_rot);
    }

    private void sendSpawned()
    {
        Message message = Message.Create(MessageSendMode.reliable, ServerToClientID.userSpawned);

        NetworkManagerServer.Singleton.Server.SendToAll(addSpawnData(message));
    }

    private void sendSpawned(ushort toClientID)
    {
        Message message = Message.Create(MessageSendMode.reliable, ServerToClientID.userSpawned);

        NetworkManagerServer.Singleton.Server.Send(addSpawnData(message),toClientID);
    }

    private Message addSpawnData(Message message)
    {
        message.AddUShort(ID);
        message.AddString(deviceName);
        message.AddUShort((ushort)deviceType);

        return message;
    }

    [MessageHandler((ushort)ClientToServerID.positionAndRotation)]
    private static void getPositionAndRotation(ushort fromClientId, Message message)
    {
        var pos = message.GetVector3();
        var quat = message.GetQuaternion();
        if (list.TryGetValue(fromClientId, out UserServer user))
        {
            user.applyPositionAndRotation(pos, quat);
        }
        // only send message to other users
        foreach (var otherUser in list.Values)
        {
            if (otherUser.ID != fromClientId)
            {
                Message bcastMessage = Message.Create(MessageSendMode.unreliable, ServerToClientID.bcastPositionAndRotation);
                bcastMessage.AddUShort(fromClientId);
                bcastMessage.AddVector3(pos);
                bcastMessage.AddQuaternion(quat);
                NetworkManagerServer.Singleton.Server.Send(bcastMessage, otherUser.ID);
            }
        }
    }

    #endregion
}
