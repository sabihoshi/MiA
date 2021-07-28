﻿using System;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Zhongli.Services.Core.Messages;

namespace Zhongli.Services.Core.Listeners
{
    /// <summary>
    ///     Listens for events from an <see cref="DiscordSocketClient" /> and dispatches them to the rest of the application,
    ///     through an <see cref="IMediator" />.
    /// </summary>
    public class DiscordSocketListener
    {
        private CancellationToken _cancellationToken;

        /// <summary>
        ///     Constructs a new <see cref="DiscordSocketListener" /> with the given dependencies.
        /// </summary>
        public DiscordSocketListener(DiscordSocketClient discordSocketClient, IServiceScopeFactory scope)
        {
            DiscordSocketClient = discordSocketClient;
            Scope               = scope;
        }

        /// <summary>
        ///     The <see cref="IServiceScopeFactory" /> to be used.
        /// </summary>
        private IServiceScopeFactory Scope { get; }

        /// <summary>
        ///     The <see cref="DiscordSocketClient" /> to be listened to.
        /// </summary>
        private DiscordSocketClient DiscordSocketClient { get; }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _cancellationToken                        =  cancellationToken;
            DiscordSocketClient.ChannelCreated        += OnChannelCreatedAsync;
            DiscordSocketClient.ChannelUpdated        += OnChannelUpdatedAsync;
            DiscordSocketClient.GuildAvailable        += OnGuildAvailableAsync;
            DiscordSocketClient.GuildMemberUpdated    += OnGuildMemberUpdatedAsync;
            DiscordSocketClient.JoinedGuild           += OnJoinedGuildAsync;
            DiscordSocketClient.MessageDeleted        += OnMessageDeletedAsync;
            DiscordSocketClient.MessageReceived       += OnMessageReceivedAsync;
            DiscordSocketClient.MessageUpdated        += OnMessageUpdatedAsync;
            DiscordSocketClient.ReactionAdded         += OnReactionAddedAsync;
            DiscordSocketClient.ReactionRemoved       += OnReactionRemovedAsync;
            DiscordSocketClient.Ready                 += OnReadyAsync;
            DiscordSocketClient.Connected             += OnConnectedAsync;
            DiscordSocketClient.Disconnected          += OnDisconnectedAsync;
            DiscordSocketClient.RoleCreated           += OnRoleCreatedAsync;
            DiscordSocketClient.RoleUpdated           += OnRoleUpdatedAsync;
            DiscordSocketClient.UserBanned            += OnUserBannedAsync;
            DiscordSocketClient.UserJoined            += OnUserJoinedAsync;
            DiscordSocketClient.UserLeft              += OnUserLeftAsync;
            DiscordSocketClient.UserVoiceStateUpdated += OnUserVoiceStateUpdatedAsync;

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            DiscordSocketClient.ChannelCreated        -= OnChannelCreatedAsync;
            DiscordSocketClient.ChannelUpdated        -= OnChannelUpdatedAsync;
            DiscordSocketClient.GuildAvailable        -= OnGuildAvailableAsync;
            DiscordSocketClient.GuildMemberUpdated    -= OnGuildMemberUpdatedAsync;
            DiscordSocketClient.JoinedGuild           -= OnJoinedGuildAsync;
            DiscordSocketClient.MessageDeleted        -= OnMessageDeletedAsync;
            DiscordSocketClient.MessageReceived       -= OnMessageReceivedAsync;
            DiscordSocketClient.MessageUpdated        -= OnMessageUpdatedAsync;
            DiscordSocketClient.ReactionAdded         -= OnReactionAddedAsync;
            DiscordSocketClient.ReactionRemoved       -= OnReactionRemovedAsync;
            DiscordSocketClient.Ready                 -= OnReadyAsync;
            DiscordSocketClient.Connected             -= OnConnectedAsync;
            DiscordSocketClient.Disconnected          -= OnDisconnectedAsync;
            DiscordSocketClient.RoleCreated           -= OnRoleCreatedAsync;
            DiscordSocketClient.RoleUpdated           -= OnRoleUpdatedAsync;
            DiscordSocketClient.UserBanned            -= OnUserBannedAsync;
            DiscordSocketClient.UserJoined            -= OnUserJoinedAsync;
            DiscordSocketClient.UserLeft              -= OnUserLeftAsync;
            DiscordSocketClient.UserVoiceStateUpdated -= OnUserVoiceStateUpdatedAsync;

            return Task.CompletedTask;
        }

        private async Task OnChannelCreatedAsync(SocketChannel channel)
        {
            var scope = Scope.CreateScope();
            await scope.ServiceProvider.GetRequiredService<IMediator>()
                .Publish(new ChannelCreatedNotification(channel), _cancellationToken);
        }

        private async Task OnChannelUpdatedAsync(SocketChannel oldChannel, SocketChannel newChannel)
        {
            var scope = Scope.CreateScope();
            await scope.ServiceProvider.GetRequiredService<IMediator>()
                .Publish(new ChannelUpdatedNotification(oldChannel, newChannel), _cancellationToken);
        }

        private async Task OnConnectedAsync()
        {
            var scope = Scope.CreateScope();
            await scope.ServiceProvider.GetRequiredService<IMediator>()
                .Publish(ConnectedNotification.Default, _cancellationToken);
        }

        private async Task OnDisconnectedAsync(Exception arg)
        {
            var scope = Scope.CreateScope();
            await scope.ServiceProvider.GetRequiredService<IMediator>()
                .Publish(new DisconnectedNotification(arg), _cancellationToken);
        }

        private async Task OnGuildAvailableAsync(SocketGuild guild)
        {
            var scope = Scope.CreateScope();
            await scope.ServiceProvider.GetRequiredService<IMediator>()
                .Publish(new GuildAvailableNotification(guild), _cancellationToken);
        }

        private async Task OnGuildMemberUpdatedAsync(SocketGuildUser oldMember, SocketGuildUser newMember)
        {
            var scope = Scope.CreateScope();
            await scope.ServiceProvider.GetRequiredService<IMediator>()
                .Publish(new GuildMemberUpdatedNotification(oldMember, newMember), _cancellationToken);
        }

        private async Task OnJoinedGuildAsync(SocketGuild guild)
        {
            var scope = Scope.CreateScope();
            await scope.ServiceProvider.GetRequiredService<IMediator>()
                .Publish(new JoinedGuildNotification(guild), _cancellationToken);
        }

        private async Task OnMessageDeletedAsync(Cacheable<IMessage, ulong> message, ISocketMessageChannel channel)
        {
            var scope = Scope.CreateScope();
            await scope.ServiceProvider.GetRequiredService<IMediator>()
                .Publish(new MessageDeletedNotification(message, channel), _cancellationToken);
        }

        private async Task OnMessageReceivedAsync(SocketMessage message)
        {
            var scope = Scope.CreateScope();
            await scope.ServiceProvider.GetRequiredService<IMediator>()
                .Publish(new MessageReceivedNotification(message), _cancellationToken);
        }

        private async Task OnMessageUpdatedAsync(
            Cacheable<IMessage, ulong> oldMessage, SocketMessage newMessage,
            ISocketMessageChannel channel)
        {
            var scope = Scope.CreateScope();
            await scope.ServiceProvider.GetRequiredService<IMediator>().Publish(
                new MessageUpdatedNotification(oldMessage, newMessage, channel), _cancellationToken);
        }

        private async Task OnReactionAddedAsync(
            Cacheable<IUserMessage, ulong> message, ISocketMessageChannel channel,
            SocketReaction reaction)
        {
            var scope = Scope.CreateScope();
            await scope.ServiceProvider.GetRequiredService<IMediator>()
                .Publish(new ReactionAddedNotification(message, channel, reaction), _cancellationToken);
        }

        private async Task OnReactionRemovedAsync(
            Cacheable<IUserMessage, ulong> message, ISocketMessageChannel channel,
            SocketReaction reaction)
        {
            var scope = Scope.CreateScope();
            await scope.ServiceProvider.GetRequiredService<IMediator>()
                .Publish(new ReactionRemovedNotification(message, channel, reaction), _cancellationToken);
        }

        private async Task OnReadyAsync()
        {
            var scope = Scope.CreateScope();
            await scope.ServiceProvider.GetRequiredService<IMediator>()
                .Publish(ReadyNotification.Default, _cancellationToken);
        }

        private async Task OnRoleCreatedAsync(SocketRole role)
        {
            var scope = Scope.CreateScope();
            await scope.ServiceProvider.GetRequiredService<IMediator>()
                .Publish(new RoleCreatedNotification(role), _cancellationToken);
        }

        private async Task OnRoleUpdatedAsync(SocketRole oldRole, SocketRole newRole)
        {
            var scope = Scope.CreateScope();
            await scope.ServiceProvider.GetRequiredService<IMediator>()
                .Publish(new RoleUpdatedNotification(oldRole, newRole), _cancellationToken);
        }

        private async Task OnUserBannedAsync(SocketUser user, SocketGuild guild)
        {
            var scope = Scope.CreateScope();
            await scope.ServiceProvider.GetRequiredService<IMediator>()
                .Publish(new UserBannedNotification(user, guild), _cancellationToken);
        }

        private async Task OnUserJoinedAsync(SocketGuildUser guildUser)
        {
            var scope = Scope.CreateScope();
            await scope.ServiceProvider.GetRequiredService<IMediator>()
                .Publish(new UserJoinedNotification(guildUser), _cancellationToken);
        }

        private async Task OnUserLeftAsync(SocketGuildUser guildUser)
        {
            var scope = Scope.CreateScope();
            await scope.ServiceProvider.GetRequiredService<IMediator>()
                .Publish(new UserLeftNotification(guildUser), _cancellationToken);
        }

        private async Task OnUserVoiceStateUpdatedAsync(SocketUser user, SocketVoiceState old, SocketVoiceState @new)
        {
            var scope = Scope.CreateScope();
            await scope.ServiceProvider.GetRequiredService<IMediator>()
                .Publish(new UserVoiceStateNotification(user, old, @new), _cancellationToken);
        }
    }
}