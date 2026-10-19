using NUnit.Framework;

public class AndroidFolderLinkTests
{
    private const string Mods = "/storage/emulated/0/Android/media/com.nephtri.super4punity/mods";

    [Test]
    public void Friendly_shows_internal_storage_and_each_folder()
    {
        Assert.AreEqual("Internal storage > Android > media > com.nephtri.super4punity > mods", AndroidFolderLink.Friendly(Mods));
        Assert.AreEqual("Internal storage > mods", AndroidFolderLink.Friendly("/sdcard/mods/"));
    }

    [Test]
    public void Friendly_keeps_unknown_locations_as_they_are()
    {
        Assert.AreEqual("/mnt/media_rw/ABCD-1234/mods", AndroidFolderLink.Friendly("/mnt/media_rw/ABCD-1234/mods"));
    }

    [Test]
    public void DocumentUri_encodes_the_relative_path()
    {
        Assert.AreEqual("content://com.android.externalstorage.documents/document/primary%3AAndroid%2Fmedia%2Fcom.nephtri.super4punity%2Fmods", AndroidFolderLink.DocumentUri(Mods));
    }

    [Test]
    public void DocumentUri_is_null_outside_primary_storage_or_empty()
    {
        Assert.IsNull(AndroidFolderLink.DocumentUri("/mnt/media_rw/ABCD-1234/mods"));
        Assert.IsNull(AndroidFolderLink.DocumentUri(""));
        Assert.IsNull(AndroidFolderLink.DocumentUri(null));
    }
}
