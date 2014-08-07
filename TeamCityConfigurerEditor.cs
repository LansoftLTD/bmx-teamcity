using Inedo.BuildMaster.Extensibility.Configurers.Extension;
using Inedo.BuildMaster.Web.Controls.Extensions;
using Inedo.Web.Controls;

namespace Inedo.BuildMasterExtensions.TeamCity
{
    internal sealed class TeamCityConfigurerEditor : ExtensionConfigurerEditorBase
    {
        private ValidatingTextBox txtServerUrl;
        private ValidatingTextBox txtUsername;
        private PasswordTextBox txtPassword;

        /// <summary>
        /// Initializes the default values.
        /// </summary>
        public override void InitializeDefaultValues()
        {
            BindToForm(new TeamCityConfigurer());
        }

        /// <summary>
        /// Binds to form.
        /// </summary>
        /// <param name="extension">The extension.</param>
        public override void BindToForm(ExtensionConfigurerBase extension)
        {
            var configurer = (TeamCityConfigurer)extension;

            this.txtServerUrl.Text = configurer.ServerUrl;
            if (!string.IsNullOrEmpty(configurer.Username))
            {
                this.txtUsername.Text = configurer.Username;
                this.txtPassword.Text = configurer.Password;
            }
        }

        /// <summary>
        /// Creates from form.
        /// </summary>
        /// <returns></returns>
        public override ExtensionConfigurerBase CreateFromForm()
        {
            var configurer = new TeamCityConfigurer()
            {
                ServerUrl = this.txtServerUrl.Text
            };
            if (!string.IsNullOrEmpty(this.txtUsername.Text))
            {
                configurer.Username = this.txtUsername.Text;
                configurer.Password = this.txtPassword.Text;
            }

            return configurer;
        }

        protected override void CreateChildControls()
        {
            this.txtServerUrl = new ValidatingTextBox() { Required = true };
            this.txtUsername = new ValidatingTextBox();
            this.txtPassword = new PasswordTextBox();

            this.Controls.Add(
                new SlimFormField("TeamCity server URL:", this.txtServerUrl)
                {
                    HelpText = "Enter the URL of the TeamCity server, typically: http://teamcityserver"
                },
                new SlimFormField("Username:", this.txtUsername)
                {
                    HelpText = "If you wish to connect to the TeamCity server with HTTP Authentication, please enter the credentials. Leaving the username field blank will connect using guest authentication."
                },
                new SlimFormField("Password:", this.txtPassword)
            );
        }
    }
}
