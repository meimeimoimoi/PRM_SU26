using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SmartDine.AI.API.Controllers;

public class QueryRequest
{
    public string Prompt { get; set; } = string.Empty;
}

public class QueryResponse
{
    public bool Success { get; set; }
    public string Answer { get; set; } = string.Empty;
}

public class IntentResult
{
    [JsonPropertyName("action")]
    public string Action { get; set; } = "general_chat";
}

public class ApiResponseWrapper<T>
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }
    [JsonPropertyName("data")]
    public T? Data { get; set; }
}

public class TableDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }
    [JsonPropertyName("number")]
    public int Number { get; set; }
    [JsonPropertyName("capacity")]
    public int Capacity { get; set; }
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;
}

public class MenuItemDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    [JsonPropertyName("price")]
    public decimal Price { get; set; }
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;
    [JsonPropertyName("category")]
    public string Category { get; set; } = string.Empty;
    [JsonPropertyName("imageUrl")]
    public string ImageUrl { get; set; } = string.Empty;
}

[ApiController]
[Route("api/v1/ai")]
[Authorize]
public class AiController : ControllerBase
{
    private readonly HttpClient _httpClient;
    private readonly string _ollamaUrl;
    private readonly string _ollamaModel;
    private readonly string _tableApiUrl;
    private readonly string _menuApiUrl;
    private readonly ILogger<AiController> _logger;

    public AiController(
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory,
        ILogger<AiController> logger)
    {
        _httpClient = httpClientFactory.CreateClient();
        _ollamaUrl = configuration["Services:Ollama"] ?? "http://localhost:11434";
        _ollamaModel = configuration["Services:OllamaModel"] ?? "qwen2.5:1.5b";
        _tableApiUrl = configuration["Services:TableApi"] ?? "http://localhost:5004";
        _menuApiUrl = configuration["Services:MenuApi"] ?? "http://localhost:5002";
        _logger = logger;
    }

    [HttpPost("query")]
    public async Task<IActionResult> Query([FromBody] QueryRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Prompt))
        {
            return BadRequest(new { success = false, message = "Prompt cannot be empty" });
        }

        try
        {
            // 1. Phân loại ý định của người dùng bằng Ollama
            var intent = await ClassifyIntentAsync(request.Prompt);
            _logger.LogInformation("Recognized intent action: {Action}", intent.Action);

            string systemPrompt = "Bạn là trợ lý AI thông minh cho nhà hàng SmartDine. Hãy trả lời câu hỏi của khách hàng bằng tiếng Việt một cách tự nhiên và lịch sự.";
            string contextData = "";

            // 2. Lấy dữ liệu động dựa trên ý định
            var authHeader = Request.Headers.Authorization.ToString();
            
            if (intent.Action == "get_occupied_tables" || intent.Action == "get_available_tables")
            {
                var tables = await FetchDataAsync<List<TableDto>>($"{_tableApiUrl}/api/v1/tables", authHeader);
                if (tables != null)
                {
                    int total = tables.Count;
                    int occupied = tables.Count(t => t.Status.Equals("OCCUPIED", StringComparison.OrdinalIgnoreCase));
                    int available = tables.Count(t => t.Status.Equals("AVAILABLE", StringComparison.OrdinalIgnoreCase));
                    var occupiedList = string.Join(", ", tables.Where(t => t.Status.Equals("OCCUPIED", StringComparison.OrdinalIgnoreCase)).Select(t => $"Bàn {t.Number}"));
                    var availableList = string.Join(", ", tables.Where(t => t.Status.Equals("AVAILABLE", StringComparison.OrdinalIgnoreCase)).Select(t => $"Bàn {t.Number}"));

                    contextData = $"[Dữ liệu bàn ăn hiện tại từ hệ thống: Tổng số bàn={total}, Số bàn trống={available} (gồm: {availableList}), Số bàn có khách={occupied} (gồm: {occupiedList})]";
                }
            }
            else if (intent.Action == "get_popular_items")
            {
                var popularItems = await FetchDataAsync<List<MenuItemDto>>($"{_menuApiUrl}/api/v1/menu-items/popular?count=5", authHeader);
                if (popularItems != null)
                {
                    var itemsText = string.Join("\n", popularItems.Select(i => $"- {i.Name} ({i.Category}): {i.Price:N0} VNĐ - {i.Description}"));
                    contextData = $"[Danh sách các món ăn phổ biến/bán chạy nhất hiện tại:\n{itemsText}]";
                }
            }

            // 3. Xây dựng prompt sinh câu trả lời cuối cùng
            var responsePrompt = request.Prompt;
            if (!string.IsNullOrEmpty(contextData))
            {
                systemPrompt = "Bạn là trợ lý AI thông minh cho nhà hàng SmartDine. Dưới đây là dữ liệu thực tế từ hệ thống: " + contextData + ". Hãy trả lời câu hỏi của người dùng dựa trên thông tin thực tế này. Trả lời bằng tiếng Việt ngắn gọn, thân thiện.";
            }

            var answer = await CallOllamaChatAsync(systemPrompt, responsePrompt);
            return Ok(new QueryResponse { Success = true, Answer = answer });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing AI query");
            return StatusCode(500, new QueryResponse { Success = false, Answer = "Xin lỗi, đã có lỗi xảy ra khi xử lý yêu cầu của bạn qua AI." });
        }
    }

    private async Task<IntentResult> ClassifyIntentAsync(string userPrompt)
    {
        var systemInstruction = @"Bạn là một AI phân loại ý định người dùng cho nhà hàng SmartDine.
Hãy đọc câu hỏi của người dùng và chọn một trong bốn hành động (action) sau đây:
1. ""get_occupied_tables"" - Nếu người dùng muốn biết số lượng bàn đang có khách, danh sách bàn bận, hoặc bàn nào đang có người ngồi.
2. ""get_available_tables"" - Nếu người dùng hỏi về bàn trống, còn bàn trống không, hoặc những bàn nào đang rảnh.
3. ""get_popular_items"" - Nếu người dùng hỏi về món ăn bán chạy nhất, món ăn phổ biến, món ăn ưa thích, nên chọn món nào.
4. ""general_chat"" - Chào hỏi hoặc câu hỏi khác.

Chỉ trả về chuỗi JSON đại diện cho hành động, tuyệt đối không trả thêm văn bản giải thích. Định dạng:
{ ""action"": ""tên_hành_động"" }";

        try
        {
            var jsonResponse = await CallOllamaChatAsync(systemInstruction, userPrompt);
            
            // Extract JSON from potential markdown blocks if LLM returns it wrapped in ```json
            var cleanJson = jsonResponse.Trim();
            if (cleanJson.StartsWith("```"))
            {
                cleanJson = cleanJson.Replace("```json", "").Replace("```", "").Trim();
            }

            var result = JsonSerializer.Deserialize<IntentResult>(cleanJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            return result ?? new IntentResult();
        }
        catch
        {
            return new IntentResult(); // Fallback to general chat
        }
    }

    private async Task<T?> FetchDataAsync<T>(string url, string authHeader)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            if (!string.IsNullOrEmpty(authHeader))
            {
                request.Headers.Add("Authorization", authHeader);
            }

            var response = await _httpClient.SendAsync(request);
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<ApiResponseWrapper<T>>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                return result != null ? result.Data : default;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching data from service: {Url}", url);
        }
        return default;
    }

    private async Task<string> CallOllamaChatAsync(string systemMessage, string userMessage)
    {
        var requestBody = new
        {
            model = _ollamaModel,
            messages = new[]
            {
                new { role = "system", content = systemMessage },
                new { role = "user", content = userMessage }
            },
            stream = false,
            options = new
            {
                temperature = 0.1
            }
        };

        var response = await _httpClient.PostAsJsonAsync($"{_ollamaUrl}/api/chat", requestBody);
        response.EnsureSuccessStatusCode();

        var responseContent = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(responseContent);
        var messageElement = doc.RootElement.GetProperty("message");
        var content = messageElement.GetProperty("content").GetString();
        return content ?? string.Empty;
    }
}
