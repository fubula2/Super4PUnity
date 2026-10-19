using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;

//Reads the library spreadsheet (xlsx) into entries. Columns are found by header label, not fixed letters.
public static class LibrarySheetParser
{
    private const string SheetName = "ModsDownloads";
    private const int HeaderScanRows = 40;
    private const int MaxCategoryLength = 60;

    private static readonly XNamespace Main = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
    private static readonly XNamespace Rel = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";

    public static LibraryCatalog Parse(Stream xlsx)
    {
        try { return ParseInternal(xlsx); }
        catch(Exception e) { return Fail("Could not read the library sheet: " + e.Message); }
    }

    private static LibraryCatalog Fail(string message)
    {
        return new LibraryCatalog { Error = message };
    }

    private static LibraryCatalog ParseInternal(Stream xlsx)
    {
        using(var zip = new ZipArchive(xlsx, ZipArchiveMode.Read, true))
        {
            var sheetPath = FindSheetPath(zip);
            if(sheetPath == null) { return Fail("Tab '" + SheetName + "' was not found in the library sheet."); }

            var strings = ReadSharedStrings(zip);
            var sheet = Load(zip, sheetPath);
            var links = ReadHyperlinks(zip, sheetPath, sheet);

            var rows = new List<Dictionary<string, string>>();
            var rowNumbers = new List<int>();
            foreach(var row in sheet.Descendants(Main + "row"))
            {
                var cells = new Dictionary<string, string>();
                foreach(var c in row.Elements(Main + "c"))
                {
                    var text = CellText(c, strings);
                    if(!string.IsNullOrEmpty(text)) { cells[Column((string)c.Attribute("r"))] = text; }
                }
                rows.Add(cells);
                rowNumbers.Add(int.Parse((string)row.Attribute("r")));
            }

            var headerIndex = -1;
            Dictionary<string, string> labels = null;
            for(var i = 0; i < Math.Min(rows.Count, HeaderScanRows); i++)
            {
                var mapped = rows[i].ToDictionary(kv => Normalize(kv.Value), kv => kv.Key, StringComparer.OrdinalIgnoreCase);
                if(mapped.ContainsKey("series name") && mapped.ContainsKey("download")) { headerIndex = i; labels = mapped; break; }
            }
            if(headerIndex < 0) { return Fail("The library sheet has no 'Series Name' and 'Download' columns."); }

            string Col(string label) { return labels.TryGetValue(label, out var c) ? c : null; }
            var seriesCol = Col("series name");
            var downloadCol = Col("download");
            var charsCol = Col("character(s)");
            var updatedCol = Col("last updated");
            var statusCol = Col("status");
            var numberCol = Col("#");

            var entries = new List<LibraryEntry>();
            string category = null;
            for(var i = headerIndex + 1; i < rows.Count; i++)
            {
                var cells = rows[i];
                cells.TryGetValue(seriesCol, out var seriesRaw);
                if(string.IsNullOrWhiteSpace(seriesRaw)) { continue; }

                links.TryGetValue(downloadCol + rowNumbers[i], out var url);
                if(string.IsNullOrEmpty(url) && cells.TryGetValue(downloadCol, out var plain) && plain.StartsWith("http")) { url = plain; }

                if(!LibraryLinkClassifier.TryClassify(url, out var kind))
                {
                    //Only the series cell filled and short: a category heading for the rows that follow
                    var onlySeries = cells.Count == 1;
                    if(onlySeries && seriesRaw.Length <= MaxCategoryLength) { category = Clean(seriesRaw, " "); }
                    continue;
                }

                string Get(string col) { return col != null && cells.TryGetValue(col, out var v) ? v : null; }
                entries.Add(new LibraryEntry
                {
                    Series = Clean(seriesRaw, " "),
                    Characters = Clean(Get(charsCol), ", "),
                    Category = category,
                    Updated = ExcelDate.FromSerial(Get(updatedCol)),
                    Status = StatusLabel(Get(statusCol)),
                    Url = url.Trim(),
                    Kind = kind
                });
            }
            return new LibraryCatalog { Entries = entries, MusicFolderUrl = FindMusicLink(rows, rowNumbers, links) };
        }
    }

    //The hyperlink on the cell labelled "Music Mods" (anywhere on the tab), or null
    private static string FindMusicLink(List<Dictionary<string, string>> rows, List<int> rowNumbers, Dictionary<string, string> links)
    {
        for(var i = 0; i < rows.Count; i++)
        {
            foreach(var cell in rows[i])
            {
                if(cell.Value.IndexOf("music mods", StringComparison.OrdinalIgnoreCase) < 0) { continue; }
                if(links.TryGetValue(cell.Key + rowNumbers[i], out var url) && !string.IsNullOrWhiteSpace(url)) { return url.Trim(); }
            }
        }
        return null;
    }

    private static string FindSheetPath(ZipArchive zip)
    {
        var workbook = Load(zip, "xl/workbook.xml");
        var sheet = workbook.Descendants(Main + "sheet").FirstOrDefault(s => string.Equals((string)s.Attribute("name"), SheetName, StringComparison.OrdinalIgnoreCase));
        if(sheet == null) { return null; }

        var id = (string)sheet.Attribute(Rel + "id");
        var rels = Load(zip, "xl/_rels/workbook.xml.rels");
        var target = rels.Descendants().FirstOrDefault(r => (string)r.Attribute("Id") == id)?.Attribute("Target")?.Value;
        if(target == null) { return null; }
        return target.StartsWith("/") ? target.TrimStart('/') : "xl/" + target;
    }

    private static Dictionary<string, string> ReadHyperlinks(ZipArchive zip, string sheetPath, XDocument sheet)
    {
        var result = new Dictionary<string, string>();
        var relsPath = sheetPath.Substring(0, sheetPath.LastIndexOf('/')) + "/_rels/" + sheetPath.Substring(sheetPath.LastIndexOf('/') + 1) + ".rels";
        if(zip.GetEntry(relsPath) == null) { return result; }

        var targets = Load(zip, relsPath).Descendants().Where(r => r.Attribute("Id") != null)
            .ToDictionary(r => (string)r.Attribute("Id"), r => (string)r.Attribute("Target"));
        foreach(var h in sheet.Descendants(Main + "hyperlink"))
        {
            var id = (string)h.Attribute(Rel + "id");
            if(id != null && targets.TryGetValue(id, out var url)) { result[(string)h.Attribute("ref")] = url; }
        }
        return result;
    }

    private static List<string> ReadSharedStrings(ZipArchive zip)
    {
        if(zip.GetEntry("xl/sharedStrings.xml") == null) { return new List<string>(); }
        return Load(zip, "xl/sharedStrings.xml").Descendants(Main + "si")
            .Select(si => string.Concat(si.Descendants(Main + "t").Select(t => t.Value))).ToList();
    }

    private static string CellText(XElement c, List<string> strings)
    {
        var type = (string)c.Attribute("t");
        if(type == "inlineStr") { return string.Concat(c.Descendants(Main + "t").Select(t => t.Value)); }

        var v = (string)c.Element(Main + "v");
        if(v == null) { return null; }
        if(type == "s") { return int.TryParse(v, out var i) && i >= 0 && i < strings.Count ? strings[i] : null; }
        return v;
    }

    private static XDocument Load(ZipArchive zip, string path)
    {
        var entry = zip.GetEntry(path) ?? throw new InvalidDataException("Missing " + path);
        using(var s = entry.Open()) { return XDocument.Load(s); }
    }

    private static string Column(string cellRef) { return new string(cellRef.TakeWhile(char.IsLetter).ToArray()); }

    private static string Normalize(string s) { return Regex.Replace(s.Trim(), @"\s+", " ").ToLowerInvariant(); }

    private static string Clean(string s, string separator)
    {
        if(s == null) { return string.Empty; }
        var parts = s.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).Select(p => p.Trim()).Where(p => p.Length > 0);
        return string.Join(separator, parts);
    }

    private static string StatusLabel(string glyph)
    {
        if(string.IsNullOrWhiteSpace(glyph)) { return string.Empty; }
        switch(glyph.Trim())
        {
            case "❤": return "Completed";
            case "►": return "In progress";
            case "●": return "Standby";
            case "P": return "Patreon exclusive";
            default: return glyph.Trim();
        }
    }
}
