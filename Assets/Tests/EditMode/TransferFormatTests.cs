using NUnit.Framework;

public class TransferFormatTests
{
    [Test]
    public void Speed_formats_MB_and_KB_per_second()
    {
        Assert.AreEqual("5.2 MB/s", TransferFormat.Speed(5.2 * 1048576));
        Assert.AreEqual("500 KB/s", TransferFormat.Speed(512000));
        Assert.AreEqual("0 KB/s", TransferFormat.Speed(0));
        Assert.AreEqual("0 KB/s", TransferFormat.Speed(-5));
        Assert.AreEqual("0 KB/s", TransferFormat.Speed(double.NaN));
    }

    [Test]
    public void TimeLeft_formats_minutes_and_hours()
    {
        Assert.AreEqual("0:12", TransferFormat.TimeLeft(12));
        Assert.AreEqual("1:05", TransferFormat.TimeLeft(65));
        Assert.AreEqual("1:02:03", TransferFormat.TimeLeft(3723));
        Assert.AreEqual("--:--", TransferFormat.TimeLeft(-1));
        Assert.AreEqual("--:--", TransferFormat.TimeLeft(double.NaN));
        Assert.AreEqual("--:--", TransferFormat.TimeLeft(double.PositiveInfinity));
    }

    [Test]
    public void Size_formats_MB_and_GB()
    {
        Assert.AreEqual("28 MB", TransferFormat.Size(29471520));
        Assert.AreEqual("1.5 GB", TransferFormat.Size((long)(1.5 * (1L << 30))));
        Assert.AreEqual("1 MB", TransferFormat.Size(10));
    }

    [Test]
    public void InlineProgress_invokes_handler_synchronously_on_caller_thread()
    {
        var seen = -1;
        var progress = new InlineProgress<int>(v => seen = v);
        progress.Report(7);
        Assert.AreEqual(7, seen);
    }
}
