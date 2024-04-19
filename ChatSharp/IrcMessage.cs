using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace ChatSharp
{
    /// <summary>
    ///     Represents a raw IRC message. This is a low-level construct - PrivateMessage is used
    ///     to represent messages sent from users.
    /// </summary>
    public class IrcMessage : IEquatable<IrcMessage>
    {
        private static readonly string[] TagUnescaped = { "\\", " ", ";", "\r", "\n" };
        private static readonly string[] TagEscaped = { @"\\", "\\s", "\\:", "\\r", "\\n" };

        public IrcMessage()
        {
        }

        public IrcMessage(string command, params string[] parameters)
        {
            Command = command.ToUpperInvariant();
            Parameters = parameters.ToList();
        }

        /// <summary>
        ///     Parse and tokenize an IRC message, given the raw message from the server.
        /// </summary>
        public IrcMessage(string line)
        {
            if (string.IsNullOrWhiteSpace(line)) throw new ArgumentNullException(nameof(line));

            string[] split;

            if (line.StartsWith('@'))
            {
                Tags = new Dictionary<string, string>();

                split = line.Split(" ", 2);
                var messageTags = split[0];
                line = split[1];

                foreach (var part in messageTags[1..].Split(';'))
                    if (part.Contains('=', StringComparison.Ordinal))
                    {
                        split = part.Split('=', 2);
                        Tags[split[0]] = UnescapeTag(split[1]);
                    }
                    else
                    {
                        Tags[part] = null;
                    }
            }

            string trailing;
            if (line.Contains(" :", StringComparison.Ordinal))
            {
                split = line.Split(" :", 2);
                line = split[0];
                trailing = split[1];
            }
            else
            {
                trailing = null;
            }

            Parameters = line.Contains(' ', StringComparison.Ordinal)
                ? line.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList()
                : new List<string> { line };

            if (Parameters[0].StartsWith(':'))
            {
                Source = Parameters[0][1..];
                Parameters.RemoveAt(0);
            }

            if (Parameters.Count > 0)
            {
                Command = Parameters[0].ToUpper(CultureInfo.InvariantCulture);
                Parameters.RemoveAt(0);
            }

            if (trailing != null) Parameters.Add(trailing);

            // Parse server-time message tag.
            // Fallback to server-info if both znc.in/server-info and the former exists.
            //
            // znc.in/server-time tag
            if (Tags?.Any(tag => tag.Key == "t") ?? false)
            {
                var tag = Tags.SingleOrDefault(x => x.Key == "t");
                Timestamp = new Timestamp(tag.Value, true);
            }
            // server-time tag
            else if (Tags?.Any(tag => tag.Key == "time") ?? false)
            {
                var tag = Tags.SingleOrDefault(x => x.Key == "time");
                Timestamp = new Timestamp(tag.Value);
            }
        }

        /// <summary>
        ///     The message source.
        /// </summary>
        public string Source { get; set; }

        public IrcUser User => new IrcUser(Source);

        /// <summary>
        ///     The message command.
        /// </summary>
        public string Command { get; set; }

        /// <summary>
        ///     Additional parameters supplied with the message.
        /// </summary>
        public List<string> Parameters { get; set; }

        /// <summary>
        ///     The message tags.
        /// </summary>
        public Dictionary<string, string> Tags { get; set; }

        /// <summary>
        ///     The message timestamp in ISO 8601 format.
        /// </summary>
        public Timestamp Timestamp { get; }

        public bool Equals(IrcMessage other)
        {
            if (other == null) return false;

            return Format() == other.Format();
        }

        public override int GetHashCode()
        {
            return Format().GetHashCode(StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as IrcMessage);
        }


        /// <summary>
        ///     Unescape ircv3 tag
        /// </summary>
        /// <param name="val">escaped string</param>
        /// <returns>unescaped string</returns>
        private static string UnescapeTag(string val)
        {
            var unescaped = new StringBuilder();

            var graphemeIterator = StringInfo.GetTextElementEnumerator(val);
            graphemeIterator.Reset();

            while (graphemeIterator.MoveNext())
            {
                var current = graphemeIterator.GetTextElement();

                if (current == @"\")
                    try
                    {
                        graphemeIterator.MoveNext();
                        var next = graphemeIterator.GetTextElement();
                        var pair = current + next;
                        unescaped.Append(TagEscaped.Contains(pair)
                            ? TagUnescaped[Array.IndexOf(TagEscaped, pair)]
                            : next);
                    }
                    catch (InvalidOperationException)
                    {
                        // ignored
                    }
                else
                    unescaped.Append(current);
            }

            return unescaped.ToString();
        }

        /// <summary>
        ///     Escape strings for use in ircv3 tags
        /// </summary>
        /// <param name="val">string to escape</param>
        /// <returns>escaped string</returns>
        private static string EscapeTag(string val)
        {
            for (var i = 0; i < TagUnescaped.Length; ++i)
                val = val?.Replace(TagUnescaped[i], TagEscaped[i], StringComparison.Ordinal);

            return val;
        }

        /// <summary>
        ///     Formats self <see cref="IrcMessage" /> as a standards-compliant IRC line
        /// </summary>
        /// <returns>formatted irc line</returns>
        public string Format()
        {
            var outs = new List<string>();

            if (Tags != null && Tags.Any())
            {
                var tags = Tags.Keys
                    .OrderBy(k => k)
                    .Select(key =>
                        string.IsNullOrWhiteSpace(Tags[key]) ? key : $"{key}={EscapeTag(Tags[key])}")
                    .ToList();

                outs.Add($"@{string.Join(";", tags)}");
            }

            if (Source != null) outs.Add($":{Source}");

            outs.Add(Command);

            if (Parameters != null && Parameters.Any())
            {
                var last = Parameters[^1];
                var withoutLast = Parameters.SkipLast(1).ToList();

                foreach (var p in withoutLast)
                {
                    if (p.Contains(' ', StringComparison.Ordinal))
                        throw new ArgumentException("non-last parameters cannot have spaces", p);

                    if (p.StartsWith(':'))
                        throw new ArgumentException("non-last parameters cannot start with colon", p);
                }

                outs.AddRange(withoutLast);

                if (string.IsNullOrWhiteSpace(last) || last.Contains(' ', StringComparison.Ordinal) ||
                    last.StartsWith(':'))
                    last = $":{last}";

                outs.Add(last);
            }

            return string.Join(" ", outs);
        }
    }
}