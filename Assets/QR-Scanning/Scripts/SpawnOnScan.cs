using UnityEngine;
using ZXing;
using ZXing.Windows.Compatibility;
using Meta.XR;

public class SpawnOnScan : MonoBehaviour
{
    public Camera xrCamera;  // Assign the Quest 3 camera in the inspector
    public GameObject qrMarkerPrefab; // Prefab to visualize the QR code position

    void Start()
    {
        // Ensure PCAScanner instance is created
        if (PCAScanner.Instance == null)
        {
            GameObject scannerObject = new GameObject("PCAScanner");
            scannerObject.AddComponent<PCAScanner>();
        }

        // Subscribe to the QR code scanned event
        PCAScanner.Instance.OnQRCodeScanned += HandleQRCodeScanned;
    }

    void OnDestroy()
    {
        // Unsubscribe from the event to avoid memory leaks
        if (PCAScanner.Instance != null)
        {
            PCAScanner.Instance.OnQRCodeScanned -= HandleQRCodeScanned;
        }
    }

    private void HandleQRCodeScanned(Result qrResult)
    {
        Vector2 screenPosition = GetQRCodeScreenPosition(qrResult);
        float depth = GetDepthAtScreenPosition(screenPosition);
        Vector3 worldPosition = ScreenToWorld(screenPosition, depth);

        // Instantiate the prefab at the world position
        Instantiate(qrMarkerPrefab, worldPosition, Quaternion.identity);
    }

    Vector2 GetQRCodeScreenPosition(Result qrResult)
    {
        // Get the center of the detected QR code in screen coordinates
        var points = qrResult.ResultPoints;
        if (points.Length > 0)
        {
            return new Vector2(points[0].X, points[0].Y);
        }
        return Vector2.zero;
    }

    float GetDepthAtScreenPosition(Vector2 screenPosition)
    {
        // Placeholder: Replace with Meta XR depth API call when available
        return 1.5f; // Default depth estimation (1.5 meters)
    }

    Vector3 ScreenToWorld(Vector2 screenPosition, float depth)
    {
        Ray ray = xrCamera.ScreenPointToRay(screenPosition);
        return ray.GetPoint(depth); // Converts 2D screen position to 3D using depth
    }
}
