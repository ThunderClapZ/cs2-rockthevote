using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Logging;

namespace cs2_rockthevote
{
    public class MapLister : IPluginDependency<Plugin, Config>
    {
        public Map[]? Maps { get; private set; } = null;
        public bool MapsLoaded { get; private set; } = false;

        public event EventHandler<Map[]>? EventMapsLoaded;

        private Plugin? _plugin;

        public MapLister()
        {

        }

        public void Clear()
        {
            MapsLoaded = false;
            Maps = null;
        }

        public void LoadMaps()
        {
            Clear();
            string mapsFile = Path.Combine(_plugin!.ModuleDirectory, "maplist.txt");
            if (!File.Exists(mapsFile))
            {
#if DEBUG
                _plugin?.Logger.LogError($"MapLister: Maps file not found at {mapsFile}");
#endif
                throw new FileNotFoundException(mapsFile);
            }
#if DEBUG
            _plugin?.Logger.LogInformation($"MapLister: Loading maps from {mapsFile}");
#endif
            Maps = File.ReadAllText(mapsFile)
                .Replace("\r\n", "\n")
                .Split("\n")
                .Select(x => x.Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x) && !x.StartsWith("//"))
                .Select(mapLine =>
                {
                    string[] args = mapLine.Split(":");
                    return new Map(
                        args[0],
                        args.Length >= 2 && !string.IsNullOrEmpty(args[1]) ? args[1] : null,
                        args.Length >= 3 ? args[2] : null
                    );
                })
                .ToArray();

            MapsLoaded = true;
#if DEBUG
            _plugin?.Logger.LogInformation($"MapLister: Successfully loaded {Maps.Length} maps");
#endif
            if (EventMapsLoaded is not null)
                EventMapsLoaded.Invoke(this, Maps!);
        }

        public void OnMapStart(string _map)
        {
            if (_plugin is not null)
            {
#if DEBUG
                _plugin.Logger.LogInformation($"MapLister: Map started, reloading maps");
#endif
                LoadMaps();
            }
        }

        public void OnLoad(Plugin plugin)
        {
            _plugin = plugin;
            LoadMaps();
        }

        // returns "" if there's no matching
        // if there's more than one matching name, list all the matching names for players to choose
        // otherwise, returns the matching name
        // Supports searching by both Name and DisplayName (translated name)
        public string GetSingleMatchingMapName(string map, CCSPlayerController player, StringLocalizer _localizer)
        {
            // First check exact match on Name (case-insensitive)
            var exactNameMatch = this.Maps!.FirstOrDefault(x => x.Name.Equals(map, StringComparison.OrdinalIgnoreCase));
            if (exactNameMatch is not null)
                return exactNameMatch.Name;

            // Then check exact match on DisplayName (case-insensitive)
            var exactDisplayMatch = this.Maps!.FirstOrDefault(x =>
                x.DisplayName != null && x.DisplayName.Equals(map, StringComparison.OrdinalIgnoreCase));
            if (exactDisplayMatch is not null)
                return exactDisplayMatch.Name;

            // Search maps where Name or DisplayName contains the input
            var matchingMaps = this.Maps!
                .Where(x => x.Name.Contains(map, StringComparison.OrdinalIgnoreCase) ||
                           (x.DisplayName != null && x.DisplayName.Contains(map, StringComparison.OrdinalIgnoreCase)))
                .ToList();

            if (matchingMaps.Count == 0)
            {
                player!.PrintToChat(_localizer.LocalizeWithPrefix("general.invalid-map"));
                return "";
            }
            else if (matchingMaps.Count > 1)
            {
                player!.PrintToChat(_localizer.LocalizeWithPrefix("nominate.multiple-maps-containing-name"));
                // Show both display name and file name for clarity
                var mapList = matchingMaps.Select(m => m.DisplayName != null ? $"{m.GetDisplayName()} ({m.Name})" : m.Name);
                player!.PrintToChat(string.Join(", ", mapList));
                return "";
            }

            return matchingMaps[0].Name;
        }

        public IEnumerable<Map> GetMaps()
        {
            return Maps ?? Enumerable.Empty<Map>();
        }
    }
}
