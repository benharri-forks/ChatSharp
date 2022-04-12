using System;

namespace ChatSharp.Events
{
    /// <summary>
    ///     Describes the features the server supports.
    /// </summary>
    public class SupportsEventArgs : EventArgs
    {
        internal SupportsEventArgs(ServerInfo serverInfo)
        {
            ServerInfo = serverInfo;
        }

        /// <summary>
        ///     The server's supported featureset.
        /// </summary>
        public ServerInfo ServerInfo { get; set; }
    }
}