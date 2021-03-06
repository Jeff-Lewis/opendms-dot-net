﻿using System;
using Http = OpenDMS.Networking.Protocols.Http;

namespace OpenDMS.Storage.Providers.CouchDB.Commands
{
    public class GetView : Base
    {
        public GetView(Uri uri)
            : base(uri, new Http.Methods.Get())
        {
        }

        public override ReplyBase MakeReply(Http.Response response)
        {
            try
            {
                return new GetViewReply(response);
            }
            catch (Exception e)
            {
                Logger.Storage.Error("An exception occurred while creating the GetViewReply.", e);
                throw;
            }
        }
    }
}
