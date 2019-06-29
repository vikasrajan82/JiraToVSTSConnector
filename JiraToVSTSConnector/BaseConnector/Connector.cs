using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JiraToVSTSConnector.Infrastructure;

namespace JiraToVSTSConnector.BaseConnector
{
    public class Connector
    {
        public static void SyncJiraToVSTS(Configuration connectorConfig)
        {
            if(connectorConfig != null)
            {
                var vstsConnection = new VSTSConnector(connectorConfig.vstsConfig);
                var jiraConnection = new JiraConnector(connectorConfig.jiraConfig);

                foreach (string jiraQuery in connectorConfig.JiraQueries)
                {
                    Console.WriteLine($"Executing query: {jiraQuery}");

                    int index = 0;
                    var issues = jiraConnection.ExecuteQueryAsBatch(jiraQuery, index++);
                    while (issues.Any())
                    {
                        Console.WriteLine($"Retrieving Batch # {index} with row count of {issues.Count()}");

                        vstsConnection.SyncToVsts(issues, jiraConnection);
                        issues = jiraConnection.ExecuteQueryAsBatch(jiraQuery, index++);
                    }
                }
            }
        }
    }
}
