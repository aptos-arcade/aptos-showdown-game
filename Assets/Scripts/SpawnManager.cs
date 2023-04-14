using UnityEngine;

public class SpawnManager : MonoBehaviour
{
    
    public static SpawnManager Instance;
    
    private void Awake()
    {
        Instance = this;
    }
    
    [SerializeField] private Transform[] spawnPoints;

    private void Start()
    {
        foreach(var spawnPoint in spawnPoints)
        {
            spawnPoint.gameObject.SetActive(false);
        }
    }
    
    public Transform GetSpawnPoint()
    {
        return spawnPoints[Random.Range(0, spawnPoints.Length)];
    }
}
