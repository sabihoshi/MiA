using System;
using Discord;
using Discord.Commands;

namespace Zhongli.Data.Models.Moderation.Infractions
{
    public static class ModerationExtensions
    {
        public static T WithModerator<T>(this T action, IGuildUser moderator) where T : IModerationAction
        {
            action.Action = new ModerationAction
            {
                Date = DateTimeOffset.UtcNow,

                GuildId     = moderator.Guild.Id,
                ModeratorId = moderator.Id
            };

            return action;
        }
    }
}