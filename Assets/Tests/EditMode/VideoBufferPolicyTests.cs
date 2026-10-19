using NUnit.Framework;

public class VideoBufferPolicyTests
{
    [Test]
    public void Desktop_always_gets_720p()
    {
        Assert.AreEqual((1280, 720), VideoBufferPolicy.For(1024, false));
        Assert.AreEqual((1280, 720), VideoBufferPolicy.For(32000, false));
    }

    [Test]
    public void Phones_get_a_buffer_that_fits_their_memory()
    {
        Assert.AreEqual((854, 480), VideoBufferPolicy.For(2048, true));
        Assert.AreEqual((960, 540), VideoBufferPolicy.For(3072, true));
        Assert.AreEqual((960, 540), VideoBufferPolicy.For(4500, true));
        Assert.AreEqual((1280, 720), VideoBufferPolicy.For(6000, true));
    }

    [Test]
    public void Unknown_memory_zero_is_treated_as_low()
    {
        Assert.AreEqual((854, 480), VideoBufferPolicy.For(0, true));
    }
}
