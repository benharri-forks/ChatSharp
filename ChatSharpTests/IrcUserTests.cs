namespace ChatSharp.Tests;

[TestClass]
public class IrcUserTests
{
    [TestMethod]
    public void GetUserModes_NotNull_FiveModes()
    {
        IrcUser user = new("~&@%+aji", "user");
        IrcClient client = new("irc.address", user);

        var userModes = client.ServerInfo.GetModesForNick(user.Nick);

        Assert.AreEqual(5, userModes.Count);
    }

    [TestMethod]
    public void GetUserModes_NotNull_FourModes()
    {
        IrcUser user = new("&@%+aji", "user");
        IrcClient client = new("irc.address", user);

        var userModes = client.ServerInfo.GetModesForNick(user.Nick);

        Assert.AreEqual(4, userModes.Count);
    }

    [TestMethod]
    public void GetUserModes_NotNull_ThreeModes()
    {
        IrcUser user = new("@%+aji", "user");
        IrcClient client = new("irc.address", user);

        var userModes = client.ServerInfo.GetModesForNick(user.Nick);

        Assert.AreEqual(3, userModes.Count);
    }

    [TestMethod]
    public void GetUserModes_NotNull_TwoModes()
    {
        IrcUser user = new("%+aji", "user");
        IrcClient client = new("irc.address", user);

        var userModes = client.ServerInfo.GetModesForNick(user.Nick);

        Assert.AreEqual(2, userModes.Count);
    }

    [TestMethod]
    public void GetUserModes_NotNull_OneMode()
    {
        IrcUser user = new("+aji", "user");
        IrcClient client = new("irc.address", user);

        var userModes = client.ServerInfo.GetModesForNick(user.Nick);

        Assert.AreEqual(1, userModes.Count);
    }

    [TestMethod]
    public void GetUserModes_IsNull()
    {
        IrcUser user = new("aji", "user");
        IrcClient client = new("irc.address", user);

        var userModes = client.ServerInfo.GetModesForNick(user.Nick);

        Assert.AreEqual(0, userModes.Count);
    }

    [TestMethod]
    public void FullHostmask()
    {
        var hostmask = new IrcUser("nick!user@host");
        Assert.AreEqual("nick", hostmask.Nick);
        Assert.AreEqual("user", hostmask.User);
        Assert.AreEqual("host", hostmask.Hostname);
    }

    [TestMethod]
    public void NoHostname()
    {
        var hostmask = new IrcUser("nick!user");
        Assert.AreEqual("nick", hostmask.Nick);
        Assert.AreEqual("user", hostmask.User);
        Assert.IsNull(hostmask.Hostname);
    }

    [TestMethod]
    public void NoUser()
    {
        var hostmask = new IrcUser("nick@host");
        Assert.AreEqual("nick", hostmask.Nick);
        Assert.IsNull(hostmask.User);
        Assert.AreEqual("host", hostmask.Hostname);
    }

    [TestMethod]
    public void OnlyNick()
    {
        var hostmask = new IrcUser("nick");
        Assert.AreEqual("nick", hostmask.Nick);
        Assert.IsNull(hostmask.User);
        Assert.IsNull(hostmask.Hostname);
    }

    [TestMethod]
    public void HostmaskFromLine()
    {
        var message = new IrcMessage(":nick!user@host PRIVMSG #channel hello");
        var hostmask = new IrcUser("nick!user@host");
        Assert.AreEqual(hostmask.ToString(), message.User.ToString());
        Assert.AreEqual("nick", message.User.Nick);
        Assert.AreEqual("user", message.User.User);
        Assert.AreEqual("host", message.User.Hostname);
    }

    [TestMethod]
    public void EmptyHostmaskFromLine()
    {
        var message = new IrcMessage("PRIVMSG #channel hello");
        Assert.IsNull(message.User.Hostname);
        Assert.IsNull(message.User.User);
        Assert.IsNull(message.User.Nick);
    }
}