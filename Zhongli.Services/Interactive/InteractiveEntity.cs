using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.Interactive.Paginator;
using Zhongli.Data;
using Zhongli.Services.Core.Listeners;
using Zhongli.Services.Interactive.Functions;
using Zhongli.Services.Utilities;

namespace Zhongli.Services.Interactive
{
    public abstract class InteractiveEntity<T> : InteractivePromptBase where T : class
    {
        private const string EmptyMatchMessage = "Unable to find any match. Provide at least 2 characters.";
        private readonly CommandErrorHandler _error;
        private readonly ZhongliContext _db;

        protected InteractiveEntity(CommandErrorHandler error, ZhongliContext db)
        {
            _error = error;
            _db    = db;
        }

        protected virtual string? Title { get; } = null;

        protected abstract (string Title, StringBuilder Value) EntityViewer(T entity);

        protected abstract bool IsMatch(T entity, string id);

        protected async Task AddEntityAsync(T entity)
        {
            var collection = await GetCollectionAsync();
            collection.Add(entity);

            await _db.SaveChangesAsync();
            await Context.Message.AddReactionAsync(new Emoji("✅"));
        }

        protected async Task PagedViewAsync(IEnumerable<T> collection, string? title = null)
        {
            var pages = collection
                .Select(EntityViewer)
                .Select((e, i) => new EmbedFieldBuilder()
                    .WithName($"{i}: {e.Title}")
                    .WithValue(e.Value));

            var paginated = new PaginatedMessage
            {
                Title   = title ?? Title,
                Pages   = pages,
                Author  = new EmbedAuthorBuilder().WithGuildAsAuthor(Context.Guild),
                Options = new PaginatedAppearanceOptions { DisplayInformationIcon = false }
            };

            await PagedReplyAsync(paginated);
        }

        protected virtual async Task RemoveEntityAsync(string id)
        {
            var collection = await GetCollectionAsync();
            var entity = await TryFindEntityAsync(id, collection);

            if (entity is null)
            {
                await _error.AssociateError(Context.Message, EmptyMatchMessage);
                return;
            }

            await RemoveEntityAsync(entity);
            await Context.Message.AddReactionAsync(new Emoji("✅"));
        }

        protected abstract Task RemoveEntityAsync(T entity);

        protected abstract Task<ICollection<T>> GetCollectionAsync();

        protected async Task<T?> TryFindEntityAsync(string id, IEnumerable<T>? collection = null)
        {
            if (id.Length < 2)
                return null;

            collection ??= await GetCollectionAsync();
            var filtered = collection
                .Where(e => IsMatch(e, id))
                .ToList();

            if (filtered.Count <= 1)
                return filtered.Count == 1 ? filtered.First() : null;

            var containsCriterion = new FuncCriterion(m =>
                int.TryParse(m.Content, out var selection)
                && selection < filtered.Count && selection > -1);

            await PagedViewAsync(filtered, "Reply with a number to select.");
            var selected = await NextMessageAsync(containsCriterion);
            return selected is null ? null : filtered.ElementAtOrDefault(int.Parse(selected.Content));
        }
    }
}