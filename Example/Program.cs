using ChatSharp;

var client = new IrcClient("irc.libera.chat", new("chatsharp", "chatsharp"));

client.ConnectionComplete += (_, _) => client.JoinChannel("##chatsharp");
client.ChannelMessageReceived += (s, e) =>
{
    var channel = client.Channels[e.PrivateMessage.Source];
    if (e.PrivateMessage.Message == ".list")
    {
        channel.SendMessage(string.Join(", ", channel.Users.Select(u => u.Nick)));
    }
    else if (e.PrivateMessage.Message.StartsWith(".ban "))
    {
        if (!channel.UsersByMode['@'].Contains(client.User))
        {
            channel.SendMessage("I'm not an op here!");
            return;
        }

        var target = e.PrivateMessage.Message[5..];
        client.WhoIs(target, whois => channel.ChangeMode($"+b *!*@{whois.User.Hostname}"));
    }
};

client.ConnectAsync();
while (true)
{
}