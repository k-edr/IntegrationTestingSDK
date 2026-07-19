using IntegrationTestingSDK.Contracts;
using IntegrationTestingSDK.ModAPI.Interfaces;
using IntegrationTestingSDK.ModAPI.Types;

namespace IntegrationTestingSDK.ModAPI.Proxies
{
    internal class TextPanelProxy : FunctionalBlockProxy, IMyTextPanel, IMyTextSurface
    {
        // IMyTextSurface members (DisplayName inherited from TerminalBlockProxy)
        public Vector2 SurfaceSize
            => new Vector2(0f, 0f); // not directly available as a terminal property

        public Vector2 TextureSize
            => new Vector2(0f, 0f); // not directly available as a terminal property

        public float FontSize
        {
            get => float.TryParse(GetProperty("FontSize"), out var v) ? v : 0f;
            set => SetProperty("FontSize", value.ToString("G"));
        }

        public string Font
        {
            get
            {
                // SE stores Font as Int64 (typeface ID), not a name
                var raw = GetProperty("Font");
                return raw ?? "";
            }
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

        public Color ScriptForegroundColor
        {
            get => Color.TryParse(GetProperty("ScriptForegroundColor"), out var c) ? c : Color.White;
            set => SetProperty("ScriptForegroundColor", value.ToPackedString());
        }

        public Color ScriptBackgroundColor
        {
            get => Color.TryParse(GetProperty("ScriptBackgroundColor"), out var c) ? c : Color.Black;
            set => SetProperty("ScriptBackgroundColor", value.ToPackedString());
        }

        public float TextPadding
        {
            get => float.TryParse(GetProperty("TextPaddingSlider"), out var v) ? v : 0f;
            set => SetProperty("TextPaddingSlider", value.ToString("G"));
        }

        public string Script
        {
            get => GetProperty("Script") ?? "";
            set => SetProperty("Script", value);
        }

        public bool PreserveAspectRatio
        {
            get => bool.TryParse(GetProperty("PreserveAspectRatio"), out var v) && v;
            set => SetProperty("PreserveAspectRatio", value.ToString());
        }

        public void WriteText(string text, bool append = false)
        {
            SetProperty("Title", text);
        }

        public string GetText()
            => GetProperty("Title");

        public void ClearImagesFromSelection()
        {
            /* Not directly supported via terminal property */
        }

        public void AddImageToSelection(string id, bool checkExistence = false)
        {
            /* Not directly supported via terminal property */
        }

        internal TextPanelProxy(IPbTestHarness harness, long gridId, BlockDto block)
            : base(harness, gridId, block) { }
    }
}
