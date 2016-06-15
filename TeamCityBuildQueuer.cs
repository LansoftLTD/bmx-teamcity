using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using Inedo.BuildMaster.Extensibility;
using Inedo.BuildMaster.Extensibility.Operations;
using Inedo.Diagnostics;
using Inedo.ExecutionEngine.Executer;

namespace Inedo.BuildMasterExtensions.TeamCity
{
    internal sealed class TeamCityBuildQueuer
    {
        private int progressPercent;
        private string progressMessage;

        public string BuildConfigurationId { get; set; }
        public string ProjectName { get; set; }
        public string BuildConfigurationName { get; set; }
        public string BranchName { get; set; }
        public bool WaitForCompletion { get; set; }
        public string AdditionalParameters { get; set; }

        public ITeamCityConnectionInfo ConnectionInfo { get; }
        public ILogger Logger { get; }
        public IGenericBuildMasterContext Context { get; }

        public TeamCityBuildQueuer(ITeamCityConnectionInfo connectionInfo, ILogger logger, IGenericBuildMasterContext context)
        {
            if (connectionInfo == null)
                throw new ArgumentNullException(nameof(connectionInfo));
            if (logger == null)
                throw new ArgumentNullException(nameof(logger));
            if (context == null)
                throw new ArgumentNullException(nameof(context));
            if (context.ApplicationId == null)
                throw new InvalidOperationException("context requires a valid application ID");

            this.ConnectionInfo = connectionInfo;
            this.Logger = logger;
            this.Context = context;
        }

        public OperationProgress GetProgress()
        {
            return new OperationProgress(this.progressPercent, this.progressMessage);
        }

        public async Task QueueBuildAsync(CancellationToken cancellationToken, bool logProgressToExecutionLog)
        {
            this.Logger.LogInformation($"Queueing build in TeamCity...");

            if (this.BuildConfigurationName != null && this.ProjectName != null && this.BuildConfigurationId == null)
            {
                await SetBuildConfigurationIdFromName().ConfigureAwait(false);
            }

            if (this.BranchName != null)
                this.Logger.LogDebug("Using branch: " + this.BranchName);

            string triggerUrl = string.Format(
                "action.html?add2Queue={0}{1}{2}",
                this.BuildConfigurationId,
                this.BranchName != null ? string.Format("&branchName={0}", Uri.EscapeDataString(this.BranchName)) : "",
                this.AdditionalParameters);

            using (var client = new TeamCityWebClient(this.ConnectionInfo))
            {
                this.Logger.LogDebug("Triggering build of configuration {0} at {1}", this.BuildConfigurationId, this.ConnectionInfo.GetApiUrl() + triggerUrl);
                await client.DownloadStringTaskAsync(triggerUrl).ConfigureAwait(false);

                this.Logger.LogInformation("Build of {0} was triggered successfully.", this.BuildConfigurationId);

                if (!this.WaitForCompletion)
                    return;

                this.Logger.LogInformation("Waiting for build to complete...");

                await Task.Delay(1000, cancellationToken).ConfigureAwait(false); // give TeamCity a second to create the build

                string getLatestBuildUrl = string.Format("app/rest/builds?locator=buildType:{0},count:1,running:true{1}", this.BuildConfigurationId, this.BranchName != null ? ",branch:" + Uri.EscapeDataString(this.BranchName) : "");
                string getLatestBuildResponse = await client.DownloadStringTaskAsync(getLatestBuildUrl).ConfigureAwait(false);
                string latestBuildId = ParseBuildId(getLatestBuildResponse);
                if (latestBuildId == null)
                {
                    this.Logger.LogError("BuildMaster has triggered a build in TeamCity, but TeamCity indicates that there are no builds running at this time, therefore BuildMaster cannot wait until the build completes.");
                    return;
                }

                string getBuildStatusUrl = string.Format("app/rest/builds/id:{0}", latestBuildId);

                TeamCityBuildStatus buildStatus;
                do
                {
                    string getBuildStatusResponse = await client.DownloadStringTaskAsync(getBuildStatusUrl).ConfigureAwait(false);
                    buildStatus = new TeamCityBuildStatus(getBuildStatusResponse);
                    
                    this.progressPercent = Interlocked.Exchange(ref this.progressPercent, buildStatus.PercentComplete);
                    this.progressMessage = Interlocked.Exchange(ref this.progressMessage, $"Building {buildStatus.ProjectName} Build #{buildStatus.BuildNumber} ({buildStatus.PercentComplete}% Complete)");

                    if (logProgressToExecutionLog)
                        this.Logger.LogInformation(this.progressMessage);

                    await Task.Delay(4 * 1000, cancellationToken).ConfigureAwait(false);

                } while (buildStatus.IsRunning);

                if (buildStatus.Status == TeamCityBuildStatus.BuildStatuses.Success)
                {
                    this.Logger.LogInformation("{0} build #{1} successful. TeamCity reports: {2}", buildStatus.ProjectName, buildStatus.BuildNumber, buildStatus.StatusText);
                }
                else if (buildStatus.Status == TeamCityBuildStatus.BuildStatuses.Failure)
                {
                    this.Logger.LogError("{0} build #{1} failed. TeamCity reports: {2}", buildStatus.ProjectName, buildStatus.BuildNumber, buildStatus.StatusText);
                }
                else
                {
                    this.Logger.LogError("{0} build #{1} encountered an error. TeamCity reports: {2}", buildStatus.ProjectName, buildStatus.BuildNumber, buildStatus.StatusText);
                }
            }
        }

        private async Task SetBuildConfigurationIdFromName()
        {
            this.Logger.LogDebug("Attempting to resolve build configuration ID from project and name...");
            using (var client = new TeamCityWebClient(this.ConnectionInfo))
            {
                this.Logger.LogDebug("Downloading build types...");
                string result = await client.DownloadStringTaskAsync("app/rest/buildTypes").ConfigureAwait(false);
                var doc = XDocument.Parse(result);
                var buildConfigurations = from e in doc.Element("buildTypes").Elements("buildType")
                                          let buildConfigurationId = (string)e.Attribute("id")
                                          let projectName = (string)e.Attribute("projectName")
                                          let buildConfigurationName = (string)e.Attribute("name")
                                          where string.Equals(projectName, this.ProjectName, StringComparison.OrdinalIgnoreCase)
                                          where string.Equals(buildConfigurationName, this.BuildConfigurationName, StringComparison.OrdinalIgnoreCase)
                                          select buildConfigurationId;

                this.BuildConfigurationId = buildConfigurations.FirstOrDefault();
                if (this.BuildConfigurationId == null)
                    throw new ExecutionFailureException($"Build configuration ID could not be found for project \"{this.ProjectName}\" and build configuration \"{this.BuildConfigurationName}\".");

                this.Logger.LogDebug("Build configuration ID resolved to: " + this.BuildConfigurationId);
            }
        }

        private static string ParseBuildId(string getBuildResponse)
        {
            #region XML Format...
            /*
             *  <builds count="1" nextHref="/httpAuth/app/rest/builds?locator=buildType:bt3,count:1,running:true&count=1&start=1">
             *      <build id="27" number="16" running="true" percentageComplete="37" status="SUCCESS" buildTypeId="bt3" startDate="20120801T103949-0400" href="/httpAuth/app/rest/builds/id:27" webUrl="http://bmteamcitysv1/viewLog.html?buildId=27&buildTypeId=bt3"/>
             *  </builds>
             */
            #endregion

            var doc = new XmlDocument();
            doc.LoadXml(getBuildResponse);

            if (doc.SelectSingleNode("/builds/@count").Value == "0")
                return null;

            return doc.SelectSingleNode("/builds/build/@id").Value;
        }

        private sealed class TeamCityBuildStatus
        {
            public enum BuildStatuses { Success, Failure, Error }

            public string BuildNumber { get; private set; }
            public BuildStatuses Status { get; private set; }
            public string StatusText { get; private set; }
            public bool IsRunning { get; private set; }
            public int PercentComplete { get; private set; }
            public string ProjectName { get; private set; }

            public TeamCityBuildStatus(string getBuildStatusResponse)
            {
                #region XML Format...
                /*
                 *  <build id="29" number="18" status="SUCCESS" href="/httpAuth/app/rest/builds/id:29" webUrl="http://bmteamcitysv1/viewLog.html?buildId=29&buildTypeId=bt3" personal="false" history="false" pinned="false" running="true">
                 *      <running-info percentageComplete="2" elapsedSeconds="1" estimatedTotalSeconds="95" currentStageText="Checking for changes" outdated="false" probablyHanging="false"/>
                 *      <statusText>Checking for changes</statusText>
                 *      <buildType id="bt3" name="Build" href="/httpAuth/app/rest/buildTypes/id:bt3" projectName="BuildMaster" projectId="project3" webUrl="http://bmteamcitysv1/viewType.html?buildTypeId=bt3"/>
                 *      <startDate>20120801T105524-0400</startDate>
                 *      <agent href="/httpAuth/app/rest/agents/id:1" id="1" name="BMTEAMCITYSV1"/>
                 *      <tags/>
                 *      <properties>
                 *          <property name="env.Configuration" value="Release"/>
                 *      </properties>
                 *      <snapshot-dependencies/>
                 *      <artifact-dependencies/>
                 *      <revisions/>
                 *      <triggered date="20120801T105524-0400">
                 *          <user href="/httpAuth/app/rest/users/id:1" id="1" name="Administrator" username="admin"/>
                 *      </triggered>
                 *      <changes count="0" href="/httpAuth/app/rest/changes?build=id:29"/>
                 *  </build>
                 */
                #endregion

                var doc = new XmlDocument();
                doc.LoadXml(getBuildStatusResponse);

                this.BuildNumber = doc.SelectSingleNode("/build/@number").Value;
                this.Status = (BuildStatuses)Enum.Parse(typeof(BuildStatuses), doc.SelectSingleNode("/build/@status").Value, true);
                this.StatusText = doc.SelectSingleNode("/build/statusText").InnerText;
                var runningAttr = doc.SelectSingleNode("/build/@running");
                this.IsRunning = runningAttr != null && bool.Parse(runningAttr.Value);
                this.PercentComplete = this.IsRunning
                    ? int.Parse(doc.SelectSingleNode("/build/running-info/@percentageComplete").Value)
                    : 100;
                this.ProjectName = doc.SelectSingleNode("/build/buildType/@projectName").Value;
            }
        }
    }
}
