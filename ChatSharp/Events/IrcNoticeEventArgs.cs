using System;

namespace ChatSharp.Events
{
    /// <summary>
    ///     Event describing an IRC notice.
    /// </summary>
    public class IrcNoticeEventArgs : EventArgs
    {
        internal IrcNoticeEventArgs(IrcMessage message)
        {
            if (message.Parameters.Count != 2)
                throw new IrcProtocolException("NOTICE was delivered in incorrect format");
            Message = message;
        }

        /// <summary>
        ///     The IRC message that describes this NOTICE.
        /// </summary>
        /// <value>The message.</value>
        public IrcMessage Message { get; set; }

        /// <summary>
        ///     The text of the notice.
        /// </summary>
        public string Notice => Message.Parameters[1];

        /// <summary>
        ///     The source of the notice (often a user).
        /// </summary>
        /// <value>The source.</value>
        public string Source => Message.Source;
    }
}