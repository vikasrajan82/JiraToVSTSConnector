using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JiraToVSTSConnector.BaseConnector;

namespace JiraToVSTSConnector
{
    class Program
    {
        static void Main(string[] args)
        {
            var config = Infrastructure.Configuration.Instance.Initialise();
            if (config == null)
            {
                Environment.ExitCode = -1;
                return;
            }

            if (!config.JiraQueries.Any())
            {
                Console.WriteLine($"Add a Jira query in appSettings to sync. Example: status not in (done) and type = epic and sprint is empty");
                return;
            }

            try
            {
                Connector.SyncJiraToVSTS(config);
            }
            catch(Exception ex)
            {
                Console.WriteLine($"Error Occured: {ex.Message}");
                if(ex.InnerException != null)
                {
                    Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                }
            }
            
            Console.ReadLine();
        }
    }
}
