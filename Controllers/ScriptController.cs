using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EduScriptAI.Data;
using EduScriptAI.Models;
using EduScriptAI.Services;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using System.Text.Json;

namespace EduScriptAI.Controllers;

public class ScriptController : Controller
{
    private readonly EduScriptContext _context;
    private readonly IGeminiService _geminiService;
    private readonly IGrammarService _grammarService;

    public ScriptController(
        EduScriptContext context,
        IGeminiService geminiService,
        IGrammarService grammarService)
    {
        _context = context;
        _geminiService = geminiService;
        _grammarService = grammarService;
    }

    public IActionResult Index()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Generate(string keywords, string level, string type, string duration)
    {
        var content = await _geminiService.GenerateScriptAsync(keywords, level, type, duration);
        
        var script = new Script
        {
            Keywords = keywords,
            Level = level,
            Type = type,
            Content = content
        };

        _context.Scripts.Add(script);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Edit), new { id = script.Id });
    }

    public async Task<IActionResult> Edit(int id)
    {
        var script = await _context.Scripts.FindAsync(id);
        if (script == null)
        {
            return NotFound();
        }

        return View(script);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int id, string actionType, [FromBody] JsonDocument? body = null)
    {
        var script = await _context.Scripts.FindAsync(id);
        if (script == null)
        {
            return NotFound();
        }

        if (body == null)
        {
            return BadRequest("Request body is required");
        }

        var root = body.RootElement;

        switch (actionType)
        {
            case "save":
                if (root.ValueKind == JsonValueKind.Object)
                {
                    var content = root.GetProperty("content").GetString();
                    if (content != null)
                    {
                        script.Content = content;
                        script.UpdatedAt = DateTime.UtcNow;
                        await _context.SaveChangesAsync();
                    }
                }
                return RedirectToAction(nameof(Edit), new { id });
            
            case "checkGrammar":
                if (root.ValueKind == JsonValueKind.Object)
                {
                    var content = root.GetProperty("content").GetString();
                    if (content != null)
                    {
                        var result = await _grammarService.CheckGrammarAsync(content);
                        return Json(result);
                    }
                }
                return BadRequest();
            
            case "rewrite":
                if (root.ValueKind == JsonValueKind.Object)
                {
                    var content = root.GetProperty("content").GetString();
                    var instruction = root.GetProperty("instruction").GetString();
                    if (content != null && instruction != null)
                    {
                        try
                        {
                            var rewritten = await _geminiService.RewriteScriptAsync(content, instruction);
                            return Json(new { content = rewritten });
                        }
                        catch (Exception ex)
                        {
                            return StatusCode(500, new { error = ex.Message });
                        }
                    }
                }
                return BadRequest();
            
            default:
                return BadRequest();
        }
    }

    public async Task<IActionResult> Manage()
    {
        var scripts = await _context.Scripts
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync();
        
        return View(scripts);
    }

    public async Task<IActionResult> Export(int id)
    {
        var script = await _context.Scripts.FindAsync(id);
        if (script == null)
        {
            return NotFound();
        }

        using var stream = new MemoryStream();
        using var document = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document);
        
        var mainPart = document.AddMainDocumentPart();
        mainPart.Document = new Document();
        var body = mainPart.Document.AppendChild(new Body());
        
        // Add content
        var paragraph = body.AppendChild(new Paragraph());
        var run = paragraph.AppendChild(new Run());
        run.AppendChild(new Text(script.Content));
        
        // Save
        mainPart.Document.Save();
        
        return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.wordprocessingml.document", $"script_{id}.docx");
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var script = await _context.Scripts.FindAsync(id);
        if (script == null)
        {
            return NotFound();
        }

        _context.Scripts.Remove(script);
        await _context.SaveChangesAsync();

        return Ok();
    }
} 