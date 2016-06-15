using System.ComponentModel;
using System.Threading.Tasks;
using Inedo.BuildMaster;
using Inedo.BuildMaster.Data;
using Inedo.BuildMaster.Extensibility;
using Inedo.BuildMaster.Extensibility.Operations;
using Inedo.Diagnostics;
using Inedo.Documentation;

namespace Inedo.BuildMasterExtensions.TeamCity.Operations
{
    [DisplayName("Import Artifact from TeamCity")]
    [Description("Downloads an artifact from the specified TeamCity server and saves it to the artifact library.")]
    [ScriptAlias("Import-Artifact")]
    [Tag(Tags.Artifacts)]
    public sealed class ImportTeamCityArtifactOperation : TeamCityOperation
    {
        [Required]
        [ScriptAlias("Artifact")]
        [DisplayName("Artifact name")]
        public string ArtifactName { get; set; }
        [Required]
        [ScriptAlias("Project")]
        [DisplayName("Project name")]
        public string ProjectName { get; set; }
        [Required]
        [ScriptAlias("BuildConfiguration")]
        [DisplayName("Build configuration")]
        public string BuildConfigurationName { get; set; }
        [ScriptAlias("BuildNumber")]
        [DisplayName("Build number")]
        [DefaultValue("lastSuccessful")]
        [PlaceholderText("lastSuccessful")]
        [Description("The build number may be a specific build number, or a special value such as \"lastSuccessful\", \"lastFinished\", or \"lastPinned\".")]
        public string BuildNumber { get; set; }
        [ScriptAlias("Branch")]
        [DisplayName("Branch")]
        [PlaceholderText("Default")]
        public string BranchName { get; set; }        

        public async override Task ExecuteAsync(IOperationExecutionContext context)
        {
            var importer = new TeamCityArtifactImporter((ITeamCityConnectionInfo)this, (ILogger)this, context)
            {
                ArtifactName = this.ArtifactName,
                ProjectName = this.ProjectName,
                BuildConfigurationName = this.BuildConfigurationName,
                BranchName = this.BranchName,
                BuildNumber = this.BuildNumber
            };

            string teamCityBuildNumber = await importer.ImportAsync().ConfigureAwait(false);

            this.LogDebug("TeamCity build number resolved to {0}, creating $TeamCityBuildNumber variable...", teamCityBuildNumber);

            await new DB.Context(false).Variables_CreateOrUpdateVariableDefinitionAsync(
                "TeamCityBuildNumber",
                Application_Id: context.ApplicationId,
                Release_Number: context.ReleaseNumber,
                Build_Number: context.BuildNumber,
                Value_Text: teamCityBuildNumber,
                Sensitive_Indicator: false,
                Environment_Id: null,
                Server_Id: null,
                ApplicationGroup_Id: null,
                Execution_Id: null,
                Promotion_Id: null,
                Deployable_Id: null
            ).ConfigureAwait(false);
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
