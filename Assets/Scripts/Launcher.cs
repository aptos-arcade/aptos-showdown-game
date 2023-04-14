using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using TMPro;

public class Launcher : MonoBehaviourPunCallbacks
{
    
    public static Launcher Instance;
    private void Awake()
    {
        Instance = this;
    }
    
    [Header("References")]
    [SerializeField] private GameObject loadingScreen;
    [SerializeField] private TMP_Text loadingText;
    [SerializeField] private GameObject menuButtons;

    [SerializeField] private GameObject findRoomScreen;
    [SerializeField] private TMP_InputField roomNameInput;
    
    [SerializeField] private GameObject roomScreen;
    [SerializeField] private TMP_Text roomNameText;
    
    [SerializeField] private GameObject errorScreen;
    [SerializeField] private TMP_Text errorText;
    
    // Start is called before the first frame update
    private void Start()
    {
        CloseMenus();
        
        loadingScreen.SetActive(true);
        loadingText.text = "Connecting to Network...";

        PhotonNetwork.ConnectUsingSettings();
    }
    
    private void CloseMenus()
    {
        loadingScreen.SetActive(false);
        findRoomScreen.SetActive(false);
        menuButtons.SetActive(false);
        roomScreen.SetActive(false);
        errorScreen.SetActive(false);
    }
    
    public override void OnConnectedToMaster()
    {
        PhotonNetwork.JoinLobby();
        loadingText.text = "Connecting to Lobby...";
    }
    
    public override void OnJoinedLobby()
    {
        CloseMenus();
        menuButtons.SetActive(true);
    }
    
    public void OpenRoomCreate()
    {
        findRoomScreen.SetActive(true);
    }
    
    public void CreateRoom()
    {
        if (string.IsNullOrEmpty(roomNameInput.text)) return;
        RoomOptions roomOptions = new()
        {
            MaxPlayers = 8
        };
        PhotonNetwork.CreateRoom(roomNameInput.text, roomOptions);
        
        CloseMenus();
        loadingScreen.SetActive(true);
        loadingText.text = "Creating Room...";
    }

    public override void OnJoinedRoom()
    {
        CloseMenus();
        roomScreen.SetActive(true);
        roomNameText.text = PhotonNetwork.CurrentRoom.Name;
    }
    
    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        CloseMenus();
        errorScreen.SetActive(true);
        errorText.text = "Room Creation Failed: " + message;
    }
    
    public void CloseErrorScreen()
    {
        CloseMenus();
        menuButtons.SetActive(true);
    }
    
    public void LeaveRoom()
    {
        PhotonNetwork.LeaveRoom();
        CloseMenus();
        loadingScreen.SetActive(true);
        loadingText.text = "Leaving Room...";
    }
    
    public override void OnLeftRoom()
    {
        CloseMenus();
        menuButtons.SetActive(true);
    }
}
