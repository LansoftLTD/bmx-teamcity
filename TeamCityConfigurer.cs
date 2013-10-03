using Inedo.BuildMaster;
using Inedo.BuildMaster.Extensibility.Configurers.Extension;
using Inedo.BuildMaster.Web;

[assembly: ExtensionConfigurer(typeof(Inedo.BuildMasterExtensions.TeamCity.TeamCityConfigurer))]

namespace Inedo.BuildMasterExtensions.TeamCity
{
    [CustomEditor(typeof(TeamCityConfigurerEditor))]
    public sealed class TeamCityConfigurer : ExtensionConfigurerBase
    {
        /// <summary>
        /// Gets or sets the server URL without the form of authentication included in the URL.
        /// </summary>
        [Persistent]
        public string ServerUrl { get; set; }

        /// <summary>
        /// Gets or sets the username.
        /// </summary>
        [Persistent]
        public string Username { get; set; }

        /// <summary>
        /// Gets or sets the password.
        /// </summary>
        [Persistent]
        public string Password { get; set; }

        /// <summary>
        /// Gets the base URL used for connections to the TeamCity server that incorporates the authentication mechanism.
        /// </summary>
        public string BaseUrl
        {
            get
            {
                return string.Format(
                    "{0}/{1}/", 
                    this.ServerUrl.TrimEnd('/'), 
                    string.IsNullOrEmpty(this.Username) ? "guestAuth" : "httpAuth"
                );
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TeamCityConfigurer"/> class.
        /// </summary>
        public TeamCityConfigurer()
        {
        }

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String"/> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return string.Empty;
        }
    }
}
