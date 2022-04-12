using System;

namespace ChatSharp.Events
{
    /// <summary>
    ///     Raised when a Error occurs.
    /// </summary>
    public class ErrorEventArgs : EventArgs
    {
        internal ErrorEventArgs(Exception error)
        {
            Error = error;
        }

        /// <summary>
        ///     The error that has occured.
        /// </summary>
        public Exception Error { get; set; }
    }
}