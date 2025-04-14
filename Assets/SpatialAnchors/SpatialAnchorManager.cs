using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using TMPro;
using System;
using System.Threading.Tasks;

public class SpatialAnchorManager : MonoBehaviour
{
    public OVRSpatialAnchor AnchorPrefab;
    public const string NUMUUIDPLAYERPREF = "numUuids";

    private Canvas m_canvas;
    private TextMeshProUGUI m_uuidText;
    private TextMeshProUGUI m_statusText;
    private OVRSpatialAnchor m_lastAnchor;
    //private OVRAnchorLoader anchorLoader;
    private List<OVRSpatialAnchor> m_anchors = new();

    //private void Awake() => anchorLoader = GetComponent<AnchorLoader>();

    private async void Update()
    {
        if (OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger))
        {
            CreateSpatialAnchor();
        }

        // Manage Saving Anchor
        if (OVRInput.GetDown(OVRInput.Button.One))
        {
            var result = await SaveLastCreatedAnchor();
            if (result)
            {
                m_statusText.text = $"Saved";
                _ = await SaveUuidToPlayerPrefs(m_lastAnchor.Uuid);
            }
            else
            {
                m_statusText.text = $"Failed to save";
            }
        }

        #region Unsaving Anchors
        if (OVRInput.GetDown(OVRInput.Button.Two))
        {
            var result = await UnsaveLastCreatedAnchor();
            if (result.Success)
            {
                m_statusText.text = $"Deleted";
                _ = new WaitForSeconds(1.5f);
                m_statusText.text = $"Not Saved";
            }
            else
            {
                m_statusText.text = $"Failed to delete";
                _ = new WaitForSeconds(1.5f);
                m_statusText.text = $"Saved";
            }
        }

        if (OVRInput.GetDown(OVRInput.Button.PrimaryThumbstick))
        {
            var result = await UnsaveAllAnchors();
            m_statusText.text = $"Deleted All";
            _ = new WaitForSeconds(1.5f);
            m_statusText.text = $"Not Saved";
        }

        #endregion
    }

    private void CreateSpatialAnchor()
    {
        var anchor = Instantiate(AnchorPrefab, transform.position, Quaternion.identity);
        m_uuidText = m_canvas.gameObject.transform.GetComponentInChildren<TextMeshProUGUI>();
        m_statusText = m_canvas.gameObject.GetComponentInChildren<TextMeshProUGUI>();

        _ = StartCoroutine(AnchorCreated(anchor));
    }

    private IEnumerator AnchorCreated(OVRSpatialAnchor anchor)
    {
        while (!anchor.Created && !anchor.Localized)
        {
            yield return new WaitForEndOfFrame();
        }

        var uuid = anchor.Uuid;
        m_anchors.Add(anchor);
        m_lastAnchor = anchor;

        m_uuidText.text = $"UUID: {uuid}";
        m_statusText.text = $"Not Saved";
    }

    private async Task<bool> SaveLastCreatedAnchor()
    {
        var result = await m_lastAnchor.SaveAnchorAsync();
        return result.Success;
    }

    private async Task<bool> SaveUuidToPlayerPrefs(Guid uuid)
    {
        if (m_lastAnchor != null && !PlayerPrefs.HasKey(NUMUUIDPLAYERPREF))
        {
            await Task.Run(() =>
            {
                PlayerPrefs.SetInt(NUMUUIDPLAYERPREF, 0);
                var playerNumUuids = PlayerPrefs.GetInt(NUMUUIDPLAYERPREF);
                PlayerPrefs.SetString("uuid" + playerNumUuids, uuid.ToString());
                PlayerPrefs.SetInt(NUMUUIDPLAYERPREF, ++playerNumUuids);
            });
            return true; // Task succeeded
        }
        else
        {
            return false; // Task failed
        }
    }

    private async OVRTask<OVRResult<OVRAnchor.EraseResult>> UnsaveLastCreatedAnchor()
    {
        var result = await m_lastAnchor.EraseAnchorAsync();
        return result.Success
            ? OVRResult<OVRAnchor.EraseResult>.FromSuccess(OVRAnchor.EraseResult.Success)
            : OVRResult<OVRAnchor.EraseResult>.FromFailure(OVRAnchor.EraseResult.Failure);
    }

    private async OVRTask<bool> UnsaveAllAnchors()
    {
        foreach (var anchor in m_anchors)
        {
            _ = await UnsaveAnchor(anchor);
        }

        m_anchors.Clear();
        ClearAllUuidsFromPlayerPrefs();
        return true;
    }


    private async OVRTask<bool> UnsaveAnchor(OVRSpatialAnchor anchor)
    {
        var result = await anchor.EraseAnchorAsync();
        if (result.Success)
        {
            var textComponetns = anchor.GetComponentsInChildren<TextMeshProUGUI>();
            if (textComponetns.Length > 1)
            {
                var statusText = textComponetns[1];
                statusText.text = $"Not Saved";
            }
            return true;
        }
        // if not successful
        return false;
    }

    private void ClearAllUuidsFromPlayerPrefs()
    {
        if (PlayerPrefs.HasKey(NUMUUIDPLAYERPREF))
        {
            var playerNumUuids = PlayerPrefs.GetInt(NUMUUIDPLAYERPREF);
            for (var i = 0; i < playerNumUuids; i++)
            {
                PlayerPrefs.DeleteKey("uuid" + i);
            }
            PlayerPrefs.DeleteKey(NUMUUIDPLAYERPREF);
            PlayerPrefs.Save();
        }
    }
}