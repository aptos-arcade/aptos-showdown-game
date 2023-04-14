using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class UIController : MonoBehaviour
{
    
    public static UIController Instance;
    
    private void Awake()
    {
        Instance = this;
    }

    [Header("References")]
    [SerializeField] private TMP_Text overheatedMessage;
    [SerializeField] private Slider heatSlider;


    private void Start()
    {
        
    }

    // Update is called once per frame
    private void Update()
    {
        
    }
    
    public void SetOverheatedMessageActive(bool isOverheated)
    {
        overheatedMessage.gameObject.SetActive(isOverheated);
    }
    
    public void SetHeatSliderValue(float heat)
    {
        heatSlider.value = heat;
    }
}
