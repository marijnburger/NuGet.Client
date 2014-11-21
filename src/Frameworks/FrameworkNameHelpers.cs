﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NuGet.Frameworks.FrameworkMappings
{
    public static class FrameworkNameHelpers
    {

        public static string GetFolderName(string identifierShortName, string versionString, string profileShortName)
        {
            return String.Format(CultureInfo.InvariantCulture, "{0}{1}{2}{3}", identifierShortName, versionString, String.IsNullOrEmpty(profileShortName) ? string.Empty : "-", profileShortName);
        }

        public static string GetVersionString(Version version)
        {
            string versionString = null;

            if (version != null)
            {
                if (version.Major > 9 || version.Minor > 9 || version.Build > 9 || version.Revision > 9)
                {
                    versionString = version.ToString();
                }
                else
                {
                    versionString = version.ToString().Replace(".", "").TrimEnd('0');
                }
            }

            return versionString;
        }

        public static Version GetVersion(string versionString)
        {
            Version version = null;

            if (String.IsNullOrEmpty(versionString))
            {
                version = new Version(0, 0);
            }
            else
            {
                if (versionString.IndexOf('.') > -1)
                {
                    // parse the version as a normal dot delimited version
                    Version.TryParse(versionString, out version);
                }
                else
                {
                    // make sure we have at least 2 digits
                    if (versionString.Length < 2)
                    {
                        versionString += "0";
                    }

                    // take only the first 4 digits and add dots
                    // 451 -> 4.5.1
                    // 81233 -> 8123
                    Version.TryParse(String.Join(".", versionString.ToCharArray().Take(4)), out version);
                }
            }

            return version;
        }
    }
}
