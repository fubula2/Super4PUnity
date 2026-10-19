using System.Collections.Generic;
using System.Linq;

//FIFO queue where each entry has a unique key
public sealed class DownloadQueue<T>
{
    private readonly List<KeyValuePair<string, T>> items = new List<KeyValuePair<string, T>>();

    public int Count => items.Count;
    public IEnumerable<string> Keys => items.Select(i => i.Key);

    public bool Contains(string key) { return items.Any(i => i.Key == key); }

    public bool Enqueue(string key, T item)
    {
        if(Contains(key)) { return false; }
        items.Add(new KeyValuePair<string, T>(key, item));
        return true;
    }

    public bool Remove(string key)
    {
        var index = items.FindIndex(i => i.Key == key);
        if(index < 0) { return false; }
        items.RemoveAt(index);
        return true;
    }

    public bool TryDequeue(out T item)
    {
        if(items.Count == 0) { item = default; return false; }
        item = items[0].Value;
        items.RemoveAt(0);
        return true;
    }
}
