namespace ChatSharp
{
    /// <summary>
    ///     A IRC capability.
    /// </summary>
    public class IrcCapability
    {
        /// <summary>
        ///     Constructs a capability given a name.
        /// </summary>
        /// <param name="name"></param>
        public IrcCapability(string name)
        {
            Name = name;
            IsEnabled = false;
        }

        /// <summary>
        ///     The capability identifier.
        /// </summary>
        public string Name { get; internal set; }

        /// <summary>
        ///     The state of the capability after negotiating with the server.
        ///     Disabled by default when the capability is created.
        /// </summary>
        public bool IsEnabled { get; internal set; }
    }
}