using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Inedo.BuildMaster.Extensibility.Recipes;
using Inedo.BuildMaster;
using Inedo.BuildMaster.Data;
using Inedo.BuildMaster.Web.Controls.Extensions;
using System.Web.UI;
using Inedo.Web.Controls;
using Inedo.BuildMaster.Web.Controls;
using System.Web.UI.WebControls;
using Inedo.Web.Controls.SimpleHtml;
using System.Net;

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
            public RecipeWizardStep TeamCityConnection = new RecipeWizardStep("TeamCity");
            public RecipeWizardStep TeamCityBuild = new RecipeWizardStep("Build"); 
            public RecipeWizardStep SelectDeploymentPath = new RecipeWizardStep("Deployment");
            

            public override RecipeWizardStep[] WizardStepOrder
            {
                get
                {
                    return new[] { this.About, this.TeamCityConnection, this.TeamCityBuild, base.SpecifyApplicationProperties, base.SpecifyWorkflowOrder, this.SelectDeploymentPath };
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

        }
        private void CreateAboutControls()
        {
            this.wizardSteps.About.Controls.Add(
                new H2("About the Deploy TeamCity Build Wizard"),
                new P(
                    "This wizard will create a basic application for deploying builds from a TeamCity server. Like may wizards, this is meant as a starting point.",
                    "After the wizard completes, you can change the servers, deployment targets, or any other aspects of the application by editing the Deployment Plan."
                ), 
                new P(
                    "To learn more about BuildMaster integration, see the ",
                    new A("TeamCity Extension"){ Href = "http://inedo.com/buildmaster/extensions/teamcity", Target="_blank" },
                    " for more details"
                )
            );
        }
        private void CreateTeamCityConnectionControls()
        {
            var configurer = TeamCityConfigurer.GetConfigurer(null);

            var txtServerUrl = new ValidatingTextBox
            {
                Required = true,
                Text = configurer == null ? null : configurer.ServerUrl,
                Width = 350
            };
            
            var txtUsername = new ValidatingTextBox
            {
                Text = configurer == null ? null : configurer.Username,
                Width = 350
            };
            var txtPassword = new PasswordTextBox
            {
                Text = configurer == null ? null : configurer.Password,
                Width = 350
            };

            txtServerUrl.ServerValidate += (s, e) =>
                {
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
                        txtServerUrl.ValidatorText = _e.Message;
                    }
                };

            this.wizardSteps.TeamCityConnection.Controls.Add(
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
                //this.BuildConfigurationId = ctlSelectBuildConfigurationPicker.SelectedValue;
                //this.ArtifactName = txtArtifactName.Text;
            };
        }
        private void CreateSelectArtifactControls()
        {
            var ctlSelectBuildConfigurationPicker = new SelectBuildConfigurationPicker
            {
                Style = "width:350px"
            };
            ctlSelectBuildConfigurationPicker.Init += (s, e) => ctlSelectBuildConfigurationPicker.FillItems(null);

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
                this.BuildConfigurationName = ctlSelectBuildConfigurationPicker.Items.Cast<ListItem>()
                    .Where(li => li.Selected)
                    .Select(li => li.Text)
                    .FirstOrDefault();
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

        public override RecipeBase CreateFromForm()
        {
            return new DeployTeamCityBuildRecipe
            {
                TargetDeploymentPath = this.TargetDeploymentPath,
                BuildConfigurationId = this.BuildConfigurationId,
                ArtifactName = this.ArtifactName
            };
        }
    }
}
