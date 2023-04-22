using UnityEngine;

public class Gun : MonoBehaviour
{
    [SerializeField] private bool isAutomatic;
    public bool IsAutomatic => isAutomatic;
    
    [SerializeField] private float timeBetweenShots = 0.1f;
    public float TimeBetweenShots => timeBetweenShots;
    
    [SerializeField] private float heatPerShot = 1f;
    public float HeatPerShot => heatPerShot;
    
    [SerializeField] private GameObject muzzleFlash;
    public GameObject MuzzleFlash => muzzleFlash;
    
    [SerializeField] private int shotDamage;
    public int ShotDamage => shotDamage;
    
    [SerializeField] private float adsZoom;
    public float AdsZoom => adsZoom;
    
    private AudioSource _audioSource;

    private void Start()
    {
        _audioSource = GetComponent<AudioSource>();
    }
    
    public void PlaySound()
    {
        _audioSource.Stop();
        _audioSource.Play();
    }
}
