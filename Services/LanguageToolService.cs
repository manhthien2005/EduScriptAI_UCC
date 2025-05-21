using System.Text.Json;
using Microsoft.Extensions.Options;
using EduScriptAI.Models;

namespace EduScriptAI.Services;

public class LanguageToolService : IGrammarService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string _apiKey;

    public LanguageToolService(IHttpClientFactory httpClientFactory, IOptions<GoogleApiOptions> options)
    {
        _httpClientFactory = httpClientFactory;
        _apiKey = options.Value.ApiKey;
    }

    public async Task<GrammarCheckResult> CheckGrammarAsync(string content)
    {
        var result = new GrammarCheckResult();
        var client = _httpClientFactory.CreateClient();

        var prompt = $@"Kiểm tra và sửa lỗi ngữ pháp trong đoạn văn sau:

{content}

Yêu cầu:
1. Liệt kê các lỗi ngữ pháp tìm thấy
2. Đưa ra gợi ý sửa lỗi
3. Trả về kết quả theo định dạng:
   Lỗi: [mô tả lỗi]
   Gợi ý: [cách sửa]";

        var request = new
        {
            contents = new[]
            {
                new
                {
                    role = "user",
                    parts = new[]
                    {
                        new { text = prompt }
                    }
                }
            },
            generationConfig = new
            {
                temperature = 0.3,
                topK = 40,
                topP = 0.95,
                maxOutputTokens = 1024
            }
        };

        var response = await client.PostAsync(
            $"https://generativelanguage.googleapis.com/v1/models/gemini-pro:generateContent?key={_apiKey}",
            new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json")
        );

        if (response.IsSuccessStatusCode)
        {
            var responseContent = await response.Content.ReadAsStringAsync();
            var json = JsonSerializer.Deserialize<JsonElement>(responseContent);
            var text = json.GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString();

            if (text != null)
            {
                var lines = text.Split('\n');
                foreach (var line in lines)
                {
                    if (line.StartsWith("Lỗi:"))
                    {
                        result.Errors.Add(line.Substring(5).Trim());
                    }
                    else if (line.StartsWith("Gợi ý:"))
                    {
                        result.Suggestions.Add(line.Substring(7).Trim());
                    }
                }
            }
        }

        return result;
    }
} 