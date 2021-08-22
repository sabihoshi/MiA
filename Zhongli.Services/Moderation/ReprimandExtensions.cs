using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Humanizer;
using Microsoft.EntityFrameworkCore;
using Zhongli.Data;
using Zhongli.Data.Models.Discord;
using Zhongli.Data.Models.Moderation.Infractions;
using Zhongli.Data.Models.Moderation.Infractions.Reprimands;
using Zhongli.Data.Models.Moderation.Infractions.Triggers;
using Zhongli.Services.Utilities;

namespace Zhongli.Services.Moderation
{
    public static class ReprimandExtensions
    {
        public static bool IsActive(this IExpirable expirable)
            => expirable.EndedAt is null || expirable.ExpireAt >= DateTimeOffset.Now;

        public static bool IsTriggered(this ITrigger trigger, uint amount)
        {
            return trigger.Mode switch
            {
                TriggerMode.Exact       => amount == trigger.Amount,
                TriggerMode.Retroactive => amount >= trigger.Amount,
                TriggerMode.Multiple    => amount % trigger.Amount is 0 && amount is not 0,
                _ => throw new ArgumentOutOfRangeException(nameof(trigger), trigger.Mode,
                    "Invalid trigger mode.")
            };
        }

        public static EmbedBuilder AddReprimands(this EmbedBuilder embed, GuildUserEntity user) => embed
            .AddField("Warnings", $"{user.WarningCount()}/{user.WarningCount(true)}", true)
            .AddReprimand<Notice>(user)
            .AddReprimand<Ban>(user)
            .AddReprimand<Kick>(user)
            .AddReprimand<Note>(user);

        public static IEnumerable<Reprimand> OfType(this IEnumerable<Reprimand> reprimands, InfractionType type)
        {
            return type switch
            {
                InfractionType.Ban      => reprimands.OfType<Ban>(),
                InfractionType.Censored => reprimands.OfType<Censored>(),
                InfractionType.Kick     => reprimands.OfType<Kick>(),
                InfractionType.Mute     => reprimands.OfType<Mute>(),
                InfractionType.Note     => reprimands.OfType<Note>(),
                InfractionType.Notice   => reprimands.OfType<Notice>(),
                InfractionType.Warning  => reprimands.OfType<Warning>(),
                InfractionType.All      => reprimands,
                _ => throw new ArgumentOutOfRangeException(nameof(type), type,
                    "Invalid Infraction type.")
            };
        }

        public static IEnumerable<T> Reprimands<T>(this GuildUserEntity user, bool countHidden = false)
            where T : Reprimand
        {
            var reprimands = user.Guild.ReprimandHistory
                .Where(r => r.UserId == user.Id
                    && r.Status is not ReprimandStatus.Deleted)
                .OfType<T>();

            return countHidden
                ? reprimands
                : reprimands.Where(IsCounted);
        }

        public static Task<GuildEntity> GetGuildAsync(this ReprimandDetails details, ZhongliContext db,
            CancellationToken cancellationToken)
            => db.Guilds.TrackGuildAsync(details.User.Guild, cancellationToken);

        public static async Task<uint> CountAsync<T>(this T reprimand,
            DbContext db, bool countHidden = false,
            CancellationToken cancellationToken = default) where T : Reprimand
        {
            var user = await reprimand.GetUserAsync(db, cancellationToken);
            if (reprimand is Warning)
                return user.WarningCount();

            return (uint) user.Reprimands<T>(countHidden).LongCount();
        }

        public static async Task<uint> CountAsync<T>(
            this T reprimand, Trigger trigger,
            DbContext db, CancellationToken cancellationToken = default) where T : Reprimand
        {
            var user = await reprimand.GetUserAsync(db, cancellationToken);
            return (uint) user.Reprimands<T>()
                .LongCount(r => r.TriggerId == trigger.Id);
        }

        public static uint HistoryCount<T>(this GuildUserEntity user) where T : Reprimand
            => user.HistoryCount<T>(false);

        public static uint WarningCount(this GuildUserEntity user, bool countHidden = false)
            => (uint) user.Reprimands<Warning>(countHidden).Sum(w => w.Count);

        public static async ValueTask<GuildEntity> GetGuildAsync(this Reprimand reprimand, DbContext db,
            CancellationToken cancellationToken = default)
        {
            return reprimand.Guild ??
                await db.FindAsync<GuildEntity>(new object[] { reprimand.GuildId }, cancellationToken);
        }

        public static async ValueTask<GuildUserEntity> GetUserAsync(this Reprimand reprimand, DbContext db,
            CancellationToken cancellationToken = default)
        {
            return reprimand.User ??
                await db.FindAsync<GuildUserEntity>(new object[] { reprimand.UserId, reprimand.GuildId },
                    cancellationToken);
        }

        public static async ValueTask<Trigger?> GetTriggerAsync(this Reprimand reprimand, DbContext db,
            CancellationToken cancellationToken = default)
        {
            var trigger = reprimand.Trigger;
            if (reprimand.TriggerId is not null)
            {
                trigger ??= await db.FindAsync<Trigger>(new object[] { reprimand.TriggerId },
                    cancellationToken);
            }

            return trigger;
        }

        private static bool IsCounted(Reprimand reprimand)
            => reprimand.Status is ReprimandStatus.Added or ReprimandStatus.Updated;

        private static EmbedBuilder AddReprimand<T>(this EmbedBuilder embed, GuildUserEntity user)
            where T : Reprimand => embed.AddField(typeof(T).Name.Pluralize(),
            $"{user.HistoryCount<T>()}/{user.HistoryCount<T>(true)}", true);

        private static uint HistoryCount<T>(this GuildUserEntity user, bool countHidden) where T : Reprimand
            => (uint) user.Reprimands<T>(countHidden).LongCount();
    }
}