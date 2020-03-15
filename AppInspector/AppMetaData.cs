﻿// Copyright (C) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using Microsoft.ApplicationInspector.RulesEngine;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Microsoft.ApplicationInspector.Commands
{
    /// <summary>
    /// Parent wrapper class for representing source characterization parts
    /// Contains data elements that represent post processing options 
    /// Contains data elements that are related to organization and presentation of tagGroups
    /// </summary>
    public class AppProfile
    {
        [JsonProperty(PropertyName = "appInspectorVer")]
        public string Version { get; set; }
        [JsonProperty(PropertyName = "sourcePath")]
        public string SourcePath { get; set; }
        [JsonProperty(PropertyName = "appInspectorArgs")]
        public string Args { get; set; }
        [JsonProperty(PropertyName = "dateScanned")]
        public string DateScanned { get; set; }
        [JsonProperty(PropertyName = "rulePaths")]
        public HashSet<string> RulePaths { get { return MetaData.RulePaths; } }
        public AppMetaData MetaData { get; set; }
        //has to be public to be visible to htmlwriter
        [JsonProperty(PropertyName = "TagReportGroupLists")]
        public Dictionary<string, List<TagInfo>> KeyedTagInfoLists { get; }//dynamic lists for grouping tag properties in reporting
        [JsonIgnore]
        public Dictionary<string, List<TagInfo>> KeyedSortedTagInfoLists { get; } //split to avoid json serialization with others
        [JsonIgnore]
        public List<MatchRecord> MatchList { get; set; }//list of MatchRecords that wrap and augment Issues class during processing
        public List<LimitedMatchRecord> FormattedMatchList { get; set; }//lighter formatted list structure more suited for json output to limit extraneous fieldo in Issues class
        [JsonIgnore]
        public List<TagCategory> TagGroupPreferences { get; set; }//read preferred list of groups and tags for profile page

        //Report properties
        [JsonIgnore]
        public bool ExcludeRollup { get; set; }
        [JsonIgnore]
        public bool SimpleTagsOnly { get; set; }
        [JsonIgnore]
        public bool UniqueTagsOnly { get; }
        [JsonIgnore]
        public bool AutoBrowserOpen { get; set; }


        /// <summary>
        /// Constructor initializes several internal lists not populated by rules processing
        /// </summary>
        /// <param name="sourcePath">code</param>
        /// <param name="rulePaths">rules</param>
        /// <param name="excludeRollup">omit aggregated rollup e.g. simple output with matches</param>
        /// <param name="simpleTagsOnly">simple output override</param>
        /// <param name="uniqueTagsOnly">avoid duplicate tag reporting</param>
        public AppProfile(string sourcePath, List<string> rulePaths, bool excludeRollup, bool simpleTagsOnly, bool uniqueTagsOnly, bool autoOpenBrowser = true)
        {
            SourcePath = sourcePath;
            Version = Utils.GetVersion();
            MatchList = new List<MatchRecord>();
            FormattedMatchList = new List<LimitedMatchRecord>();
            KeyedTagInfoLists = new Dictionary<string, List<TagInfo>>();
            KeyedSortedTagInfoLists = new Dictionary<string, List<TagInfo>>();

            //read default/user preferences on what tags to report presence on and groupings
            if (File.Exists(Utils.GetPath(Utils.AppPath.tagGroupPref)))
                TagGroupPreferences = JsonConvert.DeserializeObject<List<TagCategory>>(File.ReadAllText(Utils.GetPath(Utils.AppPath.tagGroupPref)));
            else
                TagGroupPreferences = new List<TagCategory>();

            ExcludeRollup = excludeRollup;
            SimpleTagsOnly = simpleTagsOnly;
            UniqueTagsOnly = uniqueTagsOnly;
            AutoBrowserOpen = autoOpenBrowser;

            MetaData = new AppMetaData(sourcePath, rulePaths)
            {
                RulePaths = rulePaths.ToHashSet<string>()
            };

        }

        /// <summary>
        /// Aggregate tags found into lists by organizing into customer preferred
        /// groups of taginfo objects
        /// TagGroupPreferences are organized by category i.e. profile or composition pages then by groups within
        /// file to be read
        /// </summary>
        public void PrepareReport()
        {
            //start with all unique tags to initialize which is then used to sort into groups of tagInfo lists
            MetaData.UniqueTags = GetUniqueTags();

            //for each preferred group of tag patterns determine if at least one instance was detected
            foreach (TagCategory tagCategory in TagGroupPreferences)
            {
                foreach (TagGroup tagGroup in tagCategory.Groups)
                {
                    foreach (TagSearchPattern pattern in tagGroup.Patterns)
                    {
                        pattern.Detected = MetaData.UniqueTags.Any(v => v.Contains(pattern.SearchPattern));

                        //create dynamic "category" groups of tags with pattern relationship established from TagReportGroups.json
                        //that can be used to populate reports with various attributes for each tag detected
                        if (pattern.Detected)
                        {
                            if (tagCategory.Type == TagCategory.tagInfoType.uniqueTags)
                                KeyedTagInfoLists["tagGrp" + tagGroup.DataRef] = GetUniqueMatchingTagInfoList(tagGroup);
                            else if (tagCategory.Type == TagCategory.tagInfoType.allTags)
                                KeyedTagInfoLists["tagGrp" + tagGroup.DataRef] = GetAllMatchingTagInfoList(tagGroup);
                        }
                    }
                }
            }

            //create simple ranked page lists for sorted display for app defined report page
            KeyedSortedTagInfoLists["tagGrpAllTagsByConfidence"] = GetTagInfoListByConfidence();
            KeyedSortedTagInfoLists["tagGrpAllTagsBySeverity"] = GetTagInfoListBySeverity();
            KeyedSortedTagInfoLists["tagGrpAllTagsByName"] = GetTagInfoListByName();

            foreach (MatchRecord matchRecord in MatchList)
            {
                LimitedMatchRecord matchItem = new LimitedMatchRecord(matchRecord);
                FormattedMatchList.Add(matchItem);
            }

            MetaData.PrepareReport();

        }

        #region UIAndReportResultsOrg

        /// <summary>
        /// Get a list of TagGroup for a given category section name e.g. profile 
        /// </summary>
        /// <param name="category"></param>
        /// <returns></returns>
        public List<TagGroup> GetCategoryTagGroups(string category)
        {
            List<TagGroup> result = new List<TagGroup>();
            //get all tag groups for specified category
            foreach (TagCategory categoryTagGroup in TagGroupPreferences)
            {
                if (categoryTagGroup.Name == category)
                {
                    result = categoryTagGroup.Groups;
                    break;
                }
            }

            //now get all matches for that group i.e. Authentication
            foreach (TagGroup group in result)
            {
                GetUniqueMatchingTagInfoList(group);
            }

            return result;
        }



        private HashSet<string> GetUniqueTags()
        {
            HashSet<string> results = new HashSet<string>();

            foreach (MatchRecord match in MatchList)
            {
                foreach (string tag in match.Issue.Rule.Tags)
                    results.Add(tag);
            }

            return results;
        }

        /// <summary>
        /// Builds list of matching tags by profile pattern
        /// Ensures only one instance of a given tag in results unlike GetAllMatchingTags method
        /// with highest confidence level for that tag pattern
        /// </summary>
        /// <param name="tagPattern"></param>
        /// <returns></returns>
        private List<TagInfo> GetUniqueMatchingTagInfoList(TagGroup tagGroup, bool addNotFound = true)
        {
            List<TagInfo> result = new List<TagInfo>();
            HashSet<string> hashSet = new HashSet<string>();

            foreach (TagSearchPattern pattern in tagGroup.Patterns)
            {
                if (pattern.Detected)//set at program.RollUp already so don't search for again
                {
                    var tagPatternRegex = new Regex(pattern.SearchPattern, RegexOptions.IgnoreCase);

                    foreach (var match in MatchList)
                    {
                        foreach (var tagItem in match.Issue.Rule.Tags)
                        {
                            if (tagPatternRegex.IsMatch(tagItem))
                            {
                                if (!hashSet.Contains(pattern.SearchPattern))
                                {
                                    result.Add(new TagInfo
                                    {
                                        Tag = tagItem,
                                        Confidence = match.Issue.Confidence.ToString(),
                                        Severity = match.Issue.Rule.Severity.ToString(),
                                        ShortTag = pattern.DisplayName,
                                        StatusIcon = pattern.DetectedIcon,
                                        Detected = true
                                    });

                                    hashSet.Add(pattern.SearchPattern);

                                    pattern.Confidence = match.Issue.Confidence.ToString();

                                }
                                else
                                {
                                    //we have but ensure we get highest confidence, severity as there are likly multiple matches for this tag pattern
                                    foreach (TagInfo updateItem in result)
                                    {
                                        if (updateItem.Tag == tagItem)
                                        {
                                            RulesEngine.Confidence oldConfidence;
                                            Enum.TryParse(updateItem.Confidence, out oldConfidence);
                                            if (match.Issue.Confidence > oldConfidence)
                                            {
                                                updateItem.Confidence = match.Issue.Confidence.ToString();
                                                pattern.Confidence = match.Issue.Confidence.ToString();
                                            }

                                            RulesEngine.Severity oldSeverity;
                                            Enum.TryParse(updateItem.Severity, out oldSeverity);
                                            if (match.Issue.Rule.Severity > oldSeverity)
                                            {
                                                updateItem.Severity = match.Issue.Rule.Severity.ToString();
                                            }

                                            break;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                else if (addNotFound) //allow to report on false presense items
                {
                    TagInfo tagInfo = new TagInfo
                    {
                        Tag = pattern.SearchPattern,
                        Detected = false,
                        ShortTag = pattern.DisplayName,
                        StatusIcon = pattern.NotDetectedIcon,
                        Confidence = "",
                        Severity = ""
                    };

                    pattern.Confidence = "";
                    result.Add(tagInfo);
                    hashSet.Add(tagInfo.Tag);
                }

            }


            return result;
        }



        /// <summary>
        /// Gets a set of matching tags for a set of patterns, returning for all matches
        /// </summary>
        /// <param name="patterns"></param>
        /// <param name="addNotFound"></param>
        /// <returns></returns>
        private List<TagInfo> GetAllMatchingTagInfoList(TagGroup tagGroup, bool addNotFound = true)
        {
            List<TagInfo> result = new List<TagInfo>();
            HashSet<string> hashSet = new HashSet<string>();

            foreach (TagSearchPattern pattern in tagGroup.Patterns)
            {
                if (pattern.Detected)
                {
                    var tagPatternRegex = new Regex(pattern.SearchPattern, RegexOptions.IgnoreCase);

                    foreach (var match in MatchList)
                    {
                        foreach (var tagItem in match.Issue.Rule.Tags)
                        {
                            if (tagPatternRegex.IsMatch(tagItem))
                            {
                                if (!hashSet.Contains(tagItem))
                                {
                                    result.Add(new TagInfo
                                    {
                                        Tag = tagItem,
                                        Confidence = match.Issue.PatternMatch.Confidence.ToString(),
                                        Severity = match.Issue.Rule.Severity.ToString(),
                                        ShortTag = tagItem.Substring(tagItem.LastIndexOf('.') + 1),
                                        StatusIcon = pattern.DetectedIcon,
                                        Detected = true
                                    });

                                    hashSet.Add(tagItem);
                                }
                                else
                                {//ensure we have highest confidence, severity as there are likly multiple matches for this tag pattern
                                    foreach (TagInfo updateItem in result)
                                    {
                                        if (updateItem.Tag == tagItem)
                                        {
                                            RulesEngine.Confidence oldConfidence;
                                            Enum.TryParse(updateItem.Confidence, out oldConfidence);
                                            if (match.Issue.PatternMatch.Confidence > oldConfidence)
                                            {
                                                updateItem.Confidence = match.Issue.PatternMatch.Confidence.ToString();
                                            }

                                            RulesEngine.Severity oldSeverity;
                                            Enum.TryParse(updateItem.Severity, out oldSeverity);
                                            if (match.Issue.Rule.Severity > oldSeverity)
                                            {
                                                updateItem.Severity = match.Issue.Rule.Severity.ToString();
                                            }

                                            break;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

            }

            return result;
        }


        /// <summary>
        /// List of taginfo items ordered by name
        /// </summary>
        /// <returns></returns>
        private List<TagInfo> GetTagInfoListByName()
        {
            List<string> orderedTags = MetaData.UniqueTags.ToList<string>();
            orderedTags.Sort();
            HashSet<string> dupCheck = new HashSet<string>();
            List<TagInfo> result = new List<TagInfo>();

            foreach (string tag in orderedTags)
            {
                foreach (var match in MatchList)
                {
                    foreach (string testTag in match.Issue.Rule.Tags)
                    {
                        if (tag == testTag)
                        {
                            if (dupCheck.Add(testTag))
                            {
                                result.Add(new TagInfo
                                {
                                    Tag = testTag,
                                    Confidence = match.Issue.PatternMatch.Confidence.ToString(),
                                    Severity = match.Issue.Rule.Severity.ToString(),
                                    ShortTag = testTag.Substring(testTag.LastIndexOf('.') + 1),
                                });

                                break;
                            }
                        }
                    }
                }
            }

            return result;
        }



        /// <summary>
        /// Tags sorted by confidence
        /// Todo: address array of tags in rule
        /// </summary>
        /// <returns></returns>
        private List<TagInfo> GetTagInfoListByConfidence()
        {
            List<TagInfo> result = new List<TagInfo>();
            HashSet<string> dupCheck = new HashSet<string>();
            RulesEngine.Confidence[] confidences = { Confidence.High, Confidence.Medium, Confidence.Low };

            foreach (Confidence test in confidences)
            {
                foreach (string tag in MetaData.UniqueTags)
                {
                    var searchPattern = new Regex(tag, RegexOptions.IgnoreCase);
                    foreach (var match in MatchList)
                    {
                        foreach (string testTag in match.Issue.Rule.Tags)
                        {
                            if (searchPattern.IsMatch(testTag))
                            {
                                if (match.Issue.PatternMatch.Confidence == test && dupCheck.Add(tag))
                                    result.Add(new TagInfo
                                    {
                                        Tag = testTag,
                                        Confidence = test.ToString(),
                                        Severity = match.Issue.Rule.Severity.ToString(),
                                        ShortTag = testTag.Substring(testTag.LastIndexOf('.') + 1),
                                    });
                            }
                        }
                    }
                }
            }

            return result;
        }


        /// <summary>
        /// Sorted by Severity
        /// </summary>
        /// <returns></returns>
        private List<TagInfo> GetTagInfoListBySeverity()
        {
            List<TagInfo> result = new List<TagInfo>();
            HashSet<string> dupCheck = new HashSet<string>();
            RulesEngine.Severity[] severities = { Severity.Critical, Severity.Important, Severity.Moderate, Severity.BestPractice, Severity.ManualReview };

            foreach (Severity test in severities)
            {
                foreach (string tag in MetaData.UniqueTags)
                {
                    var searchPattern = new Regex(tag, RegexOptions.IgnoreCase);
                    foreach (var match in MatchList)
                    {
                        foreach (string testTag in match.Issue.Rule.Tags)
                        {
                            if (searchPattern.IsMatch(testTag))
                            {
                                if (match.Issue.Rule.Severity == test && dupCheck.Add(tag))
                                    result.Add(new TagInfo
                                    {
                                        Tag = testTag,
                                        Confidence = match.Issue.PatternMatch.Confidence.ToString(),
                                        Severity = test.ToString(),
                                        ShortTag = testTag.Substring(testTag.LastIndexOf('.') + 1),
                                    });
                            }
                        }
                    }
                }
            }

            return result;
        }

    }

    #endregion


    /// <summary>
    /// Contains meta data elements around the source scanned
    /// Contains rollup data for reporting purposes
    /// </summary>
    public class AppMetaData
    {
        //Multi-list of elements makes it easier to pass to HTML template engine -direct getters also work
        [JsonIgnore]
        private Dictionary<string, string> _propertyTagSearchPatterns;

        [JsonIgnore] //named properties below will handle for serialization
        public Dictionary<string, HashSet<string>> KeyedPropertyLists { get; }


        public AppMetaData(string sourcePath, List<string> rulePaths)
        {
            //Initial value for ApplicationName may be replaced if rule pattern match found later
            if (Directory.Exists(sourcePath))
            {
                try
                {
                    ApplicationName = sourcePath.Substring(sourcePath.LastIndexOf(Path.DirectorySeparatorChar)).Replace(Path.DirectorySeparatorChar, ' ').Trim();
                }
                catch (Exception)
                {
                    ApplicationName = Path.GetFileNameWithoutExtension(sourcePath);
                }
            }
            else
            {
                ApplicationName = Path.GetFileNameWithoutExtension(sourcePath);
            }

            //initialize standard set groups using dynamic lists variables that may have more than one value; some are filled
            //using tag tests and others by different means like file type examination
            KeyedPropertyLists = new Dictionary<string, HashSet<string>>
            {
                ["strGrpRulePaths"] = rulePaths.ToHashSet(),
                ["strGrpPackageTypes"] = new HashSet<string>(),
                ["strGrpAppTypes"] = new HashSet<string>(),
                ["strGrpFileTypes"] = new HashSet<string>(),
                ["strGrpUniqueTags"] = new HashSet<string>(),
                ["strGrpOutputs"] = new HashSet<string>(),
                ["strGrpTargets"] = new HashSet<string>(),
                ["strGrpOSTargets"] = new HashSet<string>(),
                ["strGrpFileExtensions"] = new HashSet<string>(),
                ["strGrpFileNames"] = new HashSet<string>(),
                ["strGrpCPUTargets"] = new HashSet<string>(),
                ["strGrpCloudTargets"] = new HashSet<string>(),
                ["strGrpUniqueDependencies"] = new HashSet<string>()
            };

            //predefined standard tags to track; only some are propertygrouplist are tag based
            _propertyTagSearchPatterns = new Dictionary<string, string>();
            _propertyTagSearchPatterns.Add("strGrpOSTargets", ".OS.Targets");
            _propertyTagSearchPatterns.Add("strGrpCloudTargets", ".Cloud");
            _propertyTagSearchPatterns.Add("strGrpOutputs", ".Outputs");
            _propertyTagSearchPatterns.Add("strGrpCPUTargets", ".CPU");

            //read default/user preferences on what tags to count
            if (File.Exists(Utils.GetPath(Utils.AppPath.tagCounterPref)))
                TagCounters = JsonConvert.DeserializeObject<List<TagCounter>>(File.ReadAllText(Utils.GetPath(Utils.AppPath.tagCounterPref)));
            else
                TagCounters = new List<TagCounter>();

            HashSet<string> dupCountersCheck = new HashSet<string>();
            foreach (TagCounter counter in TagCounters)
            {
                if (!dupCountersCheck.Add(counter.Tag))
                    WriteOnce.SafeLog("Duplidate counter specified in preferences", NLog.LogLevel.Error);
            }

            Languages = new Dictionary<string, int>();
        }


        public void AddLanguage(string language)
        {
            if (Languages.ContainsKey(language))
                Languages[language]++;
            else
                Languages.Add(language, 1);
        }

        //simple properties 
        [JsonProperty(PropertyName = "applicationName")]
        public string ApplicationName { get; set; }
        [JsonProperty(PropertyName = "sourceVersion")]
        public string SourceVersion { get; set; }
        [JsonProperty(PropertyName = "authors")]
        public string Authors { get; set; }
        [JsonProperty(PropertyName = "description")]
        public string Description { get; set; }

        private DateTime _lastUpdated = DateTime.MinValue;
        [JsonProperty(PropertyName = "lastUpdated")]
        public string LastUpdated { get; set; }

        //stats
        [JsonProperty(PropertyName = "filesAnalyzed")]
        public int FilesAnalyzed { get; set; }
        [JsonProperty(PropertyName = "totalFiles")]
        public int TotalFiles { get; set; }
        [JsonProperty(PropertyName = "filesSkipped")]
        public int FilesSkipped { get; set; }
        [JsonProperty(PropertyName = "filesAffected")]
        public int FilesAffected { get; set; }
        //following "counter" methods can not use enumeration on matches list which are unique by default
        [JsonProperty(PropertyName = "totalMatchesCount")]
        public int TotalMatchesCount { get; set; }
        [JsonProperty(PropertyName = "uniqueMatchesCount")]
        public int UniqueMatchesCount { get { return UniqueTags.Count; } }

        //Wrapper getters for serialzation and easy reference of standard properties found in dynamic lists
        [JsonIgnore]
        public List<TagCounterUI> TagCountersUI { get; set; }
        [JsonProperty(PropertyName = "TagCounters")]
        public List<TagCounter> TagCounters { get; }
        [JsonProperty(PropertyName = "packageTypes")]
        public HashSet<string> PackageTypes { get { return KeyedPropertyLists["strGrpPackageTypes"]; } }
        [JsonProperty(PropertyName = "appTypes")]
        public HashSet<string> AppTypes { get { return KeyedPropertyLists["strGrpAppTypes"]; } }
        [JsonIgnore]
        public HashSet<string> RulePaths { get { return KeyedPropertyLists["strGrpRulePaths"]; } set { KeyedPropertyLists["strGrpRulePaths"] = value; } }
        [JsonIgnore]
        public HashSet<string> FileNames { get { return KeyedPropertyLists["strGrpFileNames"]; } }
        [JsonProperty(PropertyName = "uniqueTags")]
        public HashSet<string> UniqueTags
        {
            get
            {
                return KeyedPropertyLists["strGrpUniqueTags"];
            }
            set
            {
                KeyedPropertyLists["strGrpUniqueTags"] = value;
            }
        }

        //convenience getters for standard lists of values
        [JsonProperty(PropertyName = "uniqueDependencies")]
        public HashSet<string> UniqueDependencies { get { return KeyedPropertyLists["strGrpUniqueDependencies"]; } }
        [JsonProperty(PropertyName = "outputs")]
        public HashSet<string> Outputs { get { return KeyedPropertyLists["strGrpOutputs"]; } }
        [JsonProperty(PropertyName = "targets")]
        public HashSet<string> Targets { get { return KeyedPropertyLists["strGrpTargets"]; } }
        [JsonProperty(PropertyName = "languages")]
        public Dictionary<string, int> Languages;
        [JsonProperty(PropertyName = "OSTargets")]
        public HashSet<string> OSTargets { get { return KeyedPropertyLists["strGrpOSTargets"]; } }
        [JsonProperty(PropertyName = "fileExtensions")]
        public HashSet<string> FileExtensions
        { get { return KeyedPropertyLists["strGrpFileExtensions"]; } }
        [JsonProperty(PropertyName = "cloudTargets")]
        public HashSet<string> CloudTargets { get { return KeyedPropertyLists["strGrpCloudTargets"]; } }
        [JsonProperty(PropertyName = "CPUTargets")]
        public HashSet<string> CPUTargets { get { return KeyedPropertyLists["strGrpCPUTargets"]; } }
        private string ExtractValue(string s)
        {
            if (s.ToLower().Contains("</"))
                return ExtractXMLValue(s);
            else
                return ExtractJSONValue(s);
        }

        private string ExtractJSONValue(string s)
        {
            string result = "";
            try
            {
                var parts = s.Split(':');
                var value = parts[1];
                value = value.Replace("\"", "");
                result = value.Trim();

            }
            catch (Exception)
            {
                result = s;
            }

            return System.Net.WebUtility.HtmlEncode(result);
        }


        private string ExtractXMLValue(string s)
        {
            string result = "";
            try
            {
                int firstTag = s.IndexOf(">");
                int endTag = s.IndexOf("</", firstTag);
                var value = s.Substring(firstTag + 1, endTag - firstTag - 1);
                result = value;
            }
            catch (Exception)
            {
                result = s;
            }

            return System.Net.WebUtility.HtmlEncode(result);
        }

        /// <summary>
        /// Part of post processing to test for matches against app defined properties
        /// defined in MetaData class
        /// Exludes a match if specified in preferences as a counted tag with exclude true
        /// </summary>
        /// <param name="matchRecord"></param>
        public bool AddStandardProperties(ref MatchRecord matchRecord)
        {
            bool includeAsMatch = true;

            //testing for presence of a tag against the specified set in preferences for report org 
            foreach (string key in _propertyTagSearchPatterns.Keys)
            {
                var tagPatternRegex = new Regex(_propertyTagSearchPatterns[key], RegexOptions.IgnoreCase);
                if (matchRecord.Issue.Rule.Tags.Any(v => tagPatternRegex.IsMatch(v)))
                {
                    KeyedPropertyLists[key].Add(matchRecord.TextSample);
                }
            }

            // Author etc. or STANDARD METADATA properties we capture from any supported file type; others just captured as general tag matches...
            if (matchRecord.Issue.Rule.Tags.Any(v => v.Contains("Metadata.Application.Author")))
                this.Authors = ExtractValue(matchRecord.TextSample);
            if (matchRecord.Issue.Rule.Tags.Any(v => v.Contains("Metadata.Application.Publisher")))
                this.Authors = ExtractValue(matchRecord.TextSample);
            if (matchRecord.Issue.Rule.Tags.Any(v => v.Contains("Metadata.Application.Description")))
                this.Description = ExtractValue(matchRecord.TextSample);
            if (matchRecord.Issue.Rule.Tags.Any(v => v.Contains("Metadata.Application.Name")))
                this.ApplicationName = ExtractValue(matchRecord.TextSample);
            if (matchRecord.Issue.Rule.Tags.Any(v => v.Contains("Metadata.Application.Version")))
                this.SourceVersion = ExtractValue(matchRecord.TextSample);
            if (matchRecord.Issue.Rule.Tags.Any(v => v.Contains("Metadata.Application.Target.Processor")))
                this.CPUTargets.Add(ExtractValue(matchRecord.TextSample).ToLower());
            if (matchRecord.Issue.Rule.Tags.Any(v => v.Contains("Metadata.Application.Output.Type")))
                this.Outputs.Add(ExtractValue(matchRecord.TextSample).ToLower());
            if (matchRecord.Issue.Rule.Tags.Any(v => v.Contains("Platform.OS")))
                this.OSTargets.Add(ExtractValue(matchRecord.TextSample).ToLower());

            //Special handling; attempt to detect app types...review for multiple pattern rule limitation
            String solutionType = Utils.DetectSolutionType(matchRecord);
            if (!string.IsNullOrEmpty(solutionType))
                AppTypes.Add(solutionType);

            //Update metric counters for default or user specified tags
            foreach (TagCounter counter in TagCounters)
            {
                if (matchRecord.Issue.Rule.Tags.Any(v => v.Contains(counter.Tag)))
                {
                    counter.Count++;
                    includeAsMatch = counter.IncludeAsMatch;//Exclude as feature matches per preferences from reporting full match details
                }
            }

            //once patterns checked; prepare text for output blocking browser xss
            matchRecord.TextSample = System.Net.WebUtility.HtmlEncode(matchRecord.TextSample);

            return includeAsMatch;
        }


        /// <summary>
        /// Opportunity for any final data prep before report gen
        /// </summary>
        public void PrepareReport()
        {
            //TagCountersUI is liquid compatible while TagCounters is not to support json serialization; the split prevents exception
            //not fixable via json iteration disabling
            TagCountersUI = new List<TagCounterUI>();
            foreach (TagCounter counter in TagCounters)
                TagCountersUI.Add(new TagCounterUI
                {
                    Tag = counter.Tag,
                    ShortTag = counter.ShortTag,
                    Count = counter.Count
                });
        }
    }



    /// <summary>
    /// Subset of MatchRecord and Issue properties specific for json output to avoid inclusion of all
    /// MatchRecord properities like rule/pattern subobjects...
    /// </summary>

    public class LimitedMatchRecord
    {
        public LimitedMatchRecord(MatchRecord matchRecord)
        {
            FileName = matchRecord.Filename;
            SourceLabel = matchRecord.Language.Name;
            SourceType = matchRecord.Language.Type.ToString();
            StartLocationLine = matchRecord.Issue.StartLocation.Line;
            StartLocationColumn = matchRecord.Issue.StartLocation.Column;
            EndLocationLine = matchRecord.Issue.EndLocation.Line;
            EndLocationColumn = matchRecord.Issue.EndLocation.Column;
            BoundaryIndex = matchRecord.Issue.Boundary.Index;
            BoundaryLength = matchRecord.Issue.Boundary.Length;
            RuleId = matchRecord.Issue.Rule.Id;
            Severity = matchRecord.Issue.Rule.Severity.ToString();
            RuleName = matchRecord.Issue.Rule.Name;
            RuleDescription = matchRecord.Issue.Rule.Description;
            PatternConfidence = matchRecord.Issue.Confidence.ToString();
            PatternType = matchRecord.Issue.PatternMatch.PatternType.ToString();
            MatchingPattern = matchRecord.Issue.PatternMatch.Pattern;
            Sample = matchRecord.TextSample;
            Excerpt = matchRecord.Excerpt;
            Tags = matchRecord.Issue.Rule.Tags;
        }

        [JsonProperty(PropertyName = "fileName")]
        public string FileName { get; set; }
        [JsonProperty(PropertyName = "ruleId")]
        public string RuleId { get; set; }
        [JsonProperty(PropertyName = "ruleName")]
        public string RuleName { get; set; }
        [JsonProperty(PropertyName = "ruleDescription")]
        public string RuleDescription { get; set; }
        [JsonProperty(PropertyName = "pattern")]
        public string MatchingPattern { get; set; }
        [JsonProperty(PropertyName = "type")]
        public string PatternType { get; set; }
        [JsonProperty(PropertyName = "confidence")]
        public string PatternConfidence { get; set; }
        [JsonProperty(PropertyName = "severity")]
        public string Severity { get; set; }
        [JsonProperty(PropertyName = "tags")]
        public string[] Tags { get; set; }
        [JsonProperty(PropertyName = "sourceLabel")]
        public string SourceLabel { get; set; }
        [JsonProperty(PropertyName = "sourceType")]
        public string SourceType { get; set; }
        [JsonProperty(PropertyName = "sample")]
        public string Sample { get; set; }
        [JsonProperty(PropertyName = "excerpt")]
        public string Excerpt { get; set; }
        [JsonProperty(PropertyName = "startLocationLine")]
        public int StartLocationLine { get; set; }
        [JsonProperty(PropertyName = "startLocationColumn")]
        public int StartLocationColumn { get; set; }
        [JsonProperty(PropertyName = "endLocationLine")]
        public int EndLocationLine { get; set; }
        [JsonProperty(PropertyName = "endLocationColumn")]
        public int EndLocationColumn { get; set; }
        [JsonProperty(PropertyName = "boundaryIndex")]
        public int BoundaryIndex { get; set; }
        [JsonProperty(PropertyName = "boundaryLength")]
        public int BoundaryLength { get; set; }


    }

}