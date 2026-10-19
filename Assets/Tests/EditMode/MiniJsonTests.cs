using System.Collections.Generic;
using NUnit.Framework;

public class MiniJsonTests
{
    [Test]
    public void Parses_nested_values()
    {
        var o = (Dictionary<string, object>)MiniJson.Parse("{\"a\":[1,2.5,true,null,\"x\"],\"b\":{\"c\":\"d\"}}");
        var a = (List<object>)o["a"];
        Assert.AreEqual(1d, a[0]);
        Assert.AreEqual(2.5d, a[1]);
        Assert.AreEqual(true, a[2]);
        Assert.IsNull(a[3]);
        Assert.AreEqual("x", a[4]);
        Assert.AreEqual("d", ((Dictionary<string, object>)o["b"])["c"]);
    }

    [Test]
    public void Invalid_input_throws_FormatException()
    {
        Assert.Throws<System.FormatException>(() => MiniJson.Parse("{\"a\":"));
        Assert.Throws<System.FormatException>(() => MiniJson.Parse("[1,2"));
        Assert.Throws<System.FormatException>(() => MiniJson.Parse("nope"));
        Assert.Throws<System.FormatException>(() => MiniJson.Parse("{} trailing"));
    }
}
