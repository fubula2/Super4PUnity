using System.Linq;
using NUnit.Framework;

public class FfmpegArgsTests
{
    [Test]
    public void Convert_keeps_paths_as_single_arguments()
    {
        var args = FfmpegArgs.Convert("/mods/CHAR_A B/it's \"x\".mp4", "/c/out file.webm.tmp");
        Assert.IsTrue(args.Contains("/mods/CHAR_A B/it's \"x\".mp4"));
        Assert.AreEqual("/c/out file.webm.tmp", args.Last());
        Assert.AreEqual("-i", args[args.ToList().IndexOf("/mods/CHAR_A B/it's \"x\".mp4") - 1]);
    }

    [Test]
    public void Convert_requests_webm_with_vp8_and_vorbis()
    {
        var args = FfmpegArgs.Convert("in.mp4", "out.webm");
        CollectionAssert.IsSubsetOf(new[] { "libvpx", "libvorbis", "webm" }, args.ToList());
    }

    private const string Good =
        "Encoders:\n V..... = Video\n ------\n V....D libvpx               libvpx VP8 (codec vp8)\n A....D libvorbis            libvorbis (codec vorbis)\n";

    [Test]
    public void Encoders_output_with_libvpx_and_libvorbis_passes()
    {
        Assert.IsTrue(FfmpegArgs.HasRequiredEncoders(Good));
    }

    [Test]
    public void Encoders_output_missing_libvpx_or_empty_fails()
    {
        Assert.IsFalse(FfmpegArgs.HasRequiredEncoders(" A....D libvorbis            libvorbis (codec vorbis)\n V....D libvpx-vp9  libvpx VP9\n"));
        Assert.IsFalse(FfmpegArgs.HasRequiredEncoders(""));
        Assert.IsFalse(FfmpegArgs.HasRequiredEncoders(null));
    }
}
