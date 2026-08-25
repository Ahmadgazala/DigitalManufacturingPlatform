using System.Text;
using System.Text.Json;

namespace DMP.Web.Services;

public interface IWhatsAppService
{
    Task<WhatsAppGroupResult> CreateGroupAsync(string groupName, List<string> phoneNumbers);
    Task<List<WhatsAppGroup>> GetGroupsAsync();
}

public class WhatsAppService : IWhatsAppService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<WhatsAppService> _logger;

    public WhatsAppService(HttpClient httpClient, IConfiguration configuration, ILogger<WhatsAppService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<WhatsAppGroupResult> CreateGroupAsync(string groupName, List<string> phoneNumbers)
    {
        var result = new WhatsAppGroupResult { GroupName = groupName };

        try
        {
            var phoneNumberId = _configuration["WhatsApp:PhoneNumberId"];
            var accessToken = _configuration["WhatsApp:AccessToken"];

            if (string.IsNullOrEmpty(phoneNumberId) || string.IsNullOrEmpty(accessToken))
            {
                result.Success = false;
                result.ErrorMessage = "WhatsApp API credentials not configured";
                return result;
            }

            // WhatsApp Business API doesn't directly support creating groups
            // Instead, we'll create a community or use the groups feature
            // For now, we'll add participants to a new group using the API

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");

            // Create the group payload
            var payload = new
            {
                messaging_product = "whatsapp",
                subject = groupName,
                participants = phoneNumbers.Select(phone => new
                {
                    phone_number = phone,
                    role = "member"
                }).ToList()
            };

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Note: WhatsApp Business API v21+ supports group creation
            var response = await _httpClient.PostAsync(
                $"https://graph.facebook.com/v21.0/{phoneNumberId}/groups",
                content);

            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var groupResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);
                result.GroupId = groupResponse.GetProperty("id").GetString();
                result.Success = true;
                result.ParticipantsAdded = phoneNumbers.Count;
            }
            else
            {
                result.Success = false;
                result.ErrorMessage = $"API Error: {responseContent}";
                _logger.LogError("WhatsApp API error: {Error}", responseContent);
            }
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = ex.Message;
            _logger.LogError(ex, "Error creating WhatsApp group");
        }

        return result;
    }

    public async Task<List<WhatsAppGroup>> GetGroupsAsync()
    {
        var groups = new List<WhatsAppGroup>();

        try
        {
            var phoneNumberId = _configuration["WhatsApp:PhoneNumberId"];
            var accessToken = _configuration["WhatsApp:AccessToken"];

            if (string.IsNullOrEmpty(phoneNumberId) || string.IsNullOrEmpty(accessToken))
                return groups;

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");

            var response = await _httpClient.GetAsync(
                $"https://graph.facebook.com/v21.0/{phoneNumberId}/groups");

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<JsonElement>(content);

                if (result.TryGetProperty("data", out var data))
                {
                    foreach (var group in data.EnumerateArray())
                    {
                        groups.Add(new WhatsAppGroup
                        {
                            Id = group.GetProperty("id").GetString(),
                            Name = group.GetProperty("subject").GetString(),
                            ParticipantCount = group.TryGetProperty("participant_count", out var count)
                                ? count.GetInt32() : 0
                        });
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching WhatsApp groups");
        }

        return groups;
    }
}

public class WhatsAppGroupResult
{
    public bool Success { get; set; }
    public string? GroupId { get; set; }
    public string GroupName { get; set; } = string.Empty;
    public int ParticipantsAdded { get; set; }
    public string? ErrorMessage { get; set; }
}

public class WhatsAppGroup
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int ParticipantCount { get; set; }
}
