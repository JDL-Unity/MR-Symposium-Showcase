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
        // Add a listener to listen for when a QR code gets detected.
        MRUK.Instance.SceneSettings.TrackableAdded.AddListener(OnQRCodeDetected);
    }

    private void OnQRCodeDetected(MRUKTrackable qrCode)
    {
        Debug.Log("QR CODE FOUND");
        if (qrCode.TrackableType == OVRAnchor.TrackableType.Keyboard) return; 

        if (qrCode.MarkerPayloadString is not string payload) return;

        // --- DUPLICATE CHECK ---
        // Grab the position of the newly detected QR code
        Vector3 detectedPos = qrCode.transform.position;

        // Ask the AnchorManager if we already have a trackable anchor living here
        if (anchorManager.IsAnchorAlreadyAtPosition(detectedPos, duplicateProximityThreshold))
        {
            Debug.Log($"Ignored QR code with payload '{payload}' because a localised spatial anchor already exists here.");
            return; // Exit early! Do not create or spawn a duplicate.
        }
        // -----------------------
    
        // Find the correct prefab from your Odin Dictionary mapping
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
    
    // Add this helper method inside QRCodeManager.cs
    public GameObject GetPrefabByPayload(string payload)
    {
        if (QRCodeMap != null && QRCodeMap.TryGetValue(payload, out GameObject prefab))
        {
            return prefab;
        }
        return null;
    }
}