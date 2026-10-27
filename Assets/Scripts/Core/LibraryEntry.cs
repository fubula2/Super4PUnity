using System;
using System.Collections.Generic;

public enum LinkKind { File, Folder, Browser }

public sealed class LibraryEntry
{
    public string Series;
    public string Characters;
    public string Category;
    public DateTime? Updated;
    public string Status;
    public string Url;
    public LinkKind Kind;
    public int Row;           //row number on the sheet
    public string IconFile;   //series logo extracted from the sheet (local path), or null
    public string IconText;   //short text shown in place of a logo (the sheet's letters, else the first letter of the series)
}

public sealed class LibraryCatalog
{
    public IReadOnlyList<LibraryEntry> Entries = new List<LibraryEntry>();
    public string Error;
    //Set when the list comes from the saved copy because the live sheet could not be reached
    public string Warning;
    public string MusicFolderUrl;   //link under the sheet's "Music Mods" heading; null when absent
}
