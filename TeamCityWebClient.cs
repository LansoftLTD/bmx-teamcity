using System;
using System.Net;

namespace Inedo.BuildMasterExtensions.TeamCity
{
    internal sealed class TeamCityWebClient : WebClient
    {
        public TeamCityWebClient(TeamCityConfigurer configurer)
        {
            this.BaseAddress = configurer.BaseUrl;

            if (!string.IsNullOrEmpty(configurer.Username))
            {
                // Using a CredentialCache because API URLs with TeamCity variables in them will issue redirects
                // to the actual URLs, and unlike the NetworkCredential class, CredentialCache will ensure that the
                // credentials will be sent to the redirected URL as well
                var credentials = new CredentialCache();
                credentials.Add(new Uri(configurer.BaseUrl), "Basic", new NetworkCredential(configurer.Username, configurer.Password));
                this.Credentials = credentials;
            }
        }
    }
}
