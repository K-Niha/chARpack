using RiptideNetworking;
using RiptideNetworking.Utils;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UserClient : MonoBehaviour
{
    public static Dictionary<ushort, UserClient> list = new Dictionary<ushort, UserClient>();
    public ushort ID;
    public string deviceName { get; private set; }
    public myDeviceType deviceType { get; private set; }
    public bool isLocal;

    private void OnDestroy()
    {
        list.Remove(ID);
    }

    public static void spawn(ushort id_, string deviceName_, myDeviceType deviceType_)
    {
        Debug.Log($"[UserClient:spawn] Id from function call {id_}, id from NetworkManager {NetworkManagerClient.Singleton.Client.Id}");
        UserClient user;
        if (id_ == NetworkManagerClient.Singleton.Client.Id)
        {
            user = new GameObject().AddComponent<UserClient>();

            user.isLocal = true;
        }
        else
        {

            var cubeUser = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cubeUser.transform.localScale = Vector3.one * 0.2f;
            cubeUser.GetComponent<Renderer>().material = (Material)Resources.Load("materials/UserMaterial");
            user = cubeUser.AddComponent<UserClient>();
            

            user.isLocal = false;

            // view ray
            var lineRenderer = cubeUser.AddComponent<LineRenderer>();
            lineRenderer.positionCount = 2;
            lineRenderer.startWidth = 0.005f;
            lineRenderer.endWidth = 0.005f;
            var line_material = (Material)Resources.Load("prefabs/QR/yellow");
            lineRenderer.material = line_material;
        }

        user.deviceName = string.IsNullOrEmpty(deviceName_) ? $"Unknown{id_}" : deviceName_;
        user.name = user.isLocal ? "Me" : user.deviceName;
        user.ID = id_;
        user.deviceType = deviceType_;

        user.transform.parent = NetworkManagerClient.Singleton.userWorld.transform;

        list.Add(id_, user);
    }

    private void FixedUpdate()
    {
        sendPositionAndRotation();
    }

    private void applyPositionAndRotation(Vector3 pos, Quaternion quat)
    {
        var new_pos = GlobalCtrl.Singleton.atomWorld.transform.TransformPoint(pos);
        gameObject.transform.position = new_pos;
        var new_quat = GlobalCtrl.Singleton.atomWorld.transform.rotation * quat;
        gameObject.transform.rotation = new_quat;
        GetComponent<LineRenderer>().SetPosition(0, transform.position);
        GetComponent<LineRenderer>().SetPosition(1, transform.forward * 0.8f + transform.position);
    }


    #region Messages

    [MessageHandler((ushort)ServerToClientID.userSpawned)]
    private static void spawnUser(Message message)
    {
        var id = message.GetUShort();
        var name = message.GetString();
        var type = (myDeviceType)message.GetUShort();

        spawn(id,name,type);
    }

    private void sendPositionAndRotation()
    {
        Message message = Message.Create(MessageSendMode.unreliable, ClientToServerID.positionAndRotation);

        message.AddVector3(GlobalCtrl.Singleton.atomWorld.transform.InverseTransformPoint(Camera.main.transform.position));
        message.AddQuaternion(Quaternion.Inverse(GlobalCtrl.Singleton.atomWorld.transform.rotation) * Camera.main.transform.rotation);
        NetworkManagerClient.Singleton.Client.Send(message);
    }

    [MessageHandler((ushort)ServerToClientID.bcastPositionAndRotation)]
    private static void getPositionAndRotation(Message message)
    {
        var id = message.GetUShort();
        var pos = message.GetVector3();
        //var forward = message.GetVector3();
        var quat = message.GetQuaternion();
        // if (list.TryGetValue(id, out UserClient user) && !user.isLocal)
        if (list.TryGetValue(id, out UserClient user))
        {
            user.applyPositionAndRotation(pos, quat);
        }
    }

    #endregion

}
