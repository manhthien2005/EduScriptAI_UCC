using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using EduScriptAI.Models;

namespace EduScriptAI.Services;

public class GrammarService : IGrammarService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string _apiKey;

    public GrammarService(IHttpClientFactory httpClientFactory, IOptions<GoogleApiOptions> options)
    {
        _httpClientFactory = httpClientFactory;
        _apiKey = options.Value.ApiKey;
    }

    public async Task<GrammarCheckResult> CheckGrammarAsync(string content)
    {
        var result = new GrammarCheckResult();
        
        // Kiểm tra các lỗi cơ bản
        CheckBasicErrors(content, result);
        
        // Kiểm tra ngữ pháp bằng Gemini API
        await CheckWithGemini(content, result);
        
        return result;
    }

    private void CheckBasicErrors(string content, GrammarCheckResult result)
    {
        // Kiểm tra dấu câu
        if (!content.Contains(".") && !content.Contains("!") && !content.Contains("?"))
        {
            result.Errors.Add("Thiếu dấu câu kết thúc câu");
        }

        // Kiểm tra khoảng trắng
        if (Regex.IsMatch(content, @"\s{2,}"))
        {
            result.Errors.Add("Có nhiều khoảng trắng liên tiếp");
        }

        // Kiểm tra viết hoa đầu câu
        var sentences = Regex.Split(content, @"[.!?]");
        foreach (var sentence in sentences)
        {
            if (!string.IsNullOrWhiteSpace(sentence))
            {
                var trimmed = sentence.Trim();
                if (trimmed.Length > 0 && !char.IsUpper(trimmed[0]))
                {
                    result.Errors.Add($"Câu không viết hoa đầu câu: {trimmed}");
                }
            }
        }
    }

    private async Task CheckWithGemini(string content, GrammarCheckResult result)
    {
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
            new StringContent(System.Text.Json.JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json")
        );

        if (response.IsSuccessStatusCode)
        {
            var responseContent = await response.Content.ReadAsStringAsync();
            var json = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(responseContent);
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
    }
} 