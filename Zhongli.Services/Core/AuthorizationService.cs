using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Zhongli.Data;
using Zhongli.Data.Config;
using Zhongli.Data.Models.Authorization;
using Zhongli.Data.Models.Criteria;
using Zhongli.Data.Models.Discord;
using Zhongli.Services.Interactive;
using Zhongli.Services.Utilities;
using GuildPermission = Zhongli.Data.Models.Discord.GuildPermission;

namespace Zhongli.Services.Core;

public class AuthorizationService
{
    private readonly ZhongliContext _db;

    public AuthorizationService(ZhongliContext db) { _db = db; }

    public async Task<GuildEntity> AutoConfigureGuild(IGuild guild,
        CancellationToken cancellationToken = default)
    {
        var guildEntity = await GetGuildAsync(guild, cancellationToken);
        var auth = guildEntity.AuthorizationGroups;

        if (auth.Any()) return guildEntity;

        var permission = new PermissionCriterion(GuildPermission.Administrator);
        auth.AddRules(AuthorizationScope.All, await guild.GetCurrentUserAsync(), AccessType.Allow, permission);
        await _db.SaveChangesAsync(cancellationToken);

        return guildEntity;
    }

    public ValueTask<bool> IsAuthorizedAsync(ICommandContext context, AuthorizationScope scope,
        CancellationToken cancellationToken = default)
        => IsAuthorizedAsync(new Context(context), scope, cancellationToken);

    public ValueTask<bool> IsAuthorizedAsync(IInteractionContext context, AuthorizationScope scope,
        CancellationToken cancellationToken = default)
        => IsAuthorizedAsync(new Context(context), scope, cancellationToken);

    public async ValueTask<bool> IsAuthorizedAsync(Context context, AuthorizationScope scope,
        CancellationToken cancellationToken = default)
    {
        if (context.User.Id == ZhongliConfig.Configuration.Owner)
            return true;

        var user = (IGuildUser) context.User;
        var rules = await AutoConfigureGuild(user.Guild, cancellationToken);
        return rules.AuthorizationGroups.Scoped(scope)
            .OrderBy(r => r.Action.Date)
            .Aggregate(false, (current, rule) =>
            {
                var passed = rule.Judge(context, user);
                return passed ? rule.Access == AccessType.Allow : current;
            });
    }

    private async Task<GuildEntity> GetGuildAsync(IGuild guild, CancellationToken cancellationToken = default)
    {
        var guildEntity = await _db.Guilds.TrackGuildAsync(guild, cancellationToken);
        await _db.Users.TrackUserAsync(await guild.GetCurrentUserAsync(), cancellationToken);

        return guildEntity;
    }
}