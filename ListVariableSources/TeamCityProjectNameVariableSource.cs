﻿using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using Inedo.BuildMaster.Extensibility.Credentials;
using Inedo.BuildMaster.Extensibility.ListVariableSources;
using Inedo.BuildMasterExtensions.TeamCity.Credentials;
using Inedo.Documentation;
using Inedo.Serialization;

namespace Inedo.BuildMasterExtensions.TeamCity.ListVariableSources
{
    [DisplayName("TeamCity Project Name")]
    [Description("Project names from a specified TeamCity instance.")]
    public sealed class TeamCityProjectNameVariableSource : ListVariableSource, IHasCredentials<TeamCityCredentials>
    {
        [Persistent]
        [DisplayName("Credentials")]
        [Required]
        public string CredentialName { get; set; }

        public override async Task<IEnumerable<string>> EnumerateValuesAsync(ValueEnumerationContext context)
        {
            var credentials = ResourceCredentials.Create<TeamCityCredentials>(this.CredentialName);

            using (var client = new TeamCityWebClient(credentials))
            {
                return await client.GetProjectNamesAsync().ConfigureAwait(false);
            }
        }

        public override RichDescription GetDescription() =>
            new RichDescription("TeamCity (", new Hilite(this.CredentialName), ") ", " project names.");
    }
}
