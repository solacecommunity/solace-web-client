using Blazored.Toast.Services;
using Blazorise;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace SolaceWebClient.Services
{
    public class PresetGroupModel
    {
        public string GroupName { get; set; }
        public List<PresetModel> Presets { get; set; } = new List<PresetModel>(); // Initialisiere die Liste
    }

    public class PresetModel
    {
        public string Name { get; set; }
        public string Host { get; set; }
        public string VpnName { get; set; }
        public string Username { get; set; }
        public string QueueName { get; set; }
        public string Topic { get; set; }
        public string SempUrl { get; set; }
        public string sempUsername { get; set; }
    }

    public class UngroupedPresetModel : PresetModel
    {

    }


    public class PresetService
    {
        private readonly string presetsFilePath = Path.Combine("presets", "presets.json");

        public async Task<List<PresetGroupModel>> GetPresetGroupsAsync()
        {
            if (!File.Exists(presetsFilePath))
            {
                return new List<PresetGroupModel>();
            }

            var json = await File.ReadAllTextAsync(presetsFilePath);
            var allPresets = JsonSerializer.Deserialize<List<JsonElement>>(json);

            var groupedPresets = new List<PresetGroupModel>();
            var ungroupedPresets = new List<PresetModel>();

            foreach (var item in allPresets)
            {
                if (item.TryGetProperty("Presets", out _))
                {
                    var group = JsonSerializer.Deserialize<PresetGroupModel>(item.GetRawText());
                    groupedPresets.Add(group);
                }
                else if (item.TryGetProperty("Name", out _))
                {
                    var preset = JsonSerializer.Deserialize<PresetModel>(item.GetRawText());
                    ungroupedPresets.Add(preset);
                }
            }
            return groupedPresets;
        }

        public async Task<List<PresetModel>> GetUngroupedPresetsAsync()
        {
            if (!File.Exists(presetsFilePath))
            {
                return new List<PresetModel>();
            }

            var json = await File.ReadAllTextAsync(presetsFilePath);
            var allPresets = JsonSerializer.Deserialize<List<JsonElement>>(json);

            var ungroupedPresets = new List<PresetModel>();

            foreach (var item in allPresets)
            {
                if (item.TryGetProperty("Presets", out _))
                {
                    continue;
                }

                if (item.TryGetProperty("Name", out _))
                {
                    var preset = JsonSerializer.Deserialize<PresetModel>(item.GetRawText());
                    ungroupedPresets.Add(preset);
                }
            }

            return ungroupedPresets;
        }
    }

}
