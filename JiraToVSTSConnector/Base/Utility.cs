using System;
using System.Collections.Generic;
using System.Linq;
using System.Configuration;
using System.Text;
using System.Threading.Tasks;
using Atlassian.Jira;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;

namespace JiraToVSTSConnector.Base
{
    public static class Utility
    {
        static Dictionary<string, string> itemStatus;
        static Dictionary<string, string> itemPriority;
        static Dictionary<string, string> itemIssueType;

        static Utility()
        {
            itemStatus = ConfigurationManager.AppSettings[Const.JiraVstsStatus].ToDictionary();
            itemPriority = ConfigurationManager.AppSettings[Const.JiraVstsPriority].ToDictionary();
            itemIssueType = ConfigurationManager.AppSettings[Const.JiraVstsIssueType].ToDictionary();
        }

        public static string ToVsts(this IssueStatus issueStatus)
        {
            return itemStatus.Map(issueStatus.ToString());
        }

        public static string ToVsts(this IssuePriority issuePriority)
        {
            return itemPriority.Map(issuePriority.ToString());
        }

        public static string ToVsts(this IssueType issueType)
        {
            return itemIssueType.Map(issueType.ToString());
        }

        public static void ShowErrorMessage(Exception ex)
        {
            Console.WriteLine($"Error Occured: {ex.Message}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
            }
        }

        static string Map(this IDictionary<string, string> items, string value)
        {
            if (value == null) return String.Empty;
            if (!items.ContainsKey(value))
            {
                Console.WriteLine($"Cannot map {value}");
                return value;
            }
            return items[value];
        }

        /// <summary>
        /// Returns parts of the given value separated with the given separator. Empty items are excluded if excludeEmptyParts is true.
        /// </summary>
        /// <param name="value">the value for which the parts are calculated</param>
        /// <param name="separator">separator which separates the parts in the given value</param>
        /// <returns>seq of parts within the given value</returns>
        public static IEnumerable<string> GetParts(this string value, string separator)
        {
            if (string.IsNullOrEmpty(value) || string.IsNullOrEmpty(separator))
                return Enumerable.Empty<string>();

            return value.Split(new[] { separator }, StringSplitOptions.RemoveEmptyEntries);
        }

        public static Dictionary<string, string> ToDictionary(this string value, string splitRows = ";", string splitPairs = ",")
        {
            var result = value.GetParts(splitRows).Trim()
                .Select(part => part.GetParts(splitPairs).ToArray())
                .ToDictionary(split => split[0].Trim(), split => split[1].Trim(), StringComparer.OrdinalIgnoreCase);
            return result;
        }

        public static int GetParentId(this IList<WorkItemRelation> relations)
        {
            if(relations != null)
            {
                var parentRelation = relations.FirstOrDefault<WorkItemRelation>(r => r.Rel == "System.LinkTypes.Hierarchy-Reverse");

                if(parentRelation != null)
                {
                    var parentId = parentRelation.Url.Substring(parentRelation.Url.LastIndexOf('/') + 1);

                    return Convert.ToInt32(parentId);
                }
            }
            
            return -1;
        }


        public static int GetPositionId(this IList<WorkItemRelation> relations, int id)
        {
            if (relations != null && id > 0)
            {
                var index = 0;
                foreach (WorkItemRelation rel in relations)
                {
                    if (rel.Url.Substring(rel.Url.LastIndexOf('/') + 1) == id.ToString())
                        return index;

                    index++;
                }
            }

            return -1;
        }

        public static IEnumerable<string> RemoveEmpty(this IEnumerable<string> items) => items.Where(i => !string.IsNullOrEmpty(i));

        public static IEnumerable<string> Trim(this IEnumerable<string> items) => items.Select(i => i?.Trim()).RemoveEmpty();

        public static IEnumerable<string> AppendProjectFilter(this IEnumerable<string> items, string projectName) => items.Select(i => i.AppendProjectFilter(projectName));

        public static string AppendProjectFilter(this string query, string projectName) => string.Format("project = '{0}' and {1}", projectName, query);

        public static string GetCustomFieldValue(this Issue issue, string keyName)
        {
            if (issue != null)
            {
                foreach (CustomFieldValue field in issue.CustomFields)
                {
                    if (field.Name == keyName)
                    {
                        return field.Values[0];
                    }
                }
            }

            return string.Empty;
        }

        public static bool WriteToTextFile(this IEnumerable<Issue> issues)
        {
            List<string> keys = new List<string>();
            foreach(var issue in issues)
            {
                keys.Add(issue.Key.ToString());
            }

            System.IO.File.AppendAllLines("IssueKeys.txt", keys.ToArray<string>());

            return true;
        }

        public static bool IsAssignedToMSTeam(this Issue issue)
        {
            if (issue != null)
            {
                switch (issue.GetCustomFieldValue("Team").ToLowerInvariant())
                {
                    case "delta":
                    case "ninja":
                    case "nova":
                    case "spartan":
                    case "msft engineering":
                    case "vulcan":
                    case "titan":
                    case "phoenix":
                    case "jupiter":
                        return true;
                }
            }

            return false;
        }

        public static string GetComponentName(this Issue issue, bool getFirst = false)
        {
            if (issue.Components.LastOrDefault<ProjectComponent>() != null)
            {
                if (getFirst)
                {
                    return issue.Components.FirstOrDefault<ProjectComponent>().Name.Trim();
                }
                else
                {
                    return issue.Components.LastOrDefault<ProjectComponent>().Name.Trim();
                }
            }

            return string.Empty;
        }

        public static Issue GetRoadMapByName(this IEnumerable<Issue> issues, string roadMapName)
        {
            return (from rm in issues where rm.Key == roadMapName select rm).FirstOrDefault<Issue>();
        }

        public static string GetParentLink(this Issue issue)
        {
            return issue.CustomFields["Parent Link"]?.Values.FirstOrDefault();
        }

        public static string GetEpicLink(this Issue issue)
        {
            return issue.CustomFields["Epic Link"]?.Values.FirstOrDefault();
        }

        public static string Priority(this Issue issue)
        {
            var planningPriority = issue.GetCustomFieldValue("Planning Priority");
            Int16 priority;

            if (!string.IsNullOrEmpty(planningPriority) && Int16.TryParse(planningPriority, out priority))
            {
                return priority > 4 ? "4" : planningPriority;
            }

            return issue.Priority.ToVsts();
        }

        public static string GetFieldValue(this WorkItem workItem, string fieldName)
        {
            if (workItem != null && workItem.Fields.ContainsKey(fieldName))
                return workItem.Fields[fieldName].ToString();

            return string.Empty;
        }

        public static string GetAreaPath(this Issue issue, string projectName, Issue roadMap)
        {
            if (roadMap != null)
            {
                var areaPath = $"{projectName}\\";

                if (roadMap.GetComponentName().Length > 0)
                {
                    areaPath += roadMap.GetComponentName() + "\\";
                }

                areaPath = $"{areaPath}{roadMap.Summary}".EncodedAreaPathName();

                return areaPath;
            }

            return string.Empty;
        }

        public static string EncodedAreaPathName(this string vstsAreaName)
        {
            return vstsAreaName.Replace("&", "And").Replace("/", " or ").Trim();
        }
    }
}
