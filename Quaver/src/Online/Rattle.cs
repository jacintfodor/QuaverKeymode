﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
#if !PUBLIC
using Quaver.Framework.Events.Packets;
using Quaver.Framework.Events.Packets.Structures;
using Quaver.Framework.Events;
using Quaver.Logging;

namespace Quaver.Online
{
    internal static class Rattle
    {
        /// <summary>
        ///     Initialize a new OnlineEvents instance
        /// </summary>
        internal static OnlineEvents OnlineEvents { get; set; } = new OnlineEvents();

        /// <summary>
        ///     Keeps track if the user is still logged in
        /// </summary>
        internal static bool IsLoggedIn;

        /// <summary>
        ///     The online client that is logged in.
        /// </summary>
        internal static OnlineClient Client { get; set; }

        /// <summary>
        ///     The list currently online users
        /// </summary>
        internal static List<OnlineClient> OnlineClients { get; set; }

        /// <summary>
        ///     The list of chat channels the user is in
        /// </summary>
        internal static List<ChatChannel> ChatChannels { get; set; } = new List<ChatChannel>();

        /// <summary>
        ///     Initializes the online event hooking
        /// </summary>
        internal static void Initialize()
        {
            OnlineEvents.Connecting += OnConnecting;
            OnlineEvents.ConnectionError += OnConnectionError;
            OnlineEvents.Disconnection += OnDisconnection;
            OnlineEvents.RattleLoginReply += OnRattleLoginReply;
            OnlineEvents.RattleUserConnected += OnRattleUserConnected;
            OnlineEvents.RattleUserDisconnected += OnRattleUserDisconnected;
            OnlineEvents.RattleUserJoinedChatChannel += OnRattleUserJoinedChatChannel;
        }

        /// <summary>
        ///     On connection hook
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void OnConnecting(object sender, EventArgs e)
        {
            Logger.Log("Connecting to the Quaver server.", LogColors.GameInfo);
        }

        /// <summary>
        ///     On connection error hook
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void OnConnectionError(object sender, PacketEventArgs e)
        {
            // Log out user
            ResetLogin();

            Logger.Log("A connection error occurred.", LogColors.GameError);
        }

        /// <summary>
        ///     On Disconnection hook
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void OnDisconnection(object sender, PacketEventArgs e)
        {
            // Log out user
            ResetLogin();
            Logger.Log("Disconnected from the Quaver server", LogColors.GameWarning);
        }

        /// <summary>
        ///     On RattleLoginReply Hook
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void OnRattleLoginReply(object sender, LoginReplyEventArgs e)
        {
            var response = e.Data;

            // Handle incorrect login errors
            if (!response.Success)
            {
                switch (response.Error)
                {
                    case LoginError.None:
                        // This should never happen - server will always return a successful login w/ no error
                        break;
                    case LoginError.AlreadyConnected:
                        break;
                    case LoginError.Banned:
                        break;
                    case LoginError.InvalidCredentials:
                        break;
                    case LoginError.Require2FA:
                        break;
                    default:
                        break;
                }

                Logger.Log($"Login failed", LogColors.GameError);
                return;
            }

            // Set the current client 
            Client = response.CurrentClient;
            IsLoggedIn = true;

            // Add self to list of online clients
            OnlineClients = response.OnlineClients;
            OnlineClients.Add(Client);

            var log = $"Successfully logged in as {Client.Username} #{Client.UserId} \n" +
                      $"You are logging in from {Client.Country} w/ time offset: {Client.TimeOffset} \n" +
                      $"There are currently: {OnlineClients.Count} users online.";

            Logger.Log(log, LogColors.GameInfo);
        }

        /// <summary>
        ///     On RattleUserConnected hook
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void OnRattleUserConnected(object sender, UserConnectedEventArgs e)
        {
            var client = e.Data;

            // Add the new client to the list of online users
            OnlineClients.Add(client);

            var log = $"{client.Username} #{client.UserId} has logged into the server.\n" +
                      $"There are now {OnlineClients.Count} users online.";

            Logger.Log(log, LogColors.GameInfo);
        }

        /// <summary>
        ///     On RattleUserDisconnected hook
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void OnRattleUserDisconnected(object sender, UserDisconnectedEventArgs e)
        {
            var disconnectedUser = OnlineClients.Find(x => x.UserId == e.Data.UserId);
            OnlineClients.Remove(disconnectedUser);

            var log = $"User: {disconnectedUser.Username} #{disconnectedUser.UserId} has disconnected.\n" +
                      $"There are now {OnlineClients.Count} users online";

            Logger.Log(log, LogColors.GameInfo);
        }

        /// <summary>
        ///     On RattleUserJoinedChatChannel hook
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void OnRattleUserJoinedChatChannel(object sender, UserJoinedChatChannelEventArgs e)
        {
            var packet = e.Data;

            // If the packet return a failure or the chat channel already exists, return.
            if (!packet.Success || ChatChannels.Find(x => x.ChannelName == packet.Channel) != null)
                return;

            // If no channel was found, add a new one.
            ChatChannels.Add(new ChatChannel { ChannelName = packet.Channel });

            // TODO: Add some sort of UI here displaying the newly joined channel
                
            Logger.Log("Joined chat channel: " + packet.Channel, LogColors.GameInfo);
        }

        /// <summary>
        ///     Logs out the user completely
        /// </summary>
        internal static void ResetLogin(bool tellServer = false)
        {
            IsLoggedIn = false;
            Client = null;
            OnlineClients = null;
        }
    }
}
#endif