using NUnit.Framework;

public class MacAppLocationTests
{
    private const string Translocated = "/private/var/folders/ab/cdef/T/AppTranslocation/6CA31E3C-0000-4B4B-9999-AAAAAAAAAAAA/d/Super4PUnity.app";

    [Test]
    public void IsTranslocated_only_for_randomized_paths()
    {
        Assert.IsTrue(MacAppLocation.IsTranslocated(Translocated));
        Assert.IsFalse(MacAppLocation.IsTranslocated("/Users/me/Downloads/Super4PUnity_v1.2.1/Super4PUnity.app"));
        Assert.IsFalse(MacAppLocation.IsTranslocated(null));
        Assert.IsFalse(MacAppLocation.IsTranslocated(""));
    }

    [Test]
    public void Original_path_when_the_mount_source_is_the_containing_folder()
    {
        var mount = "/dev/disk3s1s1 on / (apfs, sealed, local, read-only, journaled)\n" +
            "/Users/me/Downloads/Super4PUnity_v1.2.1 on /private/var/folders/ab/cdef/T/AppTranslocation/6CA31E3C-0000-4B4B-9999-AAAAAAAAAAAA/d (nullfs, local, nodev, nosuid, read-only, nobrowse, mounted by me)\n";
        Assert.AreEqual("/Users/me/Downloads/Super4PUnity_v1.2.1/Super4PUnity.app", MacAppLocation.OriginalPathFromMount(mount, Translocated));
    }

    [Test]
    public void Original_path_when_the_mount_source_is_the_app_itself()
    {
        var mount = "/Users/me/Downloads/Super4PUnity_v1.2.1/Super4PUnity.app on /private/var/folders/ab/cdef/T/AppTranslocation/6CA31E3C-0000-4B4B-9999-AAAAAAAAAAAA/d/Super4PUnity.app (nullfs, local, nodev, nosuid, read-only, nobrowse, mounted by me)\n";
        Assert.AreEqual("/Users/me/Downloads/Super4PUnity_v1.2.1/Super4PUnity.app", MacAppLocation.OriginalPathFromMount(mount, Translocated));
    }

    [Test]
    public void Original_path_from_a_real_macOS_mount_line()
    {
        //Captured from macOS: the source is the original .app; the mount point is the folder above "d"
        var path = "/private/var/folders/3j/p74s59795sbd6dzd0bpp10cw0000gn/T/AppTranslocation/AFA5E059-3A18-4465-89F1-3C8F9B3EE8AA/d/Super4PUnity.app";
        var mount = "/dev/disk3s1s1 on / (apfs, sealed, local, read-only, journaled)\n" +
            "/Users/me/Downloads/Super4PUnity_v1.2.1/Super4PUnity.app on /private/var/folders/3j/p74s59795sbd6dzd0bpp10cw0000gn/T/AppTranslocation/AFA5E059-3A18-4465-89F1-3C8F9B3EE8AA (nullfs, local, nodev, nosuid, read-only, nobrowse, mounted by me)\n";
        Assert.AreEqual("/Users/me/Downloads/Super4PUnity_v1.2.1/Super4PUnity.app", MacAppLocation.OriginalPathFromMount(mount, path));
    }

    [Test]
    public void Folder_names_with_spaces_are_kept()
    {
        var mount = "/Users/me/My Games/Super 4PUnity on /private/var/folders/ab/cdef/T/AppTranslocation/6CA31E3C-0000-4B4B-9999-AAAAAAAAAAAA/d (nullfs, local, read-only)\n";
        Assert.AreEqual("/Users/me/My Games/Super 4PUnity/Super4PUnity.app", MacAppLocation.OriginalPathFromMount(mount, Translocated));
    }

    [Test]
    public void Unrelated_mounts_and_garbage_give_null()
    {
        var other = "/Users/me/Other on /private/var/folders/ab/cdef/T/AppTranslocation/11111111-0000-0000-0000-000000000000/d (nullfs, local)\n";
        Assert.IsNull(MacAppLocation.OriginalPathFromMount(other, Translocated));
        Assert.IsNull(MacAppLocation.OriginalPathFromMount("", Translocated));
        Assert.IsNull(MacAppLocation.OriginalPathFromMount(null, Translocated));
        Assert.IsNull(MacAppLocation.OriginalPathFromMount("not a mount line", Translocated));
        Assert.IsNull(MacAppLocation.OriginalPathFromMount("/x on /y (apfs, local)", "/y/Super4PUnity.app"));
    }

    [Test]
    public void ModsFolderNextToApp_is_always_the_mods_folder_beside_the_app_even_when_it_does_not_exist_yet()
    {
        Assert.AreEqual("/Users/me/Downloads/Super4PUnity_v1.2.1/mods", MacAppLocation.ModsFolderNextToApp("/Users/me/Downloads/Super4PUnity_v1.2.1/Super4PUnity.app"));
    }

    [Test]
    public void ModsFolderNextToApp_tolerates_a_trailing_slash_and_spaces()
    {
        Assert.AreEqual("/Users/me/Game/mods", MacAppLocation.ModsFolderNextToApp("/Users/me/Game/Super4PUnity.app/"));
        Assert.AreEqual("/Users/me/My Games/Super 4PUnity/mods", MacAppLocation.ModsFolderNextToApp("/Users/me/My Games/Super 4PUnity/Super4PUnity.app"));
    }

    [Test]
    public void ModsFolderNextToApp_gives_null_without_a_usable_app_path()
    {
        Assert.IsNull(MacAppLocation.ModsFolderNextToApp(null));
        Assert.IsNull(MacAppLocation.ModsFolderNextToApp(""));
        Assert.IsNull(MacAppLocation.ModsFolderNextToApp("Super4PUnity.app"));
    }
}
