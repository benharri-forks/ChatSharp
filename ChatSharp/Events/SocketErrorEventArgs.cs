using System;
using System.Net.Sockets;

namespace ChatSharp.Events
{
    /// <summary>
    ///     Raised when a SocketError occurs.
    /// </summary>
    public class SocketErrorEventArgs : EventArgs
    {
        internal SocketErrorEventArgs(SocketError socketError)
        {
            SocketError = socketError;
        }

        /// <summary>
        ///     The error that has occured.
        /// </summary>
        public SocketError SocketError { get; set; }
    }
}