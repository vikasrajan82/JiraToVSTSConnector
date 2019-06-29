using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using JiraToVSTSConnector.Base;

namespace JiraToVSTSConnector.Infrastructure
{
    public class Configuration
    {
        private static readonly Configuration instance = new Configuration();
        // Explicit static constructor to tell C# compiler
        // not to mark type as beforefieldinit
        static Configuration() { }
        private Configuration() { }
        public static Configuration Instance { get { return instance; } }

        public VstsConfig vstsConfig { get; } = new VstsConfig();
        public JiraConfig jiraConfig { get; } = new JiraConfig();

        private IEnumerable<string> jiraQueries;

        public IEnumerable<string> JiraQueries
        {
            get
            {
                if(jiraQueries == null)
                {
                    this.RetrieveJiraQueries();
                }

                return jiraQueries;
            }
        }

        public Configuration Initialise()
        {
            vstsConfig.Url = ConfigurationManager.AppSettings[Const.VstsUrl];
            vstsConfig.Project = ConfigurationManager.AppSettings[Const.VstsProject];
            vstsConfig.PersonalAccessToken = ConfigurationManager.AppSettings[Const.VstsPersonalAccessToken];

            jiraConfig.Url = ConfigurationManager.AppSettings[Const.JiraUrl];
            jiraConfig.UserId = ConfigurationManager.AppSettings[Const.JiraUserId];
            jiraConfig.Password = ConfigurationManager.AppSettings[Const.JiraPassword];
            jiraConfig.Project = ConfigurationManager.AppSettings[Const.JiraProject];

            var check = new Action<string, string>((string input, string keyName) =>
            {
                if (String.IsNullOrEmpty(input))
                {
                    Console.WriteLine(ConfigurationManager.AppSettings[keyName + ".Error"]);
                }
            });

            check(vstsConfig.Url, Const.VstsUrl);
            check(vstsConfig.Project, Const.VstsProject);
            check(vstsConfig.PersonalAccessToken, Const.VstsPersonalAccessToken);
            check(jiraConfig.Url, Const.JiraUrl);
            check(jiraConfig.UserId, Const.JiraUserId);
            check(jiraConfig.Password, Const.JiraPassword);
            check(jiraConfig.Project, Const.JiraProject);


            return this;
        }

        private void RetrieveJiraQueries()
        {
            this.jiraQueries = ConfigurationManager.AppSettings[Const.JiraQueries]
                .GetParts(";")
                .Trim().AppendProjectFilter(this.jiraConfig.Project);
        }


    }
}
