using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Atlassian.Jira;
using JiraToVSTSConnector.Infrastructure;
using JiraToVSTSConnector.Base;

namespace JiraToVSTSConnector.BaseConnector
{
    public class JiraConnector
    {
        Jira jiraConnection = null;

        private const int Max_Size = 25;

        private IEnumerable<Issue> roadMapCollection;

        private string projectName = string.Empty;

        public IEnumerable<Issue> RoadMapCollection
        {
            get
            {
                if (roadMapCollection == null)
                {
                    this.roadMapCollection = this.retrieveAll("type = Roadmap".AppendProjectFilter(this.projectName));
                }

                return this.roadMapCollection;
            }
        }

        public JiraConnector(JiraConfig jiraConfig)
        {
            this.jiraConnection = Jira.CreateRestClient((string)jiraConfig.Url, (string)jiraConfig.UserId, (string)jiraConfig.Password);

            this.projectName = jiraConfig.Project;
        }

        public IEnumerable<Issue> ExecuteQueryAsBatch(string query, int index)
        {
            return this.fetch(query, index, Max_Size);
        }

        private IEnumerable<Issue> retrieveAll(string query)
        {
            List<Issue> issueCollection = new List<Issue>();
            int startAt = 0;

            while (true)
            {
                var result = this.fetch(query, startAt, Max_Size);
                if (!result.Any())
                    break;

                issueCollection.AddRange(result.AsEnumerable<Issue>());

                startAt += 1;
            }

            return issueCollection;

        }
              

        private IPagedQueryResult<Issue> fetch(string jql, int index, int size)
        {
            int startAt = index * size;
            return this.jiraConnection.Issues.GetIssuesFromJqlAsync(jql, startAt: startAt, maxIssues: size).Result;
        }
    }
}
