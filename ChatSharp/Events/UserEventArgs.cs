using System;

namespace ChatSharp.Events
{
    /// <summary>
    ///     Generic event args that represent an event regarding a user.
    /// </summary>
    public class UserEventArgs : EventArgs
    {
        internal UserEventArgs(IrcUser user)
        {
            User = user;
        }

        /// <summary>
        ///     The user this regards.
        /// </summary>
        public IrcUser User { get; set; }
    }
}