using System;
using System.Collections.Generic;

namespace ChatSharp
{
    /// <summary>
    ///     A user connected to IRC.
    /// </summary>
    public class IrcUser : IEquatable<IrcUser>
    {
        private readonly string _source;

        internal IrcUser()
        {
            Channels = new ChannelCollection();
            ChannelModes = new Dictionary<IrcChannel, List<char?>>();
            Account = "*";
        }

        /// <summary>
        ///     Constructs an IrcUser given a hostmask or nick.
        /// </summary>
        public IrcUser(string source) : this()
        {
            if (source == null) return;

            _source = source;

            if (source.Contains('@', StringComparison.Ordinal))
            {
                var split = source.Split('@');

                Nick = split[0];
                Hostname = split[1];
            }
            else
            {
                Nick = source;
            }

            if (Nick.Contains('!', StringComparison.Ordinal))
            {
                var userSplit = Nick.Split('!');
                Nick = userSplit[0];
                User = userSplit[1];
            }
        }

        /// <summary>
        ///     Constructs an IrcUser given a nick and user.
        /// </summary>
        public IrcUser(string nick, string user) : this()
        {
            Nick = nick;
            User = user;
            RealName = User;
            Mode = string.Empty;
        }

        /// <summary>
        ///     The user's nick.
        /// </summary>
        public string Nick { get; internal set; }

        /// <summary>
        ///     The user's user (an IRC construct, a string that identifies your username).
        /// </summary>
        public string User { get; internal set; }

        /// <summary>
        ///     The user's password. Will not be set on anyone but your own user.
        /// </summary>
        public string Password { get; internal set; }

        /// <summary>
        ///     The user's mode.
        /// </summary>
        /// <value>The mode.</value>
        public string Mode { get; internal set; }

        /// <summary>
        ///     The user's real name.
        /// </summary>
        /// <value>The name of the real.</value>
        public string RealName { get; internal set; }

        /// <summary>
        ///     The user's hostname.
        /// </summary>
        public string Hostname { get; internal set; }

        /// <summary>
        ///     Channels this user is present in. Note that this only includes channels you are
        ///     also present in, even after a successful WHOIS.
        /// </summary>
        /// <value>The channels.</value>
        public ChannelCollection Channels { get; set; }

        /// <summary>
        ///     The user's account. If 0 or *, the user is not logged in.
        ///     Otherwise, the user is logged in with services.
        /// </summary>
        public string Account { get; set; }

        internal Dictionary<IrcChannel, List<char?>> ChannelModes { get; set; }

        /// <summary>
        ///     This user's hostmask (nick!user@host).
        /// </summary>
        public string Hostmask => $"{Nick}!{User}@{Hostname}";

        /// <summary>
        ///     True if this user is equal to another (compares hostmasks).
        /// </summary>
        public bool Equals(IrcUser other)
        {
            if (other == null) return false;
            return other._source == _source;
        }

        /// <summary>
        ///     Returns true if the user matches the given mask. Can be used to check if a ban applies
        ///     to this user, for example.
        /// </summary>
        public bool Match(string mask)
        {
            if (mask.Contains("!") && mask.Contains("@"))
            {
                if (mask.Contains('$'))
                    mask = mask.Remove(mask.IndexOf('$')); // Extra fluff on some networks
                var parts = mask.Split('!', '@');
                if (Match(parts[0], Nick) && Match(parts[1], User) && Match(parts[2], Hostname))
                    return true;
            }

            return false;
        }

        /// <summary>
        ///     Checks if the given hostmask matches the given mask.
        /// </summary>
        public static bool Match(string mask, string value)
        {
            value ??= string.Empty;
            var i = 0;
            var j = 0;
            for (; j < value.Length && i < mask.Length; j++)
                if (mask[i] == '?')
                {
                    i++;
                }
                else if (mask[i] == '*')
                {
                    i++;
                    if (i >= mask.Length)
                        return true;
                    while (++j < value.Length && value[j] != mask[i])
                    {
                    }

                    if (j-- == value.Length)
                        return false;
                }
                else
                {
                    if (char.ToUpper(mask[i]) != char.ToUpper(value[j]))
                        return false;
                    i++;
                }

            return i == mask.Length && j == value.Length;
        }

        /// <summary>
        ///     True if this user is equal to another (compares hostmasks).
        /// </summary>
        public override bool Equals(object obj)
        {
            return Equals(obj as IrcUser);
        }

        /// <summary>
        ///     Returns the hash code of the user's hostmask.
        /// </summary>
        public override int GetHashCode()
        {
            return _source.GetHashCode(StringComparison.Ordinal);
        }

        /// <summary>
        ///     Returns the user's hostmask.
        /// </summary>
        public override string ToString()
        {
            return _source;
        }
    }
}