using System.Web.UI.WebControls;
using Inedo.BuildMaster.Extensibility.Actions;
using Inedo.BuildMaster.Web.Controls;
using Inedo.BuildMaster.Web.Controls.Extensions;
using Inedo.Web.Controls;

namespace Inedo.BuildMasterExtensions.TeamCity
{
    internal sealed class GetArtifactActionEditor : ActionEditorBase
    {
        private ValidatingTextBox txtArtifactName;
        private ValidatingTextBox txtBuildConfigurationId;
        private ValidatingTextBox txtBuildNumber;
        private CheckBox chkExtractFilesToTargetDirectory;

        /// <summary>
        /// Gets a value indicating whether [display target directory].
        /// </summary>
        /// <value>
        /// 	<c>true</c> if [display target directory]; otherwise, <c>false</c>.
        /// </value>
        public override bool DisplayTargetDirectory { get { return true; } }

        /// <summary>
        /// Binds to form.
        /// </summary>
        /// <param name="extension">The extension.</param>
        public override void BindToForm(ActionBase extension)
        {
            var action = (GetArtifactAction)extension;

            this.txtArtifactName.Text = action.ArtifactName;
            this.txtBuildConfigurationId.Text = action.BuildConfigurationId;
            this.txtBuildNumber.Text = action.BuildNumber;
            this.chkExtractFilesToTargetDirectory.Checked = action.ExtractFilesToTargetDirectory;
        }

        /// <summary>
        /// Creates from form.
        /// </summary>
        /// <returns></returns>
        public override ActionBase CreateFromForm()
        {
            return new GetArtifactAction()
            {
                ArtifactName = this.txtArtifactName.Text,
                BuildConfigurationId = this.txtBuildConfigurationId.Text,
                BuildNumber = this.txtBuildNumber.Text,
                ExtractFilesToTargetDirectory = this.chkExtractFilesToTargetDirectory.Checked
            };
        }

        protected override void CreateChildControls()
        {
            this.txtArtifactName = new ValidatingTextBox()
            {
                Required = true,
                Width = 300
            };

            this.txtBuildConfigurationId = new ValidatingTextBox()
            {
                Required = true
            };

            this.txtBuildNumber = new ValidatingTextBox()
            {
                Required = true
            };

            this.chkExtractFilesToTargetDirectory = new CheckBox()
            {
                Text = "Extract files in artifact to target directory"
            };

            CUtil.Add(this,
                new FormFieldGroup(
                    "Artifact Name",
                    "The name of artifact, for example: <br />\"ideaIC-118.SNAPSHOT.win.zip\". This value can also take a form of \"artifactName!archivePath\" for reading archive's content",
                    false,
                    new StandardFormField("Artifact Name:", this.txtArtifactName)
                ),
                new FormFieldGroup(
                    "Build Configuration ID",
                    "This value can be found in a browser address bar when corresponding configuration is browsed within TeamCity. <br /><br />As an example, teamcity.jetbrains.com/viewLog.html?buildId=64797&buildTypeId=<strong>bt343</strong>&tab=...",
                    false,
                    new StandardFormField("Build Configuration ID:", this.txtBuildConfigurationId)
                ),
                new FormFieldGroup(
                    "Build Number",
                    "The build number or one of predefined constants: \"lastSuccessful\", \"lastPinned\", or \"lastFinished\".",
                    false,
                    new StandardFormField("Build Number:", this.txtBuildNumber)
                ),
                new FormFieldGroup(
                    "Additional Options",
                    "Select any addition options for this action.",
                    true,
                    new StandardFormField("", this.chkExtractFilesToTargetDirectory)
                )
            );
        }
    }
}
