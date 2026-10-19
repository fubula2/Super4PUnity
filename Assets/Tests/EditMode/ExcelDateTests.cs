using System;
using NUnit.Framework;

public class ExcelDateTests
{
    [Test]
    public void Serial_converts_to_date()
    {
        Assert.AreEqual(new DateTime(2020, 8, 18), ExcelDate.FromSerial("44061"));
        Assert.AreEqual(new DateTime(2020, 8, 18), ExcelDate.FromSerial("44061.0"));
        Assert.AreEqual(new DateTime(2020, 8, 18), ExcelDate.FromSerial("44061.75"));
    }

    [Test]
    public void Dash_empty_text_and_nonpositive_are_null()
    {
        Assert.IsNull(ExcelDate.FromSerial("-"));
        Assert.IsNull(ExcelDate.FromSerial(""));
        Assert.IsNull(ExcelDate.FromSerial(null));
        Assert.IsNull(ExcelDate.FromSerial("abc"));
        Assert.IsNull(ExcelDate.FromSerial("0"));
        Assert.IsNull(ExcelDate.FromSerial("-5"));
    }
}
