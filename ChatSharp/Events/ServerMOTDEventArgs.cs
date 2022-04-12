using System;

namespace ChatSharp.Events
{
    /// <summary>
    ///     Raised when we have received the MOTD from the server.
    /// </summary>
    public class ServerMOTDEventArgs : EventArgs
    {
        internal ServerMOTDEventArgs(string motd)
        {
            MOTD = motd;
        }

        /// <summary>
        ///     The message of the day.
        /// </summary>
        public string MOTD { get; set; }
    }
}