using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Text;
using System.Timers;
using ChatSharp.Events;
using ChatSharp.Handlers;
using ErrorEventArgs = ChatSharp.Events.ErrorEventArgs;

namespace ChatSharp
{
    /// <summary>
    ///     An IRC client.
    /// </summary>
    public sealed partial class IrcClient
    {
        /// <summary>
        ///     A raw IRC message handler.
        /// </summary>
        public delegate void MessageHandler(IrcClient client, IrcMessage message);

        private const int ReadBufferLength = 1024;

        /// <summary>
        ///     Creates a new IRC client, but will not connect until ConnectAsync is called.
        /// </summary>
        /// <param name="serverAddress">Server address including port in the form of "hostname:port".</param>
        /// <param name="user">The IRC user to connect as.</param>
        /// <param name="useSSL">Connect with SSL if true.</param>
        public IrcClient(string serverAddress, IrcUser user, bool useSSL = false)
        {
            User = user ?? throw new ArgumentNullException(nameof(user));
            ServerAddress = serverAddress ?? throw new ArgumentNullException(nameof(serverAddress));
            Encoding = Encoding.UTF8;
            Settings = new ClientSettings();
            Handlers = new Dictionary<string, MessageHandler>();
            MessageHandlers.RegisterDefaultHandlers(this);
            RequestManager = new RequestManager();
            UseSSL = useSSL;
            WriteQueue = new ConcurrentQueue<string>();
            ServerInfo = new ServerInfo();
            PrivmsgPrefix = "";
            Channels = User.Channels = new ChannelCollection(this);
            // Add self to user pool
            Users = new UserPool { User };
            Capabilities = new CapabilityPool();

            // List of supported capabilities
            Capabilities.AddRange(new[]
            {
                "server-time", "multi-prefix", "cap-notify", "znc.in/server-time", "znc.in/server-time-iso",
                "account-notify", "chghost", "userhost-in-names", "sasl"
            });

            IsNegotiatingCapabilities = false;
            IsAuthenticatingSasl = false;

            RandomNumber = new Random();
        }

        private Dictionary<string, MessageHandler> Handlers { get; }

        internal static Random RandomNumber { get; private set; }

        private byte[] ReadBuffer { get; set; }
        private int ReadBufferIndex { get; set; }
        private string ServerHostname { get; set; }
        private int ServerPort { get; set; }
        private Timer PingTimer { get; set; }
        private Socket Socket { get; set; }
        private ConcurrentQueue<string> WriteQueue { get; }
        private bool IsWriting { get; set; }

        internal RequestManager RequestManager { get; set; }

        internal string ServerNameFromPing { get; set; }

        /// <summary>
        ///     The address this client is connected to, or will connect to. Setting this
        ///     after the client is connected will not cause a reconnect.
        /// </summary>
        public string ServerAddress
        {
            get => ServerHostname + ":" + ServerPort;
            internal set
            {
                var parts = value.Split(':');
                if (parts.Length > 2 || parts.Length == 0)
                    throw new FormatException("Server address is not in correct format ('hostname:port')");
                ServerHostname = parts[0];
                ServerPort = parts.Length > 1 ? int.Parse(parts[1]) : 6667;
            }
        }

        /// <summary>
        ///     The low level TCP stream for this client.
        /// </summary>
        public Stream NetworkStream { get; set; }

        /// <summary>
        ///     If true, SSL will be used to connect.
        /// </summary>
        public bool UseSSL { get; }

        /// <summary>
        ///     If true, invalid SSL certificates are ignored.
        /// </summary>
        public bool IgnoreInvalidSSL { get; set; }

        /// <summary>
        ///     The character encoding to use for the connection. Defaults to UTF-8.
        /// </summary>
        /// <value>The encoding.</value>
        public Encoding Encoding { get; set; }

        /// <summary>
        ///     The user this client is logged in as.
        /// </summary>
        /// <value>The user.</value>
        public IrcUser User { get; set; }

        /// <summary>
        ///     The channels this user is joined to.
        /// </summary>
        public ChannelCollection Channels { get; }

        /// <summary>
        ///     Settings that control the behavior of ChatSharp.
        /// </summary>
        public ClientSettings Settings { get; set; }

        /// <summary>
        ///     Information about the server we are connected to. Servers may not send us this information,
        ///     but it's required for ChatSharp to function, so by default this is a guess. Handle
        ///     IrcClient.ServerInfoReceived if you'd like to know when it's populated with real information.
        /// </summary>
        public ServerInfo ServerInfo { get; set; }

        /// <summary>
        ///     A string to prepend to all PRIVMSGs sent. Many IRC bots prefix their messages with \u200B, to
        ///     indicate to other bots that you are a bot.
        /// </summary>
        public string PrivmsgPrefix { get; set; }

        /// <summary>
        ///     A list of users on this network that we are aware of.
        /// </summary>
        public UserPool Users { get; set; }

        /// <summary>
        ///     A list of capabilities supported by the library, along with enabled and disabled capabilities
        ///     after negotiating with the server.
        /// </summary>
        public CapabilityPool Capabilities { get; set; }

        /// <summary>
        ///     Set to true when the client is negotiating IRC capabilities with the server.
        ///     If set to False, capability negotiation is finished.
        /// </summary>
        public bool IsNegotiatingCapabilities { get; internal set; }

        /// <summary>
        ///     Set to True when the client is authenticating with SASL.
        ///     If set to False, SASL authentication is finished.
        /// </summary>
        public bool IsAuthenticatingSasl { get; internal set; }

        /// <summary>
        ///     Sets a custom handler for an IRC message. This applies to the low level IRC protocol,
        ///     not for private messages.
        /// </summary>
        public void SetHandler(string message, MessageHandler handler)
        {
#if DEBUG
            // This is the default behavior if 3rd parties want to handle certain messages themselves
            // However, if it happens from our own code, we probably did something wrong
            if (Handlers.ContainsKey(message.ToUpper()))
                Console.WriteLine("Warning: {0} handler has been overwritten", message);
#endif
            message = message.ToUpper();
            Handlers[message] = handler;
        }

        internal static DateTime DateTimeFromIrcTime(int time)
        {
            return new DateTime(1970, 1, 1).AddSeconds(time);
        }

        /// <summary>
        ///     Connects to the IRC server.
        /// </summary>
        public void ConnectAsync()
        {
            if (Socket is { Connected: true })
                throw new InvalidOperationException("Socket is already connected to server.");
            Socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            ReadBuffer = new byte[ReadBufferLength];
            ReadBufferIndex = 0;
            PingTimer = new Timer(30000);
            PingTimer.Elapsed += (x, y) =>
            {
                if (!string.IsNullOrEmpty(ServerNameFromPing))
                    SendRawMessage("PING :{0}", ServerNameFromPing);
            };
            var checkQueue = new Timer(1000);
            checkQueue.Elapsed += (x, y) =>
            {
                if (!WriteQueue.IsEmpty)
                {
                    string nextMessage;
                    while (!WriteQueue.TryDequeue(out nextMessage))
                    {
                    }

                    SendRawMessage(nextMessage);
                }
            };
            checkQueue.Start();
            Socket.BeginConnect(ServerHostname, ServerPort, ConnectComplete, null);
        }

        /// <summary>
        ///     Send a QUIT message and disconnect.
        /// </summary>
        public void Quit()
        {
            Quit(null);
        }

        /// <summary>
        ///     Send a QUIT message with a reason and disconnect.
        /// </summary>
        public void Quit(string reason)
        {
            if (reason == null)
                SendRawMessage("QUIT");
            else
                SendRawMessage("QUIT :{0}", reason);
            Socket.BeginDisconnect(false, ar =>
            {
                Socket.EndDisconnect(ar);
                NetworkStream.Dispose();
                NetworkStream = null;
            }, null);
            PingTimer.Dispose();
        }

        private void ConnectComplete(IAsyncResult result)
        {
            try
            {
                Socket.EndConnect(result);
                NetworkStream = new NetworkStream(Socket);
                if (UseSSL)
                {
                    NetworkStream = IgnoreInvalidSSL
                        ? new SslStream(NetworkStream, false, (a, b, c, d) => true)
                        : new SslStream(NetworkStream);
                    ((SslStream)NetworkStream).AuthenticateAsClient(ServerHostname);
                }

                NetworkStream.BeginRead(ReadBuffer, ReadBufferIndex, ReadBuffer.Length, DataReceived, null);
                // Begin capability negotiation
                SendRawMessage("CAP LS 302");
                // Write login info
                if (!string.IsNullOrEmpty(User.Password))
                    SendRawMessage("PASS {0}", User.Password);
                SendRawMessage("NICK {0}", User.Nick);
                // hostname, servername are ignored by most IRC servers
                SendRawMessage("USER {0} hostname servername :{1}", User.User, User.RealName);
                PingTimer.Start();
            }
            catch (SocketException e)
            {
                OnNetworkError(new SocketErrorEventArgs(e.SocketErrorCode));
            }
            catch (Exception e)
            {
                OnError(new ErrorEventArgs(e));
            }
        }

        private void DataReceived(IAsyncResult result)
        {
            if (NetworkStream == null)
            {
                OnNetworkError(new SocketErrorEventArgs(SocketError.NotConnected));
                return;
            }

            int length;
            try
            {
                length = NetworkStream.EndRead(result) + ReadBufferIndex;
            }
            catch (IOException e)
            {
                if (e.InnerException is SocketException socketException)
                    OnNetworkError(new SocketErrorEventArgs(socketException.SocketErrorCode));
                else
                    throw;
                return;
            }

            ReadBufferIndex = 0;
            while (length > 0)
            {
                var messageLength = Array.IndexOf(ReadBuffer, (byte)'\n', 0, length);
                if (messageLength == -1) // Incomplete message
                {
                    ReadBufferIndex = length;
                    break;
                }

                messageLength++;
                var message = Encoding.GetString(ReadBuffer, 0, messageLength - 2); // -2 to remove \r\n
                HandleMessage(message);
                Array.Copy(ReadBuffer, messageLength, ReadBuffer, 0, length - messageLength);
                length -= messageLength;
            }

            NetworkStream.BeginRead(ReadBuffer, ReadBufferIndex, ReadBuffer.Length - ReadBufferIndex, DataReceived,
                null);
        }

        private void HandleMessage(string rawMessage)
        {
            OnRawMessageReceived(new RawMessageEventArgs(rawMessage, false));
            var message = new IrcMessage(rawMessage);
            if (Handlers.TryGetValue(message.Command, out var handler))
            {
                handler(this, message);
            }
        }

        /// <summary>
        ///     Send a raw IRC message. Behaves like /quote in most IRC clients.
        /// </summary>
        public void SendRawMessage(string message, params object[] format)
        {
            if (NetworkStream == null)
            {
                OnNetworkError(new SocketErrorEventArgs(SocketError.NotConnected));
                return;
            }

            message = string.Format(message, format);
            var data = Encoding.GetBytes(message + "\r\n");

            if (!IsWriting)
            {
                IsWriting = true;
                NetworkStream.BeginWrite(data, 0, data.Length, MessageSent, message);
            }
            else
            {
                WriteQueue.Enqueue(message);
            }
        }

        /// <summary>
        ///     Send a raw IRC message. Behaves like /quote in most IRC clients.
        /// </summary>
        public void SendIrcMessage(IrcMessage message)
        {
            SendRawMessage(message.Format());
        }

        private void MessageSent(IAsyncResult result)
        {
            if (NetworkStream == null)
            {
                OnNetworkError(new SocketErrorEventArgs(SocketError.NotConnected));
                IsWriting = false;
                return;
            }

            try
            {
                NetworkStream.EndWrite(result);
            }
            catch (IOException e)
            {
                if (e.InnerException is SocketException socketException)
                    OnNetworkError(new SocketErrorEventArgs(socketException.SocketErrorCode));
                else
                    throw;
                return;
            }
            finally
            {
                IsWriting = false;
            }

            OnRawMessageSent(new RawMessageEventArgs((string)result.AsyncState, true));

            string nextMessage;
            if (!WriteQueue.IsEmpty)
            {
                while (!WriteQueue.TryDequeue(out nextMessage))
                {
                }

                SendRawMessage(nextMessage);
            }
        }

        /// <summary>
        ///     IRC Error Replies. rfc1459 6.1.
        /// </summary>
        public event EventHandler<ErrorReplyEventArgs> ErrorReply;

        internal void OnErrorReply(ErrorReplyEventArgs e)
        {
            ErrorReply?.Invoke(this, e);
        }

        /// <summary>
        ///     Raised for errors.
        /// </summary>
        public event EventHandler<ErrorEventArgs> Error;

        internal void OnError(ErrorEventArgs e)
        {
            Error?.Invoke(this, e);
        }

        /// <summary>
        ///     Raised for socket errors. ChatSharp does not automatically reconnect.
        /// </summary>
        public event EventHandler<SocketErrorEventArgs> NetworkError;

        internal void OnNetworkError(SocketErrorEventArgs e)
        {
            NetworkError?.Invoke(this, e);
        }

        /// <summary>
        ///     Occurs when a raw message is sent.
        /// </summary>
        public event EventHandler<RawMessageEventArgs> RawMessageSent;

        internal void OnRawMessageSent(RawMessageEventArgs e)
        {
            RawMessageSent?.Invoke(this, e);
        }

        /// <summary>
        ///     Occurs when a raw message received.
        /// </summary>
        public event EventHandler<RawMessageEventArgs> RawMessageReceived;

        internal void OnRawMessageReceived(RawMessageEventArgs e)
        {
            RawMessageReceived?.Invoke(this, e);
        }

        /// <summary>
        ///     Occurs when a notice received.
        /// </summary>
        public event EventHandler<IrcNoticeEventArgs> NoticeReceived;

        internal void OnNoticeReceived(IrcNoticeEventArgs e)
        {
            NoticeReceived?.Invoke(this, e);
        }

        /// <summary>
        ///     Occurs when the server has sent us part of the MOTD.
        /// </summary>
        public event EventHandler<ServerMOTDEventArgs> MOTDPartReceived;

        internal void OnMOTDPartReceived(ServerMOTDEventArgs e)
        {
            MOTDPartReceived?.Invoke(this, e);
        }

        /// <summary>
        ///     Occurs when the entire server MOTD has been received.
        /// </summary>
        public event EventHandler<ServerMOTDEventArgs> MOTDReceived;

        internal void OnMOTDReceived(ServerMOTDEventArgs e)
        {
            MOTDReceived?.Invoke(this, e);
        }

        /// <summary>
        ///     Occurs when a private message received. This can be a channel OR a user message.
        /// </summary>
        public event EventHandler<PrivateMessageEventArgs> PrivateMessageReceived;

        internal void OnPrivateMessageReceived(PrivateMessageEventArgs e)
        {
            PrivateMessageReceived?.Invoke(this, e);
        }

        /// <summary>
        ///     Occurs when a message is received in an IRC channel.
        /// </summary>
        public event EventHandler<PrivateMessageEventArgs> ChannelMessageReceived;

        internal void OnChannelMessageReceived(PrivateMessageEventArgs e)
        {
            ChannelMessageReceived?.Invoke(this, e);
        }

        /// <summary>
        ///     Occurs when a message is received from a user.
        /// </summary>
        public event EventHandler<PrivateMessageEventArgs> UserMessageReceived;

        internal void OnUserMessageReceived(PrivateMessageEventArgs e)
        {
            UserMessageReceived?.Invoke(this, e);
        }

        /// <summary>
        ///     Raised if the nick you've chosen is in use. By default, ChatSharp will pick a
        ///     random nick to use instead. Set ErronousNickEventArgs.DoNotHandle to prevent this.
        /// </summary>
        public event EventHandler<ErroneousNickEventArgs> NickInUse;

        internal void OnNickInUse(ErroneousNickEventArgs e)
        {
            NickInUse?.Invoke(this, e);
        }

        /// <summary>
        ///     Occurs when a user or channel mode is changed.
        /// </summary>
        public event EventHandler<ModeChangeEventArgs> ModeChanged;

        internal void OnModeChanged(ModeChangeEventArgs e)
        {
            ModeChanged?.Invoke(this, e);
        }

        /// <summary>
        ///     Occurs when a user joins a channel.
        /// </summary>
        public event EventHandler<ChannelUserEventArgs> UserJoinedChannel;

        internal void OnUserJoinedChannel(ChannelUserEventArgs e)
        {
            UserJoinedChannel?.Invoke(this, e);
        }

        /// <summary>
        ///     Occurs when a user parts a channel.
        /// </summary>
        public event EventHandler<ChannelUserEventArgs> UserPartedChannel;

        internal void OnUserPartedChannel(ChannelUserEventArgs e)
        {
            UserPartedChannel?.Invoke(this, e);
        }

        /// <summary>
        ///     Occurs when we have received the list of users present in a channel.
        /// </summary>
        public event EventHandler<ChannelEventArgs> ChannelListReceived;

        internal void OnChannelListReceived(ChannelEventArgs e)
        {
            ChannelListReceived?.Invoke(this, e);
        }

        /// <summary>
        ///     Occurs when we have received the topic of a channel.
        /// </summary>
        public event EventHandler<ChannelTopicEventArgs> ChannelTopicReceived;

        internal void OnChannelTopicReceived(ChannelTopicEventArgs e)
        {
            ChannelTopicReceived?.Invoke(this, e);
        }

        /// <summary>
        ///     Occurs when the IRC connection is established and it is safe to begin interacting with the server.
        /// </summary>
        public event EventHandler<EventArgs> ConnectionComplete;

        internal void OnConnectionComplete(EventArgs e)
        {
            ConnectionComplete?.Invoke(this, e);
        }

        /// <summary>
        ///     Occurs when we receive server info (such as max nick length).
        /// </summary>
        public event EventHandler<SupportsEventArgs> ServerInfoReceived;

        internal void OnServerInfoReceived(SupportsEventArgs e)
        {
            ServerInfoReceived?.Invoke(this, e);
        }

        /// <summary>
        ///     Occurs when a user is kicked.
        /// </summary>
        public event EventHandler<KickEventArgs> UserKicked;

        internal void OnUserKicked(KickEventArgs e)
        {
            UserKicked?.Invoke(this, e);
        }

        /// <summary>
        ///     Occurs when a WHOIS response is received.
        /// </summary>
        public event EventHandler<WhoIsReceivedEventArgs> WhoIsReceived;

        internal void OnWhoIsReceived(WhoIsReceivedEventArgs e)
        {
            WhoIsReceived?.Invoke(this, e);
        }

        /// <summary>
        ///     Occurs when a user has changed their nick.
        /// </summary>
        public event EventHandler<NickChangedEventArgs> NickChanged;

        internal void OnNickChanged(NickChangedEventArgs e)
        {
            NickChanged?.Invoke(this, e);
        }

        /// <summary>
        ///     Occurs when a user has quit.
        /// </summary>
        public event EventHandler<UserEventArgs> UserQuit;

        internal void OnUserQuit(UserEventArgs e)
        {
            UserQuit?.Invoke(this, e);
        }

        /// <summary>
        ///     Occurs when a WHO (WHOX protocol) is received.
        /// </summary>
        public event EventHandler<WhoxReceivedEventArgs> WhoxReceived;

        internal void OnWhoxReceived(WhoxReceivedEventArgs e)
        {
            WhoxReceived?.Invoke(this, e);
        }
    }
}