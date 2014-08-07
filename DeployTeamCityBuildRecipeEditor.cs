using System;
using System.Linq;
using System.Net;
using Inedo.BuildMaster;
using Inedo.BuildMaster.Data;
using Inedo.BuildMaster.Extensibility.Recipes;
using Inedo.BuildMaster.Web.Controls;
using Inedo.BuildMaster.Web.Controls.Extensions;
using Inedo.Web.Controls;
using Inedo.Web.Controls.SimpleHtml;

namespace Inedo.BuildMasterExtensions.TeamCity
{
    [RecipeProperties(
        "Deploy TeamCity Build",
        "An application that captures a build artifact from TeamCity and deploys through multiple environments",
        RecipeScopes.NewApplication)]
    internal sealed class DeployTeamCityBuildRecipeEditor : RecipeEditorBase
    {
        private sealed class DeployTeamCityBuildRecipeEditordSteps : RecipeWizardSteps
        {
            public RecipeWizardStep About = new RecipeWizardStep("About");
            public RecipeWizardStep TeamCityConnection = new RecipeWizardStep("TeamCity Server");
            public RecipeWizardStep TeamCityBuild = new RecipeWizardStep("Build Import"); 
            public RecipeWizardStep SelectDeploymentPath = new RecipeWizardStep("Deployment");
            public RecipeWizardStep Summary = new RecipeWizardStep("Summary");
            

            public override RecipeWizardStep[] WizardStepOrder
            {
                get
                {
                    return new[] { this.About, this.TeamCityConnection, this.TeamCityBuild, base.SpecifyApplicationProperties, base.SpecifyWorkflowOrder, this.SelectDeploymentPath, this.Summary };
                }
            }
        }

        private DeployTeamCityBuildRecipeEditordSteps wizardSteps = new DeployTeamCityBuildRecipeEditordSteps();

        private int TargetServerId
        {
            get { return (int)(this.ViewState["TargetServerId"] ?? 0); }
            set { this.ViewState["TargetServerId"] = value; }
        }

        private string TargetDeploymentPath
        {
            get { return (string)this.ViewState["TargetDeploymentPath"]; }
            set { this.ViewState["TargetDeploymentPath"] = value; }
        }

        private string BuildConfigurationId
        {
            get { return (string)this.ViewState["BuildConfigurationId"]; }
            set { this.ViewState["BuildConfigurationId"] = value; }
        }
        private string BuildConfigurationName
        {
            get { return (string)this.ViewState["BuildConfigurationName"]; }
            set { this.ViewState["BuildConfigurationName"] = value; }
        }

        private string ArtifactName
        {
            get { return (string)this.ViewState["ArtifactName"]; }
            set { this.ViewState["ArtifactName"] = value; }
        }

        public override bool DisplayAsWizard { get { return true; } }

        public override RecipeWizardSteps GetWizardStepsControl()
        {
            return this.wizardSteps;
        }

        public override string DefaultNewApplicationName
        {
            get
            {
                return InedoLib.Util.NullIf(this.BuildConfigurationName, string.Empty);
            }
        }

        protected override void CreateChildControls()
        {
            base.CreateChildControls();

            this.CreateAboutControls();
            this.CreateTeamCityConnectionControls();
            this.CreateSelectArtifactControls();
            this.CreateSelectDeploymentPathControls();
            this.CreateSummaryControls();
        }
        private void CreateAboutControls()
        {
            this.wizardSteps.About.Controls.Add(
                new H2("About the Deploy TeamCity Build Wizard"),
                new P(
                    "This wizard will create a basic application for importing and deploying builds from a TeamCity server. Like many wizards, this is meant as a starting point. ",
                    "After the wizard completes, you can change the servers, deployment targets, or any other aspects of the application by editing its workflows or deployment plans."
                ), 
                new P(
                    "To learn more about BuildMaster integration, see the ",
                    new A("TeamCity Extension"){ Href = "http://inedo.com/buildmaster/extensions/teamcity", Target="_blank" },
                    " for more details."
                )
            );
        }
        private void CreateTeamCityConnectionControls()
        {
            var defaultCfg = TeamCityConfigurer.GetConfigurer(null) ?? new TeamCityConfigurer();
            var ctlError = new InfoBox { BoxType = InfoBox.InfoBoxTypes.Error, Visible = false };

            var txtServerUrl = new ValidatingTextBox
            {
                Required = true,
                Text = defaultCfg.ServerUrl,
                Width = 350
            };
            
            var txtUsername = new ValidatingTextBox
            {
                Text = defaultCfg.Username,
                Width = 350
            };
            var txtPassword = new PasswordTextBox
            {
                Text = defaultCfg.Password,
                Width = 350
            };

            txtServerUrl.ServerValidate += (s, e) =>
                {
                    var configurer = new TeamCityConfigurer
                    {
                        ServerUrl = txtServerUrl.Text,
                        Username = txtUsername.Text,
                        Password = txtPassword.Text
                    };
                    try
                    {
                        using (var client = new WebClient())
                        {
                            client.BaseAddress = configurer.BaseUrl;
                            if (!string.IsNullOrEmpty(configurer.Username))
                                client.Credentials = new NetworkCredential(configurer.Username, configurer.Password);

                            client.DownloadString("app/rest/buildTypes");
                        }
                    }
                    catch (Exception _e)
                    {
                        e.IsValid = false;
                        ctlError.Visible = true;
                        ctlError.Controls.Add(new P("An error occurred while attempting to connect: " + _e.Message));
                    }
                };

            this.wizardSteps.TeamCityConnection.Controls.Add(
                ctlError,
                new FormFieldGroup(
                    "TeamCity Server URL",
                    "Enter the URL of the TeamCity server, typically: http://teamcityserver",
                    false,
                    new StandardFormField("Server URL:", txtServerUrl)
                ),
                new FormFieldGroup(
                    "Authentication",
                    "If you wish to connect to the TeamCity server with HTTP Authentication, please enter the credentials. Leaving the username field blank will connect using guest authentication.",
                    true,
                    new StandardFormField("Username:", txtUsername),
                    new StandardFormField("Password:", txtPassword)
                )
            );
            this.WizardStepChange += (s, e) =>
            {
                if (e.CurrentStep != this.wizardSteps.TeamCityConnection) return;

                defaultCfg.ServerUrl = txtServerUrl.Text;
                defaultCfg.Username = txtUsername.Text;
                defaultCfg.Password = txtPassword.Text;
                var defaultProfile = StoredProcs
                        .ExtensionConfiguration_GetConfigurations(TeamCityConfigurer.ConfigurerName)
                        .Execute()
                        .Where(p => p.Default_Indicator == Domains.YN.Yes)
                        .FirstOrDefault() ?? new Tables.ExtensionConfigurations();
                
                StoredProcs
                    .ExtensionConfiguration_SaveConfiguration(
                        Util.NullIf(defaultProfile.ExtensionConfiguration_Id, 0),
                        TeamCityConfigurer.ConfigurerName,
                        defaultProfile.Profile_Name ?? "Default",
                        Util.Persistence.SerializeToPersistedObjectXml(defaultCfg),
                        Domains.YN.Yes)
                    .Execute();
            };
        }
        private void CreateSelectArtifactControls()
        {
            var ctlSelectBuildConfigurationPicker = new SelectBuildConfigurationPicker { Width = 350 };
            ctlSelectBuildConfigurationPicker.PreRender += (s, e) => ctlSelectBuildConfigurationPicker.FillItems(TeamCityConfigurer.GetConfigurer());

            var txtArtifactName = new ValidatingTextBox
            {
                Required = true,
                Width = 350
            };

            this.wizardSteps.TeamCityBuild.Controls.Add(
                new FormFieldGroup(
                    "Build Configuration",
                    "This is the build configuration where an artifact will be retrieved from. The last successful build will be used for the wizard, but you can change this later.",
                    false,
                    new StandardFormField("Build Configuration:", ctlSelectBuildConfigurationPicker)
                ),
                new FormFieldGroup(
                    "Artifact Name",
                    "The name of artifact, for example: <br />\"ideaIC-118.SNAPSHOT.win.zip\". The wizard assumes this artifact is a zip file, but you can change this later.",
                    false,
                    new StandardFormField("Artifact Name:", txtArtifactName)
                )
            );
            this.WizardStepChange += (s, e) =>
            {
                if (e.CurrentStep != this.wizardSteps.TeamCityBuild) return;
                this.BuildConfigurationId = ctlSelectBuildConfigurationPicker.SelectedValue;
                this.BuildConfigurationName = ctlSelectBuildConfigurationPicker.SelectedItem.Text;
                this.ArtifactName = txtArtifactName.Text;
            };
        }
        private void CreateSelectDeploymentPathControls()
        {
            var ctlTargetDeploymentPath = new SourceControlFileFolderPicker()
            {
                DisplayMode = SourceControlBrowser.DisplayModes.Folders,
                ServerId = 1
            };

            this.wizardSteps.SelectDeploymentPath.Controls.Add(
                new FormFieldGroup(
                    "Deployment Target",
                    "Select a directory where the artifact will be deployed. You can change the server/path in which this gets deployed to later.",
                    true,
                    new StandardFormField("Target Directory:", ctlTargetDeploymentPath)
                )
            );
            this.WizardStepChange += (s, e) =>
            {
                if (e.CurrentStep != this.wizardSteps.SelectDeploymentPath)
                    return;
                this.TargetDeploymentPath = ctlTargetDeploymentPath.Text;
            };
        }

        private void CreateSummaryControls()
        {
            this.wizardSteps.Summary.Controls.Add(
                new H2("Deploy TeamCity Artifact Example Wizard Summary"),
                new P(
                    "This wizard will create a basic application for importing and deploying builds from a TeamCity server. Like many wizards, this is meant as a starting point. ",
                    "After the wizard completes, you can change the servers, deployment targets, or any other aspects of the application by editing its workflows or deployment plans."
                ),
                new P("This details of this recipe include:"),
                new Ul(
                    new Li(new B("Build configuration name: "), GetRenderDelegator(() => this.BuildConfigurationName), " (id=", GetRenderDelegator(() => this.BuildConfigurationId), ")"),
                    new Li(new B("Target deployment path: "), GetRenderDelegator(() => this.TargetDeploymentPath)),
                    new Li(new B("Artifact name: "), GetRenderDelegator(() => this.ArtifactName))
                )                
            );
        }

        private RenderDelegator GetRenderDelegator(Func<string> getValue)
        {
            return new RenderDelegator(w => w.Write(getValue() ?? ""));
        }

        public override RecipeBase CreateFromForm()
        {
            return new DeployTeamCityBuildRecipe
            {
                TargetDeploymentPath = this.TargetDeploymentPath,
                BuildConfigurationId = this.BuildConfigurationId,
                BuildConfigurationName = this.BuildConfigurationName,
                ArtifactName = this.ArtifactName
            };
        }
    }
}
