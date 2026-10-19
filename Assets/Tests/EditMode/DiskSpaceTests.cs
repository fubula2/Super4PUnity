using NUnit.Framework;

public class DiskSpaceTests
{
    [Test]
    public void Needs_2_2_times_pack_size()
    {
        Assert.IsTrue(DiskSpace.HasRoom(2200, 1000));
        Assert.IsFalse(DiskSpace.HasRoom(2199, 1000));
        Assert.IsTrue(DiskSpace.HasRoom(long.MaxValue, 2_500_000_000L));
    }

    [Test]
    public void Zero_or_negative_values_are_not_enough()
    {
        Assert.IsFalse(DiskSpace.HasRoom(1000, 0));
        Assert.IsFalse(DiskSpace.HasRoom(1000, -5));
        Assert.IsFalse(DiskSpace.HasRoom(-1, 10));
    }
}
