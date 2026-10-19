using System;
using System.Globalization;

public static class ExcelDate
{
    //Excel stores dates as days since 1899-12-30; the time-of-day fraction is dropped
    public static DateTime? FromSerial(string cell)
    {
        if(string.IsNullOrWhiteSpace(cell)) { return null; }
        if(!double.TryParse(cell.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var serial)) { return null; }
        if(serial < 1 || serial > 2958465) { return null; }
        return new DateTime(1899, 12, 30).AddDays(Math.Floor(serial));
    }
}
