using System;

namespace ChatSharp.Events
{
    /// <summary>
    ///     Describes the response to a WHO (WHOX protocol) query. Note that ChatSharp may generate WHO
    ///     queries that the user did not ask for.
    /// </summary>
    public class WhoxReceivedEventArgs : EventArgs
    {
        internal WhoxReceivedEventArgs(ExtendedWho[] whoxResponse)
        {
            WhoxResponse = whoxResponse;
        }

        /// <summary>
        ///     The WHOIS response from the server.
        /// </summary>
        public ExtendedWho[] WhoxResponse { get; set; }
    }
}