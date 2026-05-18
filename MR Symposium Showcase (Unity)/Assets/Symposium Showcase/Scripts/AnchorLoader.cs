using System;
using TMPro;
using UnityEngine;

public class AnchorLoader : MonoBehaviour
{
    private OVRSpatialAnchor anchorPrefab;
    private SpatialAnchorManager  anchorManager;

    private Action<OVRSpatialAnchor.UnboundAnchor, bool> _onLoadAnchor;

    private void Awake()
    {
        anchorManager = GetComponent<SpatialAnchorManager>();
        anchorPrefab = anchorManager.anchorPrefab;
        _onLoadAnchor = OnLocalized;
    }

    public void LoadAnchorsByUuid()
    {
        if (!PlayerPrefs.HasKey(SpatialAnchorManager.NumUuidsPlayerPref))
        {
            PlayerPrefs.SetInt(SpatialAnchorManager.NumUuidsPlayerPref, 0);
        }
        
        var playerUuidCount = PlayerPrefs.GetInt(SpatialAnchorManager.NumUuidsPlayerPref);

        if (playerUuidCount == 0) return;
        
        var uuids = new Guid[playerUuidCount];
        for (int i = 0; i < playerUuidCount; i++)
        {
            var uuidKey = "uuid" + i;
            var currentUuid = PlayerPrefs.GetString(uuidKey);
            uuids[i] = new Guid(currentUuid);
        }
        
        Load(new OVRSpatialAnchor.LoadOptions
        {
            Timeout = 0,
            StorageLocation = OVRSpace.StorageLocation.Local,
            Uuids = uuids
        });
    }

    private void Load(OVRSpatialAnchor.LoadOptions options)
    {
        OVRSpatialAnchor.LoadUnboundAnchors(options, anchors =>
        {
            if (anchors == null) return;

            foreach (var anchor in anchors)
            {
                if (anchor.Localized)
                {
                    _onLoadAnchor(anchor, true);
                }
                else if (!anchor.Localizing)
                {
                    anchor.Localize(_onLoadAnchor);
                }
            }
        });
    }

    private void OnLocalized(OVRSpatialAnchor.UnboundAnchor anchor, bool success)
    {
        if (!success) return;

        var pose = anchor.Pose;
        var spatialAnchor = Instantiate(anchorPrefab, pose.position, pose.rotation);
        anchor.BindTo(spatialAnchor);

        if (spatialAnchor.TryGetComponent<OVRSpatialAnchor>(out var anchorObj))
        {
            // Fetch UI text components safely
            var textComponents = spatialAnchor.GetComponentsInChildren<TextMeshProUGUI>();
            if (textComponents.Length >= 3)
            {
                var uuidText = textComponents[0];
                var savedStatusText = textComponents[1];
                var nameText = textComponents[2];

                uuidText.text = "UUID: " + spatialAnchor.Uuid.ToString();
                savedStatusText.text = "Loaded from Device";
            
                // Find the matching name from PlayerPrefs using the UUID
                nameText.text = GetAnchorNameFromStorage(spatialAnchor.Uuid);
            }
        }
    }
    
    // Helper method to scan PlayerPrefs for the matching UUID and return its name
    private string GetAnchorNameFromStorage(Guid uuid)
    {
        if (!PlayerPrefs.HasKey(SpatialAnchorManager.NumUuidsPlayerPref)) return "Unknown Anchor";

        int playerNumUuids = PlayerPrefs.GetInt(SpatialAnchorManager.NumUuidsPlayerPref);
        string targetUuidStr = uuid.ToString();

        for (int i = 0; i < playerNumUuids; i++)
        {
            if (PlayerPrefs.GetString("uuid" + i) == targetUuidStr)
            {
                // Found the matching UUID index, return the paired name
                return PlayerPrefs.GetString("uuidName" + i, "Unnamed");
            }
        }

        return "QR Anchor"; // Fallback name if not found
    }
}
