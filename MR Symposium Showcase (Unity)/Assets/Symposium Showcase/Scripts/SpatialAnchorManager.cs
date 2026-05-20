using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class SpatialAnchorManager : MonoBehaviour
{
    public OVRSpatialAnchor anchorPrefab;
    public QRCodeManager qrCodeManager;
    
    public const string NumUuidsPlayerPref = "NumUuids";    
    private Canvas canvas;
    private TextMeshProUGUI uuidText;
    private TextMeshProUGUI savedStatusText;
    private TextMeshProUGUI nameText;
    
    private List<OVRSpatialAnchor> anchors = new List<OVRSpatialAnchor>();
    private OVRSpatialAnchor prevAnchor;

    private Dictionary<OVRSpatialAnchor, GameObject> spawnedModelsMap = new Dictionary<OVRSpatialAnchor, GameObject>();

    void OnEnable()
    {
        OVRManager.HMDMounted += OnHeadsetPutOn;
        OVRManager.HMDUnmounted += OnHeadsetTakenOff;
    }

    void OnDisable()
    {
        OVRManager.HMDMounted -= OnHeadsetPutOn;
        OVRManager.HMDUnmounted -= OnHeadsetTakenOff;
    }

    void Start()
    {
        LoadSavedAnchors();
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

    private void OnHeadsetTakenOff()
    {
        // Headset lost tracking / put in standby. Hide everything to prevent visual jumps.
        ToggleAllVirtualContent(false);
    }

    private void OnHeadsetPutOn()
    {
        // The user put the headset back on. Start monitoring the anchors to check when Meta finishes relocalizing.
        StartCoroutine(WaitForRoomLocalizationRoutine());
    }

    private IEnumerator WaitForRoomLocalizationRoutine()
    {
        Debug.Log("Headset donned. Holding virtual content visibility until tracking updates settle...");
        
        yield return new WaitForSeconds(0.5f);

        bool allAnchorsReady = false;
        int timeoutFrames = 300; 

        while (!allAnchorsReady && timeoutFrames > 0)
        {
            allAnchorsReady = true;
            timeoutFrames--;

            foreach (var anchor in anchors)
            {
                if (anchor == null) continue;

                if (!anchor.Localized)
                {
                    allAnchorsReady = false;
                    break;
                }
            }

            if (!allAnchorsReady)
            {
                yield return null; 
            }
        }

        ToggleAllVirtualContent(true);
        Debug.Log("Meta tracking localized successfully. Restoring virtual elements.");
    }

    private void ToggleAllVirtualContent(bool isVisible)
    {
        foreach (var kvp in spawnedModelsMap)
        {
            if (kvp.Value != null)
            {
                kvp.Value.SetActive(isVisible);
            }
            
            Canvas anchorCanvas = kvp.Key.GetComponentInChildren<Canvas>();
            if (anchorCanvas != null)
            {
                anchorCanvas.enabled = isVisible;
            }
        }
    }

    private void UnsaveAllAnchors()
    {
        foreach (var anchor in anchors)
        {
            UnsaveAnchor(anchor);
        }
        
        spawnedModelsMap.Clear();
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
                PlayerPrefs.DeleteKey("uuidName" + i);
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
                if (spawnedModelsMap.ContainsKey(erasedAnchor))
                {
                    Destroy(spawnedModelsMap[erasedAnchor]);
                    spawnedModelsMap.Remove(erasedAnchor);
                }

                var textComponent = erasedAnchor.GetComponentsInChildren<TextMeshProUGUI>();
                if (textComponent.Length > 1)
                {
                    savedStatusText.text = "Not Saved";
                }
            }
        });    
    }

    private void SaveLastCreatedAnchor()
    {
        if (prevAnchor == null) return;

        prevAnchor.Save((anchor, success) =>
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
    
        PlayerPrefs.SetString("uuid" + playerNumUuids, uuid.ToString());
        PlayerPrefs.SetString("uuidName" + playerNumUuids, anchorName); 
    
        PlayerPrefs.SetInt(NumUuidsPlayerPref, ++playerNumUuids);
        PlayerPrefs.Save();
    }

    private void UnsaveLastCreatedAnchor()
    {
        if (prevAnchor == null) return;
        prevAnchor.Erase((anchor, success) =>
        {
            if (success)
            {
                if (spawnedModelsMap.ContainsKey(anchor))
                {
                    Destroy(spawnedModelsMap[anchor]);
                    spawnedModelsMap.Remove(anchor);
                }
                savedStatusText.text = "Not Saved";
            }
        });
    }

    // --- SPAWNING FROM QR CODE DETECTED ---
    public void CreateSpatialAnchorAtQRCode(Vector3 targetPos, Quaternion rotation, string name, GameObject modelPrefab)
    {
        OVRSpatialAnchor anchor = Instantiate(anchorPrefab, targetPos, rotation);
    
        if (modelPrefab != null)
        {
            GameObject spawnedModel = Instantiate(modelPrefab, anchor.transform);
            spawnedModel.transform.localPosition = Vector3.zero;
            spawnedModel.transform.localRotation = Quaternion.identity;
        
            DebugCube debugCube = spawnedModel.GetComponent<DebugCube>();
            if (debugCube != null)
            {
                spawnedModel.transform.localPosition = new Vector3(0, debugCube.height, 0);
            }

            spawnedModelsMap[anchor] = spawnedModel;
        }

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

    private void LoadSavedAnchors()
    {
        if (!PlayerPrefs.HasKey(NumUuidsPlayerPref)) return;

        int count = PlayerPrefs.GetInt(NumUuidsPlayerPref);
        List<Guid> uuidsToLoad = new List<Guid>();

        for (int i = 0; i < count; i++)
        {
            string uuidStr = PlayerPrefs.GetString("uuid" + i);
            if (Guid.TryParse(uuidStr, out Guid resultGuid))
            {
                uuidsToLoad.Add(resultGuid);
            }
        }

        if (uuidsToLoad.Count == 0) return;

        var queryOptions = new OVRSpatialAnchor.LoadOptions
        {
            StorageLocation = OVRSpace.StorageLocation.Local,
            Uuids = uuidsToLoad
        };

        OVRSpatialAnchor.LoadUnboundAnchors(queryOptions, OnUnboundAnchorsLoaded);
    }

    private void OnUnboundAnchorsLoaded(OVRSpatialAnchor.UnboundAnchor[] unboundAnchors)
    {
        if (unboundAnchors == null || unboundAnchors.Length == 0)
        {
            Debug.LogWarning("No unbound spatial anchors found or retrieved from storage.");
            return;
        }

        foreach (var unboundAnchor in unboundAnchors)
        {
            if (unboundAnchor.Localized)
            {
                ProcessAndSpawnAnchor(unboundAnchor);
            }
            else
            {
                unboundAnchor.Localize((anchor, success) =>
                {
                    if (success)
                    {
                        ProcessAndSpawnAnchor(anchor);
                    }
                    else
                    {
                        Debug.LogError($"Failed to physically localize spatial anchor UUID: {anchor.Uuid}");
                    }
                });
            }
        }
    }

    private void ProcessAndSpawnAnchor(OVRSpatialAnchor.UnboundAnchor unboundAnchor)
    {
        OVRSpatialAnchor localizedAnchor = Instantiate(anchorPrefab);
        unboundAnchor.BindTo(localizedAnchor);
        
        anchors.Add(localizedAnchor);

        string savedPayloadName = FindNameByUuid(localizedAnchor.Uuid);

        if (qrCodeManager != null)
        {
            GameObject modelPrefab = qrCodeManager.GetPrefabByPayload(savedPayloadName);
            if (modelPrefab != null)
            {
                GameObject restoredModel = Instantiate(modelPrefab, localizedAnchor.transform);
                restoredModel.transform.localPosition = Vector3.zero;
                restoredModel.transform.localRotation = Quaternion.identity;
                
                DebugCube debugCube = restoredModel.GetComponent<DebugCube>();
                if (debugCube != null)
                {
                    restoredModel.transform.localPosition = new Vector3(0, debugCube.height, 0);
                }
                
                // Keep tracking link for loaded structures as well
                spawnedModelsMap[localizedAnchor] = restoredModel;
                Debug.Log($"Successfully spawned model for payload: {savedPayloadName}");
            }
            else
            {
                Debug.LogWarning($"No matching 3D Model mapping found in QRCodeMap dictionary for payload string: {savedPayloadName}");
            }
        }
        else
        {
            Debug.LogError("QRCodeManager reference is completely missing on your SpatialAnchorManager component!");
        }

        Canvas canvasOverlay = localizedAnchor.GetComponentInChildren<Canvas>();
        if (canvasOverlay != null && canvasOverlay.gameObject.transform.childCount >= 3)
        {
            canvasOverlay.gameObject.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = $"UUID: {localizedAnchor.Uuid.ToString()}";
            canvasOverlay.gameObject.transform.GetChild(1).GetComponent<TextMeshProUGUI>().text = "Saved (Loaded)";
            canvasOverlay.gameObject.transform.GetChild(2).GetComponent<TextMeshProUGUI>().text = savedPayloadName;
        }
    }

    private string FindNameByUuid(Guid uuid)
    {
        if (!PlayerPrefs.HasKey(NumUuidsPlayerPref)) return "Unknown Object";

        int count = PlayerPrefs.GetInt(NumUuidsPlayerPref, 0);
        string targetUuidStr = uuid.ToString();

        for (int i = 0; i < count; i++)
        {
            if (PlayerPrefs.GetString("uuid" + i) == targetUuidStr)
            {
                return PlayerPrefs.GetString("uuidName" + i, "Unknown Object");
            }
        }
        return "Unknown Object";
    }
    
    public bool IsAnchorAlreadyAtPosition(Vector3 checkPosition, float threshold)
    {
        foreach (var anchor in anchors)
        {
            if (anchor == null) continue;

            // Measure distance between the new QR code detection and the active anchor
            if (Vector3.Distance(anchor.transform.position, checkPosition) < threshold)
            {
                return true; // Match found! An anchor is already tracking here.
            }
        }
        return false;
    }
}