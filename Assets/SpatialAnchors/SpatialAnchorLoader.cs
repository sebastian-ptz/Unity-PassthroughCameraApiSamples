using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using TMPro;

public class SpatialAnchorLoader : MonoBehaviour
{

    private OVRSpatialAnchor m_anchorPrefab;
    public bool Debugging;

    private SpatialAnchorManager m_spatialAnchorManager;

    private void Awake()
    {
        if (m_spatialAnchorManager == null)
        {
            m_spatialAnchorManager = SpatialAnchorManager.Instance;
        }

        m_anchorPrefab = m_spatialAnchorManager.AnchorPrefab;

        if (m_anchorPrefab == null)
        {
            Debug.LogError("AnchorPrefab is null. Assigned in SpatialAnchorManager.");
            return;
        }
    }

    public async Task<List<OVRSpatialAnchor>> LoadAndLocalizeAnchors()
    {
        var anchors = new List<OVRSpatialAnchor>();
        var playerNumCount = PlayerPrefs.GetInt("numUuids", 0);

        if (playerNumCount == 0)
        {
            Debug.Log("No saved anchors found.");
            return anchors;
        }

        var uuids = new List<Guid>();
        for (var i = 0; i < playerNumCount; i++)
        {
            var key = $"uuid{i}";
            var currentUuid = PlayerPrefs.GetString(key, string.Empty); // empty string if key is missing
            if (!string.IsNullOrEmpty(currentUuid))
            {
                uuids.Add(new Guid(currentUuid));
            }
            else
            {
                Debug.LogWarning($"Invalid or empty UUID found for key: {key}");
            }
        }

        // Load unbound anchors
        var unboundAnchors = new List<OVRSpatialAnchor.UnboundAnchor>();
        var loadResult = await OVRSpatialAnchor.LoadUnboundAnchorsAsync(uuids, unboundAnchors);

        if (!loadResult.Success)
        {
            Debug.LogError($"Failed to load unbound anchors: {loadResult.Status}");
            return anchors; // Return early if loading fails
        }

        Debug.Log($"Successfully loaded {unboundAnchors.Count} unbound anchors.");

        // Localize unbound anchors
        foreach (var unboundAnchor in unboundAnchors)
        {
            var localized = await unboundAnchor.LocalizeAsync();
            if (localized)
            {
                Debug.Log($"Anchor localized successfully: {unboundAnchor.Uuid}");
                var spatialAnchor = InstantiateAndBindAnchor(unboundAnchor);
                if (spatialAnchor != null)
                {
                    anchors.Add(spatialAnchor);
                }
            }
            else if (!localized && Debugging)
            {
                Debug.LogWarning($"Failed to localize anchor: {unboundAnchor.Uuid}");
            }
        }

        return anchors;
    }

    private OVRSpatialAnchor InstantiateAndBindAnchor(OVRSpatialAnchor.UnboundAnchor unboundAnchor)
    {
        if (!unboundAnchor.TryGetPose(out var pose))
        {
            Debug.LogWarning($"Failed to get pose for localized anchor: {unboundAnchor.Uuid}");
            return null;
        }

        // Instantiate spatialAnchor and Bind anchor
        var spatialAnchor = Instantiate(m_anchorPrefab, pose.position, pose.rotation);
        unboundAnchor.BindTo(spatialAnchor);

        // Update the spatial anchor prefab
        var textComponents = spatialAnchor.GetComponentsInChildren<TextMeshProUGUI>();
        if (textComponents.Length > 1)
        {
            var uuidText = textComponents[0];
            var statusText = textComponents[1];

            uuidText.text = $"UUID: {unboundAnchor.Uuid}";
            statusText.text = "Localized";
        }
        else
        {
            Debug.LogWarning("TextMeshProUGUI components for UUID and status are missing in the prefab.");
        }

        return spatialAnchor;
    }
}
