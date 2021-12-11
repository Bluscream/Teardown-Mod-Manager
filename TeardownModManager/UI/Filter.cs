using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace TeardownModManager
{
    public partial class MainForm
    {
        void OnlyShowDisabled_Click(object sender, EventArgs e)
        {
            onlyShowEnabled.Checked = onlyShowDisabled.Checked;
            onlyShowDisabled.Checked = !onlyShowDisabled.Checked;
            FilterByText();
        }

        void OnlyShowEnabled_Click(object sender, EventArgs e)
        {
            onlyShowDisabled.Checked = onlyShowEnabled.Checked;
            onlyShowEnabled.Checked = !onlyShowEnabled.Checked;
            FilterByText();
        }

        void FilterMenuItem_Click(object sender, EventArgs e) => SwitchFilter((ToolStripMenuItem)sender);

        void SwitchFilter(ToolStripMenuItem selectedMenuItem)
        {
            selectedMenuItem.Checked ^= true;
            var filterItems = new List<ToolStripMenuItem>();

            foreach (var ltoolStripMenuItem in from object item in selectedMenuItem.Owner.Items
                                               let ltoolStripMenuItem = item as ToolStripMenuItem
                                               where ltoolStripMenuItem != null
                                               select ltoolStripMenuItem)
            {
                if (ltoolStripMenuItem.Name != "onlyShowDisabled")
                    filterItems.Add(ltoolStripMenuItem);
            }

            if (selectedMenuItem.Name == "searchInEverything")
            {
                foreach (var ltoolStripMenuItem in filterItems)
                    ltoolStripMenuItem.Checked = true;
            }
            else
            {
                foreach (var item in filterItems.Where(item => !item.Equals(selectedMenuItem))) item.Checked = false;
            }

            selectedMenuItem.Owner.Show();
            FilterByText();
        }

        void Txt_mods_filter_TextChanged(object sender, EventArgs e) => FilterByText();

        async void FilterByText()
        {
            var txt = txt_mods_filter.Text.ToLowerInvariant();
            var matchingMods = new List<Teardown.Mod>();

            if (!txt.IsNullOrEmpty())
            {
                if (searchInEverything.Checked) matchingMods.AddRange(modsInCategory.Where(m => m.ToJson().ToLowerInvariant().Contains(txt)).ToList());
                else if (searchInNames.Checked) matchingMods.AddRange(modsInCategory.Where(m => m.Name.ToLowerInvariant().Contains(txt)).ToList());
                else if (searchInDescriptions.Checked)
                {
                    foreach (var mod in modsInCategory)
                    {
                        if (mod.Details is null) await mod.UpdateModDetailsAsync(webClient);
                        if (mod.Details != null && mod.Details.description != null && mod.Details.description.ToLowerInvariant().Contains(txt)) matchingMods.Add(mod);
                    }
                }
            }
            else { matchingMods.AddRange(modsInCategory); }

            if (onlyShowDisabled.Checked) matchingMods = matchingMods.Where(m => m.Disabled).ToList();
            else if (onlyShowEnabled.Checked) matchingMods = matchingMods.Where(m => !m.Disabled).ToList();

            FillModList(matchingMods);
        }
    }
}