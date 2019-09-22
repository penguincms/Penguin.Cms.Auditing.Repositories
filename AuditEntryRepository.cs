﻿using Penguin.Entities;
using Penguin.Messaging.Abstractions.Interfaces;
using Penguin.Messaging.Core;
using Penguin.Messaging.Persistence.Interfaces;
using Penguin.Messaging.Persistence.Messages;
using Penguin.Persistence.Abstractions.Interfaces;
using Penguin.Persistence.Abstractions.Models.Base;
using Penguin.Persistence.Repositories;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Text;

namespace Penguin.Cms.Auditing.Repositories
{
    /// <summary>
    /// A repository with a message handler that accepts persistence context changes so it can log them
    /// </summary>
    public class AuditEntryRepository : EntityRepository<AuditEntry>, IMessageHandler<IUpdating<KeyedObject>>
    {
        private Guid ContextId = Guid.NewGuid();

        /// <summary>
        /// Constructs a new instance of this repository
        /// </summary>
        /// <param name="dbContext"></param>
        /// <param name="messageBus"></param>
        public AuditEntryRepository(IPersistenceContext<AuditEntry> dbContext, MessageBus messageBus = null) : base(dbContext, messageBus)
        {
        }

        /// <summary>
        /// Accepts an object update message pre-commit so that it can add the change to the underlying context
        /// </summary>
        /// <param name="message">The object update message</param>
        public void AcceptMessage(IUpdating<KeyedObject> message)
        {
            Contract.Requires(message != null);

            if(message.Target is AuditEntry)
            {
                return;
            }

            if(message.Target is AuditableEntity a)
            {
                if(!a.AuditLogChanges)
                {
                    return;
                }
            }

            foreach (KeyValuePair<string, object> newValue in message.NewValues)
            {
                AuditEntry thisEntry = new AuditEntry()
                {
                    ContextId = ContextId,
                    NewValue = newValue.Value.ToString(),
                    PropertyName = newValue.Key,
                    Target = (message.Target as Entity)?.Guid ?? Guid.Empty,
                    Target_Id = message.Target._Id,
                    TypeName = message.Target.GetType().Name,
                    TypeNamespace = message.Target.GetType().Namespace
                };

                this.Context.Add(newValue);
            }
            
        }
    }
}
