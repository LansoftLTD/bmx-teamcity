using Inedo.BuildMaster;
using Inedo.BuildMaster.Extensibility;
using Inedo.BuildMaster.Extensibility.BuildImporters;
using Inedo.BuildMaster.Web;

namespace Inedo.BuildMasterExtensions.TeamCity
{
    [CustomEditor(typeof(TeamCityBuildImporterTemplateEditor))]
    internal sealed class TeamCityBuildImporterTemplate : BuildImporterTemplateBase<TeamCityBuildImporter>
    {
        [Persistent]
        public string ArtifactName { get; set; }
        [Persistent]
        public string BuildConfigurationId { get; set; }
        [Persistent]
        public string BuildConfigurationDisplayName { get; set; }
        [Persistent]
        public bool ArtifactNameLocked { get; set; }
        [Persistent]
        public string BuildNumber { get; set; }

        public override ExtensionComponentDescription GetDescription()
        {
            var desc = new ExtensionComponentDescription("Import the ");
            desc.AppendContent(new Hilite(this.ArtifactName));
            desc.AppendContent(" artifact from TeamCity build configuration ");
            desc.AppendContent(new Hilite(this.BuildConfigurationDisplayName));
            if (!string.IsNullOrEmpty(this.BuildNumber))
            {
                desc.AppendContent(" using the special build type: ");
                desc.AppendContent(new Hilite(this.BuildNumber));
            }
            else
            {
                desc.AppendContent(" requiring a special build type or specific build number to be selected at build import time.");
            }
            return desc;
        }
    }
}
