using NUnit.Framework;
using System.Linq;

public class DownloadQueueTests
{
    [Test]
    public void Is_first_in_first_out()
    {
        var q = new DownloadQueue<string>();
        q.Enqueue("a", "A");
        q.Enqueue("b", "B");
        Assert.IsTrue(q.TryDequeue(out var first));
        Assert.AreEqual("A", first);
        Assert.IsTrue(q.TryDequeue(out var second));
        Assert.AreEqual("B", second);
        Assert.IsFalse(q.TryDequeue(out _));
    }

    [Test]
    public void Duplicate_key_is_rejected()
    {
        var q = new DownloadQueue<string>();
        Assert.IsTrue(q.Enqueue("a", "A"));
        Assert.IsFalse(q.Enqueue("a", "A2"));
        Assert.AreEqual(1, q.Count);
        Assert.IsTrue(q.Contains("a"));
    }

    [Test]
    public void Remove_deletes_only_that_key_and_keeps_order()
    {
        var q = new DownloadQueue<string>();
        q.Enqueue("a", "A"); q.Enqueue("b", "B"); q.Enqueue("c", "C");
        Assert.IsTrue(q.Remove("b"));
        Assert.IsFalse(q.Remove("b"));
        CollectionAssert.AreEqual(new[] { "a", "c" }, q.Keys.ToArray());
    }

    [Test]
    public void Key_can_be_queued_again_after_dequeue()
    {
        var q = new DownloadQueue<string>();
        q.Enqueue("a", "A");
        q.TryDequeue(out _);
        Assert.IsTrue(q.Enqueue("a", "A again"));
    }
}
