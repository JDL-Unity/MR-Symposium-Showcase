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
    
    private void Start()
    {
        // Add a listener to listen for when a QR code gets detected.
        MRUK.Instance.SceneSettings.TrackableAdded.AddListener(OnQRCodeDetected);
    }

    private void OnQRCodeDetected(MRUKTrackable qrCode)
    {
        Debug.LogWarning("QR CODE FOUND");
        if (qrCode.TrackableType == OVRAnchor.TrackableType.Keyboard) return; // If we detect a keyboard, ignore it.

        // Checks if the QR code contains a payload
        if (qrCode.MarkerPayloadString is not string payload) return;
        
        if (QRCodeMap.TryGetValue(payload, out GameObject prefab))
        {
            Console.WriteLine($"Payload Found for {payload} object");
        }
        else
        {
            Debug.LogWarning($"QR Code not found with payload {payload}");
            return;
        }
        
        // Setting the position and rotation to place the object on
        Vector3 targetPos = qrCode.transform.position;
        Quaternion targetRotation = qrCode.transform.rotation;
        
        targetRotation *= Quaternion.Euler(90f, 0f, 0f);
        
        // // Spawn the object ontop of the QR code
        // GameObject spawnedObj = Instantiate(prefab, targetPos, rotation);
        //
        //
        // // Setting the scale to fit the QR code
        // float width = qrCode.PlaneRect.Value.width;
        // float height = qrCode.PlaneRect.Value.height;
        //
        // float scaleVal = (width + height) / 2;
        // spawnedObj.transform.localScale = new Vector3(scaleVal, scaleVal, scaleVal);
        //
        // // This ensures that the object follows the QR code
        // spawnedObj.transform.parent = qrCode.transform;
        
        anchorManager.CreateSpatialAnchorAtQRCode(targetPos, targetRotation, payload);
        
        
    }
}
