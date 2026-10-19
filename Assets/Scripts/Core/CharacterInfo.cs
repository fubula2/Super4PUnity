using System.Text.RegularExpressions;

public static class CharacterInfo
{
    private static readonly Regex CountOnly = new Regex(@"^\d+\+?$");

    //False when the sheet shows only a count ("61+"), a dash or nothing instead of character names
    public static bool NamesKnown(string characterCell)
    {
        if(string.IsNullOrWhiteSpace(characterCell)) { return false; }
        var cell = characterCell.Trim();
        return cell != "-" && !CountOnly.IsMatch(cell);
    }
}
