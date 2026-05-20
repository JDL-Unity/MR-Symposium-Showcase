using UnityEngine;
using System;
using Sirenix.OdinInspector;

public enum Mode
{
    Default,
    SetUp
}

public class ModeManager : MonoBehaviour
{
    public static ModeManager Instance { get; private set; }
    
    // Changing this to a property expression or forcing a redraw ensures Odin updates
    [SerializeField, ReadOnly]
    private Mode _mode = Mode.Default;
    
    
    public event Action<Mode> OnModeChanged;
    
    public Mode mode
    {
        get {
            return _mode;
        }
        set
        {
            if (_mode == value) return;
            _mode = value;
            Debug.Log($"Mode has been changed to: {_mode.ToString()}");
            InvokeModeEvent();
        }
    }
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InvokeModeEvent()
    {
        OnModeChanged?.Invoke(mode);
    }
    
    [HorizontalGroup("Buttons")]
    [Button(ButtonSizes.Large)]
    [DisableIf("@_mode == Mode.SetUp")]
    [GUIColor(0.4f, 0.8f, 0.4f)]
    public void EnableSetup()
    {
        mode = Mode.SetUp;
        GUI.changed = true; // Forces the Unity Editor to redraw immediately
    }

    [HorizontalGroup("Buttons")]
    [Button(ButtonSizes.Large)]
    [DisableIf("@_mode == Mode.Default")]
    [GUIColor(0.8f, 0.4f, 0.4f)]
    public void DisableSetup()
    {
        mode = Mode.Default;
        GUI.changed = true; // Forces the Unity Editor to redraw immediately
    }
    
}