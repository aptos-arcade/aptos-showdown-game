using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DestroyOverTime : MonoBehaviour
{
    
    [SerializeField] private float timeToDestroy = 1.5f;
    
    private void Start()
    {
        Destroy(gameObject, timeToDestroy);
    }
}
