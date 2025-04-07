using PassthroughCameraSamples;
using System.Collections;
using UnityEngine.UI;
using UnityEngine;
using System;
using ZXing;
using ZXing.Windows.Compatibility;

public class PCAScanner : MonoBehaviour
{
    public static PCAScanner Instance { get; private set; }

    [SerializeField] private WebCamTextureManager m_webCamTextureManager;
    private IBarcodeReader m_barcodeReader;
    private bool m_isScanning;

    [Header("Scanning")]
    [SerializeField] private string m_scanResult;
    [SerializeField] private Text m_textDisplay;
    [SerializeField] private bool m_showResult = false;

    public event Action<Result> OnQRCodeScanned;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private IEnumerator Start()
    {
        while (m_webCamTextureManager.WebCamTexture == null)
        {
            yield return null;
            Debug.Log($"Loading WebCamTexture... [{Time.realtimeSinceStartup} s]");
            m_textDisplay.text = $"Loading WebCamTexture... [{Time.realtimeSinceStartup} s]";
        }
        Debug.Log("WebCamTexture loaded!");

        // Initialize the barcode reader
        m_barcodeReader = new BarcodeReader();
        m_barcodeReader.Options.TryHarder = false;
        m_barcodeReader.Options.PossibleFormats = new BarcodeFormat[] { BarcodeFormat.QR_CODE };
    }

    private void Update()
    {
        #region Input Logic
        if (OVRInput.GetDown(OVRInput.Button.Two)) m_showResult = !m_showResult; // Toggle result display
        if ((Input.GetKeyDown(KeyCode.S) || OVRInput.GetDown(OVRInput.Button.One))
            && !m_isScanning)
        {
            m_isScanning = true;
            Debug.Log("Scanning...");
            m_textDisplay.text = "Scanning...";
            _ = StartCoroutine(ScanQRCode());
        }

        if (m_showResult)
        {
            m_textDisplay.text = m_scanResult;
        }
        #endregion
    }

    #region Scanning
    /// <summary>
    /// Scans the WebCamTexture for a QR code and decodes it using ZXing.
    /// </summary>
    /// <returns>An IEnumerator for coroutine management.</returns>
    private IEnumerator ScanQRCode()
    {
        yield return new WaitForSeconds(0.5f); // delay

        var texture = new Texture2D(m_webCamTextureManager.WebCamTexture.width, m_webCamTextureManager.WebCamTexture.height);
        var pixels = m_webCamTextureManager.WebCamTexture.GetPixels32();
        texture.SetPixels32(pixels);
        texture.Apply();

        var luminanceBytes = new byte[pixels.Length];
        for (var i = 0; i < pixels.Length; i++)
        {
            var color = pixels[i];
            // Lumanacne formula: 0.299 * R + 0.587 * G + 0.114 * B
            luminanceBytes[i] = (byte)(0.299f * color.r + 0.587f * color.g + 0.114f * color.b);
        }

        // Create a luminance source from byte array
        var luminanceSource = new RGBLuminanceSource(luminanceBytes, texture.width, texture.height, RGBLuminanceSource.BitmapFormat.Gray8);

        try // to decode the QR code
        {
            var result = m_barcodeReader.Decode(luminanceSource);
            if (result != null)
            {
                m_textDisplay.text = "QR Code: " + m_scanResult;
                Debug.Log("QR Code: " + m_scanResult);
                OnQRCodeScanned?.Invoke(result);
                m_scanResult = result.Text;
            }
            else
            {
                m_textDisplay.text = "No QR code found.";
                Debug.Log("No QR code found.");
            }
        }
        catch (Exception e)
        {
            m_textDisplay.text = "Scanning failed: " + e.Message;
            Debug.Log("Scanning failed: " + e.Message);
        }

        // Stop scan and clean up
        yield return new WaitForSeconds(0.5f); // avoid multiscan
        Destroy(texture);
        m_isScanning = false;
        StopCoroutine(ScanQRCode());
    }
    #endregion
}
