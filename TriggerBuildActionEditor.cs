using System.Web;
using System.Web.UI.WebControls;
using Inedo.BuildMaster.Extensibility.Actions;
using Inedo.BuildMaster.Web.Controls;
using Inedo.BuildMaster.Web.Controls.Extensions;
using Inedo.Web.Controls;

namespace Inedo.BuildMasterExtensions.TeamCity
{
    internal sealed class TriggerBuildActionEditor : ActionEditorBase
    {
        private ValidatingTextBox txtBuildConfigurationId;
        private ValidatingTextBox txtAdditionalParameters;
        private CheckBox chkWaitForCompletion;

        /// <summary>
        /// Binds to form.
        /// </summary>
        /// <param name="extension">The extension.</param>
        public override void BindToForm(ActionBase extension)
        {
            var action = (TriggerBuildAction)extension;

            this.txtBuildConfigurationId.Text = action.BuildConfigurationId;
            this.txtAdditionalParameters.Text = action.AdditionalParameters;
            this.chkWaitForCompletion.Checked = action.WaitForCompletion;
        }

        /// <summary>
        /// Creates from form.
        /// </summary>
        /// <returns></returns>
        public override ActionBase CreateFromForm()
        {
            return new TriggerBuildAction()
            {
                BuildConfigurationId = this.txtBuildConfigurationId.Text,
                AdditionalParameters = this.txtAdditionalParameters.Text,
                WaitForCompletion = this.chkWaitForCompletion.Checked
            };
        }

        protected override void CreateChildControls()
        {
            this.txtBuildConfigurationId = new ValidatingTextBox()
            {
                Required = true
            };

            this.txtAdditionalParameters = new ValidatingTextBox()
            {
                Required = false,
                Width = 300
            };

            this.chkWaitForCompletion = new CheckBox()
            {
                Text = "Wait for build to complete",
                Checked = true
            };

            CUtil.Add(this, 
                new FormFieldGroup(
                    "Build Configuration ID",
                    "This value can be found in a browser address bar when corresponding configuration is browsed within TeamCity. <br /><br />As an example, teamcity.jetbrains.com/viewLog.html?buildId=64797&buildTypeId=<strong>bt343</strong>&tab=...", 
                    false, 
                    new StandardFormField("Build Configuration ID:", this.txtBuildConfigurationId)
                ),
                new FormFieldGroup(
                    "Additional Parameters",
                    "Optionally enter any additional parameters accepted by the TeamCity API in query string format, for example:<br/> " + HttpUtility.HtmlEncode("&agent=<agent Id>&system.name=<property name1>&system.value=<value1>"),
                    false,
                    new StandardFormField("Additional Parameters:", this.txtAdditionalParameters)
                ),
                new FormFieldGroup(
                    "Wait for Completion",
                    "Specify whether BuildMaster should pause the action until the TeamCity build has completed.",
                    true,
                    new StandardFormField("", this.chkWaitForCompletion)
                )
            );
        }
    }
}
