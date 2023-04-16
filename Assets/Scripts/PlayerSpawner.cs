using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class PlayerSpawner : MonoBehaviour
{
    public static PlayerSpawner Instance;

    private void Awake()
    {
        Instance = this;
    }

    [Header("References")]
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private GameObject deathEffect;
    
    [Header("Settings")]
    [SerializeField] private float deathTime = 2f;
    
    // private player spawner state
    private GameObject _player;

    private void Start()
    {
        if (PhotonNetwork.IsConnected)
        {
            SpawnPlayer();
        }
    }

    public void SpawnPlayer()
    {
        var spawnPoint = SpawnManager.Instance.GetSpawnPoint();
        _player = PhotonNetwork.Instantiate(playerPrefab.name, spawnPoint.position, spawnPoint.rotation);
    }
    
    public void DestroyPlayer()
    {
        PhotonNetwork.Destroy(_player);
    }
    
    public void OnDeath(string killerName)
    {
        UIController.Instance.SetDeathText(killerName);
        MatchManager.Instance.UpdateStatSend(PhotonNetwork.LocalPlayer.ActorNumber, 1, 1);
        if(_player != null) StartCoroutine(DeathCoroutine());
    }
    
    private IEnumerator DeathCoroutine()
    {
        PhotonNetwork.Instantiate(deathEffect.name, _player.transform.position, Quaternion.identity);
        DestroyPlayer();
        UIController.Instance.SetDeathScreenActive(true);
        
        yield return new WaitForSeconds(deathTime);
        
        UIController.Instance.SetDeathScreenActive(false);
        SpawnPlayer();
    }
}
