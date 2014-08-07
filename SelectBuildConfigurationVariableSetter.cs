using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Inedo.Web.Controls;
using Inedo.BuildMaster.Web.Controls.Extensions;
using System.Net;
using System.Xml.Linq;
using System.Web.UI.WebControls;

namespace Inedo.BuildMasterExtensions.TeamCity
{
    public sealed class SelectBuildConfigurationVariableSetter : SelectBuildConfigurationPicker, IVariableSetter<SelectBuildConfigurationVariable>
    {
        public SelectBuildConfigurationVariableSetter()
        {
            this.OptionGroupSeparator = ":";
        }


        string IVariableSetter.VariableValue
        {
            get
            {
                return this.SelectedValue;
            }
            set
            {
                this.SelectedValue = value;
            }
        }

        void IVariableSetter<SelectBuildConfigurationVariable>.BindToVariable(SelectBuildConfigurationVariable variable, string defaultValue)
        {
            if (variable == null) throw new ArgumentNullException("variable");
            
            this.FillItems(TeamCityConfigurer.GetConfigurer(variable.ConfigurationProfileName));
            this.SelectedValue = InedoLib.Util.CoalesceStr(variable.Value, defaultValue);
        }
    }
}
