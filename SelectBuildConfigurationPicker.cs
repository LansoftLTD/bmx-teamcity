using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Inedo.Web.Controls;
using Inedo.BuildMaster.Web.Controls.Extensions;
using System.Net;
using System.Xml.Linq;
using System.Web.UI.WebControls;
using Inedo.BuildMaster;
using Inedo.BuildMaster.Extensibility;

namespace Inedo.BuildMasterExtensions.TeamCity
{
    public class SelectBuildConfigurationPicker : ComboSelect
    {
        public SelectBuildConfigurationPicker()
        {
            this.OptionGroupSeparator = ":";
        }

        protected override void OnPreRender(EventArgs e)
        {
            this.EnsureID();
            base.OnPreRender(e);
        }

        internal void FillItems(TeamCityConfigurer configurer)
        {
            if (configurer == null)
                return;

            using (var client = new WebClient())
            {
                client.BaseAddress = configurer.BaseUrl;
                if (!string.IsNullOrEmpty(configurer.Username))
                    client.Credentials = new NetworkCredential(configurer.Username, configurer.Password);

                this.Items.AddRange(XDocument
                    .Parse(client.DownloadString("app/rest/buildTypes"))
                    .Element("buildTypes")
                    .Elements("buildType")
                    .Select(e => new
                    {
                        Id = (string)e.Attribute("id"),
                        Project = (string)e.Attribute("projectName"),
                        Name = (string)e.Attribute("name")
                    })
                    .Select(bt => new ListItem
                    {
                        Value = bt.Id,
                        Text = bt.Project + ": " + bt.Name
                    })
                    .ToArray());
            }
        }

    }
}
