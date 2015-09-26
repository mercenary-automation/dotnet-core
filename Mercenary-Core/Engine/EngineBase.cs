using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;
using System.ServiceProcess;
using System.Text;
using Grapevine;
using Newtonsoft.Json.Linq;
using Mercenary.Core.Server;
using Mercenary.Core.Target;

namespace Mercenary.Core.Engine
{
    public abstract class EngineBase : RestServer
    {
        #region Static Properties and Methods

        public static string Brand = "Mercenary";

        public static EngineBase GetEngine()
        {
            return GetEngine(null);
        }

        public static EngineBase GetEngine(ServiceBase service)
        {
            return (EngineConfig.GetConfig().Role == EngineConfig.Roles.SERVER) ? (EngineBase)new ServerEngine(service) : (EngineBase)new TargetEngine(service);
        }

        #endregion

        #region Protected Variables

        protected EngineConfig config = EngineConfig.GetConfig();
        protected EventLog log = new EventLog();
        protected CompositionContainer container;
        protected ServiceBase service;

        #endregion

        #region Constructors

        public EngineBase() : this(null) { }

        public EngineBase(ServiceBase service)
        {
            this.service   = service;
            this.IsRunning = false;
            this.Host      = "*";
            this.Port      = this.config.Port;

            var catalog = new AggregateCatalog();
            List<string> paths = new List<string>();

            try
            {
                Assembly[] assemblies = new Assembly[] { Assembly.GetCallingAssembly(), Assembly.GetExecutingAssembly(), Assembly.GetEntryAssembly() };
                foreach (Assembly assembly in assemblies)
                {
                    var path = Path.Combine(Path.GetDirectoryName(assembly.Location), @"/plugins");

                    if ((!paths.Contains(path)) && (Directory.Exists(path)))
                    {
                        paths.Add(path);
                        catalog.Catalogs.Add(new DirectoryCatalog(path));
                    }
                }

                this.container = new CompositionContainer(catalog);
                this.container.ComposeParts(this);
            }
            catch (CompositionException cex)
            {
                Console.WriteLine(cex.ToString());
            }
        }

        #endregion

        #region Starting and Stopping Engine

        public bool IsRunning { get; private set; }

        public void StartRunning()
        {
            this.StartLogging();
            this.Log("Starting " + this.Label);

            try
            {
                this.Start();
                this.Log("Started " + this.Label + " listening on port " + this.Port);
                this.IsRunning = true;
            }
            catch (HttpListenerException httpex)
            {
                StringBuilder error = new StringBuilder("Error attempting to start " + this.Label + " on port " + this.config.Port);
                error.Append(Environment.NewLine);
                error.Append(httpex.Message);
                error.Append(Environment.NewLine);
                error.Append(httpex.StackTrace);
                this.Log(error.ToString());
            }
        }

        public void StopRunning()
        {
            this.Log("Stopping " + this.Label);

            try
            {
                this.Stop();
                this.IsRunning = false;
                this.Log(this.Label + " stopped");
            }
            catch (Exception e)
            {
                StringBuilder error = new StringBuilder("Error attempting to stop " + this.Label + " on port " + this.config.Port);
                error.Append(Environment.NewLine);
                error.Append(e.Message);
                error.Append(Environment.NewLine);
                error.Append(e.StackTrace);
                this.Log(error.ToString());
            }

            this.StopLogging();
        }

        public void RemoteStop()
        {
            if (Object.ReferenceEquals(this.service, null))
            {
                this.StopRunning();
            }
            else
            {
                this.service.Stop();
            }
        }

        #endregion

        #region Logging Methods and Properties

        protected void StartLogging()
        {
            if (!this.IsLogging)
            {
                this.Label = Brand + " " + this.config.Role.ToString().Capitalize();

                if (!EventLog.SourceExists(this.Label))
                {
                    EventLog.CreateEventSource(this.Label, Brand);
                }

                this.log.Source = this.Label;
                this.log.Log = Brand;
                this.log.WriteEntry(this.Label + " Engine Created");

                this.IsLogging = true;
            }
        }

        protected void StopLogging()
        {
            if (this.IsLogging)
            {
                try
                {
                    this.log.Close();
                    this.IsLogging = false;
                }
                catch { return; }
            }
        }

        public void Log(string message)
        {
            if (this.IsLogging)
            {
                this.log.WriteEntry(message);
            }
        }

        public bool IsLogging { get; private set; }

        public string Label { get; private set; }

        #endregion

        #region Rest Routes and Helpers

        protected void SendACK(HttpListenerContext context)
        {
            SendTextResponse(context, "ACK");
        }

        protected void SendNAK(HttpListenerContext context)
        {
            context.Response.ContentType = "text/plain"; 
            context.Response.StatusDescription = "Conflict";
            context.Response.StatusCode = 409;
            SendTextResponse(context, "NAK");
        }

        [RestRoute(Method = HttpMethod.GET, PathInfo = @"^/ping$")]
        public void Ping(HttpListenerContext context)
        {
            this.SendTextResponse(context, "Pong");
        }

        [RestRoute(Method = HttpMethod.GET, PathInfo = @"^/config$")]
        public void GetConfig(HttpListenerContext context)
        {
            this.SendJsonResponse(context, config.Json);
        }

        [RestRoute(Method = HttpMethod.POST, PathInfo = @"^/config$")]
        [RestRoute(Method = HttpMethod.PUT, PathInfo = @"^/config$")]
        public void SetConfig(HttpListenerContext context)
        {
            JObject data = GetJsonPayload(context.Request);
            File.WriteAllText(config.FilePath, data.ToString());

            EngineConfig.ResetConfig();
            this.config = EngineConfig.GetConfig();

            this.SendACK(context);
        }

        protected JObject GetJsonPayload(HttpListenerRequest request)
        {
            JObject json = null;
            string jsonstring = null;

            try
            {
                using (var reader = new StreamReader(request.InputStream, request.ContentEncoding))
                {
                    jsonstring = reader.ReadToEnd();
                }
                json = JObject.Parse(jsonstring);
            }
            catch (Exception e)
            {
                StringBuilder msg = new StringBuilder("Error parsing JSON from " + request.RemoteEndPoint);
                msg.Append(Environment.NewLine + "Via : (" + request.HttpMethod + ") : " + request.RawUrl);
                msg.Append(Environment.NewLine + Environment.NewLine + e.GetType() + " : " + e.Message);

                if (jsonstring != null)
                {
                    msg.Append(Environment.NewLine + Environment.NewLine + jsonstring);
                }

                this.Log(msg.ToString());
            }

            return json;
        }

        protected void SendJsonResponse(HttpListenerContext context, JObject json)
        {
            var buffer = Encoding.UTF8.GetBytes(json.ToString());
            var length = buffer.Length;

            context.Response.ContentType = ContentType.JSON.ToValue();
            context.Response.ContentEncoding = Encoding.UTF8;
            context.Response.ContentLength64 = length;
            context.Response.OutputStream.Write(buffer, 0, length);
            context.Response.OutputStream.Close();

            context.Response.Close();
        }

        #endregion
    }
}
