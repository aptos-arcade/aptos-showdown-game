using UnityEngine;
using Photon.Pun;
using UnityEngine.SceneManagement;

public class MatchManager : MonoBehaviour
{
    public static MatchManager Instance;
    
    private void Awake()
    {
        Instance = this;
    }

    // Start is called before the first frame update
    private void Start()
    {
        if (!PhotonNetwork.IsConnected) SceneManager.LoadScene(Scenes.MainMenuBuildIndex);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
