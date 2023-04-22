using UnityEngine;
using TMPro;
using Photon.Realtime;

public class RoomButton : MonoBehaviour
{
    
    [SerializeField] private TMP_Text roomNameText;
    
    private RoomInfo _roomInfo;

    public void SetRoomDetails(RoomInfo roomInfo)
    {
        _roomInfo = roomInfo;
        roomNameText.text = roomInfo.Name;
    }
    
    public void JoinRoom()
    {
        Launcher.Instance.JoinRoom(_roomInfo);
    }
}
