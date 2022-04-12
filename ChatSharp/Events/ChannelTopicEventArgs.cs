using System;

namespace ChatSharp.Events
{
    /// <summary>
    ///     Details of a channel topic event.
    /// </summary>
    public class ChannelTopicEventArgs : EventArgs
    {
        internal ChannelTopicEventArgs(IrcChannel channel, string oldTopic, string topic)
        {
            Channel = channel;
            Topic = topic;
            OldTopic = oldTopic;
        }

        /// <summary>
        ///     The channel whose topic has changed.
        /// </summary>
        public IrcChannel Channel { get; set; }

        /// <summary>
        ///     The new topic
        /// </summary>
        public string Topic { get; set; }

        /// <summary>
        ///     The original topic.
        /// </summary>
        public string OldTopic { get; set; }
    }
}