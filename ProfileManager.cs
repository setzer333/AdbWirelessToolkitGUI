using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace AdbWirelessToolkitGUI;

public class DeviceProfile
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("hostPort")]
    public string HostPort { get; set; } = "";

    [JsonPropertyName("pairingCode")]
    public string PairingCode { get; set; } = "";

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [JsonPropertyName("lastUsed")]
    public DateTime LastUsed { get; set; } = DateTime.Now;

    public string DisplayName => string.IsNullOrWhiteSpace(Name) 
        ? $"{HostPort} ({CreatedAt:yyyy-MM-dd})" 
        : $"{Name} ({HostPort})";
}

public static class ProfileManager
{
    private const int MaxProfiles = 10;
    private static readonly string ProfilesFilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "AdbWirelessToolkitGUI",
        "profiles.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public static ObservableCollection<DeviceProfile> Profiles { get; private set; } = new();

    public static async Task InitializeAsync()
    {
        await LoadProfilesAsync();
    }

    public static async Task LoadProfilesAsync()
    {
        try
        {
            var directory = Path.GetDirectoryName(ProfilesFilePath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory!);
            }

            if (File.Exists(ProfilesFilePath))
            {
                var json = await File.ReadAllTextAsync(ProfilesFilePath);
                var profiles = JsonSerializer.Deserialize<List<DeviceProfile>>(json, JsonOptions);
                if (profiles != null)
                {
                    Profiles.Clear();
                    foreach (var p in profiles.OrderByDescending(x => x.LastUsed))
                    {
                        Profiles.Add(p);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            // Silently fail - profiles are optional
            System.Diagnostics.Debug.WriteLine($"[ProfileManager] Error loading profiles: {ex.Message}");
        }
    }

    public static async Task SaveProfilesAsync()
    {
        try
        {
            var directory = Path.GetDirectoryName(ProfilesFilePath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory!);
            }

            var json = JsonSerializer.Serialize(Profiles.ToList(), JsonOptions);
            await File.WriteAllTextAsync(ProfilesFilePath, json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ProfileManager] Error saving profiles: {ex.Message}");
        }
    }

    public static async Task AddOrUpdateProfileAsync(string hostPort, string pairingCode, string? customName = null)
    {
        // Normalize inputs
        hostPort = hostPort.Trim();
        pairingCode = pairingCode.Trim();

        // Check if profile already exists (same host:port)
        var existing = Profiles.FirstOrDefault(p => p.HostPort.Equals(hostPort, StringComparison.OrdinalIgnoreCase));
        
        if (existing != null)
        {
            // Update existing
            existing.PairingCode = pairingCode;
            existing.LastUsed = DateTime.Now;
            if (!string.IsNullOrWhiteSpace(customName))
                existing.Name = customName.Trim();
        }
        else
        {
            // Check if we need to make room
            if (Profiles.Count >= MaxProfiles)
            {
                // Remove oldest (last in list since ordered by LastUsed desc)
                Profiles.RemoveAt(Profiles.Count - 1);
            }

            // Add new
            var newProfile = new DeviceProfile
            {
                Name = string.IsNullOrWhiteSpace(customName) ? "" : customName.Trim(),
                HostPort = hostPort,
                PairingCode = pairingCode,
                CreatedAt = DateTime.Now,
                LastUsed = DateTime.Now
            };
            Profiles.Insert(0, newProfile);
        }

        await SaveProfilesAsync();
    }

    public static async Task<bool> RemoveProfileAsync(DeviceProfile profile)
    {
        var removed = Profiles.Remove(profile);
        if (removed)
        {
            await SaveProfilesAsync();
        }
        return removed;
    }

    public static async Task ClearAllProfilesAsync()
    {
        Profiles.Clear();
        await SaveProfilesAsync();
    }

    public static string GetProfilesFilePath() => ProfilesFilePath;

    public static int MaxProfileCount => MaxProfiles;
    public static int CurrentCount => Profiles.Count;
    public static bool IsFull => Profiles.Count >= MaxProfiles;
}