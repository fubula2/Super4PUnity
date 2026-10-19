public enum MusicEntryKind { Folder, File }

public sealed class MusicEntry
{
    public string Id;
    public string Name;
    public MusicEntryKind Kind;
}
