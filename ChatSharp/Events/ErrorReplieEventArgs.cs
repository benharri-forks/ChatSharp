using System;

namespace ChatSharp.Events
{
    /// <summary>
    ///     Raised when a IRC Error reply occurs. See rfc1459 6.1 for details.
    /// </summary>
    public class ErrorReplyEventArgs : EventArgs
    {
        internal ErrorReplyEventArgs(IrcMessage message)
        {
            Message = message;
        }

        /// <summary>
        ///     The IRC error reply that has occured.
        /// </summary>
        public IrcMessage Message { get; set; }
    }
}