using IntegrationTestingSDK.Contracts;
using IntegrationTestingSDK.ModAPI.Interfaces;
using IntegrationTestingSDK.ModAPI.Types;

namespace IntegrationTestingSDK.ModAPI.Proxies
{
    internal class TextPanelProxy : FunctionalBlockProxy, IMyTextPanel
    {
        // IMyTextSurface members (DisplayName inherited from TerminalBlockProxy)
        public Vector2 SurfaceSize
            => new Vector2(0f, 0f); // not directly available as a terminal property

        public float FontSize
        {
            get => float.TryParse(GetProperty("FontSize"), out var v) ? v : 0f;
            set => SetProperty("FontSize", value.ToString("G"));
        }

        public string Font
        {
            get => GetProperty("Font");
            set => SetProperty("Font", value);
        }

        public Color FontColor
        {
            get => Color.TryParse(GetProperty("FontColor"), out var c) ? c : Color.White;
            set => SetProperty("FontColor", value.ToPackedString());
        }

        public Color BackgroundColor
        {
            get => Color.TryParse(GetProperty("BackgroundColor"), out var c) ? c : Color.Black;
            set => SetProperty("BackgroundColor", value.ToPackedString());
        }

        public void WriteText(string text, bool append = false)
        {
            SetProperty("Text", text);
            ExecuteAction("WriteText");
        }

        public string GetText()
            => GetProperty("Text");

        internal TextPanelProxy(IPbTestHarness harness, long gridId, BlockDto block)
            : base(harness, gridId, block) { }
    }
}
