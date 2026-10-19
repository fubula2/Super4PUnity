//One music provider (Google Drive today). Pure link logic lives here in Core so it can be tested;
//the runtime fetches ListingUrl(...) pages and parses them with the provider's parser.
public interface IMusicSource
{
    string Name { get; }
    bool Handles(System.Uri url);
    string RootFolderId(string folderUrl);   //null when the link is not a folder link
    string ListingUrl(string folderId);
    string FileUrl(string fileId);
    System.Collections.Generic.IReadOnlyList<MusicEntry> ParseListing(string body);
}
