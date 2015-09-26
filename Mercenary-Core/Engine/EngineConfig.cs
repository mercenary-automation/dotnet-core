using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json.Linq;

namespace Mercenary.Core.Engine
{
    public class EngineConfig
    {
        private static EngineConfig config;
        public enum Roles { SERVER = 6565, TARGET = 6464 };

        private Dictionary<String, String> environment;
        private Dictionary<String, JObject> plugins;

        public static void ResetConfig ()
        {
            config = new EngineConfig();
        }

        public static EngineConfig GetConfig()
        {
            if (Object.ReferenceEquals(config, null))
            {
                config = new EngineConfig();
            }

            return config;
        }

        private EngineConfig()
        {
            this.Role = Roles.SERVER; 
            this.Port = ((int)Roles.SERVER).ToString();

            this.FilePath = GetJsonPath();
            if (!Object.ReferenceEquals(this.FilePath, null))
            {
                try
                {
                    string data = File.ReadAllText(this.FilePath);
                    this.Json = JObject.Parse(data);

                    String rvalue = (String) this.Json["role"];
                    if ((rvalue != null) && (rvalue.Length > 0))
                    {
                        this.Role = (Roles)Enum.Parse(typeof(Roles), rvalue.ToUpper());
                        this.Port = ((int)this.Role).ToString();
                    }

                    String pvalue = (String) this.Json["port"];
                    if ((pvalue != null) && (pvalue.Length > 0))
                    {
                        this.Port = pvalue;
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("This is a problem");
                    Console.WriteLine(e.Message);
                    this.Json = JObject.Parse("{}");
                }
            }
        }

        private String GetJsonPath()
        {
            String[] args = System.Environment.GetCommandLineArgs();
            String path = Path.GetDirectoryName(Assembly.GetCallingAssembly().Location) + @"\config.json";

            if ((args.Length > 1) && (File.Exists(args[1])))
            {
                return args[1];
            }
            else if (File.Exists(path))
            {
                return path;
            }

            return null;
        }

        public String GetProperty(String key, String def)
        {
            String val = GetProperty(key);
            return (Object.ReferenceEquals(val, null)) ? val : def;
        }

        public String GetProperty(String key)
        {
            JProperty prop = this.Json.Property(key);
            if (Object.ReferenceEquals(prop, null))
            {
                return (string)prop.Value;
            }

            return null;
        }

        public Dictionary<String, String> Environment
        {
            get
            {
                if (Object.ReferenceEquals(environment, null))
                {
                    this.environment = new Dictionary<String, String>();
                    if (this.Role == Roles.TARGET)
                    {
                        JProperty prop = this.Json.Property("environment");
                        if (Object.ReferenceEquals(prop, null))
                        {
                            JObject items = (JObject)prop.Value;
                            IList<string> keys = items.Properties().Select(p => p.Name).ToList();
                            foreach (string key in keys)
                            {
                                environment.Add(key, (string)items[key]);
                            }
                        }
                    }
                }

                return this.environment;
            }
        }

        public Dictionary<String, JObject> Plugins
        {
            get
            {
                if (Object.ReferenceEquals(plugins, null))
                {
                    this.plugins = new Dictionary<String, JObject>();
                    JProperty prop = this.Json.Property("plugins");
                    if (Object.ReferenceEquals(prop, null))
                    {
                        JObject items = (JObject)prop.Value;
                        IList<string> keys = items.Properties().Select(p => p.Name).ToList();
                        foreach (string key in keys)
                        {
                            plugins.Add(key, (JObject)items[key]);
                        }
                    }
                }

                return this.plugins;
            }
        }

        public Roles Role { get; private set; }

        public String Port { get; private set; }

        public JObject Json { get; private set; }

        public string FilePath { get; private set; }
    }
}
