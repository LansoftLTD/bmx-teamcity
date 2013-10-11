using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Inedo.BuildMaster.Extensibility.Variables;
using Inedo.BuildMaster;
using System.ComponentModel;
using Inedo.BuildMaster.Web;

namespace Inedo.BuildMasterExtensions.TeamCity
{
    [VariableProperties("TeamCity Build Configuration", "Allows selection of a Build Configuration from a TeamCity Instance")]
    [CustomSetter(typeof(SelectBuildConfigurationVariableSetter))]
    //[CustomEditor(typeof(SelectBuildConfigurationVariableEditor))]
    public sealed class SelectBuildConfigurationVariable : VariableBase
    {
        [Persistent]
        [DisplayName("Configuration Profile")]
        [Category("Configuration")]
        [Description("When set, uses the specified configuration TeamCity profile instead of the default. This can be important when you have multiple TeamCity instances configured within BuildMaster.")]
        public string ConfigurationProfileName { get; set; }
        
        [Persistent]
        [DisplayName("Project Name Filter")]
        [Category("Project")]
        [Description("When set, filters the selectable build configurations to the specified project.")]
        public string ProjectNameFilter { get; set; }
    }
}
