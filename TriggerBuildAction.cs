using System;
using System.Threading;
using System.Xml;
using Inedo.BuildMaster;
using Inedo.BuildMaster.Extensibility.Actions;
using Inedo.BuildMaster.Web;

namespace Inedo.BuildMasterExtensions.TeamCity
{
    /// <summary>
    /// Triggers a build on a TeamCity server.
    /// </summary>
    [ActionProperties(
        "Trigger TeamCity Build",
        "Triggers a build in TeamCity using the specified build configuration ID.",
        DefaultToLocalServer = true)]
    [CustomEditor(typeof(TriggerBuildActionEditor))]
    [Tag(Tags.ContinuousIntegration)]
    public sealed class TriggerBuildAction : TeamCityActionBase
    {
        /// <summary>
        /// Gets or sets the build configuration id.
        /// </summary>
        [Persistent]
        public string BuildConfigurationId { get; set; }

        /// <summary>
        /// Gets or sets the additional parameters.
        /// </summary>
        [Persistent]
        public string AdditionalParameters { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [wait until complete].
        /// </summary>
        [Persistent]
        public bool WaitForCompletion { get; set; }

        /// <summary>
        /// Gets or sets the name of the branch.
        /// </summary>
        [Persistent]
        public string BranchName { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="TriggerBuildAction"/> class.
        /// </summary>
        public TriggerBuildAction()
        {
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
                "Triggers a build of the configuration \"{0}\" in TeamCity{1}{2}.", 
                this.BuildConfigurationId,
                Util.ConcatNE(" with the additional parameters \"", this.AdditionalParameters ,"\""),
                !string.IsNullOrEmpty(this.BranchName) ? " using branch " + this.BranchName : ""
            );
        }

        protected override void Execute()
        {
            var configurer = this.GetExtensionConfigurer();
            string branch = this.GetBranchName(configurer);
            if (branch != null) 
                this.LogDebug("Using branch: " + branch);

            string triggerUrl = string.Format(
                "action.html?add2Queue={0}{1}{2}", 
                this.BuildConfigurationId, 
                branch != null ? string.Format("&branchName={0}", Uri.EscapeDataString(this.BranchName)) : "",
                this.AdditionalParameters);
            
            using (var client = new TeamCityWebClient(configurer))
            {
                this.LogDebug("Triggering build of configuration {0} at {1}", this.BuildConfigurationId, GetExtensionConfigurer().BaseUrl + triggerUrl);
                client.DownloadString(triggerUrl);

                this.LogInformation("Build of {0} was triggered successfully.", this.BuildConfigurationId);

                if (!this.WaitForCompletion) 
                    return;

                Thread.Sleep(1000); // give TeamCity a second to create the build

                string getLatestBuildUrl = string.Format("app/rest/builds?locator=buildType:{0},count:1,running:true{1}", this.BuildConfigurationId, branch != null ? ",branch:" + Uri.EscapeDataString(branch) : "");
                string getLatestBuildResponse = client.DownloadString(getLatestBuildUrl);
                string latestBuildId = ParseBuildId(getLatestBuildResponse);
                if (latestBuildId == null)
                {
                    this.LogError("BuildMaster has triggered a build in TeamCity, but TeamCity indicates that there are no builds running at this time, therefore BuildMaster cannot wait until the build completes.");
                    return;
                }

                string getBuildStatusUrl = string.Format("app/rest/builds/id:{0}", latestBuildId);

                TeamCityBuildStatus buildStatus;
                do 
                {
                    string getBuildStatusResponse = client.DownloadString(getBuildStatusUrl);
                    buildStatus = new TeamCityBuildStatus(getBuildStatusResponse);

                    this.LogInformation("Building {0} Build #{1} ({2}% Complete)", buildStatus.ProjectName, buildStatus.BuildNumber, buildStatus.PercentComplete);

                    Thread.Sleep(4000);
                    this.ThrowIfCanceledOrTimeoutExpired();

                } while(buildStatus.IsRunning);

                if (buildStatus.Status == TeamCityBuildStatus.BuildStatuses.Success)
                {
                    this.LogInformation("{0} build #{1} successful. TeamCity reports: {2}", buildStatus.ProjectName, buildStatus.BuildNumber, buildStatus.StatusText);
                }
                else if (buildStatus.Status == TeamCityBuildStatus.BuildStatuses.Failure) 
                {
                    this.LogError("{0} build #{1} failed. TeamCity reports: {2}", buildStatus.ProjectName, buildStatus.BuildNumber, buildStatus.StatusText);
                }
                else 
                {
                    this.LogError("{0} build #{1} encountered an error. TeamCity reports: {2}", buildStatus.ProjectName, buildStatus.BuildNumber, buildStatus.StatusText);
                }
            }
        }

        private string GetBranchName(TeamCityConfigurer configurer)
        {
            if (!string.IsNullOrEmpty(this.BranchName))
                return this.BranchName;

            if (!string.IsNullOrEmpty(configurer.DefaultBranchName))
                return configurer.DefaultBranchName;

            return null;
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
