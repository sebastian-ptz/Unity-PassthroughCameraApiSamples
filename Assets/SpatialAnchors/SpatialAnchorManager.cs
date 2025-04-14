using System.Collections.Generic;
using UnityEngine;

public class SpatialAnchorManager : MonoBehaviour
{
    [SerializeField] private List<OVRSpatialAnchor> m_anchors = new();
    [SerializeField] private GameObject m_anchorPrefab;
    private bool m_anchorCreated = false;

    private void Start()
    {
        // Load saved anchors when the application starts
        LoadAnchors();
    }

    private void Update()
    {
        if (OVRInput.GetDown(OVRInput.Button.One, OVRInput.Controller.RTouch) && !m_anchorCreated)
        {
            // Get the position and rotation of the right controller
            var position = OVRInput.GetLocalControllerPosition(OVRInput.Controller.RTouch);
            var rotation = OVRInput.GetLocalControllerRotation(OVRInput.Controller.RTouch);

            var anchorObject = Instantiate(m_anchorPrefab, position, rotation);
            if (anchorObject.TryGetComponent<OVRSpatialAnchor>(out var spatialAnchor))
            {
                SaveAnchor(spatialAnchor);
                m_anchors.Add(spatialAnchor);
            }
            else
            {
                Debug.LogError("OVRSpatialAnchor component not found on the anchor prefab.");
            }


            m_anchorCreated = true;
        }

        // Monitor and log the position of all anchors
        foreach (var anchor in m_anchors)
        {
            Debug.Log($"Anchor position: {anchor.transform.position}");
        }
    }

    private async void SaveAnchor(OVRSpatialAnchor anchor)
    {
        // Save the anchor asynchronously and handle the result
        var result = await anchor.SaveAsync();
        OnAnchorSaveCompleted(anchor, result);
    }

    private void OnAnchorSaveCompleted(OVRSpatialAnchor anchor, OVRSpatialAnchor.OperationResult result)
    {
        if (result == OVRSpatialAnchor.OperationResult.Success)
        {
            Debug.Log("Anchor saved successfully at position: " + anchor.transform.position);
            // Use the anchor's position here
            UseAnchorPosition(anchor.transform.position);
        }
        else
        {
            Debug.LogError("Failed to save anchor: " + result);
        }
    }

    private void UseAnchorPosition(Vector3 position)
    {
        // Implement your logic to use the anchor's position
        Debug.Log("Using anchor position: " + position);
    }

    private async void LoadAnchors()
    {
        // Load saved anchors asynchronously
        var loadResult = await OVRSpatialAnchor.LoadAnchor;

        if (loadResult != null && loadResult.Count > 0)
        {
            foreach (var anchor in loadResult)
            {
                Debug.Log("Loaded anchor at position: " + anchor.transform.position);
                // Instantiate the anchor prefab at the loaded anchor's position
                GameObject anchorObject = Instantiate(m_anchorPrefab, anchor.transform.position, anchor.transform.rotation);

                // Get the OVRSpatialAnchor component from the instantiated prefab
                OVRSpatialAnchor spatialAnchor = anchorObject.GetComponent<OVRSpatialAnchor>();

                if (spatialAnchor != null)
                {
                    spatialAnchor = anchor;
                    m_anchors.Add(spatialAnchor);
                }
            }
        }
        else
        {
            Debug.Log("No saved anchors found.");
        }
    }

    public void CalibrateEnvironment()
    {
        // Implement your calibration logic using the positions of the anchors
        foreach (var anchor in m_anchors)
        {
            Vector3 anchorPosition = anchor.transform.position;
            Debug.Log($"Calibrating using anchor at position: {anchorPosition}");
            // Use the anchorPosition to adjust your environment
        }
    }
}
