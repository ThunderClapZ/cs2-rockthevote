namespace cs2_rockthevote
{
    public class Map
    {
        public string? Id { get; set; }
        public string Name { get; set; }
        public string? DisplayName { get; set; }

        public Map(string name, string? id, string? displayName = null)
        {
            Id = id?.Trim();
            Name = name.Trim().ToLower();
            DisplayName = displayName?.Trim();
        }

        public string GetDisplayName() => DisplayName ?? Name;
    }
}
