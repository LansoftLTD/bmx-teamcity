using System.IO;
using Inedo.BuildMaster;
using Inedo.BuildMaster.Extensibility.Actions;
using Inedo.BuildMaster.Extensibility.Agents;
using Inedo.BuildMaster.Web;

namespace Inedo.BuildMasterExtensions.TeamCity
{
    /// <summary>
    /// Gets an artifact from a TeamCity server.
    /// </summary>
    [ActionProperties(
        "Get TeamCity Artifact",
        "Gets an artifact from a TeamCity server.",
        DefaultToLocalServer = true)]
    [CustomEditor(typeof(GetArtifactActionEditor))]
    [Tag(Tags.ContinuousIntegration)]
    public sealed class GetArtifactAction : TeamCityActionBase
    {
        /// <summary>
        /// Gets or sets the name of the artifact.
        /// </summary>
        [Persistent]
        public string ArtifactName { get; set; }

        /// <summary>
        /// Gets or sets the build configuration id.
        /// </summary>
        [Persistent]
        public string BuildConfigurationId { get; set; }

        /// <summary>
        /// Gets or sets the build number.
        /// </summary>
        [Persistent]
        public string BuildNumber { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [extract files to target directory].
        /// </summary>
        [Persistent]
        public bool ExtractFilesToTargetDirectory { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="GetArtifactAction"/> class.
        /// </summary>
        public GetArtifactAction()
        {
            this.ExtractFilesToTargetDirectory = true;
        }

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String"/> that represents this instance.
        /// </returns>
        /// <remarks>
        /// This should return a user-friendly string describing what the Action does
        /// and the state of its important persistent properties.
        /// </remarks>
        public override string ToString()
        {
            return string.Format(
                "Get the artifact \"{0}\" of Build #{1} of the configuration \"{2}\" from TeamCity and {3} to {4}.", 
                this.ArtifactName, 
                this.BuildNumber, 
                this.BuildConfigurationId,
                this.ExtractFilesToTargetDirectory ? "deploy its contents" : "copy the artifact",
                Util.CoalesceStr(this.OverriddenTargetDirectory, "the default directory")
            );
        }

        protected override void Execute()
        {
            string relativeUrl = string.Format("repository/download/{0}/{1}/{2}", this.BuildConfigurationId, this.BuildNumber, this.ArtifactName);

            LogDebug("Downloading TeamCity artifact \"{0}\" from {1} to {2}", this.ArtifactName, GetExtensionConfigurer().BaseUrl + relativeUrl, this.Context.TargetDirectory);

            var fileOps = this.Context.Agent.GetService<IFileOperationsExecuter>();
            var remoteZip = this.Context.Agent.GetService<IRemoteZip>();

            string tempFile;
            using (var client = new TeamCityWebClient(this.GetExtensionConfigurer()))
            {
                tempFile = Path.GetTempFileName();
                client.DownloadFile(relativeUrl, tempFile);
            }

            if (this.ExtractFilesToTargetDirectory)
            {
                LogDebug("Transferring artifact to {0} before extracting...", this.Context.TempDirectory);
                string remoteTempPath = fileOps.CombinePath(this.Context.TempDirectory, this.ArtifactName);
                fileOps.WriteFileBytes(
                    remoteTempPath,
                    File.ReadAllBytes(tempFile)
                );

                LogDebug("Extracting TeamCity artifact to {0}...", this.Context.TargetDirectory);
                remoteZip.ExtractZipFile(remoteTempPath, this.Context.TargetDirectory, true);
            }
            else
            {
                LogDebug("Transferring artifact to {0}...", this.Context.TargetDirectory);
                fileOps.WriteFileBytes(
                    fileOps.CombinePath(this.Context.TargetDirectory, this.ArtifactName),
                    File.ReadAllBytes(tempFile)
                );
            }

            LogInformation("Artifact retrieved successfully.");
        }
    }
}
