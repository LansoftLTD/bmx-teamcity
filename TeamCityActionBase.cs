using System.Net;
using Inedo.BuildMaster;
using Inedo.BuildMaster.Extensibility.Actions;

namespace Inedo.BuildMasterExtensions.TeamCity
{
    public abstract class TeamCityActionBase : AgentBasedActionBase
    {
        protected new TeamCityConfigurer GetExtensionConfigurer()
        {
            return (TeamCityConfigurer)base.GetExtensionConfigurer();
        }

        public sealed override bool IsConfigurerSettingRequired()
        {
            return true;
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
