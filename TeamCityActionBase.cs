using System;
using System.Net;
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

            var client = new WebClient() { BaseAddress = configurer.BaseUrl };
            
            if (!string.IsNullOrEmpty(configurer.Username))
            {
                // Using a CredentialCache because API URLs with TeamCity variables in them will issue redirects
                // to the actual URLs, and unlike the NetworkCredential class, CredentialCache will ensure that the
                // credentials will be sent to the redirected URL as well
                var credentials = new CredentialCache();
                credentials.Add(new Uri(configurer.BaseUrl), "Basic", new NetworkCredential(configurer.Username, configurer.Password));
                client.Credentials = credentials;
            }            

            return client;
        }
    }
}