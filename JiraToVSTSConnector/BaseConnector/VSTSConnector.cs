using Atlassian.Jira;
using JiraToVSTSConnector.Base;
using JiraToVSTSConnector.Infrastructure;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;
using Microsoft.VisualStudio.Services.WebApi.Patch;
using Microsoft.VisualStudio.Services.WebApi.Patch.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Configuration;

namespace JiraToVSTSConnector.BaseConnector
{
    public class VSTSConnector
    {
        string vstsProjectName = string.Empty;

        VssConnection vstsConnection = null;

        WorkItemTrackingHttpClient witClient = null;
        
        public VSTSConnector(VstsConfig vstsConfig)
        {
            this.vstsConnection = new VssConnection(new Uri(vstsConfig.Url), new VssBasicCredential(string.Empty, vstsConfig.PersonalAccessToken));

            this.vstsProjectName = vstsConfig.Project;

            this.witClient = vstsConnection.GetClient<WorkItemTrackingHttpClient>();
        }

        public void SyncToVsts(IEnumerable<Issue> issues, JiraConnector jiraConnector)
        {
            if (!issues.AsEmptyIfNull().Any()) return;

            foreach (var issue in issues)
            {
                var existingWorkItem = this.GetWorkItemByTitleAsync(issue.Key.ToString());

                bool isNew = !(existingWorkItem != null && existingWorkItem.Id.HasValue);

                var doc = new JsonPatchDocument();

                this.UpdateFieldsBasedOnIssueType(issue, doc, jiraConnector, existingWorkItem);

                AddField(doc, "Microsoft.Services.Custom.InScope", issue.GetCustomFieldValue("Team").ToLowerInvariant() == "msft engineering" ? "20 - In-SOW" : "");

                AddField(doc, "System.Title", $"{issue.Key.ToString()} - {issue.Summary}");

                AddField(doc, "System.Description", issue.Description == null ? string.Empty : WebUtility.HtmlEncode(issue.Description).Replace("\r\n", "<br>"));

                AddField(doc, "System.State", issue.Status.ToVsts());

                AddField(doc, "Microsoft.VSTS.Common.Priority", issue.Priority());

                AddField(doc, "System.IterationPath", this.vstsProjectName + "\\Solution Model Prep");

                AddField(doc, "System.Tags", string.Join(";", issue.Labels));

                AddField(doc, "Contoso.Custom.Components", issue.GetComponentName(true));

                if (isNew)
                {
                    AddField(doc, "System.CreatedDate", issue.Created);

                    AddField(doc, "System.ChangedDate", issue.Updated);

                    AddField(doc, "System.History", $"Imported from Jira {DateTime.Now} ({TimeZone.CurrentTimeZone.StandardName}). Original Jira ID: {issue.Key}");
                }

                try
                {
                    var workItem = isNew
                          ? this.witClient.CreateWorkItemAsync(doc, this.vstsProjectName, issue.Type.ToVsts(), bypassRules: true, validateOnly: false).Result
                          : this.witClient.UpdateWorkItemAsync(doc, existingWorkItem.Id.Value, bypassRules: true, validateOnly: false).Result;
                }
                catch (Exception ex)
                {
                    Utility.ShowErrorMessage(ex);

                    this.DisplayDetailedErrorMessage(doc);

                    throw new Exception("Ignore");
                }
            }
        }

		private void DisplayDetailedErrorMessage(JsonPatchDocument doc)
        {
            Console.Write("Would you like to view the values being updated(y/n): ");

            var answer = Console.ReadKey();

            if (answer.KeyChar == 'y')
            {
                Console.WriteLine();

                PrintDocAttributes(doc);
            }
        }

        private void PrintDocAttributes(JsonPatchDocument doc)
        {
            if(doc != null)
            {
                for(int iCount = 0; iCount < doc.Count; iCount++)
                {
                    Console.WriteLine($"{doc[iCount].Path.Replace("/fields/","")}:{(doc[iCount].Value.ToString().Length > 100 ? doc[iCount].Value.ToString().Substring(0,100) : doc[iCount].Value.ToString())}");
                }
            }
        }

        private void UpdateFieldsBasedOnIssueType(Issue issue, JsonPatchDocument doc, JiraConnector jiraConnector, WorkItem existingWorkItem)
        {
            switch (issue.Type.ToString())
            {
                case "Epic":
                    if (!string.IsNullOrEmpty(issue.GetParentLink()))
                    {
                        //var roadMapIssue = jiraConnector.RoadMapCollection.GetRoadMapByName(issue.GetParentLink());

                        //if (roadMapIssue != null)
                        //{
                        //    AddField(doc, "System.AreaPath", issue.GetAreaPath(this.vstsProjectName, roadMapIssue));
                        //}

                        AddField(doc, "System.AreaPath", issue.GetAreaPath(this.vstsProjectName, issue));
                    }
                    break;
                case "Story":
                    IList<WorkItemRelation> relations = null;

                    if (!string.IsNullOrEmpty(issue.GetEpicLink()))
                    {
                        var epicParent = this.GetWorkItemByTitleAsync(issue.GetEpicLink());
                        //if (epicParent != null)
                        //{
                        //    AddField(doc, "System.AreaPath", epicParent.GetFieldValue("System.AreaPath"));
                        //}

                        if (existingWorkItem != null && existingWorkItem.Id.HasValue)
                        {
                            relations = this.GetWorkItemRelations(existingWorkItem.Id.Value);

                            if (relations == null)
                            {
                                AddRelationship(doc, "System.LinkTypes.Hierarchy-Reverse", epicParent);
                            }
                            else
                            {
                                var parentId = relations.GetParentId();
                                if (!(parentId == epicParent.Id.Value))
                                {
                                    if (relations.GetPositionId(epicParent.Id.Value) > -1)
                                    {
                                        RemoveRelationship(doc, relations.GetPositionId(epicParent.Id.Value));
                                    }

                                    if (parentId > 0)
                                    {
                                        RemoveRelationship(doc, relations.GetPositionId(parentId));
                                    }

                                    AddRelationship(doc, "System.LinkTypes.Hierarchy-Reverse", epicParent);
                                }
                            }
                        }
                        else
                        {
                            AddRelationship(doc, "System.LinkTypes.Hierarchy-Reverse", epicParent);
                        }
                    }
                    else
                    {
                        //if (!string.IsNullOrEmpty(issue.GetComponentName(true)))
                        //{
                        //    AddField(doc, "System.AreaPath", $"{this.vstsProjectName}\\{issue.GetComponentName(true).EncodedAreaPathName()}");
                        //}

                        if (existingWorkItem != null && existingWorkItem.Id.HasValue)
                        {
                            relations = this.GetWorkItemRelations(existingWorkItem.Id.Value);

                            if (relations != null)
                            {
                                var parentId = relations.GetParentId();

                                if (parentId > 0)
                                {
                                    RemoveRelationship(doc, relations.GetPositionId(parentId));
                                }
                            }
                        }

                    }

                    AddField(doc, "System.AreaPath", issue.GetAreaPath(this.vstsProjectName, issue));

                    AddField(doc, "Contoso.Custom.JiraDispositionType", issue.GetCustomFieldValue("Issue Category"));

                    if (!string.IsNullOrEmpty(issue.GetCustomFieldValue("Team")))
                    {
                        AddField(doc, "Contoso.Custom.Team", issue.GetCustomFieldValue("Team"));
                    }

                    if (string.IsNullOrEmpty(existingWorkItem.GetFieldValue("Microsoft.Services.Custom.TypeofRequirement")))
                    {
                        AddField(doc, "Microsoft.Services.Custom.TypeofRequirement", issue.GetCustomFieldValue("Issue Category"));
                    }
                    break;
            }

            if (existingWorkItem != null && existingWorkItem.GetFieldValue("System.WorkItemType") != String.Empty)
            {
                if (issue.Type.ToVsts() != existingWorkItem.GetFieldValue("System.WorkItemType"))
                {
                    ChangeWorkItemType(doc, issue.Type.ToVsts());
                }
            }
        }

        private WorkItem GetWorkItemByTitleAsync(string title)
        {
            var query = new Wiql { Query = $"Select [System.Id],[System.AreaPath],[System.Links.LinkType],[Microsoft.Services.Custom.TypeofRequirement] from WorkItems Where [System.TeamProject] = '{this.vstsProjectName}' AND [System.Title] CONTAINS '{title} - '" };
            var qResult = witClient.QueryByWiqlAsync(query).Result;
            var id = qResult.WorkItems.AsEmptyIfNull().FirstOrDefault()?.Id;
            return id != null
                ? witClient.GetWorkItemAsync(id.Value).Result
                : null;
        }

        private IList<WorkItemRelation> GetWorkItemRelations(int id)
        {
            var linkedRelations = witClient.GetWorkItemAsync(id, expand: WorkItemExpand.Relations).Result;

            if (linkedRelations != null)
                return linkedRelations.Relations;

            return null;
        }

        void AddField(JsonPatchDocument doc, string path, object value)
        {
            if (value == null) return;
            if (value is string && string.IsNullOrEmpty(value.ToString())) return;
            doc.Add(new JsonPatchOperation { Operation = Operation.Add, Path = $"/fields/{path}", Value = value });
        }

        void RemoveRelationship(JsonPatchDocument doc, int index)
        {
            doc.Add(new JsonPatchOperation()
            {
                Operation = Operation.Remove,
                Path = $"/relations/{index.ToString()}"
            });
        }

        void ChangeWorkItemType(JsonPatchDocument doc, string workItemType)
        {
            doc.Add(new JsonPatchOperation()
            {
                Operation = Operation.Add,
                Path = "/fields/System.WorkItemType",
                Value = workItemType
            });
        }

        void AddRelationship(JsonPatchDocument doc, string rel, WorkItem parent)
        {
            if (string.IsNullOrEmpty(rel)) return;
            if (parent == null) return;
            doc.Add(new JsonPatchOperation()
            {
                Operation = Operation.Add,
                Path = "/relations/-",
                Value = new { rel, url = parent.Url, attributes = new { comment = "Link supplied via Jira import" } }
            });
        }
    }
}
