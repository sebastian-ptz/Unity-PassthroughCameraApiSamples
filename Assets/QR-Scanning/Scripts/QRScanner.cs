using PassthroughCameraSamples;
using System.Collections;
using UnityEngine;
using System;
using ZXing;
using ZXing.Windows.Compatibility;

public class QRScanner : MonoBehaviour
{
    [SerializeField] private WebCamTextureManager m_webCamTextureManager;
    private IBarcodeReader m_barcodeReader;
    private bool m_isScanning;

    private void Start()
    {
        // Initialize the barcode reader
        m_barcodeReader = new BarcodeReader();
        m_barcodeReader.Options.TryHarder = false;
        m_barcodeReader.Options.PossibleFormats = new BarcodeFormat[] { BarcodeFormat.QR_CODE };
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.O) && !m_isScanning && m_webCamTextureManager.WebCamTexture.didUpdateThisFrame)
        {
            m_isScanning = true;
            _ = StartCoroutine(ScanQRCode());
        }
        else if (!m_webCamTextureManager.WebCamTexture.didUpdateThisFrame)
        {
            Debug.Log("WebCamTexture not ready yet.");
        }
        else if (m_isScanning)
        {
            Debug.Log("Scanning in progress...");
        }
    }

    private IEnumerator ScanQRCode()
    {
        // Create a Texture2D from the WebCamTexture
        var texture = new Texture2D(m_webCamTextureManager.WebCamTexture.width, m_webCamTextureManager.WebCamTexture.height);
        var pixels = m_webCamTextureManager.WebCamTexture.GetPixels32();
        texture.SetPixels32(pixels);
        texture.Apply();

        // Convert Color32[] to grayscale byte[]
        var luminanceBytes = new byte[pixels.Length];
        for (int i = 0; i < pixels.Length; i++)
        {
            var color = pixels[i];
            // Calculate luminance using the formula: 0.299 * R + 0.587 * G + 0.114 * B
            luminanceBytes[i] = (byte)(0.299f * color.r + 0.587f * color.g + 0.114f * color.b);
        }

        // Create a luminance source from the byte array
        var luminanceSource = new RGBLuminanceSource(luminanceBytes, texture.width, texture.height, RGBLuminanceSource.BitmapFormat.Gray8);

        // Decode the QR code using ZXing
        try
        {
            var result = m_barcodeReader.Decode(luminanceSource);
            if (result != null)
            {
                Debug.Log("QR Code Detected: " + result.Text);
                // You can also trigger any UI or event here when a QR code is detected.
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning("QR code scanning failed: " + e.Message);
        }

        // Clean up
        Destroy(texture);
        m_isScanning = false;
        yield return new WaitForSeconds(0.5f); // Delay to avoid multiple scans in a single frame
        StopCoroutine(ScanQRCode());
    }
}
