using System;
using System.Linq;

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
        public bool UseEntireInventory { get; set; } = false;

        // Raw strings from config
        private string _whitelist = "";
        private string _blacklist = "";

        // Parsed arrays for efficient lookup
        private string[] _whitelistPatterns = Array.Empty<string>();
        private string[] _blacklistPatterns = Array.Empty<string>();

        public string Whitelist
        {
            get => _whitelist;
            set
            {
                _whitelist = value;
                _whitelistPatterns = ParsePatternList(value);
            }
        }

        public string Blacklist
        {
            get => _blacklist;
            set
            {
                _blacklist = value;
                _blacklistPatterns = ParsePatternList(value);
            }
        }

        public bool WhitelistOnly { get; set; } = false;

        // Public accessors for the parsed arrays
        public string[] WhitelistPatterns => _whitelistPatterns;
        public string[] BlacklistPatterns => _blacklistPatterns;

        // Parses a comma-separated pattern list into a trimmed array
        private string[] ParsePatternList(string patternList)
        {
            if (string.IsNullOrEmpty(patternList))
                return Array.Empty<string>();

            return patternList
                .Split(',')
                .Select(p => p.Trim())
                .Where(p => !string.IsNullOrEmpty(p))
                .ToArray();
        }
    }
}