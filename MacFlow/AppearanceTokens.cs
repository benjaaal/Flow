using System;
using System.IO;
using System.Runtime.Serialization;
namespace MacFlow
{
    [DataContract] internal sealed class AppearanceTokens
    {
        [DataMember] public int Schema=1,PanelRadius=24;
        [DataMember] public string Accent;
        [DataMember] public double? GlassOpacity;
        internal static void Apply(string directory)
        {
            string file=Path.Combine(directory,"macflow.appearance.json");var tokens=ModuleStore.Read<AppearanceTokens>(file);if(!File.Exists(file))ModuleStore.Write(file,tokens);
            Design.PanelRadius=Math.Max(8,Math.Min(28,tokens.PanelRadius));if(tokens.Accent!=null&&System.Text.RegularExpressions.Regex.IsMatch(tokens.Accent,"^#[0-9A-Fa-f]{6}$"))Design.Accent=tokens.Accent;
            if(tokens.GlassOpacity.HasValue&&!Double.IsNaN(tokens.GlassOpacity.Value)&&!Double.IsInfinity(tokens.GlassOpacity.Value))Design.GlassOpacity=Math.Max(.06,Math.Min(.65,tokens.GlassOpacity.Value));
        }
    }
}
