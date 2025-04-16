using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using TMPro;
using System;

public class SpatialAnchorManager : MonoBehaviour
{
    public static SpatialAnchorManager Instance;

    public bool Debugging = true;
    public bool IsSaving = false;
    public OVRSpatialAnchor AnchorPrefab;

    private TextMeshProUGUI m_uuidText;
    private TextMeshProUGUI m_statusText;
    private OVRSpatialAnchor m_lastAnchor;
    private SpatialAnchorLoader m_anchorLoader;
    private List<OVRSpatialAnchor> m_anchors = new();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Debug.LogError("Multiple instances of SpatialAnchorManager detected!");
            Destroy(this);
        }

        m_anchorLoader = GetComponent<SpatialAnchorLoader>();
    }

    private async void Start()
    {
        // Ensure numUuids exists with a default value of 0
        if (!PlayerPrefs.HasKey("numUuids"))
        {
            PlayerPrefs.SetInt("numUuids", 0);
            PlayerPrefs.Save();
            if (Debugging) Debug.Log($"Initialized numUuids with value 0.");
        }

        // Load and localize anchors
        m_anchors = await m_anchorLoader.LoadAndLocalizeAnchors();
        if (m_anchors.Count > 0)
        {
            m_lastAnchor = m_anchors[^1];
        }
    }

    private async void Update()
    {
        if (OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger))
        {
            CreateSpatialAnchor();
        }

        #region Saving Anchors
        if (OVRInput.GetDown(OVRInput.Button.One))
        {
            var result = await SaveLastCreatedAnchor();
            if (result.Success)
            {
                _ = SaveUuidToPlayerPrefs(m_lastAnchor.Uuid);
            }
        }
        #endregion

        #region Unsaving Anchors
        if (OVRInput.GetDown(OVRInput.Button.Two))
        {
            var result = await UnsaveLastCreatedAnchor();
            if (result) RemoveLastUuidFromPlayerPrefs();
        }

        if (OVRInput.GetDown(OVRInput.Button.PrimaryThumbstick))
        {
            _ = await UnsaveAllAnchors();
        }

        #endregion
    }

    private void CreateSpatialAnchor()
    {
        var controllerPosition = OVRInput.GetLocalControllerPosition(OVRInput.Controller.RTouch);
        var controllerRotation = OVRInput.GetLocalControllerRotation(OVRInput.Controller.RTouch);

        var anchor = Instantiate(AnchorPrefab, controllerPosition, controllerRotation);

        var textComponents = anchor.GetComponentsInChildren<TextMeshProUGUI>();
        if (textComponents.Length > 1)
        {
            m_uuidText = textComponents[0];
            m_statusText = textComponents[1];
        }
        else
        {
            Debug.LogWarning("TextMeshProUGUI components for UUID and status are missing in the prefab.");
        }

        _ = StartCoroutine(AnchorCreated(anchor));
    }

    private IEnumerator AnchorCreated(OVRSpatialAnchor anchor)
    {
        while (!anchor.Created && !anchor.Localized)
        {
            yield return new WaitForEndOfFrame();
            if (Debugging) Debug.Log($"Waiting for anchor to be created: {anchor.Uuid}");
        }

        var uuid = anchor.Uuid;
        m_anchors.Add(anchor);
        m_lastAnchor = anchor;

        m_uuidText.text = $"UUID: {uuid}";
        m_statusText.text = $"Not Saved";

        if (Debugging) Debug.Log($"Anchor created: {uuid}");
    }

    private async OVRTask<OVRResult<OVRAnchor.SaveResult>> SaveLastCreatedAnchor()
    {
        if (IsSaving)
        {
            if (Debugging) Debug.Log("Save already in progress. Skipping.");
            return OVRResult<OVRAnchor.SaveResult>.FromFailure(OVRAnchor.SaveResult.Failure);
        }

        IsSaving = true;

        var textComponents = m_lastAnchor.GetComponentsInChildren<TextMeshProUGUI>();
        var result = await m_lastAnchor.SaveAnchorAsync();

        if (result.Success)
        {
            if (textComponents.Length > 1)
            {
                var saved = SaveUuidToPlayerPrefs(m_lastAnchor.Uuid);
                if (saved)
                {
                    var statusText = textComponents[1];
                    statusText.text = $"Saved";
                    if (Debugging) Debug.Log($"Saved anchor: {m_lastAnchor.Uuid}");
                }
            }
        }
        else if (Debugging)
        {
            Debug.Log($"Failed to save anchor: {result.Status}");
        }

        IsSaving = false;
        return result;
    }

    private bool SaveUuidToPlayerPrefs(Guid uuid)
    {
        if (m_lastAnchor == null)
        {
            Debug.LogError("SaveUuidToPlayerPrefs failed: m_lastAnchor is null.");
            return false;
        }

        try
        {
            // Ensure PlayerPrefNumUuids exists
            var playerNumUuids = PlayerPrefs.GetInt("numUuids", 0);

            // Save the UUID
            var key = $"uuid{playerNumUuids}";
            PlayerPrefs.SetString(key, uuid.ToString());
            PlayerPrefs.SetInt("numUuids", playerNumUuids + 1);
            PlayerPrefs.Save();

            if (Debugging) Debug.Log($"Saved UUID: {uuid} to PlayerPrefs with key: {key}");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to save UUID to PlayerPrefs: {e.Message}");
            return false;
        }
    }

    private async OVRTask<OVRResult<OVRAnchor.EraseResult>> UnsaveLastCreatedAnchor()
    {
        var result = await m_lastAnchor.EraseAnchorAsync();
        var textComponetns = m_lastAnchor.GetComponentsInChildren<TextMeshProUGUI>();

        if (result.Success)
        {
            var statusText = textComponetns[1];
            statusText.text = $"Not Saved";
            if (Debugging) Debug.Log($"Unsaved anchor: {m_lastAnchor.Uuid}");
        }
        else if (!result.Success && Debugging)
        {
            Debug.Log($"Failed to unsave anchor: {result.Status}");
        }

        return result;
    }

    private async OVRTask<bool> UnsaveAllAnchors()
    {
        foreach (var anchor in m_anchors)
        {
            var textComponetns = anchor.GetComponentsInChildren<TextMeshProUGUI>();
            var result = await anchor.EraseAnchorAsync();
            if (result.Success)
            {
                var statusText = textComponetns[1];
                statusText.text = $"Not Saved";
                if (Debugging) Debug.Log($"Unsaved anchor: {anchor.Uuid}");
            }
            else if (!result.Success && Debugging)
            {
                Debug.Log($"Failed to unsave anchor: {result.Status}");
            }
        }

        m_anchors.Clear();
        DeleteNumUuisFromPlayerPrefs();

        return true;
    }

    private void DeleteNumUuisFromPlayerPrefs()
    {
        if (PlayerPrefs.HasKey("numUuids"))
        {
            var playerNumUuids = PlayerPrefs.GetInt("numUuids");
            for (var i = 0; i < playerNumUuids; i++)
            {
                var key = $"uuid{i}";
                PlayerPrefs.DeleteKey(key);
                if (Debugging) Debug.Log($"PlayerPrefs - Deleted UUID key: {key}");
            }
            PlayerPrefs.Save();
        }
    }

    private void RemoveLastUuidFromPlayerPrefs()
    {
        if (PlayerPrefs.HasKey("numUuids"))
        {
            var playerNumUuids = PlayerPrefs.GetInt("numUuids");

            if (playerNumUuids > 0)
            {
                var lastKey = $"uuid{playerNumUuids - 1}";

                if (PlayerPrefs.HasKey(lastKey))
                {
                    PlayerPrefs.DeleteKey(lastKey);
                    if (Debugging) Debug.Log($"PlayerPrefs - Deleted UUID key: {lastKey}");
                }

                PlayerPrefs.SetInt("numUuids", playerNumUuids - 1);
                PlayerPrefs.Save();
            }
            else
            {
                if (Debugging) Debug.LogWarning("PlayerPrefs - No UUIDs to remove.");
            }
        }
        else
        {
            if (Debugging) Debug.LogWarning("PlayerPrefs - numUuids key does not exist.");
        }
    }
}