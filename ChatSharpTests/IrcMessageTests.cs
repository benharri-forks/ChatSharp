using System.Linq;

namespace ChatSharp.Tests;

[TestClass]
public class IrcMessageTests
{
    [TestMethod]
    public void NewValidMessage()
    {
        try
        {
            _ = new IrcMessage(":user!~ident@host PRIVMSG target :Lorem ipsum dolor sit amet");
        }
        catch (Exception e)
        {
            Assert.Fail("Expected no exception, got: {0}", e.Message);
        }
    }

    [TestMethod]
    public void NewInvalidMessage()
    {
        Assert.ThrowsException<ArgumentException>(() => new IrcMessage("USER", "user", "0 *", "real name").Format());
    }

    [TestMethod]
    public void NewValidMessage_Command()
    {
        IrcMessage fromMessage = new(":user!~ident@host PRIVMSG target :Lorem ipsum dolor sit amet");
        Assert.AreEqual("PRIVMSG", fromMessage.Command);
    }

    [TestMethod]
    public void NewValidMessage_Prefix()
    {
        IrcMessage fromMessage = new(":user!~ident@host PRIVMSG target :Lorem ipsum dolor sit amet");
        Assert.AreEqual("user!~ident@host", fromMessage.Source);
    }

    [TestMethod]
    public void NewValidMessage_Params()
    {
        IrcMessage fromMessage = new(":user!~ident@host PRIVMSG target :Lorem ipsum dolor sit amet");
        var compareParams = new[] { "target", "Lorem ipsum dolor sit amet" };
        
        CollectionAssert.AreEqual(compareParams, fromMessage.Parameters);
    }

    [TestMethod]
    public void NewValidMessage_UppercaseCommand()
    {
        IrcMessage fromMessage = new(":user!~ident@host privmsg target :Lorem ipsum dolor sit amet");
        Assert.AreEqual("PRIVMSG", fromMessage.Command);
    }

    [TestMethod]
    public void NewValidMessage_Tags()
    {
        IrcMessage fromMessage =
            new("@a=123;b=456;c=789 :user!~ident@host PRIVMSG target :Lorem ipsum dolor sit amet");
        
        var compareTags = new[]
        {
            new KeyValuePair<string, string>("a", "123"),
            new KeyValuePair<string, string>("b", "456"),
            new KeyValuePair<string, string>("c", "789")
        };
        
        CollectionAssert.AreEqual(compareTags, fromMessage.Tags);
    }

    [TestMethod]
    public void NewValidMessage_Tags02()
    {
        IrcMessage fromMessage = new("@aaa=bbb;ccc;example.com/ddd=eee :nick!ident@host.com PRIVMSG me :Hello");
        
        var compareTags = new[]
        {
            new KeyValuePair<string, string>("aaa", "bbb"),
            new KeyValuePair<string, string>("ccc", null),
            new KeyValuePair<string, string>("example.com/ddd", "eee")
        };
        
        CollectionAssert.AreEqual(fromMessage.Tags, compareTags);
    }

    [TestMethod]
    public void NewValidMessage_TagsWithSemicolon()
    {
        IrcMessage fromMessage =
            new(@"@a=123\:456;b=456\:789;c=789\:123 :user!~ident@host PRIVMSG target :Lorem ipsum dolor sit amet");
        
        var compareTags = new[]
        {
            new KeyValuePair<string, string>("a", "123;456"),
            new KeyValuePair<string, string>("b", "456;789"),
            new KeyValuePair<string, string>("c", "789;123")
        };
        
        CollectionAssert.AreEqual(fromMessage.Tags, compareTags);
    }

    [TestMethod]
    public void NewValidMessage_TagsNoValue()
    {
        IrcMessage fromMessage = new("@a=;b :nick!ident@host.com PRIVMSG me :Hello");
        
        var compareTags = new[]
        {
            new KeyValuePair<string, string>("a", ""),
            new KeyValuePair<string, string>("b", null)
        };
        
        CollectionAssert.AreEqual(fromMessage.Tags, compareTags);
    }
    
    [TestMethod]
    public void TagsMissing()
    {
        var line = new IrcMessage("PRIVMSG #channel");
        Assert.IsNull(line.Tags);
    }

    [TestMethod]
    public void TagsMissingValue()
    {
        var line = new IrcMessage("@id= PRIVMSG #channel");
        Assert.AreEqual(string.Empty, line.Tags["id"]);
    }

    [TestMethod]
    public void TagsMissingEqual()
    {
        var line = new IrcMessage("@id PRIVMSG #channel");
        Assert.IsNull(line.Tags["id"]);
    }

    [TestMethod]
    public void TagsUnescape()
    {
        var line = new IrcMessage(@"@id=1\\\:\r\n\s2 PRIVMSG #channel");
        Assert.AreEqual("1\\;\r\n 2", line.Tags["id"]);
    }

    [TestMethod]
    public void TagsOverlap()
    {
        var line = new IrcMessage(@"@id=1\\\s\\s PRIVMSG #channel");
        Assert.AreEqual(@"1\ \s", line.Tags["id"]);
    }

    [TestMethod]
    public void TagsLoneEndSlash()
    {
        var line = new IrcMessage("@id=1\\ PRIVMSG #channel");
        Assert.AreEqual("1", line.Tags["id"]);
    }

    [TestMethod]
    public void SourceWithoutTags()
    {
        var line = new IrcMessage(":nick!user@host PRIVMSG #channel");
        Assert.AreEqual("nick!user@host", line.Source);
    }

    [TestMethod]
    public void SourceWithTags()
    {
        var line = new IrcMessage("@id=123 :nick!user@host PRIVMSG #channel");
        Assert.AreEqual("nick!user@host", line.Source);
    }

    [TestMethod]
    public void SourceMissingWithoutTags()
    {
        var line = new IrcMessage("PRIVMSG #channel");
        Assert.IsNull(line.Source);
    }

    [TestMethod]
    public void SourceMissingWithTags()
    {
        var line = new IrcMessage("@id=123 PRIVMSG #channel");
        Assert.IsNull(line.Source);
    }

    [TestMethod]
    public void Command()
    {
        var line = new IrcMessage("privmsg #channel");
        Assert.AreEqual("PRIVMSG", line.Command);
    }

    [TestMethod]
    public void ParamsTrailing()
    {
        var line = new IrcMessage("PRIVMSG #channel :hello world");
        CollectionAssert.AreEqual(new List<string> {"#channel", "hello world"}, line.Parameters);
    }

    [TestMethod]
    public void ParamsOnlyTrailing()
    {
        var line = new IrcMessage("PRIVMSG :hello world");
        CollectionAssert.AreEqual(new List<string> {"hello world"}, line.Parameters);
    }

    [TestMethod]
    public void ParamsMissing()
    {
        var line = new IrcMessage("PRIVMSG");
        Assert.AreEqual("PRIVMSG", line.Command);
        CollectionAssert.AreEqual(new List<string>(), line.Parameters);
    }

    [TestMethod]
    public void AllTokens()
    {
        var line = new IrcMessage("@id=123 :nick!user@host PRIVMSG #channel :hello world");
        CollectionAssert.AreEqual(new Dictionary<string, string> {{"id", "123"}}, line.Tags);
        Assert.AreEqual("nick!user@host", line.Source);
        Assert.AreEqual("PRIVMSG", line.Command);
        CollectionAssert.AreEqual(new List<string> {"#channel", "hello world"}, line.Parameters);
    }
    
    [TestMethod]
    public void Tags()
    {
        var line = new IrcMessage("PRIVMSG", "#channel", "hello")
        {
            Tags = new() {{"id", "\\" + " " + ";" + "\r\n"}}
        }.Format();

        Assert.AreEqual(@"@id=\\\s\:\r\n PRIVMSG #channel hello", line);
    }

    [TestMethod]
    public void MissingTag()
    {
        var line = new IrcMessage("PRIVMSG", "#channel", "hello").Format();

        Assert.AreEqual("PRIVMSG #channel hello", line);
    }

    [TestMethod]
    public void NullTag()
    {
        var line = new IrcMessage("PRIVMSG", "#channel", "hello") {Tags = new() {{"a", null}}}
            .Format();

        Assert.AreEqual("@a PRIVMSG #channel hello", line);
    }

    [TestMethod]
    public void EmptyTag()
    {
        var line = new IrcMessage("PRIVMSG", "#channel", "hello") {Tags = new() {{"a", ""}}}
            .Format();

        Assert.AreEqual("@a PRIVMSG #channel hello", line);
    }

    [TestMethod]
    public void Source()
    {
        var line = new IrcMessage("PRIVMSG", "#channel", "hello") {Source = "nick!user@host"}.Format();

        Assert.AreEqual(":nick!user@host PRIVMSG #channel hello", line);
    }

    [TestMethod]
    public void CommandLowercase()
    {
        var line = new IrcMessage {Command = "privmsg"}.Format();
        Assert.AreEqual("privmsg", line);
    }

    [TestMethod]
    public void CommandUppercase()
    {
        var line = new IrcMessage {Command = "PRIVMSG"}.Format();
        Assert.AreEqual("PRIVMSG", line);
    }

    [TestMethod]
    public void TrailingSpace()
    {
        var line = new IrcMessage("PRIVMSG", "#channel", "hello world").Format();

        Assert.AreEqual("PRIVMSG #channel :hello world", line);
    }

    [TestMethod]
    public void TrailingNoSpace()
    {
        var line = new IrcMessage("PRIVMSG", "#channel", "helloworld").Format();

        Assert.AreEqual("PRIVMSG #channel helloworld", line);
    }

    [TestMethod]
    public void TrailingDoubleColon()
    {
        var line = new IrcMessage("PRIVMSG", "#channel", ":helloworld").Format();

        Assert.AreEqual("PRIVMSG #channel ::helloworld", line);
    }

    [TestMethod]
    public void InvalidNonLastSpace()
    {
        Assert.ThrowsException<ArgumentException>(() => { new IrcMessage("USER", "user", "0 *", "real name").Format(); });
    }

    [TestMethod]
    public void InvalidNonLastColon()
    {
        Assert.ThrowsException<ArgumentException>(() => { new IrcMessage("PRIVMSG", ":#channel", "hello").Format(); });
    }

    [TestMethod]
    public void Timestamp_CompareISOString()
    {
        IrcMessage[] messages =
        [
            new("@time=2011-10-19T16:40:51.620Z :Angel!angel@example.org PRIVMSG Wiz :Hello"),
            new("@time=2012-06-30T23:59:59.419Z :John!~john@1.2.3.4 JOIN #chan")
        ];

        string[] timestamps =
        [
            "2011-10-19T16:40:51.620Z",
            "2012-06-30T23:59:59.419Z"
        ];

        CollectionAssert.AreEqual(timestamps, messages.Select(m => m.Timestamp.ToISOString()).ToList());
    }

    [TestMethod]
    public void Timestamp_FromTimestamp()
    {
        IrcMessage[] messages =
        [
            new("@t=1504923966 :Angel!angel@example.org PRIVMSG Wiz :Hello"),
            new("@t=1504923972 :John!~john@1.2.3.4 JOIN #chan")
        ];

        string[] timestamps =
        [
            "2017-09-09T02:26:06.000Z",
            "2017-09-09T02:26:12.000Z"
        ];

        CollectionAssert.AreEqual(timestamps, messages.Select(m => m.Timestamp.ToISOString()).ToList());
    }

    [TestMethod]
    public void Timestamp_FailOnLeap()
    {
        Assert.ThrowsException<ArgumentException>(() =>
            new IrcMessage("@time=2012-06-30T23:59:60.419Z :John!~john@1.2.3.4 JOIN #chan"));
    }
}