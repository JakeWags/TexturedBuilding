namespace TexturedBuilding
{
    public class TBConfig
    {
        public bool RandomModeEnabled { get; set; } = false;
        
        // Default: FALSE (Don't randomize these types)
        public bool AllowFood { get; set; } = false;
        public bool AllowPlants { get; set; } = false;
        public bool AllowBlockEntities { get; set; } = false; // Chests, signs, etc.
        public bool AllowLiquids { get; set; } = false;
        public bool AllowClay { get; set; } = false; // Clay blocks (crocks, bowls, planters, etc)

        // Debugging
        public bool DebugMode { get; set; } = false;
    }
}