using System;
using UnityEngine;
using Meta.XR.MRUtilityKit;
using System.Collections.Generic;
using Sirenix.OdinInspector;

// SerializedMonoBehaviour
public class QRCodeManager : SerializedMonoBehaviour
{
    [Title("References")]
    public SpatialAnchorManager anchorManager;
    public Dictionary<string, GameObject> QRCodeMap;

    [Tooltip("Distance threshold in meters to assume an anchor already exists at this QR code.")]
    [SerializeField] private float duplicateProximityThreshold = 0.1f; // 10 centimeters
    
    private void Start()
    {
        MRUK.Instance.SceneSettings.TrackableAdded.AddListener(OnQRCodeDetected);
    }

    private void OnQRCodeDetected(MRUKTrackable qrCode)
    {
        Debug.Log("QR CODE FOUND");
        if (qrCode.TrackableType == OVRAnchor.TrackableType.Keyboard) return; 

        if (qrCode.MarkerPayloadString is not string payload) return;
        
        Vector3 detectedPos = qrCode.transform.position;

        if (anchorManager.IsAnchorAlreadyAtPosition(detectedPos, duplicateProximityThreshold))
        {
            Debug.Log($"Ignored QR code with payload '{payload}' because a localised spatial anchor already exists here.");
            return; 
        }
    
        if (QRCodeMap.TryGetValue(payload, out GameObject prefab))
        {
            Debug.Log($"Payload Found for {payload} object");
        
            Vector3 targetPos = qrCode.transform.position;
            Quaternion targetRotation = qrCode.transform.rotation;
            targetRotation *= Quaternion.Euler(90f, 0f, 0f);
        
            anchorManager.CreateSpatialAnchorAtQRCode(targetPos, targetRotation, payload, prefab);
        }
        else
        {
            Debug.LogWarning($"QR Code not found with payload {payload}");
            return;
        }
    }
    
    public GameObject GetPrefabByPayload(string payload)
    {
        if (QRCodeMap != null && QRCodeMap.TryGetValue(payload, out GameObject prefab))
        {
            return prefab;
        }
        return null;
    }
}