using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JiraToVSTSConnector.Infrastructure
{
    public class VstsConfig
    {
        public string Url { get; set; }
        public string PersonalAccessToken { get; set; }
        public string Project { get; set; }
    }
}
