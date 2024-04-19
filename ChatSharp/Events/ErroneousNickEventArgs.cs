using System;

namespace ChatSharp.Events
{
    /// <summary>
    ///     Describes an invalid nick event.
    /// </summary>
    public class ErroneousNickEventArgs : EventArgs
    {
        private static Random _random;

        internal ErroneousNickEventArgs(string invalidNick)
        {
            InvalidNick = invalidNick;
            NewNick = GenerateRandomNick();
            DoNotHandle = false;
        }

        /// <summary>
        ///     The nick that was not accepted by the server.
        /// </summary>
        /// <value>The invalid nick.</value>
        public string InvalidNick { get; set; }

        /// <summary>
        ///     The nick ChatSharp intends to use instead.
        /// </summary>
        /// <value>The new nick.</value>
        public string NewNick { get; set; }

        /// <summary>
        ///     Set to true to instruct ChatSharp NOT to send a valid nick.
        /// </summary>
        public bool DoNotHandle { get; set; }

        private static string GenerateRandomNick()
        {
            const string nickCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";

            _random ??= new Random();
            var nick = new char[8];
            for (var i = 0; i < nick.Length; i++)
                nick[i] = nickCharacters[_random.Next(nickCharacters.Length)];
            return new string(nick);
        }
    }
}