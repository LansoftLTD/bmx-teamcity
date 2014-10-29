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
    }
}