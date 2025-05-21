namespace EduScriptAI.Models;

public class LanguageToolResult
{
    public List<LanguageToolMatch> Matches { get; set; } = new();
}

public class LanguageToolMatch
{
    public string Message { get; set; } = string.Empty;
    public int Offset { get; set; }
    public int Length { get; set; }
    public List<string> Replacements { get; set; } = new();
} 