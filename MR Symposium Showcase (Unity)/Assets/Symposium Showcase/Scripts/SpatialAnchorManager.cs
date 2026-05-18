using System;
using System.Collections;
using System.Collections.Generic;
using Oculus.Interaction.OVR.Input;
using TMPro;
using UnityEngine;

public class SpatialAnchorManager : MonoBehaviour
{
    public OVRSpatialAnchor anchorPrefab;
    public const string NumUuidsPlayerPref = "NumUuids";    
    private Canvas canvas;
    private TextMeshProUGUI uuidText;
    private TextMeshProUGUI savedStatusText;
    private TextMeshProUGUI nameText;
    private List<OVRSpatialAnchor>  anchors = new List<OVRSpatialAnchor>();
    private OVRSpatialAnchor prevAnchor;

    private AnchorLoader anchorLoader;

    private void Awake()
    {
        anchorLoader = GetComponent<AnchorLoader>();
    }

    void Update()
    {
        if (OVRInput.GetDown(OVRInput.Button.One, OVRInput.Controller.LTouch))
        {
            SaveLastCreatedAnchor();
        }

        if (OVRInput.GetDown(OVRInput.Button.Two, OVRInput.Controller.LTouch))
        {
            UnsaveLastCreatedAnchor();
        }
        
        if (OVRInput.GetDown(OVRInput.Button.Two, OVRInput.Controller.RTouch))
        {
            LoadSavedAnchors();
        }
        
        if (OVRInput.GetDown(OVRInput.Button.One, OVRInput.Controller.RTouch))
        {
            UnsaveAllAnchors();
        }
        
        
    }

    private void UnsaveAllAnchors()
    {
        foreach (var anchor in anchors)
        {
            UnsaveAnchor(anchor);
        }
        
        anchors.Clear();
        ClearAllUuidsFromPlayerPrefs();
    }

    private void ClearAllUuidsFromPlayerPrefs()
    {
        if (PlayerPrefs.HasKey(NumUuidsPlayerPref))
        {
            int playerNumUuids = PlayerPrefs.GetInt(NumUuidsPlayerPref);
            for (int i = 0; i < playerNumUuids; i++)
            {
                PlayerPrefs.DeleteKey("uuid" + i);
                PlayerPrefs.DeleteKey("uuidName" + i); // Clear the names too
            }
        
            PlayerPrefs.DeleteKey(NumUuidsPlayerPref);
            PlayerPrefs.Save();
        }
    }

    private void UnsaveAnchor(OVRSpatialAnchor anchor)
    {
        anchor.Erase((erasedAnchor, success) =>
        {
            if (success)
            {
                var textComponent = erasedAnchor.GetComponentsInChildren<TextMeshProUGUI>();
                if (textComponent.Length > 1)
                {
                    var savedStatus = textComponent[1];
                    savedStatusText.text = "Not Saved";
                }
            }
        });    
    }

    private void LoadSavedAnchors()
    {
        anchorLoader.LoadAnchorsByUuid();
    }

    private void SaveLastCreatedAnchor()
    {
        prevAnchor.Save((prevAnchor, success) =>
        {
            if (success)
            {
                savedStatusText.text = "Saved";
            }
        });
        
        SaveUuidToPlayerPrefs(prevAnchor.Uuid, nameText.text);
        
    }

    private void SaveUuidToPlayerPrefs(Guid uuid, string anchorName)
    {
        if (!PlayerPrefs.HasKey(NumUuidsPlayerPref))
        {
            PlayerPrefs.SetInt(NumUuidsPlayerPref, 0);
        }

        int playerNumUuids = PlayerPrefs.GetInt(NumUuidsPlayerPref);
    
        // Save both the UUID and the custom name using the same index
        PlayerPrefs.SetString("uuid" + playerNumUuids, uuid.ToString());
        PlayerPrefs.SetString("uuidName" + playerNumUuids, anchorName); 
    
        PlayerPrefs.SetInt(NumUuidsPlayerPref, ++playerNumUuids);
        PlayerPrefs.Save(); // Force save to disk
    }

    private void UnsaveLastCreatedAnchor()
    {
        prevAnchor.Erase((prevAnchor, success) =>
        {
            if (success)
            {
                savedStatusText.text = "Not Saved";
            }
        });
    }

    public void CreateSpatialAnchorAtQRCode(Vector3 targetPos, Quaternion rotation, string name)
    {
        OVRSpatialAnchor anchor = Instantiate(anchorPrefab, targetPos, rotation);
        
        canvas = anchor.gameObject.GetComponentInChildren<Canvas>();
        uuidText = canvas.gameObject.transform.GetChild(0).GetComponent<TextMeshProUGUI>();
        savedStatusText = canvas.gameObject.transform.GetChild(1).GetComponent<TextMeshProUGUI>();
        nameText =  canvas.gameObject.transform.GetChild(2).GetComponent<TextMeshProUGUI>();
        
        
        StartCoroutine(AnchorCreated(anchor, name));
    }

    private IEnumerator AnchorCreated(OVRSpatialAnchor anchor, string name)
    {
        while (!anchor.Created && !anchor.Localized)
        {
            yield return new WaitForEndOfFrame();
        }

        Guid anchorGuid = anchor.Uuid;
        anchors.Add(anchor);
        prevAnchor = anchor;

        uuidText.text = $"UUID: {anchorGuid.ToString()}";
        savedStatusText.text = "Not Saved";
        nameText.text = name;
    }
    
}
