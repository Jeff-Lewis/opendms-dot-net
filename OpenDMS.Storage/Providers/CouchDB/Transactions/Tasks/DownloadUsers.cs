﻿using System;
using System.Collections.Generic;

namespace OpenDMS.Storage.Providers.CouchDB.Transactions.Tasks
{
    public class DownloadUsers : Base
    {
        private IDatabase _db;

        public List<Security.User> Users { get; private set; }

        public DownloadUsers(IDatabase db,
            int sendTimeout, int receiveTimeout, int sendBufferSize, int receiveBufferSize)
            : base(sendTimeout, receiveTimeout, sendBufferSize, receiveBufferSize)
        {
            _db = db;
        }

        public override void Process()
        {
            Remoting.GetView rem;

            TriggerOnActionChanged(EngineActionType.GettingUsers, true);

            try
            {
                rem = new Remoting.GetView(_db, "users", "GetAll", _sendTimeout, _receiveTimeout,
                    _sendBufferSize, _receiveBufferSize);
            }
            catch (Exception e)
            {
                Logger.Storage.Error("An exception occurred while instantiating the Transactions.Tasks.Remoting.GetView object.", e);
                throw;
            }

            rem.OnComplete += delegate(Remoting.Base sender, ICommandReply reply)
            {
                Transitions.UserCollection txUc = new Transitions.UserCollection();
                Users = txUc.Transition(rem.View);
                TriggerOnComplete(reply);
            };
            rem.OnError += delegate(Remoting.Base sender, string message, Exception exception)
            {
                TriggerOnError(message, exception);
            };
            rem.OnProgress += delegate(Remoting.Base sender, OpenDMS.Networking.Protocols.Tcp.DirectionType direction, int packetSize, decimal sendPercentComplete, decimal receivePercentComplete)
            {
                TriggerOnProgress(direction, packetSize, sendPercentComplete, receivePercentComplete);
            };
            rem.OnTimeout += delegate(Remoting.Base sender)
            {
                TriggerOnTimeout();
            };

            rem.Process();
        }
    }
}
