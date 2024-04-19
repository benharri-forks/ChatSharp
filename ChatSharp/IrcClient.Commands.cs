using System;
using System.Collections.Generic;
using System.Linq;

namespace ChatSharp
{
    public partial class IrcClient
    {
        /// <summary>
        ///     Changes your nick.
        /// </summary>
        public void Nick(string newNick)
        {
            SendRawMessage("NICK {0}", newNick);
            User.Nick = newNick;
        }

        /// <summary>
        ///     Sends a message to one or more destinations (channels or users).
        /// </summary>
        public void SendMessage(string message, params string[] destinations)
        {
            IllegalCharacters(message, destinations);
            var to = string.Join(",", destinations);
            SendRawMessage("PRIVMSG {0} :{1}{2}", to, PrivmsgPrefix, message);
        }

        /// <summary>
        ///     Sends a CTCP action (i.e. "* SirCmpwn waves hello") to one or more destinations.
        /// </summary>
        public void SendAction(string message, params string[] destinations)
        {
            IllegalCharacters(message, destinations);
            var to = string.Join(",", destinations);
            SendRawMessage("PRIVMSG {0} :\x0001ACTION {1}{2}\x0001", to, PrivmsgPrefix, message);
        }

        /// <summary>
        ///     Sends a NOTICE to one or more destinations (channels or users).
        /// </summary>
        public void SendNotice(string message, params string[] destinations)
        {
            IllegalCharacters(message, destinations);
            var to = string.Join(",", destinations);
            SendRawMessage("NOTICE {0} :{1}{2}", to, PrivmsgPrefix, message);
        }

        private static void IllegalCharacters(string message, string[] destinations)
        {
            const string illegalCharacters = "\r\n\0";
            if (destinations == null || !destinations.Any())
                throw new InvalidOperationException("Message must have at least one target.");
            if (illegalCharacters.Any(message.Contains))
                throw new ArgumentException("Illegal characters are present in message.", nameof(message));
        }

        /// <summary>
        ///     Leaves the specified channel.
        /// </summary>
        public void PartChannel(string channel)
        {
            if (!Channels.Contains(channel))
                throw new InvalidOperationException("Client is not present in channel.");
            SendRawMessage("PART {0}", channel);
        }

        /// <summary>
        ///     Leaves the specified channel, giving a reason for your departure.
        /// </summary>
        public void PartChannel(string channel, string reason)
        {
            if (!Channels.Contains(channel))
                throw new InvalidOperationException("Client is not present in channel.");
            SendRawMessage("PART {0} :{1}", channel, reason);
        }

        /// <summary>
        ///     Joins the specified channel.
        /// </summary>
        public void JoinChannel(string channel, string key = null)
        {
            if (Channels.Contains(channel))
                throw new InvalidOperationException("Client is already present in channel.");

            var joinCmd = $"JOIN {channel}";
            if (!string.IsNullOrEmpty(key))
                joinCmd += $" {key}";

            SendRawMessage(joinCmd, channel);

            // account-notify capability
            const WhoxField flags = WhoxField.Nick | WhoxField.Hostname | WhoxField.AccountName | WhoxField.Username;

            if (Capabilities.IsEnabled("account-notify"))
                Who(channel, WhoxFlag.None, flags, whoList =>
                {
                    if (whoList.Count > 0)
                        foreach (var whoQuery in whoList)
                        {
                            var user = Users.GetOrAdd(whoQuery.User.Hostmask);
                            user.Account = whoQuery.User.Account;
                        }
                });
        }

        /// <summary>
        ///     Sets the topic for the specified channel.
        /// </summary>
        public void SetTopic(string channel, string topic)
        {
            if (!Channels.Contains(channel))
                throw new InvalidOperationException("Client is not present in channel.");
            SendRawMessage("TOPIC {0} :{1}", channel, topic);
        }

        /// <summary>
        ///     Retrieves the topic for the specified channel.
        /// </summary>
        public void GetTopic(string channel)
        {
            SendRawMessage("TOPIC {0}", channel);
        }

        /// <summary>
        ///     Kicks the specified user from the specified channel.
        /// </summary>
        public void KickUser(string channel, string user)
        {
            SendRawMessage("KICK {0} {1} :{1}", channel, user);
        }

        /// <summary>
        ///     Kicks the specified user from the specified channel.
        /// </summary>
        public void KickUser(string channel, string user, string reason)
        {
            SendRawMessage("KICK {0} {1} :{2}", channel, user, reason);
        }

        /// <summary>
        ///     Invites the specified user to the specified channel.
        /// </summary>
        public void InviteUser(string channel, string user)
        {
            SendRawMessage("INVITE {1} {0}", channel, user);
        }

        /// <summary>
        ///     Sends a WHOIS query asking for information on the given nick, and a callback
        ///     to run when we have received the response.
        /// </summary>
        public void WhoIs(string nick, Action<WhoIs> callback = null)
        {
            var whois = new WhoIs();
            var message = $"WHOIS {nick}";
            RequestManager.QueueOperation(message,
                new RequestOperation(whois, ro => { callback?.Invoke((WhoIs)ro.State); }));
            SendRawMessage(message);
        }

        /// <summary>
        ///     Sends an extended WHO query asking for specific information about a single user
        ///     or the users in a channel, and runs a callback when we have received the response.
        /// </summary>
        public void Who(string target, WhoxFlag flags, WhoxField whoxField, Action<List<ExtendedWho>> callback)
        {
            if (ServerInfo.ExtendedWho)
            {
                var whox = new List<ExtendedWho>();

                // Generate random querytype for WHO query
                var queryType = RandomNumber.Next(0, 999);

                // Add the querytype field if it wasn't defined
                var fields = whoxField;
                if ((whoxField & WhoxField.QueryType) == 0)
                    fields |= WhoxField.QueryType;

                var whoQuery = $"WHO {target} {flags.AsString()}%{fields.AsString()},{queryType}";
                var queryKey = $"WHO {target} {queryType} {fields:D}";

                RequestManager.QueueOperation(queryKey,
                    new RequestOperation(whox, ro => { callback?.Invoke((List<ExtendedWho>)ro.State); }));
                SendRawMessage(whoQuery);
            }
            else
            {
                var whox = new List<ExtendedWho>();
                var whoQuery = $"WHO {target}";

                RequestManager.QueueOperation(whoQuery,
                    new RequestOperation(whox, ro => { callback?.Invoke((List<ExtendedWho>)ro.State); }));
                SendRawMessage(whoQuery);
            }
        }

        /// <summary>
        ///     Requests the mode of a channel from the server, and passes it to a callback later.
        /// </summary>
        public void GetMode(string channel, Action<IrcChannel> callback = null)
        {
            var message = $"MODE {channel}";
            RequestManager.QueueOperation(message, new RequestOperation(channel, ro =>
            {
                var c = Channels[(string)ro.State];
                callback?.Invoke(c);
            }));
            SendRawMessage(message);
        }

        /// <summary>
        ///     Sets the mode of a target.
        /// </summary>
        public void ChangeMode(string target, string change)
        {
            SendRawMessage("MODE {0} {1}", target, change);
        }

        /// <summary>
        ///     Gets a collection of masks from a channel by a mode. This can be used, for example,
        ///     to get a list of bans.
        /// </summary>
        public void GetModeList(string channel, char mode, Action<MaskCollection> callback)
        {
            RequestManager.QueueOperation($"MODE {mode} {channel}",
                new RequestOperation(new MaskCollection(), ro => callback?.Invoke((MaskCollection)ro.State)));
            SendRawMessage("MODE {0} {1}", channel, mode);
        }
    }
}