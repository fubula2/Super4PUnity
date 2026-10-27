using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security;
using System.Text;

//Builds a minimal xlsx in memory: one named sheet, shared strings, numeric cells and external hyperlinks
public class XlsxFixture
{
    public class Cell { public string Ref; public string Text; public double? Number; }

    private readonly List<Cell> cells = new List<Cell>();
    private readonly Dictionary<string, string> links = new Dictionary<string, string>();
    private readonly string sheetName;
    private readonly List<(int Col, int Row, byte[] Data)> images = new List<(int, int, byte[])>();

    public XlsxFixture(string sheetName = "ModsDownloads") { this.sheetName = sheetName; }

    public XlsxFixture Text(string cellRef, string text) { cells.Add(new Cell { Ref = cellRef, Text = text }); return this; }
    public XlsxFixture Number(string cellRef, double value) { cells.Add(new Cell { Ref = cellRef, Number = value }); return this; }
    public XlsxFixture Link(string cellRef, string url, string displayText = "Download") { Text(cellRef, displayText); links[cellRef] = url; return this; }

    //A picture anchored at the cell (like the series logos in the real sheet)
    public XlsxFixture Image(string cellRef, byte[] data)
    {
        var col = 0;
        foreach(var ch in cellRef.TakeWhile(char.IsLetter)) { col = col * 26 + (ch - 'A' + 1); }
        images.Add((col - 1, int.Parse(new string(cellRef.Where(char.IsDigit).ToArray())) - 1, data));
        return this;
    }

    public Stream Build()
    {
        var strings = new List<string>();
        int Idx(string s) { var i = strings.IndexOf(s); if(i < 0) { strings.Add(s); i = strings.Count - 1; } return i; }

        var sheet = new StringBuilder("<worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\"><sheetData>");
        foreach(var row in cells.GroupBy(c => int.Parse(new string(c.Ref.Where(char.IsDigit).ToArray()))).OrderBy(g => g.Key))
        {
            sheet.Append("<row r=\"" + row.Key + "\">");
            foreach(var c in row.OrderBy(c => c.Ref.Length).ThenBy(c => c.Ref))
            {
                if(c.Number.HasValue) { sheet.Append("<c r=\"" + c.Ref + "\"><v>" + c.Number.Value.ToString(System.Globalization.CultureInfo.InvariantCulture) + "</v></c>"); }
                else { sheet.Append("<c r=\"" + c.Ref + "\" t=\"s\"><v>" + Idx(c.Text) + "</v></c>"); }
            }
            sheet.Append("</row>");
        }
        sheet.Append("</sheetData>");
        if(links.Count > 0)
        {
            sheet.Append("<hyperlinks>");
            var n = 1;
            foreach(var l in links) { sheet.Append("<hyperlink ref=\"" + l.Key + "\" r:id=\"rId" + n++ + "\"/>"); }
            sheet.Append("</hyperlinks>");
        }
        if(images.Count > 0) { sheet.Append("<drawing r:id=\"rIdDraw\"/>"); }
        sheet.Append("</worksheet>");

        var rels = new StringBuilder("<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">");
        var k = 1;
        foreach(var l in links) { rels.Append("<Relationship Id=\"rId" + k++ + "\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/hyperlink\" Target=\"" + SecurityElement.Escape(l.Value) + "\" TargetMode=\"External\"/>"); }
        if(images.Count > 0) { rels.Append("<Relationship Id=\"rIdDraw\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/drawing\" Target=\"../drawings/drawing1.xml\"/>"); }
        rels.Append("</Relationships>");

        var ms = new MemoryStream();
        using(var zip = new ZipArchive(ms, ZipArchiveMode.Create, true))
        {
            Add(zip, "xl/workbook.xml", "<workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\"><sheets><sheet name=\"" + SecurityElement.Escape(sheetName) + "\" sheetId=\"1\" r:id=\"rId1\"/></sheets></workbook>");
            Add(zip, "xl/_rels/workbook.xml.rels", "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"/xl/worksheets/sheet1.xml\"/></Relationships>");
            Add(zip, "xl/sharedStrings.xml", "<sst xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\">" + string.Concat(strings.Select(s => "<si><t xml:space=\"preserve\">" + SecurityElement.Escape(s) + "</t></si>")) + "</sst>");
            Add(zip, "xl/worksheets/sheet1.xml", sheet.ToString());
            Add(zip, "xl/worksheets/_rels/sheet1.xml.rels", rels.ToString());
            if(images.Count > 0)
            {
                var drawing = new StringBuilder("<xdr:wsDr xmlns:xdr=\"http://schemas.openxmlformats.org/drawingml/2006/spreadsheetDrawing\" xmlns:a=\"http://schemas.openxmlformats.org/drawingml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\">");
                var drels = new StringBuilder("<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">");
                for(var i = 0; i < images.Count; i++)
                {
                    var img = images[i];
                    drawing.Append("<xdr:oneCellAnchor><xdr:from><xdr:col>" + img.Col + "</xdr:col><xdr:colOff>0</xdr:colOff><xdr:row>" + img.Row + "</xdr:row><xdr:rowOff>0</xdr:rowOff></xdr:from><xdr:ext cx=\"857250\" cy=\"857250\"/><xdr:pic><xdr:blipFill><a:blip r:embed=\"rId" + (i + 1) + "\"/></xdr:blipFill></xdr:pic></xdr:oneCellAnchor>");
                    drels.Append("<Relationship Id=\"rId" + (i + 1) + "\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/image\" Target=\"../media/image" + (i + 1) + ".png\"/>");
                    var media = zip.CreateEntry("xl/media/image" + (i + 1) + ".png");
                    using(var s = media.Open()) { s.Write(img.Data, 0, img.Data.Length); }
                }
                drawing.Append("</xdr:wsDr>");
                drels.Append("</Relationships>");
                Add(zip, "xl/drawings/drawing1.xml", drawing.ToString());
                Add(zip, "xl/drawings/_rels/drawing1.xml.rels", drels.ToString());
            }
        }
        ms.Position = 0;
        return ms;
    }

    private static void Add(ZipArchive zip, string name, string content)
    {
        var entry = zip.CreateEntry(name);
        using(var w = new StreamWriter(entry.Open(), new UTF8Encoding(false))) { w.Write(content); }
    }
}
