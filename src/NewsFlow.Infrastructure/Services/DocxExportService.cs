using System.IO.Compression;
using System.Text;
using System.Xml.Linq;
using NewsFlow.Application.Common.Interfaces;
using NewsFlow.Application.Documents.DTOs;

namespace NewsFlow.Infrastructure.Services;

public class DocxExportService : IDocxExportService
{
    private static readonly XNamespace W = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";
    private static readonly XNamespace R = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
    private static readonly XNamespace WpR = "http://schemas.openxmlformats.org/package/2006/relationships";
    private static readonly XNamespace CT = "http://schemas.openxmlformats.org/package/2006/content-types";

    public byte[] ExportToDocx(DocumentDto doc)
    {
        using var ms = new MemoryStream();
        using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
        {
            AddEntry(zip, "[Content_Types].xml", BuildContentTypes());
            AddEntry(zip, "_rels/.rels", BuildRootRels());
            AddEntry(zip, "word/_rels/document.xml.rels", BuildDocumentRels());
            AddEntry(zip, "word/styles.xml", BuildStyles());
            AddEntry(zip, "word/document.xml", BuildDocument(doc));
        }

        return ms.ToArray();
    }

    private static void AddEntry(ZipArchive zip, string path, string content)
    {
        var entry = zip.CreateEntry(path, CompressionLevel.Optimal);
        using var writer = new StreamWriter(entry.Open(), Encoding.UTF8);
        writer.Write(content);
    }

    private static string BuildContentTypes()
    {
        var doc = new XDocument(
            new XDeclaration("1.0", "UTF-8", "yes"),
            new XElement(CT + "Types",
                new XElement(CT + "Default",
                    new XAttribute("Extension", "rels"),
                    new XAttribute("ContentType", "application/vnd.openxmlformats-package.relationships+xml")),
                new XElement(CT + "Default",
                    new XAttribute("Extension", "xml"),
                    new XAttribute("ContentType", "application/xml")),
                new XElement(CT + "Override",
                    new XAttribute("PartName", "/word/document.xml"),
                    new XAttribute("ContentType", "application/vnd.openxmlformats-officedocument.wordprocessingml.document.main+xml")),
                new XElement(CT + "Override",
                    new XAttribute("PartName", "/word/styles.xml"),
                    new XAttribute("ContentType", "application/vnd.openxmlformats-officedocument.wordprocessingml.styles+xml"))));
        return doc.Declaration + doc.ToString();
    }

    private static string BuildRootRels()
    {
        var doc = new XDocument(
            new XDeclaration("1.0", "UTF-8", "yes"),
            new XElement(WpR + "Relationships",
                new XElement(WpR + "Relationship",
                    new XAttribute("Id", "rId1"),
                    new XAttribute("Type", "http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument"),
                    new XAttribute("Target", "word/document.xml"))));
        return doc.Declaration + doc.ToString();
    }

    private static string BuildDocumentRels()
    {
        var doc = new XDocument(
            new XDeclaration("1.0", "UTF-8", "yes"),
            new XElement(WpR + "Relationships",
                new XElement(WpR + "Relationship",
                    new XAttribute("Id", "rId1"),
                    new XAttribute("Type", "http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles"),
                    new XAttribute("Target", "styles.xml"))));
        return doc.Declaration + doc.ToString();
    }

    private static string BuildStyles()
    {
        var doc = new XDocument(
            new XDeclaration("1.0", "UTF-8", "yes"),
            new XElement(W + "styles",
                new XAttribute(XNamespace.Xmlns + "w", W),
                new XElement(W + "docDefaults",
                    new XElement(W + "rPrDefault",
                        new XElement(W + "rPr",
                            new XElement(W + "rFonts",
                                new XAttribute(W + "ascii", "Times New Roman"),
                                new XAttribute(W + "hAnsi", "Times New Roman"),
                                new XAttribute(W + "cs", "Times New Roman")),
                            new XElement(W + "sz", new XAttribute(W + "val", "24")),
                            new XElement(W + "szCs", new XAttribute(W + "val", "24"))))),
                StyleDef("Heading1", "heading 1", "32", bold: true),
                StyleDef("Heading2", "heading 2", "26", bold: true),
                StyleDef("Subtitle", "Subtitle", "20", italic: true, color: "666666")));
        return doc.Declaration + doc.ToString();
    }

    private static XElement StyleDef(string id, string name, string fontSize,
        bool bold = false, bool italic = false, string? color = null)
    {
        var rPr = new XElement(W + "rPr",
            new XElement(W + "sz", new XAttribute(W + "val", fontSize)),
            new XElement(W + "szCs", new XAttribute(W + "val", fontSize)));

        if (bold) rPr.Add(new XElement(W + "b"));
        if (italic) rPr.Add(new XElement(W + "i"));
        if (color != null) rPr.Add(new XElement(W + "color", new XAttribute(W + "val", color)));

        return new XElement(W + "style",
            new XAttribute(W + "type", "paragraph"),
            new XAttribute(W + "styleId", id),
            new XElement(W + "name", new XAttribute(W + "val", name)),
            rPr);
    }

    private static string BuildDocument(DocumentDto doc)
    {
        var body = new XElement(W + "body");

        body.Add(Heading(doc.Title, "Heading1"));

        if (!string.IsNullOrEmpty(doc.RegistrationNumber))
            body.Add(MetaLine("Рег. номер", doc.RegistrationNumber));
        body.Add(MetaLine("Статус", doc.Status.ToString()));
        body.Add(MetaLine("Приоритет", doc.Priority.ToString()));
        body.Add(MetaLine("Автор", doc.CreatedBy));
        body.Add(MetaLine("Дата создания", doc.CreatedAt.ToString("dd.MM.yyyy HH:mm")));

        if (doc.EvaluationScore.HasValue)
        {
            body.Add(MetaLine("Оценка", $"{doc.EvaluationScore}/5"));
            if (!string.IsNullOrEmpty(doc.EvaluationCommentary))
                body.Add(MetaLine("Комментарий к оценке", doc.EvaluationCommentary));
        }

        body.Add(EmptyParagraph());

        if (doc.SourceMaterials.Count > 0)
        {
            body.Add(Heading("Исходные материалы", "Heading2"));
            foreach (var mat in doc.SourceMaterials)
                body.Add(BulletParagraph(mat.Title));
            body.Add(EmptyParagraph());
        }

        body.Add(Heading("Содержание", "Heading2"));
        foreach (var line in doc.Content.Split('\n'))
            body.Add(TextParagraph(line));

        if (doc.Comments.Count > 0)
        {
            body.Add(EmptyParagraph());
            body.Add(Heading("Замечания", "Heading2"));
            foreach (var c in doc.Comments)
                body.Add(CommentParagraph(c));
        }

        var document = new XDocument(
            new XDeclaration("1.0", "UTF-8", "yes"),
            new XElement(W + "document",
                new XAttribute(XNamespace.Xmlns + "w", W),
                new XAttribute(XNamespace.Xmlns + "r", R),
                body));

        return document.Declaration + document.ToString();
    }

    private static XElement Heading(string text, string styleId) =>
        new(W + "p",
            new XElement(W + "pPr",
                new XElement(W + "pStyle", new XAttribute(W + "val", styleId))),
            Run(text));

    private static XElement MetaLine(string label, string value) =>
        new(W + "p",
            Run(label + ": ", bold: true),
            Run(value));

    private static XElement TextParagraph(string text) =>
        new(W + "p", Run(text));

    private static XElement EmptyParagraph() =>
        new(W + "p");

    private static XElement BulletParagraph(string text) =>
        new(W + "p", Run($"\u2022  {text}"));

    private static XElement CommentParagraph(DocumentCommentDto c) =>
        new(W + "p",
            Run($"{c.Author} ({c.CreatedAt:dd.MM.yyyy HH:mm}): ", bold: true),
            Run(c.Text));

    private static XElement Run(string text, bool bold = false)
    {
        var run = new XElement(W + "r");
        if (bold)
            run.Add(new XElement(W + "rPr", new XElement(W + "b")));
        run.Add(new XElement(W + "t",
            new XAttribute(XNamespace.Xml + "space", "preserve"),
            text));
        return run;
    }
}
