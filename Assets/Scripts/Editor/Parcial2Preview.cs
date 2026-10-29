using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Captura una vista previa de la escena del Parcial 2 sin entrar en Play.
/// Se usa para revisar el mapa y las zonas desde el agente.
/// Menu: Tools > Parcial 2 > Capture Preview.
/// </summary>
public static class Parcial2Preview
{
    private const string ScenePath = "Assets/Scenes/Parcial2.unity";
    private const string OutputPath = "C:/Users/choko/AppData/Local/Temp/opencode/parcial2_preview.png";

    [MenuItem("Tools/Parcial 2/Capture Preview")]
    public static void CapturePreview()
    {
        Scene scene = SceneManager.GetActiveScene();

        if (scene.path != ScenePath)
            scene = EditorSceneManager.OpenScene(ScenePath);

        Camera camera = Camera.main;

        if (camera == null)
        {
            Debug.LogError("[Parcial 2] No hay Main Camera en la escena.");
            return;
        }

        const int width = 1600;
        const int height = 900;

        RenderTexture target = new(width, height, 24, RenderTextureFormat.ARGB32);
        RenderTexture previous = RenderTexture.active;

        camera.targetTexture = target;
        camera.Render();

        RenderTexture.active = target;
        Texture2D texture = new(width, height, TextureFormat.RGB24, false);
        texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        texture.Apply();

        RenderTexture.active = previous;
        camera.targetTexture = null;

        File.WriteAllBytes(OutputPath, texture.EncodeToPNG());

        Object.DestroyImmediate(texture);
        target.Release();
        Object.DestroyImmediate(target);

        Debug.Log("[Parcial 2] Preview guardada en " + OutputPath);
    }
}
