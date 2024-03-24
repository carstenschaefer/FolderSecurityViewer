// FolderSecurityViewer is an easy-to-use NTFS permissions tool that helps you effectively trace down all security owners of your data.
// Copyright (C) 2015 - 2024  Carsten Schäfer, Matthias Friedrich, and Ritesh Gite
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as
// published by the Free Software Foundation, either version 3 of the
// License, or (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Affero General Public License for more details.
// 
// You should have received a copy of the GNU Affero General Public License
// along with this program.  If not, see <https://www.gnu.org/licenses/>.

namespace FSV.AdServices
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Configuration;
    using Microsoft.Extensions.Logging;

    public class ActiveDirectoryScanOptions
    {
        private readonly ILogger<ActiveDirectoryScanOptions> logger;

        public ActiveDirectoryScanOptions(ILogger<ActiveDirectoryScanOptions> logger)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public List<ConfigItem> TranslatedItems { get; set; }

        public List<ConfigItem> PermissionGridColumns { get; set; }

        public List<string> BuiltInGroups { get; set; }

        public List<ConfigItem> ExclusionGroups { get; set; }

        public Action<int> Progress { get; set; }

        public bool SkipBuiltInGroups { get; set; }

        public ActiveDirectoryScanResult<int> ProgressData { get; set; }

        public string GetTranslatedRight(string origin)
        {
            if (origin == null)
            {
                throw new ArgumentNullException(nameof(origin));
            }

            string ret = origin;
            try
            {
                ConfigItem transText = this.TranslatedItems.FirstOrDefault(p => string.Equals(p.Name, origin, StringComparison.CurrentCultureIgnoreCase));

                if (transText != null && !string.IsNullOrEmpty(transText.DisplayName))
                {
                    ret = transText.DisplayName;
                }
                else
                {
                    // Checking for older version of config in which the origin string
                    // does not contain Allow/Deny.
                    origin = origin.Substring(origin.IndexOf(": ") + 2);
                    transText = this.TranslatedItems.FirstOrDefault(p => p.Name.ToLower() == origin.ToLower());
                    if (transText != null && !string.IsNullOrEmpty(transText.DisplayName))
                    {
                        ret = transText.DisplayName;
                    }
                }
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Failed to obtain display-text for privilege ({Right}).", origin);
            }

            return ret;
        }

        public bool CheckExclusionGroups(string currGroupName, string sid)
        {
            var skipScan = false;

            // direct Match
            if (this.ExclusionGroups.Any(p => string.Equals(p.Name, currGroupName, StringComparison.CurrentCultureIgnoreCase)))
            {
                skipScan = true;
            }
            else
                // check config entries against current group
            {
                foreach (string exgroup in this.ExclusionGroups.Select(p => p.Name))
                    // wildcard handling
                {
                    if (exgroup.Contains("*"))
                    {
                        string groupTocheck = exgroup.Replace("*", string.Empty);

                        if (currGroupName.ToLower().Contains(groupTocheck.ToLower()))
                        {
                            skipScan = true;
                        }
                    }
                    else
                    {
                        string[] onlygroupName = currGroupName.Split('\\');

                        // check only groupname witout domain
                        if (onlygroupName.Count() == 2)
                        {
                            if (string.Equals(exgroup, onlygroupName[1], StringComparison.CurrentCultureIgnoreCase))
                            {
                                skipScan = true;
                            }
                        }
                        else
                        {
                            // check config groupname without domain
                            string[] configgroup = exgroup.Split('\\');
                            if (configgroup.Count() == 2)
                            {
                                if (string.Equals(currGroupName, configgroup[1], StringComparison.CurrentCultureIgnoreCase))
                                {
                                    skipScan = true;
                                }
                            }
                        }
                    }
                }
            }

            if (skipScan)
            {
                this.logger.LogDebug($"Exclusion Group skipped: {currGroupName}");
            }
            else if (this.SkipBuiltInGroups)
            {
                if (this.BuiltInGroups.Contains(sid))
                {
                    // Skip Builtin Groups
                    this.logger.LogDebug($"Builtin Group skipped: {currGroupName}");
                    skipScan = true;
                }
            }

            return skipScan;
        }
    }
}