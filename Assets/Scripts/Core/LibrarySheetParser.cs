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

    //iconDir (optional): where the series logos embedded in the sheet are written; without it entries only get IconText
    public static LibraryCatalog Parse(Stream xlsx, string iconDir = null)
    {
        try { return ParseInternal(xlsx, iconDir); }
        catch(Exception e) { return Fail("Could not read the library sheet: " + e.Message); }
    }

    private static LibraryCatalog Fail(string message)
    {
        return new LibraryCatalog { Error = message };
    }

    private static LibraryCatalog ParseInternal(Stream xlsx, string iconDir)
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
            var iconCol = NextColumn(seriesCol);
            var icons = iconDir == null ? new Dictionary<int, string>() : ReadIcons(zip, sheetPath, ColumnIndex(iconCol), iconDir);

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
                    Kind = kind,
                    Row = rowNumbers[i],
                    IconFile = icons.TryGetValue(rowNumbers[i], out var icon) ? icon : null,
                    IconText = IconLabel(Get(iconCol), seriesRaw)
                });
            }
            return new LibraryCatalog { Entries = entries, MusicFolderUrl = FindMusicLink(rows, rowNumbers, links) };
        }
    }

    //The sheet shows a logo or a few letters before each series; letters come from the cell next to the name, else the name's first letter
    private static string IconLabel(string cell, string series)
    {
        var text = Clean(cell, " ");
        if(text.Length > 0 && text.Length <= 4 && text != "❤" && text != "►" && text != "●") { return text; }
        var name = Clean(series, " ");
        return name.Length > 0 ? name.Substring(0, 1).ToUpperInvariant() : string.Empty;
    }

    private static string NextColumn(string column)
    {
        var n = ColumnIndex(column) + 1;
        var s = string.Empty;
        while(n >= 0) { s = (char)('A' + n % 26) + s; n = n / 26 - 1; }
        return s;
    }

    private static int ColumnIndex(string column)
    {
        var n = 0;
        foreach(var ch in column.ToUpperInvariant()) { n = n * 26 + (ch - 'A' + 1); }
        return n - 1;
    }

    private static readonly XNamespace Xdr = "http://schemas.openxmlformats.org/drawingml/2006/spreadsheetDrawing";
    private static readonly XNamespace Dml = "http://schemas.openxmlformats.org/drawingml/2006/main";

    //Pictures placed on the sheet in the given column: sheet row number -> extracted file path
    private static Dictionary<int, string> ReadIcons(ZipArchive zip, string sheetPath, int column, string iconDir)
    {
        var result = new Dictionary<int, string>();
        try
        {
            var sheetDir = sheetPath.Substring(0, sheetPath.LastIndexOf('/'));
            var relsPath = sheetDir + "/_rels/" + sheetPath.Substring(sheetPath.LastIndexOf('/') + 1) + ".rels";
            if(zip.GetEntry(relsPath) == null) { return result; }
            var drawingTarget = Load(zip, relsPath).Descendants().FirstOrDefault(r => ((string)r.Attribute("Type") ?? string.Empty).EndsWith("/drawing"))?.Attribute("Target")?.Value;
            if(drawingTarget == null) { return result; }
            var drawingPath = Resolve(sheetDir, drawingTarget);
            if(zip.GetEntry(drawingPath) == null) { return result; }

            var drawingDir = drawingPath.Substring(0, drawingPath.LastIndexOf('/'));
            var drawingRels = drawingDir + "/_rels/" + drawingPath.Substring(drawingPath.LastIndexOf('/') + 1) + ".rels";
            if(zip.GetEntry(drawingRels) == null) { return result; }
            var media = Load(zip, drawingRels).Descendants().Where(r => r.Attribute("Id") != null)
                .ToDictionary(r => (string)r.Attribute("Id"), r => (string)r.Attribute("Target"));

            Directory.CreateDirectory(iconDir);
            foreach(var anchor in Load(zip, drawingPath).Root.Elements())
            {
                var from = anchor.Element(Xdr + "from");
                var blip = anchor.Descendants(Dml + "blip").FirstOrDefault();
                var embed = (string)blip?.Attribute(Rel + "embed");
                if(from == null || embed == null || !media.TryGetValue(embed, out var target)) { continue; }
                if(!int.TryParse((string)from.Element(Xdr + "col"), out var col) || col != column) { continue; }
                if(!int.TryParse((string)from.Element(Xdr + "row"), out var row)) { continue; }

                var entry = zip.GetEntry(Resolve(drawingDir, target));
                if(entry == null) { continue; }
                var file = Path.Combine(iconDir, Path.GetFileName(entry.FullName));
                if(!File.Exists(file) || new FileInfo(file).Length != entry.Length)
                {
                    using(var input = entry.Open()) using(var output = File.Create(file)) { input.CopyTo(output); }
                }
                result[row + 1] = file;
            }
        }
        catch(Exception e) when(e is IOException || e is InvalidDataException || e is UnauthorizedAccessException || e is System.Xml.XmlException) { }
        return result;
    }

    //A relationship target relative to a folder inside the package ("../media/x.png" from "xl/drawings")
    private static string Resolve(string folder, string target)
    {
        if(target.StartsWith("/")) { return target.TrimStart('/'); }
        var parts = new List<string>(folder.Split('/'));
        foreach(var piece in target.Split('/'))
        {
            if(piece == "..") { if(parts.Count > 0) { parts.RemoveAt(parts.Count - 1); } }
            else if(piece != ".") { parts.Add(piece); }
        }
        return string.Join("/", parts);
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
