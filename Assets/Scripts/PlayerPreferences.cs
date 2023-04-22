using UnityEngine;

public class PlayerPreferences : MonoBehaviour
{

    public static PlayerPreferences Instance;
    
    private void Awake()
    {
        Instance = this;
        _mouseSensitivity = PlayerPrefs.GetFloat(MouseSensitivityKey, 1f);
        _invertLook = PlayerPrefs.GetInt(InvertLookKey, 0) == 1;
    }
    
    // preference values
    private float _mouseSensitivity;
    private bool _invertLook;
    
    // preference keys
    private const string MouseSensitivityKey = "MouseSensitivity";
    private const string InvertLookKey = "InvertLook";

    public float MouseSensitivity
    {
        get => _mouseSensitivity;
        set
        {
            _mouseSensitivity = value;
            PlayerPrefs.SetFloat(MouseSensitivityKey, value);
        }
    }
    
    public bool InvertLook
    {
        get => _invertLook;
        set
        {
            _invertLook = value;
            PlayerPrefs.SetInt(InvertLookKey, value ? 1 : 0);
        }
    }
}
