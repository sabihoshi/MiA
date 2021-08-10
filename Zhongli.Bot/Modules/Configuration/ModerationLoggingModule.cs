using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Humanizer;
using Zhongli.Data;
using Zhongli.Data.Models.Authorization;
using Zhongli.Data.Models.Logging;
using Zhongli.Services.Core.Preconditions;
using Zhongli.Services.Utilities;

namespace Zhongli.Bot.Modules.Configuration
{
    [Group("logging")]
    [Name("Moderation Logging Configuration")]
    [RequireAuthorization(AuthorizationScope.Configuration)]
    public class ModerationLoggingModule : ModuleBase<SocketCommandContext>
    {
        private readonly ZhongliContext _db;

        public ModerationLoggingModule(ZhongliContext db) { _db = db; }

        [Command]
        [Summary("Configure the moderation logging options.")]
        public async Task AnonymousAsync(
            [Summary("The logging option to configure. Leave blank for default (none).")]
            LoggingOptions type = LoggingOptions.None,
            [Summary("Set to 'true' or 'false'. Leave blank to toggle.")]
            bool? state = null)
        {
            var guild = await _db.Guilds.FindAsync(Context.Guild.Id);
            guild.LoggingRules.Options = type is LoggingOptions.All
                ? LoggingOptions.All
                : guild.LoggingRules.Options.SetValue(type, state);

            await _db.SaveChangesAsync();
            await ReplyAsync($"New value: {guild.LoggingRules.Options.Humanize()}");
        }

        [Command("channel")]
        [Summary("Configures the Logging Channel that logs will be sent on.")]
        public async Task ConfigureLoggingAsync(
            [Summary("Mention, ID, or name of the text channel that the logs will be sent.")]
            ITextChannel channel)
        {
            var guild = await _db.Guilds.FindAsync(Context.Guild.Id);
            guild.LoggingRules.ModerationChannelId = channel.Id;

            await _db.SaveChangesAsync();
            await Context.Message.AddReactionAsync(new Emoji("✅"));
        }

        [Command("notify")]
        [Summary("Notify user on a reprimand type.")]
        public async Task ConfigureNotificationAsync(
            [Summary("The type of reprimand to notify on. Leave blank for all.")]
            ReprimandNoticeType type = ReprimandNoticeType.All,
            [Summary("Set to 'true' or 'false'. Leave blank to toggle.")]
            bool? notifyUser = null)
        {
            var guild = await _db.Guilds.TrackGuildAsync(Context.Guild);

            guild.LoggingRules.NotifyReprimands = type is ReprimandNoticeType.None
                ? ReprimandNoticeType.None
                : guild.LoggingRules.NotifyReprimands.SetValue(type, notifyUser);

            await _db.SaveChangesAsync();
            await ReplyAsync($"New value: {guild.LoggingRules.NotifyReprimands.Humanize()}");
        }

        [Command("appeal message")]
        [Summary("Set the appeal message when someone is reprimanded.")]
        public async Task ConfigureAppealMessageAsync(
            [Summary("Leave empty to disable the appeal message.")] [Remainder]
            string? message = null)
        {
            var guild = await _db.Guilds.TrackGuildAsync(Context.Guild);
            guild.LoggingRules.ReprimandAppealMessage = message;
            await _db.SaveChangesAsync();

            await Context.Message.AddReactionAsync(new Emoji("✅"));
        }

        [Command("appeal")]
        [Summary("Show the appeal message on a reprimand type.")]
        public async Task ConfigureAppealAsync(
            [Summary("The type of reprimand to show the appeal message on. Leave blank for all.")]
            ReprimandNoticeType type = ReprimandNoticeType.All,
            [Summary("Set to 'true' or 'false'. Leave blank to toggle.")]
            bool? showAppeal = null)
        {
            var guild = await _db.Guilds.TrackGuildAsync(Context.Guild);

            guild.LoggingRules.ShowAppealOnReprimands = type is ReprimandNoticeType.All
                ? ReprimandNoticeType.All
                : guild.LoggingRules.ShowAppealOnReprimands.SetValue(type, showAppeal);

            await _db.SaveChangesAsync();
            await ReplyAsync($"New value: {guild.LoggingRules.ShowAppealOnReprimands.Humanize()}");
        }
    }
}