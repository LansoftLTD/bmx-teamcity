using System;
using System.IO;
using System.Net;
using System.Xml.Linq;
using Inedo.BuildMaster;
using Inedo.BuildMaster.Artifacts;
using Inedo.BuildMaster.Data;
using Inedo.BuildMaster.Extensibility;
using Inedo.BuildMaster.Extensibility.Agents;
using Inedo.BuildMaster.Extensibility.BuildImporters;
using Inedo.BuildMaster.Files;
using Inedo.BuildMaster.Web;
using Inedo.Diagnostics;
using Inedo.IO;

namespace Inedo.BuildMasterExtensions.TeamCity
{
    [BuildImporterProperties(
        "TeamCity",
        "Imports artifacts from a build in TeamCity.",
        typeof(TeamCityBuildImporterTemplate))]
    [CustomEditor(typeof(TeamCityBuildImporterEditor))]
    public sealed class TeamCityBuildImporter : BuildImporterBase, ICustomBuildNumberProvider
    {
        [Persistent]
        public string ArtifactName { get; set; }
        [Persistent]
        public string BuildConfigurationId { get; set; }
        [Persistent]
        public string BuildConfigurationDisplayName { get; set; }
        [Persistent]
        public string BuildNumber { get; set; }
        [Persistent]
        public string BranchName { get; set; }

        string ICustomBuildNumberProvider.BuildNumber
        {
            get
            {
                return GetActualBuildNumber(this.BuildNumber);
            }
        }

        public new TeamCityConfigurer GetExtensionConfigurer()
        {
            return (TeamCityConfigurer)base.GetExtensionConfigurer();
        }

        public override void Import(IBuildImporterContext context)
        {
            string relativeUrl = string.Format("repository/download/{0}/{1}/{2}", this.BuildConfigurationId, this.BuildNumber, this.ArtifactName);
            var configurer = this.GetExtensionConfigurer();
            string branchName = this.GetBranchName(configurer);
            if (branchName != null)
            {
                this.LogDebug("Branch name was specified: " + branchName);
                relativeUrl += "?branch=" + Uri.EscapeDataString(this.BranchName);
            }

            this.LogDebug("Importing TeamCity artifact \"{0}\" from {1}...", this.ArtifactName, this.GetExtensionConfigurer().BaseUrl + relativeUrl);

            string tempFile;
            using (var client = new TeamCityWebClient(configurer))
            {
                tempFile = Path.GetTempFileName();
                this.LogDebug("Downloading temp file to \"{0}\"...", tempFile);
                try
                {
                    client.DownloadFile(relativeUrl, tempFile);
                }
                catch (WebException wex)
                {
                    var response = wex.Response as HttpWebResponse;
                    if (response != null && response.StatusCode == HttpStatusCode.NotFound)
                        this.LogWarning("The TeamCity request returned a 404 - this could mean that the branch name, build number, or build configuration is invalid.");

                    throw;
                }
            }

            this.LogInformation("Importing artifact into BuildMaster...");
            ArtifactBuilder.ImportZip(
                new ArtifactIdentifier(
                    context.ApplicationId,
                    context.ReleaseNumber,
                    context.BuildNumber,
                    context.DeployableId,
                    PathEx.GetFileName(this.ArtifactName)),
                Util.Agents.CreateLocalAgent().GetService<IFileOperationsExecuter>(),
                new FileEntryInfo(this.ArtifactName, tempFile)
            );

            string teamCityBuildNumber = this.GetActualBuildNumber(this.BuildNumber);
            this.LogDebug("TeamCity build number resolved to {0}, creating $TeamCityBuildNumber variable...", teamCityBuildNumber);

            StoredProcs.Variables_CreateOrUpdateVariableDefinition(
                "TeamCityBuildNumber",
                null,
                null,
                null,
                context.ApplicationId,
                null,
                context.ReleaseNumber,
                context.BuildNumber,
                null,
                teamCityBuildNumber,
                Domains.YN.No
              ).Execute();
        }

        private string GetActualBuildNumber(string buildNumber)
        {
            string apiUrl = this.TryGetPredefinedConstantBuildNumberApiUrl(buildNumber);
            if (apiUrl == null)
            {
                this.LogDebug("Using explicit build number: {0}", buildNumber);
                return buildNumber;
            }

            this.LogDebug("Build number is the predefined constant \"{0}\", resolving...", buildNumber);

            try
            {
                var configurer = this.GetExtensionConfigurer();
                string branch = this.GetBranchName(configurer);
                if (branch != null)
                    apiUrl += ",branch:" + Uri.EscapeDataString(branch);

                using (var client = new TeamCityWebClient(configurer))
                {
                    string xml = client.DownloadString(apiUrl);
                    var doc = XDocument.Parse(xml);
                    return doc.Element("build").Attribute("number").Value;
                }
            }
            catch (Exception ex)
            {
                this.LogError("Could not parse actual build number from TeamCity. Exception details: {0}", ex);
                return null;
            }
        }

        private string TryGetPredefinedConstantBuildNumberApiUrl(string buildNumber)
        {
            if (string.Equals(buildNumber, "lastSuccessful", StringComparison.OrdinalIgnoreCase))
                return string.Format("app/rest/builds/buildType:{0},running:false,status:success,count:1", Uri.EscapeDataString(this.BuildConfigurationId));

            if (string.Equals(buildNumber, "lastPinned", StringComparison.OrdinalIgnoreCase))
                return string.Format("app/rest/builds/buildType:{0},running:false,pinned:true,count:1", Uri.EscapeDataString(this.BuildConfigurationId));

            if (string.Equals(buildNumber, "lastFinished", StringComparison.OrdinalIgnoreCase))
                return string.Format("app/rest/builds/buildType:{0},running:false,count:1", Uri.EscapeDataString(this.BuildConfigurationId));

            return null;
        }

        private string GetBranchName(TeamCityConfigurer configurer)
        {
            if (!string.IsNullOrEmpty(this.BranchName))
                return this.BranchName;

            if (!string.IsNullOrEmpty(configurer.DefaultBranchName))
                return configurer.DefaultBranchName;

            return null;
        }
    }
}