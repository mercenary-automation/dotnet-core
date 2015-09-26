using System;
using System.Collections.Specialized;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Grapevine;
using Newtonsoft.Json.Linq;
using System.Net;
using Mercenary.Core.Engine;

namespace Mercenary.Core.Server
{
    public class RemoteTarget : RemoteBase
    {
        protected RestClient client;
        protected Dictionary<String, RestRequest> requests;

        public RemoteTarget(string url, JObject registration) : base(url)
        {
            this.IsAvailable = true;
            this.IsRetired   = false;

            this.requests.Add("refresh",    new RestRequest(HttpMethod.GET, "/refresh"));
            this.requests.Add("status",     new RestRequest(HttpMethod.GET, "/status"));
            this.requests.Add("retire",     new RestRequest(HttpMethod.DELETE, "/retire"));

            this.requests.Add("assigntask", new RestRequest(HttpMethod.GET, "/task/{id}"));
            this.requests.Add("taskstatus", new RestRequest(HttpMethod.POST, "/task/{id}")); 
            this.requests.Add("canceltask", new RestRequest(HttpMethod.DELETE, "/task/{id}"));
        }

        #region Public Properties

        public bool IsAvailable { get; private set; }

        public bool IsRetired { get; private set; }

        #endregion

        #region Request Methods

        public bool Refresh()
        {
            if (!this.IsRetired)
            {
                RestResponse response = this.client.Execute(requests["refresh"]);
                return (response.StatusCode == HttpStatusCode.OK) ? true : false;
            }
            return false;
        }

        public JObject GetStatus()
        {
            var response = client.Execute(requests["status"]);
            return (this.GetJsonPayload(response.Content));
        }

        public bool Retire()
        {
            if (!this.IsRetired)
            {
                RestResponse response = this.client.Execute(requests["retire"]);
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    this.IsRetired = true;
                    this.IsAvailable = false;
                    return true;
                }
            }
            return false;
        }

        #endregion
    }
}
