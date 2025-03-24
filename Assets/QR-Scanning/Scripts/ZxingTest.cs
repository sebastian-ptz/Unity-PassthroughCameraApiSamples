using UnityEngine;
using ZXing;
using ZXing.Windows.Compatibility;


public class ZXingTest : MonoBehaviour
{
    [SerializeField] private Texture2D m_texture; // Ensure this is a Texture2D
    private IBarcodeReader m_barcodeReader;

    private void Start()
    {
        // Initialize the barcode reader
        m_barcodeReader = new BarcodeReader();
        m_barcodeReader.Options.TryHarder = true;
        m_barcodeReader.Options.PossibleFormats = new BarcodeFormat[] { BarcodeFormat.QR_CODE };

        // Ensure the texture is readable
        if (m_texture == null)
        {
            Debug.LogError("No texture assigned.");
            return;
        }

        if (!m_texture.isReadable)
        {
            Debug.LogError("The provided texture is not readable. Please enable 'Read/Write Enabled' in the texture import settings.");
            return;
        }

        // Get the Color32 array from the texture
        var pixels = m_texture.GetPixels32();
        Debug.Log("Pixels length: " + pixels.Length);

        // Convert Color32[] to grayscale byte[]
        var luminanceBytes = new byte[pixels.Length];
        for (int i = 0; i < pixels.Length; i++)
        {
            var color = pixels[i];
            // Calculate luminance using the formula: 0.299 * R + 0.587 * G + 0.114 * B
            luminanceBytes[i] = (byte)(0.299f * color.r + 0.587f * color.g + 0.114f * color.b);
        }

        // Create a luminance source from the byte array
        var luminanceSource = new RGBLuminanceSource(luminanceBytes, m_texture.width, m_texture.height, RGBLuminanceSource.BitmapFormat.Gray8);

        // Decode the QR code using ZXing
        var result = m_barcodeReader.Decode(luminanceSource);

        // Check if a result was found
        if (result != null)
        {
            Debug.Log("ZXing.Net is correctly installed. Decoded text: " + result.Text);
        }
        else
        {
            Debug.Log("ZXing.Net is correctly installed, but no QR code was detected.");
        }
    }
}
