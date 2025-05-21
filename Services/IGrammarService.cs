using EduScriptAI.Models;

namespace EduScriptAI.Services;

public interface IGrammarService
{
    Task<GrammarCheckResult> CheckGrammarAsync(string content);
}

public class GrammarCheckResult
{
    public List<string> Errors { get; set; } = new();
    public List<string> Suggestions { get; set; } = new();
} 