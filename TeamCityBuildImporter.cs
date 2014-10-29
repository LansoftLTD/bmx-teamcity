using System;
using System.IO;
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
            
            this.LogDebug("Importing TeamCity artifact \"{0}\" from {1}...", this.ArtifactName, this.GetExtensionConfigurer().BaseUrl + relativeUrl);

            string tempFile;
            using (var client = new TeamCityWebClient(this.GetExtensionConfigurer()))
            {
                tempFile = Path.GetTempFileName();
                client.DownloadFile(relativeUrl, tempFile);
            }

            ArtifactBuilder.ImportZip(
                new ArtifactIdentifier(
                    context.ApplicationId,
                    context.ReleaseNumber,
                    context.BuildNumber,
                    context.DeployableId,
                    this.ArtifactName),
                Util.Agents.CreateLocalAgent().GetService<IFileOperationsExecuter>(),
                new FileEntryInfo(this.ArtifactName + ".zip", tempFile)
            );

            string teamCityBuildNumber = this.GetActualBuildNumber(this.BuildNumber);

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
            if (InedoLib.Util.Int.ParseN(buildNumber) != null)
                return this.BuildNumber;

            string apiUrl;
            switch (buildNumber)
            {
                case "lastPinned":
                    apiUrl = string.Format("app/rest/builds/buildType:{0},running:false,pinned:true,count:1", Uri.EscapeDataString(this.BuildConfigurationId));
                    break;
                case "lastFinished":
                    apiUrl = string.Format("app/rest/builds/buildType:{0},running:false,count:1", Uri.EscapeDataString(this.BuildConfigurationId));
                    break;
                default: // lastSuccessful
                    apiUrl = string.Format("app/rest/builds/buildType:{0},running:false,status:success,count:1", Uri.EscapeDataString(this.BuildConfigurationId));
                    break;
            }
            try
            {
                using (var client = new TeamCityWebClient(this.GetExtensionConfigurer()))
                {
                    string xml = client.DownloadString(apiUrl);
                    var doc = XDocument.Parse(xml);
                    return doc.Element("build").Attribute("number").Value;
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Could not parse actual build number from Team City. Exception details: {0}", ex);
                return null;
            }
        }
    }
}