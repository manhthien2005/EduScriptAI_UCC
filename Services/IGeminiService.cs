namespace EduScriptAI.Services;

public interface IGeminiService
{
    Task<string> GenerateScriptAsync(string keywords, string level, string type, string duration);
    Task<string> RewriteScriptAsync(string content, string instruction);
} 