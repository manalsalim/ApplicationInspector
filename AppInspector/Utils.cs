﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Microsoft.ApplicationInspector.Commands
{
    static public class Utils
    {
        static string _basePath;
        public enum AppPath { basePath, defaultRulesSrc, defaultRulesPackedFile, defaultLog, tagGroupPref, tagCounterPref };


        public static string GetVersionString()
        {
            return String.Format("Microsoft Application Inspector {0}", GetVersion());
        }

        public static string GetVersion()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            FileVersionInfo fileVersionInfo = FileVersionInfo.GetVersionInfo(assembly.Location);
            return fileVersionInfo.ProductVersion;
        }

        static public string GetPath(AppPath pathType)
        {
            string result = "";
            switch (pathType)
            {
                case AppPath.basePath:
                    result = GetBaseAppPath();
                    break;
                case AppPath.defaultLog:
                    result = Path.Combine(GetBaseAppPath(), "log.txt");
                    break;
                case AppPath.defaultRulesSrc:
                    result = Path.GetFullPath(Path.Combine(GetBaseAppPath(), "..", "..", "..", "..", "AppInspector", "rules", "default"));//used to ref project folder
                    break;
                case AppPath.defaultRulesPackedFile:
                    result = Path.Combine(System.AppContext.BaseDirectory, "..", "..", "..", "..", "AppInspector", "Resources", "defaultRules.json");//packed default file in project resources
                    break;
                case AppPath.tagGroupPref:
                    result = Path.Combine(GetBaseAppPath(), "preferences", "tagreportgroups.json");
                    break;
                case AppPath.tagCounterPref:
                    result = Path.Combine(GetBaseAppPath(), "preferences", "tagcounters.json");
                    break;

            }

            return result;
        }

        static private string GetBaseAppPath()
        {
            if (!String.IsNullOrEmpty(_basePath))
                return _basePath;

            _basePath = Path.GetFullPath(System.AppContext.BaseDirectory);
            return _basePath;
        }



        /// <summary>
        /// Attempt to map application type tags or file type or language to identify
        /// WebApplications, Windows Services, Client Apps, WebServices, Azure Functions etc.
        /// </summary>
        /// <param name="match"></param>
        static public String DetectSolutionType(MatchRecord match)
        {
            string result = "";
            if (match.Issue.Rule.Tags.Any(s => s.Contains("Application.Type")))
            {
                foreach (string tag in match.Issue.Rule.Tags)
                {
                    int index = tag.IndexOf("Application.Type");
                    if (-1 != index)
                    {
                        result = tag.Substring(index + 17);
                        break;
                    }
                }
            }
            else
            {
                switch (match.Filename)
                {
                    case "web.config":
                        result = "Web.Application";
                        break;
                    case "app.config":
                        result = ".NETclient";
                        break;
                    default:
                        switch (Path.GetExtension(match.Filename))
                        {
                            case ".cshtml":
                                result = "Web.Application";
                                break;
                            case ".htm":
                            case ".html":
                            case ".js":
                            case ".ts":
                                result = "Web.Application";
                                break;
                            case "powershell":
                            case "shellscript":
                            case "wincmdscript":
                                result = "script";
                                break;
                            default:
                                switch (match.Language.Name)
                                {
                                    case "ruby":
                                    case "perl":
                                    case "php":
                                        result = "Web.Application";
                                        break;
                                }
                                break;
                        }
                        break;
                }

            }

            return result.ToLower();
        }



        public static void OpenBrowser(string url)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {

                try
                {
                    Process.Start(new ProcessStartInfo("cmd", $"/c start {url}"));
                    WriteOnce.General(ErrMsg.GetString(ErrMsg.ID.BROWSER_START_SUCCESS));
                }
                catch (Exception)
                {
                    WriteOnce.General(ErrMsg.GetString(ErrMsg.ID.BROWSER_START_FAIL));
                }
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("BROWSER")))
                {
                    try
                    {
                        Process.Start("xdg-open", "\"" + url + "\"");
                        WriteOnce.General(ErrMsg.GetString(ErrMsg.ID.BROWSER_START_SUCCESS));
                    }
                    catch (Exception)
                    {
                        WriteOnce.SafeLog("Unable to open browser using BROWSER environment var", NLog.LogLevel.Error);
                    }
                }
                else
                {
                    WriteOnce.General(ErrMsg.GetString(ErrMsg.ID.BROWSER_ENVIRONMENT_VAR));
                }
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                try
                {
                    Process.Start("open", "\"" + url + "\"");
                    WriteOnce.General(ErrMsg.GetString(ErrMsg.ID.BROWSER_START_SUCCESS));
                }
                catch (Exception)
                {
                    WriteOnce.General(ErrMsg.GetString(ErrMsg.ID.BROWSER_START_FAIL));
                }
            }
        }

    }

}
