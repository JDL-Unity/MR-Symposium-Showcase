using System;
using UnityEngine;

public class AnchorManager : MonoBehaviour
{
    public GameObject anchorModel;
    public GameObject anchorCanvas;
    
    private ModeManager modeManager;
    
    private void OnEnable()
    {
        ModeManager.Instance.OnModeChanged += OnModeChanged;
        
        if (ModeManager.Instance.mode == Mode.SetUp) EnableSetUp();
        else DisableSetup();
    }

    private void OnDisable()
    {
        ModeManager.Instance.OnModeChanged -= OnModeChanged;
    }

    private void OnModeChanged(Mode mode)
    {
        switch (mode)
        {
            case Mode.Default:
                DisableSetup();
                break;
            case Mode.SetUp:
                EnableSetUp();
                break;
        }
    }

    private void EnableSetUp()
    {
        anchorModel.SetActive(true);
        anchorCanvas.SetActive(true);
    }

    private void DisableSetup()
    {
        anchorModel.SetActive(false);
        anchorCanvas.SetActive(false);
    }
}
