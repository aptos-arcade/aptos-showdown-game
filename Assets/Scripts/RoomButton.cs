using System;
using UnityEngine;
using TMPro;
using Photon.Realtime;
using UnityEngine.UI;

public class RoomButton : MonoBehaviour
{
    [SerializeField] private TMP_Text roomNameText;
    
    private RoomInfo _roomInfo;

    private void Start()
    {
        GetComponent<Button>().onClick.AddListener(JoinRoom);
    }

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
