using Penguin.Auditing.Abstractions.Attributes;
using Penguin.Cms.Entities;
using Penguin.Cms.Repositories;
using Penguin.Messaging.Abstractions.Interfaces;
using Penguin.Messaging.Core;
using Penguin.Messaging.Persistence.Interfaces;
using Penguin.Persistence.Abstractions;
using Penguin.Persistence.Abstractions.Interfaces;
using Penguin.Security.Abstractions.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Reflection;

namespace Penguin.Cms.Auditing.Repositories
{
    /// <summary>
    /// A repository with a message handler that accepts persistence context changes so it can log them
    /// </summary>
    public class AuditEntryRepository : EntityRepository<AuditEntry>, IMessageHandler<IUpdating<KeyedObject>>
    {
        private IUserSession UserSession { get; set; }
        private Guid ContextId = Guid.NewGuid();

        /// <summary>
        /// Constructs a new instance of this repository
        /// </summary>
        /// <param name="dbContext"></param>
        /// <param name="userSession"></param>
        /// <param name="messageBus"></param>
        public AuditEntryRepository(IPersistenceContext<AuditEntry> dbContext, IUserSession userSession, MessageBus messageBus = null) : base(dbContext, messageBus)
        {
            UserSession = userSession;
        }

        /// <summary>
        /// Accepts an object update message pre-commit so that it can add the change to the underlying context
        /// </summary>
        /// <param name="message">The object update message</param>
        public void AcceptMessage(IUpdating<KeyedObject> message)
        {
            Contract.Requires(message != null);

            if (message.Target.GetType().GetCustomAttribute<DontAuditChangesAttribute>() != null)
            {
                return;
            }

            foreach (KeyValuePair<string, object> newValue in message.NewValues)
            {
                AuditEntry thisEntry = new AuditEntry()
                {
                    ContextId = ContextId,
                    NewValue = newValue.Value?.ToString(),
                    PropertyName = newValue.Key,
                    Target = (message.Target as Entity)?.Guid ?? Guid.Empty,
                    Target_Id = message.Target._Id,
                    TypeName = message.Target.GetType().Name,
                    TypeNamespace = message.Target.GetType().Namespace,
                    Source = UserSession?.LoggedInUser?.Guid ?? Guid.Empty
                };

                this.Context.Add(thisEntry);
            }
        }
    }
}