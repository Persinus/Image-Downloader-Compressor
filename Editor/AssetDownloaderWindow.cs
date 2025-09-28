using UnityEngine;
using UnityEditor;
using UnityEngine.Networking;
using System.IO;
using System.Threading.Tasks;

public class ImageDownloaderCompressor : EditorWindow
{
    private string imageUrl = "";
    private string saveFolder = "Assets/DownloadedImages";
    private string toastMessage = "";
    private float toastTime = 0f;
    private const float TOAST_DURATION = 2.5f;
    private float progress = 0f;

    [MenuItem("Tools/Image Downloader & Compressor")]
    public static void ShowWindow()
    {
        GetWindow<ImageDownloaderCompressor>("Image Downloader");
    }

    private void OnGUI()
    {
        // ---------------- Mini-toast ----------------
        if (!string.IsNullOrEmpty(toastMessage))
        {
            EditorGUILayout.BeginVertical("box");
            GUILayout.Label(toastMessage, EditorStyles.wordWrappedLabel);
            EditorGUILayout.EndVertical();
        }

        GUILayout.Space(10);

        // ---------------- Guide ----------------
        EditorGUILayout.HelpBox(
            "📖 Image Downloader & Compressor\n\n" +
            "👉 Purpose:\n" +
            "- Download images from any URL and automatically compress them (JPG, quality 70%).\n" +
            "- Import images directly into Unity project.\n\n" +
            " Persinus: https://github.com/Persinus",
            MessageType.Info
        );

        GUILayout.Space(10);

        // ---------------- Folder Selection ----------------
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("Save Folder:", GUILayout.Width(80));
        saveFolder = EditorGUILayout.TextField(saveFolder);
        if (GUILayout.Button("Select Folder", GUILayout.Width(120)))
        {
            string selected = EditorUtility.OpenFolderPanel("Select Save Folder", Application.dataPath, "");
            if (!string.IsNullOrEmpty(selected)) saveFolder = selected;
        }
        EditorGUILayout.EndHorizontal();

        GUILayout.Space(10);

        // ---------------- URL Input ----------------
        GUILayout.Label("📥 Download & Compress Image", EditorStyles.boldLabel);
        imageUrl = EditorGUILayout.TextField("Image URL:", imageUrl);

        if (GUILayout.Button("Download & Import"))
        {
            if (!string.IsNullOrEmpty(imageUrl))
                _ = DownloadAndImport(imageUrl, saveFolder);
            else
                ShowToast("❌ Please enter a valid Image URL!");
        }

        // ---------------- Progress bar ----------------
        if (progress > 0f && progress < 1f)
        {
            Rect progressRect = GUILayoutUtility.GetRect(position.width - 20, 20);
            EditorGUI.ProgressBar(progressRect, progress, $"Progress: {Mathf.RoundToInt(progress * 100)}%");
        }

        RepaintToast();
    }

    private async Task DownloadAndImport(string url, string folder)
    {
        Directory.CreateDirectory(folder);
        progress = 0f;
        ShowToast("⬇️ Downloading image...");

        using (UnityWebRequest www = UnityWebRequestTexture.GetTexture(url))
        {
            var asyncOp = www.SendWebRequest();
            while (!asyncOp.isDone)
            {
                await Task.Delay(50);
                progress = asyncOp.progress * 0.5f; // 50% cho download
            }

            if (www.result != UnityWebRequest.Result.Success)
            {
                ShowToast("❌ Download Failed: " + www.error);
                progress = 0f;
                return;
            }

            Texture2D tex = DownloadHandlerTexture.GetContent(www);

            // Original size (PNG)
            byte[] originalBytes = tex.EncodeToPNG();
            long originalSizeKB = originalBytes.Length / 1024;

            // ✨ Compress to JPG quality 70%
            byte[] compressed = tex.EncodeToJPG(70);
            long compressedSizeKB = compressed.Length / 1024;

            // % reduction
            float reduction = (originalSizeKB - compressedSizeKB) / (float)originalSizeKB * 100f;

            // Save file
            string fileName = Path.GetFileNameWithoutExtension(url);
            if (string.IsNullOrEmpty(fileName)) fileName = "downloaded_image";
            string saveFile = Path.Combine(folder, fileName + ".jpg");
            File.WriteAllBytes(saveFile, compressed);

            progress = 1f;
            AssetDatabase.Refresh();

            // Mini-toast success với thông tin giảm dung lượng + tri ân
            ShowToast($"✅ Image optimized!\nOriginal: {originalSizeKB} KB, Compressed: {compressedSizeKB} KB, Reduced: {reduction:F1}%\n🎨 Thanks to Persinus");
            await Task.Delay(2000); // show toast 2s
            progress = 0f;
        }
    }

    private void ShowToast(string msg)
    {
        toastMessage = msg;
        toastTime = (float)EditorApplication.timeSinceStartup;
    }

    private void RepaintToast()
    {
        if (!string.IsNullOrEmpty(toastMessage))
        {
            if ((float)EditorApplication.timeSinceStartup - toastTime > TOAST_DURATION)
                toastMessage = "";
            Repaint();
        }
    }
}
