﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Volo.Abp;

namespace Volo.CmsKit.Reactions
{
    public class ReactionManager : CmsKitDomainServiceBase
    {
        protected IReactionDefinitionStore ReactionDefinitionStore { get; }
        protected IUserReactionRepository UserReactionRepository { get; }

        public ReactionManager(
            IUserReactionRepository userReactionRepository,
            IReactionDefinitionStore reactionDefinitionStore)
        {
            UserReactionRepository = userReactionRepository;
            ReactionDefinitionStore = reactionDefinitionStore;
        }

        public virtual async Task<List<ReactionDefinition>> GetReactionsAsync(
            [CanBeNull] string entityType = null)
        {
            return await ReactionDefinitionStore.GetReactionsAsync(entityType);
        }

        public virtual async Task<List<ReactionSummary>> GetSummariesAsync(
            [NotNull] string entityType,
            [NotNull] string entityId)
        {
            Check.NotNullOrWhiteSpace(entityType, nameof(entityType));
            Check.NotNullOrWhiteSpace(entityId, nameof(entityId));

            var userReactionCounts = (await UserReactionRepository.GetSummariesAsync(entityType, entityId))
                .ToDictionary(x => x.ReactionName, x => x.Count);

            var reactions = await ReactionDefinitionStore
                .GetReactionsAsync(
                    entityType
                );

            return reactions
                .Select(reaction => new ReactionSummary
                {
                    Reaction = reaction,
                    Count = userReactionCounts.GetOrDefault(reaction.Name)
                })
                .ToList();
        }

        public virtual async Task<UserReaction> CreateAsync(
            Guid userId,
            [NotNull] string entityType,
            [NotNull] string entityId,
            [NotNull] string reactionName)
        {
            Check.NotNullOrWhiteSpace(entityType, nameof(entityType));
            Check.NotNullOrWhiteSpace(entityId, nameof(entityId));
            Check.NotNullOrWhiteSpace(reactionName, nameof(reactionName));

            var existingReaction = await UserReactionRepository.FindAsync(userId, entityType, entityId, reactionName);
            if (existingReaction != null)
            {
                return existingReaction;
            }

            return await UserReactionRepository.InsertAsync(
                new UserReaction(
                    GuidGenerator.Create(),
                    userId,
                    entityType,
                    entityId,
                    reactionName,
                    Clock.Now
                )
            );
        }

        public virtual async Task<bool> DeleteAsync(
            Guid userId,
            [NotNull] string entityType,
            [NotNull] string entityId,
            [NotNull] string reactionName)
        {
            Check.NotNullOrWhiteSpace(entityType, nameof(entityType));
            Check.NotNullOrWhiteSpace(entityId, nameof(entityId));
            Check.NotNullOrWhiteSpace(reactionName, nameof(reactionName));

            var existingReaction = await UserReactionRepository.FindAsync(userId, entityType, entityId, reactionName);
            if (existingReaction == null)
            {
                return false;
            }

            await UserReactionRepository.DeleteAsync(existingReaction);
            return true;
        }
    }
}