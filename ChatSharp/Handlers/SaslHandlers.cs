using System;
using System.Linq;
using System.Text;

namespace ChatSharp.Handlers
{
    internal static class SaslHandlers
    {
        public static void HandleAuthentication(IrcClient client, IrcMessage message)
        {
            if (client.IsAuthenticatingSasl)
                if (message.Parameters[0] == "+")
                {
                    // Based off irc-framework implementation
                    var plainString = $"{client.User.Nick}\0{client.User.Nick}\0{client.User.Password}";
                    var b64Bytes = Encoding.UTF8.GetBytes(Convert.ToBase64String(Encoding.UTF8.GetBytes(plainString)));

                    while (b64Bytes.Length >= 400)
                    {
                        var chunk = b64Bytes.Take(400).ToArray();
                        b64Bytes = b64Bytes.Skip(400).ToArray();
                        client.SendRawMessage($"AUTHENTICATE {Encoding.UTF8.GetString(chunk)}");
                    }

                    client.SendRawMessage(b64Bytes.Length > 0
                        ? $"AUTHENTICATE {Encoding.UTF8.GetString(b64Bytes)}"
                        : "AUTHENTICATE +");

                    client.IsAuthenticatingSasl = false;
                }
        }

        public static void HandleError(IrcClient client, IrcMessage message)
        {
            if (client.IsNegotiatingCapabilities && !client.IsAuthenticatingSasl)
            {
                client.SendRawMessage("CAP END");
                client.IsNegotiatingCapabilities = false;
            }
        }
    }
}