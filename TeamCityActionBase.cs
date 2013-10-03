using System.Net;
using Inedo.BuildMaster;
using Inedo.BuildMaster.Extensibility.Actions;

namespace Inedo.BuildMasterExtensions.TeamCity
{
    public abstract class TeamCityActionBase : RemoteActionBase
    {
        /// <summary>
        /// Returns a value indicating whether the extension's configurer currently needs to be
        /// configured.
        /// </summary>
        /// <returns>
        /// True if configurer requires configuration; otherwise false.
        /// </returns>
        /// <remarks>
        /// Unless overridden by an action, this method always returns false.
        /// </remarks>
        public override bool IsConfigurerSettingRequired()
        {
            var configurer = Util.Actions.GetConfigurer(GetType()) as TeamCityConfigurer;

            if (configurer != null)
                return string.IsNullOrEmpty(configurer.ServerUrl);
            else
                return true;
        }

        protected new TeamCityConfigurer GetExtensionConfigurer()
        {
            return (TeamCityConfigurer)base.GetExtensionConfigurer();
        }

        protected WebClient CreateClient()
        {
            var configurer = GetExtensionConfigurer();

            var client = new WebClient()
            {
                BaseAddress = configurer.BaseUrl
            };
            if (!string.IsNullOrEmpty(configurer.Username))
                client.Credentials = new NetworkCredential(configurer.Username, configurer.Password);

            return client;
        }
    }
}
