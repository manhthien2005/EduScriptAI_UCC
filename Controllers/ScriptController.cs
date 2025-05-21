using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EduScriptAI.Data;
using EduScriptAI.Models;
using EduScriptAI.Services;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using System.Text.Json;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using WordDocument = DocumentFormat.OpenXml.Wordprocessing.Document;
using WordParagraph = DocumentFormat.OpenXml.Wordprocessing.Paragraph;
using WordText = DocumentFormat.OpenXml.Wordprocessing.Text;
using WordPageSize = DocumentFormat.OpenXml.Wordprocessing.PageSize;
using PdfDocument = QuestPDF.Fluent.Document;

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
        mainPart.Document = new WordDocument();
        var body = mainPart.Document.AppendChild(new Body());

        // Add title with style
        var titlePara = body.AppendChild(new WordParagraph());
        var titleRun = titlePara.AppendChild(new Run());
        titleRun.AppendChild(new RunProperties(new Bold()));
        titleRun.AppendChild(new WordText(script.Keywords));
        
        // Add metadata
        var metaPara = body.AppendChild(new WordParagraph());
        var metaRun = metaPara.AppendChild(new Run());
        metaRun.AppendChild(new WordText($"Cấp học: {script.Level}"));
        
        var typePara = body.AppendChild(new WordParagraph());
        var typeRun = typePara.AppendChild(new Run());
        typeRun.AppendChild(new WordText($"Loại: {script.Type}"));
        
        // Add content with proper formatting
        var contentPara = body.AppendChild(new WordParagraph());
        var contentRun = contentPara.AppendChild(new Run());
        contentRun.AppendChild(new WordText(script.Content));
        
        // Save
        mainPart.Document.Save();
        document.Save();
        
        stream.Position = 0;
        return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.wordprocessingml.document", $"{script.Keywords}.docx");
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

    public async Task<IActionResult> ExportPdf(int id)
    {
        var script = await _context.Scripts.FindAsync(id);
        if (script == null)
        {
            return NotFound();
        }

        var document = PdfDocument.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(12));

                page.Header().Element(ComposeHeader);
                page.Content().Element(x => ComposeContent(x, script));
                page.Footer().Element(ComposeFooter);
            });
        });

        var pdfBytes = document.GeneratePdf();
        return File(pdfBytes, "application/pdf", $"{script.Keywords}.pdf");
    }

    private void ComposeHeader(IContainer container)
    {
        container.Row(row =>
        {
            row.RelativeItem().Column(column =>
            {
                column.Item().Text(text =>
                {
                    text.Span("EduScriptAI").FontSize(20).SemiBold();
                });
            });
        });
    }

    private void ComposeContent(IContainer container, Script script)
    {
        container.Column(column =>
        {
            column.Item().Text(text =>
            {
                text.Span(script.Keywords).FontSize(20).SemiBold();
            });

            column.Item().PaddingTop(10).Text(text =>
            {
                text.Span($"Cấp học: {script.Level}");
            });

            column.Item().Text(text =>
            {
                text.Span($"Loại: {script.Type}");
            });

            column.Item().PaddingTop(10).Text(text =>
            {
                text.Span("Nội dung:");
            });

            column.Item().PaddingTop(5).Text(text =>
            {
                text.Span(script.Content);
            });
        });
    }

    private void ComposeFooter(IContainer container)
    {
        container.Row(row =>
        {
            row.RelativeItem().Text(text =>
            {
                text.Span("Trang ").FontSize(10);
                text.CurrentPageNumber().FontSize(10);
                text.Span(" / ").FontSize(10);
                text.TotalPages().FontSize(10);
            });
        });
    }
} 