using IntegrationTestingSDK.ModAPI.Types;

namespace IntegrationTestingSDK.ModAPI.Interfaces
{
    /// <summary>
    ///     Text surface interface mirroring Sandbox.ModAPI.Ingame.IMyTextSurface.
    /// </summary>
    public interface IMyTextSurface
    {
        string DisplayName { get; }
        Vector2 SurfaceSize { get; }
        Vector2 TextureSize { get; }
        float FontSize { get; set; }
        string Font { get; set; }
        Color FontColor { get; set; }
        Color BackgroundColor { get; set; }
        Color ScriptForegroundColor { get; set; }
        Color ScriptBackgroundColor { get; set; }
        float TextPadding { get; set; }
        string Script { get; set; }
        bool PreserveAspectRatio { get; set; }

        void WriteText(string text, bool append = false);
        string GetText();
        void ClearImagesFromSelection();
        void AddImageToSelection(string id, bool checkExistence = false);
    }
}
