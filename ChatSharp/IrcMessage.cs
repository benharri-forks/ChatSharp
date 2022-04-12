using System;
using System.Collections.Generic;
using System.Linq;

namespace ChatSharp
{
    /// <summary>
    ///     Represents a raw IRC message. This is a low-level construct - PrivateMessage is used
    ///     to represent messages sent from users.
    /// </summary>
    public class IrcMessage
    {
        /// <summary>
        ///     Initializes and decodes an IRC message, given the raw message from the server.
        /// </summary>
        public IrcMessage(string rawMessage)
        {
            RawMessage = rawMessage;
            Tags = Array.Empty<KeyValuePair<string, string>>();

            if (rawMessage.StartsWith("@"))
            {
                var rawTags = rawMessage[1..rawMessage.IndexOf(' ')];
                rawMessage = rawMessage[(rawMessage.IndexOf(' ') + 1)..];

                // Parse tags as key value pairs
                var tags = new List<KeyValuePair<string, string>>();
                foreach (var rawTag in rawTags.Split(';'))
                {
                    var replacedTag = rawTag.Replace(@"\:", ";");
                    // The spec declares `@a=` as a tag with an empty value, while `@b;` as a tag with a null value
                    KeyValuePair<string, string> tag = new(replacedTag, null);

                    if (replacedTag.Contains("="))
                    {
                        var key = replacedTag.Substring(0, replacedTag.IndexOf("=", StringComparison.Ordinal));
                        var value = replacedTag[(replacedTag.IndexOf("=", StringComparison.Ordinal) + 1)..];
                        tag = new(key, value);
                    }

                    tags.Add(tag);
                }

                Tags = tags.ToArray();
            }

            if (rawMessage.StartsWith(":"))
            {
                Prefix = rawMessage[1..rawMessage.IndexOf(' ')];
                rawMessage = rawMessage[(rawMessage.IndexOf(' ') + 1)..];
            }

            if (rawMessage.Contains(' '))
            {
                Command = rawMessage.Remove(rawMessage.IndexOf(' '));
                rawMessage = rawMessage[(rawMessage.IndexOf(' ') + 1)..];
                // Parse parameters
                var parameters = new List<string>();
                while (!string.IsNullOrEmpty(rawMessage))
                {
                    if (rawMessage.StartsWith(":"))
                    {
                        parameters.Add(rawMessage[1..]);
                        break;
                    }

                    if (!rawMessage.Contains(' '))
                    {
                        parameters.Add(rawMessage);
                        rawMessage = string.Empty;
                        break;
                    }

                    parameters.Add(rawMessage.Remove(rawMessage.IndexOf(' ')));
                    rawMessage = rawMessage[(rawMessage.IndexOf(' ') + 1)..];
                }

                Parameters = parameters.ToArray();
            }
            else
            {
                // Violates RFC 1459, but we'll parse it anyway
                Command = rawMessage;
                Parameters = Array.Empty<string>();
            }

            // Parse server-time message tag.
            // Fallback to server-info if both znc.in/server-info and the former exists.
            //
            // znc.in/server-time tag
            if (Tags.Any(tag => tag.Key == "t"))
            {
                var tag = Tags.SingleOrDefault(x => x.Key == "t");
                Timestamp = new(tag.Value, true);
            }
            // server-time tag
            else if (Tags.Any(tag => tag.Key == "time"))
            {
                var tag = Tags.SingleOrDefault(x => x.Key == "time");
                Timestamp = new(tag.Value);
            }
        }

        /// <summary>
        ///     The unparsed message.
        /// </summary>
        public string RawMessage { get; }

        /// <summary>
        ///     The message prefix.
        /// </summary>
        public string Prefix { get; }

        /// <summary>
        ///     The message command.
        /// </summary>
        public string Command { get; }

        /// <summary>
        ///     Additional parameters supplied with the message.
        /// </summary>
        public string[] Parameters { get; }

        /// <summary>
        ///     The message tags.
        /// </summary>
        public KeyValuePair<string, string>[] Tags { get; }

        /// <summary>
        ///     The message timestamp in ISO 8601 format.
        /// </summary>
        public Timestamp Timestamp { get; }
    }
}