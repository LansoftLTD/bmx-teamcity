using Inedo.BuildMaster;
using Inedo.BuildMaster.Data;
using Inedo.BuildMaster.Extensibility.Recipes;
using Inedo.BuildMaster.Web;

namespace Inedo.BuildMasterExtensions.TeamCity
{
    [RecipeProperties(
        "Deploy TeamCity Build",
        "An application that captures a build artifact from TeamCity and deploys through multiple environments",
        RecipeScopes.NewApplication)]
    [CustomEditor(typeof(DeployTeamCityBuildRecipeEditor))]
    public sealed class DeployTeamCityBuildRecipe : RecipeBase, IApplicationCreatingRecipe, IWorkflowCreatingRecipe
    {
        public string ApplicationGroup { get; set; }
        public string ApplicationName { get; set; }
        public int ApplicationId { get; set; }

        public string WorkflowName { get; set; }
        public int[] WorkflowSteps { get; set; }
        public int WorkflowId { get; set; }

        public string BuildConfigurationId { get; set; }
        public string ArtifactName { get; set; }
        public string TargetDeploymentPath { get; set; }
        public string BuildConfigurationName { get; set; }

        public override void Execute()
        {
            int deployableId = Util.Recipes.CreateDeployable(this.ApplicationId, this.ApplicationName);
            string deployableName = this.ApplicationName;
            int firstEnvironmentId = this.WorkflowSteps[0];

            var template = new TeamCityBuildImporterTemplate() 
            {
                ArtifactName = this.ArtifactName,
                ArtifactNameLocked = false,
                BuildConfigurationId = this.BuildConfigurationId,
                BuildNumber = "lastSuccessful",
                BuildConfigurationDisplayName = this.BuildConfigurationName
            };

            Util.Recipes.CreateBuildStepBuildImporter(this.WorkflowId, template);

            int firstDeploymentPlanId = Util.Recipes.CreateDeploymentPlanForWorkflowStep(this.WorkflowId, 1);

            int actionGroupId = Util.Recipes.CreateDeploymentPlanActionGroup(
                firstDeploymentPlanId,
                deployableId: null,
                deployableName: null,
                name: "Stop Application",
                description: "Stop/shutdown/disable the application or application servers prior to deployment."
            );

            actionGroupId = Util.Recipes.CreateDeploymentPlanActionGroup(
                firstDeploymentPlanId,
                deployableId: deployableId,
                deployableName: null,
                name: "Deploy " + deployableName,
                description: "Deploy the artifacts created in the build actions, and then any configuration files needed."
            );

            Util.Recipes.AddAction(actionGroupId, 1, Util.Recipes.Munging.MungeCoreExAction(
                "Inedo.BuildMaster.Extensibility.Actions.Artifacts.DeployArtifactAction", new
                {
                    ArtifactName = this.ArtifactName,
                    OverriddenTargetDirectory = this.TargetDeploymentPath,
                    DoNotClearTargetDirectory = false
                })
            );

            Util.Recipes.CreateDeploymentPlanActionGroup(
                firstDeploymentPlanId,
                deployableId: null,
                deployableName: null,
                name: "Start Application",
                description: "Start the application or application servers after deployment, and possibly run some post-startup automated testing."
            );


            Util.Recipes.CreateSetupRelease(this.ApplicationId, Domains.ReleaseNumberSchemes.MajorMinor, this.WorkflowId, new[] { deployableId });
        }
    }
}
