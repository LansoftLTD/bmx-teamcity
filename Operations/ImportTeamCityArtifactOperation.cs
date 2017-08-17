﻿using System.ComponentModel;
using System.Threading.Tasks;
using Inedo.BuildMaster;
using Inedo.BuildMaster.Data;
using Inedo.BuildMaster.Extensibility;
using Inedo.BuildMaster.Extensibility.Operations;
using Inedo.BuildMaster.Web.Controls;
using Inedo.Diagnostics;
using Inedo.Documentation;

namespace Inedo.BuildMasterExtensions.TeamCity.Operations
{
    [DisplayName("Import Artifact from TeamCity")]
    [Description("Downloads an artifact from the specified TeamCity server and saves it to the artifact library.")]
    [ScriptAlias("Import-Artifact")]
    [Tag(Tags.Artifacts)]
    [Tag("teamcity")]
    public sealed class ImportTeamCityArtifactOperation : TeamCityOperation
    {
        [ScriptAlias("Credentials")]
        [DisplayName("Credentials")]
        public override string CredentialName { get; set; }

        [ScriptAlias("Project")]
        [DisplayName("Project name")]
        [SuggestibleValue(typeof(ProjectNameSuggestionProvider))]
        public string ProjectName { get; set; }
        [ScriptAlias("BuildConfiguration")]
        [DisplayName("Build configuration")]
        [SuggestibleValue(typeof(BuildConfigurationNameSuggestionProvider))]
        public string BuildConfigurationName { get; set; }

        [ScriptAlias("BuildConfigurationId")]
        [DisplayName("Build configuration ID")]
        [Description("TeamCity identifier that targets a single build configuration. May be specified instead of the Project name and Build configuration name.")]
        public string BuildConfigurationId { get; set; }

        [ScriptAlias("BuildNumber")]
        [DisplayName("Build number")]
        [DefaultValue("lastSuccessful")]
        [PlaceholderText("lastSuccessful")]
        [Description("The build number may be a specific build number, or a special value such as \"lastSuccessful\", \"lastFinished\", or \"lastPinned\".")]
        [SuggestibleValue(typeof(BuildNumberSuggestionProvider))]
        public string BuildNumber { get; set; }
        [Required]
        [ScriptAlias("Artifact")]
        [DisplayName("Artifact name")]
        public string ArtifactName { get; set; }
        [ScriptAlias("Branch")]
        [DisplayName("Branch")]
        [PlaceholderText("Default")]
        public string BranchName { get; set; }
        [Output]
        [ScriptAlias("TeamCityBuildNumber")]
        [DisplayName("Set build number to variable")]
        [Description("The TeamCity build number can be output into a runtime variable")]
        [PlaceholderText("e.g. $TeamCityBuildNumber")]
        public string TeamCityBuildNumber { get; set; }

        public async override Task ExecuteAsync(IOperationExecutionContext context)
        {
            var importer = new TeamCityArtifactImporter((ITeamCityConnectionInfo)this, (ILogger)this, context)
            {
                ArtifactName = this.ArtifactName,
                ProjectName = this.ProjectName,
                BuildConfigurationId = this.BuildConfigurationId,
                BuildConfigurationName = this.BuildConfigurationName,
                BranchName = this.BranchName,
                BuildNumber = this.BuildNumber
            };

            this.TeamCityBuildNumber = await importer.ImportAsync().ConfigureAwait(false);

            this.LogInformation($"TeamCity build number \"{this.TeamCityBuildNumber}\" imported.");
        }

        protected override ExtendedRichDescription GetDescription(IOperationConfiguration config)
        {
            string buildNumber = config[nameof(this.BuildNumber)];
            string branchName = config[nameof(this.BranchName)];

            return new ExtendedRichDescription(
                new RichDescription("Import TeamCity ", new Hilite(config[nameof(this.ArtifactName)]), " Artifact "),
                new RichDescription("of build ",
                    AH.ParseInt(buildNumber) != null ? "#" : "",
                    new Hilite(buildNumber),
                    !string.IsNullOrEmpty(branchName) ? " on branch " + branchName : "",
                    " of project ", 
                    new Hilite(config[nameof(this.ProjectName)]),
                    " using configuration \"",
                    config[nameof(this.BuildConfigurationName)]
                )
            );
        }
    }
}
