using System;
using System.Net;

namespace Inedo.BuildMasterExtensions.TeamCity
{
    internal sealed class TeamCityWebClient : WebClient
    {
        public TeamCityWebClient(ITeamCityConnectionInfo connectionInfo)
        {
            this.BaseAddress = connectionInfo.GetApiUrl();

            if (!string.IsNullOrEmpty(connectionInfo.UserName))
            {
                // Using a CredentialCache because API URLs with TeamCity variables in them will issue redirects
                // to the actual URLs, and unlike the NetworkCredential class, CredentialCache will ensure that the
                // credentials will be sent to the redirected URL as well
                var credentials = new CredentialCache();
                credentials.Add(new Uri(connectionInfo.GetApiUrl()), "Basic", new NetworkCredential(connectionInfo.UserName, connectionInfo.Password));
                this.Credentials = credentials;
            }
        }

        protected override WebRequest GetWebRequest(Uri address)
        {
            var request = base.GetWebRequest(address);

            if (request.Method == "POST")
                request.ContentType = "application/xml";

            return request;
        }
    }
}
