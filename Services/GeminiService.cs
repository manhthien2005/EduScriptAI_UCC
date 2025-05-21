using System.Text.Json;
using Microsoft.Extensions.Options;
using EduScriptAI.Models;

namespace EduScriptAI.Services;

public class GeminiService : IGeminiService
{
    private readonly string _apiKey;
    private readonly IHttpClientFactory _httpClientFactory;
    private const string API_URL = "https://generativelanguage.googleapis.com/v1/models/gemini-2.0-flash:generateContent";

    public GeminiService(IOptions<GoogleApiOptions> options, IHttpClientFactory httpClientFactory)
    {
        _apiKey = options.Value.ApiKey;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<string> GenerateScriptAsync(string keywords, string level, string type, string duration)
    {
        var client = _httpClientFactory.CreateClient();
        var prompt = $@"Tạo kịch bản video YouTube với chủ đề: {keywords} cho đối tượng {level} theo phong cách {type}

Thời lượng video: {duration}

Yêu cầu:
1. Định dạng HTML:
   - Tiêu đề chính: <h2>...</h2>
   - Tiêu đề phụ: <h3>...</h3>
   - Đoạn văn: <p>...</p>
   - Danh sách: <ul><li>...</li></ul>
   - In đậm: <strong>...</strong>
   - In nghiêng: <em>...</em>
   - Xuống dòng: <br>
   - Chú thích: <div class='note'>...</div>

2. Cấu trúc video:
   {GetDurationStructure(duration)}

3. Yếu tố bắt buộc:
   - Thời gian cho mỗi phần (ghi rõ phút:giây)
   - Hình ảnh minh họa cần có (mô tả chi tiết)
   - Hiệu ứng chuyển cảnh (fade, cut, zoom...)
   - Nhạc nền gợi ý (thể loại, mood)
   - Call-to-action cuối video

4. Tương tác (Chọn 2-3):
   - Câu hỏi cho khán giả
   - Khảo sát ý kiến
   - Thử thách
   - Kêu gọi like, share, subscribe
   - Giới thiệu video liên quan

5. Lưu ý:
   - Ngôn ngữ phù hợp với đối tượng
   - Tốc độ nói và nhịp độ video
   - Chuẩn bị dụng cụ, tài liệu

6. Định dạng đầu ra:
   - Mỗi phần phải có thời gian cụ thể
   - Hình ảnh và hiệu ứng phải được mô tả trong ngoặc đơn
   - Call-to-action phải có đầy đủ: like, share, subscribe
   - Kết thúc phải có nhạc nền và hình ảnh phù hợp

7. Cấu trúc HTML:
<h2>Tiêu đề video</h2>

<h3>1. Hook (0:00 - 0:15)</h3>
<p>[Nội dung hook]</p>
<div class='note'>(Hình ảnh: [mô tả])</div>
<div class='note'>(Hiệu ứng: [mô tả])</div>

<h3>2. Giới thiệu (0:15 - 0:30)</h3>
<p>[Nội dung giới thiệu]</p>
<div class='note'>(Hình ảnh: [mô tả])</div>
<div class='note'>(Hiệu ứng: [mô tả])</div>

<h3>3. Nội dung chính (0:30 - 4:00)</h3>
<p>[Nội dung chính]</p>
<div class='note'>(Hình ảnh: [mô tả])</div>
<div class='note'>(Hiệu ứng: [mô tả])</div>

<h3>4. Kết luận (4:00 - 5:00)</h3>
<p>[Nội dung kết luận]</p>
<div class='note'>(Hình ảnh: [mô tả])</div>
<div class='note'>(Hiệu ứng: [mô tả])</div>

<h3>Call-to-action</h3>
<p>[Nội dung call-to-action]</p>

<h3>Lưu ý</h3>
<ul>
<li>[Lưu ý 1]</li>
<li>[Lưu ý 2]</li>
</ul>";
        
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
                temperature = 0.7,
                topK = 40,
                topP = 0.95,
                maxOutputTokens = 2048,
                stopSequences = new[] { "###" }
            },
            safetySettings = new[]
            {
                new
                {
                    category = "HARM_CATEGORY_HARASSMENT",
                    threshold = "BLOCK_MEDIUM_AND_ABOVE"
                },
                new
                {
                    category = "HARM_CATEGORY_HATE_SPEECH",
                    threshold = "BLOCK_MEDIUM_AND_ABOVE"
                },
                new
                {
                    category = "HARM_CATEGORY_SEXUALLY_EXPLICIT",
                    threshold = "BLOCK_MEDIUM_AND_ABOVE"
                },
                new
                {
                    category = "HARM_CATEGORY_DANGEROUS_CONTENT",
                    threshold = "BLOCK_MEDIUM_AND_ABOVE"
                }
            }
        };

        var response = await client.PostAsync(
            $"{API_URL}?key={_apiKey}",
            new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json")
        );

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new Exception($"Gemini API error: {error}");
        }

        var result = await response.Content.ReadAsStringAsync();
        var json = JsonSerializer.Deserialize<JsonElement>(result);
        
        return json.GetProperty("candidates")[0]
            .GetProperty("content")
            .GetProperty("parts")[0]
            .GetProperty("text")
            .GetString() ?? string.Empty;
    }

    private string GetDurationStructure(string duration)
    {
        return duration switch
        {
            "short" => @"- Ngắn (3-5 phút):
   + Hook (0:00 - 0:15)
   + Giới thiệu (0:15 - 0:30)
   + Nội dung chính (0:30 - 4:00)
   + Kết luận (4:00 - 5:00)",
            
            "medium" => @"- Trung bình (5-10 phút):
   + Hook (0:00 - 0:30)
   + Giới thiệu (0:30 - 1:00)
   + Nội dung chính (1:00 - 8:00)
   + Kết luận (8:00 - 10:00)",
            
            "long" => @"- Dài (10-20 phút):
   + Hook (0:00 - 0:45)
   + Giới thiệu (0:45 - 1:30)
   + Nội dung chính (1:30 - 18:00)
   + Kết luận (18:00 - 20:00)",
            
            _ => throw new ArgumentException("Invalid duration. Must be 'short', 'medium', or 'long'")
        };
    }

    public async Task<string> RewriteScriptAsync(string content, string instruction)
    {
        var client = _httpClientFactory.CreateClient();
        var prompt = $@"Viết lại kịch bản video YouTube theo yêu cầu: {instruction}

Nội dung gốc:
{content}

Yêu cầu:
1. Giữ nguyên cấu trúc HTML
2. Cải thiện nội dung theo hướng dẫn
3. Đảm bảo định dạng:
   - Tiêu đề chính: <h2>...</h2>
   - Tiêu đề phụ: <h3>...</h3>
   - Đoạn văn: <p>...</p>
   - Danh sách: <ul><li>...</li></ul>
   - In đậm: <strong>...</strong>
   - In nghiêng: <em>...</em>
   - Xuống dòng: <br>
   - Chú thích: <div class='note'>...</div>

4. Cập nhật:
   - Thời gian cho mỗi phần (ghi rõ phút:giây)
   - Hình ảnh minh họa (mô tả chi tiết)
   - Hiệu ứng chuyển cảnh (fade, cut, zoom...)
   - Call-to-action (like, share, subscribe)
   - Nhạc nền và hình ảnh kết thúc";
        
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
                temperature = 0.7,
                topK = 40,
                topP = 0.95,
                maxOutputTokens = 2048,
                stopSequences = new[] { "###" }
            },
            safetySettings = new[]
            {
                new
                {
                    category = "HARM_CATEGORY_HARASSMENT",
                    threshold = "BLOCK_MEDIUM_AND_ABOVE"
                },
                new
                {
                    category = "HARM_CATEGORY_HATE_SPEECH",
                    threshold = "BLOCK_MEDIUM_AND_ABOVE"
                },
                new
                {
                    category = "HARM_CATEGORY_SEXUALLY_EXPLICIT",
                    threshold = "BLOCK_MEDIUM_AND_ABOVE"
                },
                new
                {
                    category = "HARM_CATEGORY_DANGEROUS_CONTENT",
                    threshold = "BLOCK_MEDIUM_AND_ABOVE"
                }
            }
        };

        var response = await client.PostAsync(
            $"{API_URL}?key={_apiKey}",
            new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json")
        );

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new Exception($"Gemini API error: {error}");
        }

        var result = await response.Content.ReadAsStringAsync();
        var json = JsonSerializer.Deserialize<JsonElement>(result);
        
        return json.GetProperty("candidates")[0]
            .GetProperty("content")
            .GetProperty("parts")[0]
            .GetProperty("text")
            .GetString() ?? string.Empty;
    }
} 