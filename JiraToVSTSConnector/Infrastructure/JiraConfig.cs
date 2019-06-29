using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JiraToVSTSConnector.Infrastructure
{
    public class JiraConfig
    {
        public string Project { get; set; }
        public string UserId { get; set; }
        public string Password { get; set; }
        public string Url { get; set; }
    }
}
