using System;
using System.Collections.Generic;
using Grapevine;
using Newtonsoft.Json.Linq;

namespace Mercenary.Core.Engine
{
    public abstract class RemoteBase
    {
        protected RestClient client;
        protected Dictionary<String, RestRequest> requests;

        public RemoteBase(string url)
        {
            this.Url      = url;
            this.client   = new RestClient(this.Url);
            this.requests = new Dictionary<string, RestRequest>();
        }

        protected JObject GetJsonPayload(string jsonstring)
        {
            try
            {
                JObject json = JObject.Parse(jsonstring);
                return json;
            }
            catch { return null; }
        }

        public string Url { get; protected set; }
    }
}
