using NUnit.Framework;

public class ConversionSettingsTests
{
    [Test]
    public void Small_machines_still_get_one_worker()
    {
        Assert.AreEqual(1, ConversionSettings.Workers(1, 10));
        Assert.AreEqual(1, ConversionSettings.Workers(2, 10));
        Assert.AreEqual(1, ConversionSettings.Workers(0, 10));
    }

    [Test]
    public void Workers_grow_with_the_cores_without_a_fixed_cap()
    {
        Assert.AreEqual(2, ConversionSettings.Workers(4, 20));
        Assert.AreEqual(8, ConversionSettings.Workers(16, 20));
        Assert.AreEqual(16, ConversionSettings.Workers(32, 40));
    }

    [Test]
    public void Never_more_workers_than_clips()
    {
        Assert.AreEqual(3, ConversionSettings.Workers(32, 3));
        Assert.AreEqual(1, ConversionSettings.Workers(32, 0));
    }

    [Test]
    public void Slow_machines_use_the_faster_encoder_setting()
    {
        Assert.AreEqual(8, ConversionSettings.CpuUsed(2));
        Assert.AreEqual(6, ConversionSettings.CpuUsed(8));
    }
}
