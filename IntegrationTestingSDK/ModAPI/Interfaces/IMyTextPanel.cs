using IntegrationTestingSDK.ModAPI.Types;

namespace IntegrationTestingSDK.ModAPI.Interfaces
{
    /// <summary>
    ///     Interface for text panel blocks.
    ///     Combines IMyFunctionalBlock members with IMyTextSurface members.
    /// </summary>
    public interface IMyTextPanel : IMyFunctionalBlock
    {
        // IMyTextSurface members (DisplayName inherited from IMyEntity)
        Vector2 SurfaceSize { get; }
        float FontSize { get; set; }
        string Font { get; set; }
        Color FontColor { get; set; }
        Color BackgroundColor { get; set; }

        void WriteText(string text, bool append = false);
        string GetText();
    }
}
