using System;

namespace TexturedBuilding
{
    public class TexturedBuildingSettings
    {
        public bool AllowFood { get; set; } = false;
        public bool AllowPlants { get; set; } = false;
        public bool AllowBlockEntities { get; set; } = false;
        public bool AllowLiquids { get; set; } = false;
        public bool AllowClay { get; set; } = false;
        public bool DebugMode { get; set; } = false;

        public String Whitelist { get; set; } = "";
        public bool WhitelistOnly { get; set; } = false;

        public String Blacklist { get; set; } = "";
    }
}