using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

//Small dependency-free JSON reader: objects -> Dictionary<string, object>, arrays -> List<object>,
//numbers -> double, plus string, bool and null. Throws FormatException on invalid input.
public static class MiniJson
{
    public static object Parse(string json)
    {
        if(json == null) { throw new FormatException("null json"); }
        var reader = new Reader(json);
        var value = reader.ReadValue();
        reader.SkipWhitespace();
        if(!reader.AtEnd) { throw new FormatException("trailing characters"); }
        return value;
    }

    private sealed class Reader
    {
        private readonly string s;
        private int i;

        public Reader(string text) { s = text; }
        public bool AtEnd => i >= s.Length;

        public void SkipWhitespace()
        {
            while(i < s.Length && char.IsWhiteSpace(s[i])) { i++; }
        }

        public object ReadValue()
        {
            SkipWhitespace();
            if(AtEnd) { throw new FormatException("unexpected end"); }

            switch(s[i])
            {
                case '{': return ReadObject();
                case '[': return ReadArray();
                case '"': return ReadString();
                case 't': Expect("true"); return true;
                case 'f': Expect("false"); return false;
                case 'n': Expect("null"); return null;
                default: return ReadNumber();
            }
        }

        private Dictionary<string, object> ReadObject()
        {
            var result = new Dictionary<string, object>();
            i++;
            SkipWhitespace();
            if(Peek('}')) { i++; return result; }

            while(true)
            {
                SkipWhitespace();
                if(AtEnd || s[i] != '"') { throw new FormatException("expected key"); }
                var key = ReadString();
                SkipWhitespace();
                if(AtEnd || s[i] != ':') { throw new FormatException("expected ':'"); }
                i++;
                result[key] = ReadValue();
                SkipWhitespace();
                if(AtEnd) { throw new FormatException("unterminated object"); }
                if(s[i] == ',') { i++; continue; }
                if(s[i] == '}') { i++; return result; }
                throw new FormatException("expected ',' or '}'");
            }
        }

        private List<object> ReadArray()
        {
            var result = new List<object>();
            i++;
            SkipWhitespace();
            if(Peek(']')) { i++; return result; }

            while(true)
            {
                result.Add(ReadValue());
                SkipWhitespace();
                if(AtEnd) { throw new FormatException("unterminated array"); }
                if(s[i] == ',') { i++; continue; }
                if(s[i] == ']') { i++; return result; }
                throw new FormatException("expected ',' or ']'");
            }
        }

        private string ReadString()
        {
            var sb = new StringBuilder();
            i++;
            while(true)
            {
                if(AtEnd) { throw new FormatException("unterminated string"); }
                var c = s[i++];
                if(c == '"') { return sb.ToString(); }
                if(c != '\\') { sb.Append(c); continue; }

                if(AtEnd) { throw new FormatException("bad escape"); }
                var e = s[i++];
                switch(e)
                {
                    case '"': sb.Append('"'); break;
                    case '\\': sb.Append('\\'); break;
                    case '/': sb.Append('/'); break;
                    case 'b': sb.Append('\b'); break;
                    case 'f': sb.Append('\f'); break;
                    case 'n': sb.Append('\n'); break;
                    case 'r': sb.Append('\r'); break;
                    case 't': sb.Append('\t'); break;
                    case 'u':
                        if(i + 4 > s.Length || !int.TryParse(s.Substring(i, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var code)) { throw new FormatException("bad unicode escape"); }
                        sb.Append((char)code);
                        i += 4;
                        break;
                    default: throw new FormatException("bad escape");
                }
            }
        }

        private object ReadNumber()
        {
            var start = i;
            while(i < s.Length && "+-0123456789.eE".IndexOf(s[i]) >= 0) { i++; }
            if(i == start || !double.TryParse(s.Substring(start, i - start), NumberStyles.Float, CultureInfo.InvariantCulture, out var n)) { throw new FormatException("bad value"); }
            return n;
        }

        private bool Peek(char c) { return i < s.Length && s[i] == c; }

        private void Expect(string word)
        {
            if(string.CompareOrdinal(s, i, word, 0, word.Length) != 0) { throw new FormatException("bad literal"); }
            i += word.Length;
        }
    }
}
