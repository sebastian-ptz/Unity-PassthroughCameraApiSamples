using UnityEngine.XR.ARFoundation;
using UnityEngine;
using ZXing;

public class SpawnOnScan : MonoBehaviour
{
    [Header("3D Conversion")]
    public Camera XrCamera; // CenterEyeAnchor
    private AROcclusionManager m_arOcclusionManager;
    [SerializeField] private Vector2 m_screenPosition;
    [SerializeField] private Vector3 m_worldPosition;
    [SerializeField] private float m_depth;

    [Header("Spawn Prefabs")]
    public GameObject CubePrefab;

    private void Start()
    {
        // Ensure PCAScanner instance is created
        if (PCAScanner.Instance == null)
        {
            var scannerObject = new GameObject("PCAScanner");
            _ = scannerObject.AddComponent<PCAScanner>();
        }

        // Subscribe to the QR code scanned event
        PCAScanner.Instance.OnQRCodeScanned += SpawnOnResult;

        m_arOcclusionManager = FindObjectOfType<AROcclusionManager>();
    }

    private void OnDestroy()
    {
        // Unsubscribe from the event
        if (PCAScanner.Instance != null)
        {
            PCAScanner.Instance.OnQRCodeScanned -= SpawnOnResult;
        }
    }

    #region Spawn Logic
    /// <summary>
    /// For Specific results spawns a Prefabs at the detected position.
    /// </summary>
    /// <param name="qrResult">The result of the QR code scan.</param>
    private void SpawnOnResult(Result qrResult)
    {
        if (qrResult == null)
        {
            Debug.LogError("QR code result is null.");
            return;
        }
        else
        {
            m_screenPosition = GetQRCodeScreenPosition(qrResult);
            m_depth = GetDepthAtScreenPosition(m_screenPosition);
            m_worldPosition = ScreenToWorld(m_screenPosition, m_depth);
        }

        switch (qrResult.Text)
        {
            case "cube":
                _ = Instantiate(CubePrefab, m_worldPosition, Quaternion.identity);
                break;

            default:
                Debug.Log($"QR code scanned: {qrResult.Text}");
                break;
        }
    }
    #endregion

    #region Screen to World Conversion
    /// <summary>Gets the screen position of the QR code.</summary>
    /// <param name="qrResult">The result of the QR code scan.</param>
    /// <returns>[Vector2] Screen position. Default: Vector2.zero</returns>
    private Vector2 GetQRCodeScreenPosition(Result qrResult)
    {
        var points = qrResult.ResultPoints;
        return points.Length > 0 ? new Vector2(points[0].X, points[0].Y) : Vector2.zero;
    }

    /// <summary>
    /// Gets the depth value at the specified screen position.
    /// </summary>
    /// <param name="screenPosition">The screen position to get the depth for.</param>
    /// <returns>[float] Depth value at the specified screen position. Default: 1.5 meters</returns>
    private float GetDepthAtScreenPosition(Vector2 screenPosition)
    {
        if (m_arOcclusionManager != null)
        {
            var depthTexture = m_arOcclusionManager.environmentDepthTexture;
            if (depthTexture != null)
            {
                // Convert screen position to texture coordinates
                var texCoords = new Vector2(screenPosition.x / Screen.width, screenPosition.y / Screen.height);
                var depth = SampleDepthTexture(depthTexture, texCoords);
                return depth;
            }
        }
        return 1.5f; // Default depth estimation (1.5 meters)
    }

    /// <summary>
    /// Samples a depth texture at the given texture coordinates.
    /// </summary>
    /// <param name="depthTexture">The depth texture to sample from.</param>
    /// <param name="texCoords">The texture coordinates to sample at.</param>
    /// <returns>Depth value at the specified texture coordinates.</returns>
    private float SampleDepthTexture(Texture2D depthTexture, Vector2 texCoords)
    {
        // Sample the depth texture at the given texture coordinates
        var pixelX = (int)(texCoords.x * depthTexture.width);
        var pixelY = (int)(texCoords.y * depthTexture.height);
        var depthColor = depthTexture.GetPixel(pixelX, pixelY);
        return depthColor.r; // Assuming the depth is stored in the red channel
    }

    /// <summary>
    /// Converts a 2D screen position to a 3D world position by using depth.
    /// </summary>
    /// <param name="screenPosition">The 2D screen position.</param>
    /// <param name="depth">The depth value at the screen position.</param>
    /// <returns>[Vector3] corresponding 3D world position.</returns>
    private Vector3 ScreenToWorld(Vector2 screenPosition, float depth)
    {
        var ray = XrCamera.ScreenPointToRay(screenPosition);
        return ray.GetPoint(depth); // Converts 2D screen position to 3D using depth
    }
    #endregion
}