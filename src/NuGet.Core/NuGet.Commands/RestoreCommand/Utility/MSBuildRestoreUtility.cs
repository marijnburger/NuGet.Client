// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NuGet.Common;
using NuGet.Configuration;
using NuGet.Frameworks;
using NuGet.LibraryModel;
using NuGet.ProjectModel;
using NuGet.RuntimeModel;
using NuGet.Versioning;

namespace NuGet.Commands
{
    /// <summary>
    /// Helpers for dealing with dg files and processing msbuild related inputs.
    /// </summary>
    public static class MSBuildRestoreUtility
    {
        // Clear keyword for properties.
        public static readonly string Clear = nameof(Clear);
        private static readonly string[] HttpPrefixes = new string[] { "http:", "https:" };
        private const string DoubleSlash = "//";

        /// <summary>
        /// Convert MSBuild items to a DependencyGraphSpec.
        /// </summary>
        public static DependencyGraphSpec GetDependencySpec(IEnumerable<IMSBuildItem> items)
        {
            if (items == null)
            {
                throw new ArgumentNullException(nameof(items));
            }

            // Unique names created by the MSBuild restore target are project paths, these
            // can be different on case-insensitive file systems for the same project file.
            // To workaround this unique names should be compared based on the OS.
            var uniqueNameComparer = PathUtility.GetStringComparerBasedOnOS();

            var graphSpec = new DependencyGraphSpec();
            var itemsById = new Dictionary<string, List<IMSBuildItem>>(uniqueNameComparer);
            var restoreSpecs = new HashSet<string>(uniqueNameComparer);
            var validForRestore = new HashSet<string>(uniqueNameComparer);
            var projectPathLookup = new Dictionary<string, string>(uniqueNameComparer);
            var toolItems = new List<IMSBuildItem>();

            // Sort items and add restore specs
            foreach (var item in items)
            {
                var projectUniqueName = item.GetProperty("ProjectUniqueName");

                if (item.IsType("restorespec"))
                {
                    restoreSpecs.Add(projectUniqueName);
                }
                else if (!string.IsNullOrEmpty(projectUniqueName))
                {
                    List<IMSBuildItem> idItems;
                    if (!itemsById.TryGetValue(projectUniqueName, out idItems))
                    {
                        idItems = new List<IMSBuildItem>(1);
                        itemsById.Add(projectUniqueName, idItems);
                    }

                    idItems.Add(item);
                }
            }

            // Add projects
            var validProjectSpecs = itemsById.Values.Select(GetPackageSpec).Where(e => e != null);

            foreach (var spec in validProjectSpecs)
            {
                // Keep track of all project path casings
                var uniqueName = spec.RestoreMetadata.ProjectUniqueName;
                if (uniqueName != null && !projectPathLookup.ContainsKey(uniqueName))
                {
                    projectPathLookup.Add(uniqueName, uniqueName);
                }

                var projectPath = spec.RestoreMetadata.ProjectPath;
                if (projectPath != null && !projectPathLookup.ContainsKey(projectPath))
                {
                    projectPathLookup.Add(projectPath, projectPath);
                }

                if (spec.RestoreMetadata.ProjectStyle == ProjectStyle.PackageReference
                    || spec.RestoreMetadata.ProjectStyle == ProjectStyle.ProjectJson
                    || spec.RestoreMetadata.ProjectStyle == ProjectStyle.DotnetCliTool
                    || spec.RestoreMetadata.ProjectStyle == ProjectStyle.Standalone
                    || spec.RestoreMetadata.ProjectStyle == ProjectStyle.DotnetToolReference)
                {
                    validForRestore.Add(spec.RestoreMetadata.ProjectUniqueName);
                }

                graphSpec.AddProject(spec);
            }

            // Fix project reference casings to match the original project on case insensitive file systems.
            NormalizePathCasings(projectPathLookup, graphSpec);

            // Remove references to projects that could not be read by restore.
            RemoveMissingProjects(graphSpec);

            // Add valid projects to restore section
            foreach (var projectUniqueName in restoreSpecs.Intersect(validForRestore))
            {
                graphSpec.AddRestore(projectUniqueName);
            }

            return graphSpec;
        }

        /// <summary>
        /// Insert asset flags into dependency, based on ;-delimited string args
        /// </summary>
        public static void ApplyIncludeFlags(LibraryDependency dependency, string includeAssets, string excludeAssets, string privateAssets)
        {
            var includeFlags = GetIncludeFlags(includeAssets, LibraryIncludeFlags.All);
            var excludeFlags = GetIncludeFlags(excludeAssets, LibraryIncludeFlags.None);

            dependency.IncludeType = includeFlags & ~excludeFlags;
            dependency.SuppressParent = GetIncludeFlags(privateAssets, LibraryIncludeFlagUtils.DefaultSuppressParent);
        }

        /// <summary>
        /// Insert asset flags into project dependency, based on ;-delimited string args
        /// </summary>
        public static void ApplyIncludeFlags(ProjectRestoreReference dependency, string includeAssets, string excludeAssets, string privateAssets)
        {
            dependency.IncludeAssets = GetIncludeFlags(includeAssets, LibraryIncludeFlags.All);
            dependency.ExcludeAssets = GetIncludeFlags(excludeAssets, LibraryIncludeFlags.None);
            dependency.PrivateAssets = GetIncludeFlags(privateAssets, LibraryIncludeFlagUtils.DefaultSuppressParent);
        }

        /// <summary>
        /// Convert MSBuild items to a PackageSpec.
        /// </summary>
        public static PackageSpec GetPackageSpec(IEnumerable<IMSBuildItem> items)
        {
            if (items == null)
            {
                throw new ArgumentNullException(nameof(items));
            }

            PackageSpec result = null;

            // There should only be one ProjectSpec per project in the item set, 
            // but if multiple do appear take only the first one in an effort
            // to handle this gracefully.
            var specItem = GetItemByType(items, "projectSpec").FirstOrDefault();

            if (specItem != null)
            {
                var restoreType = GetProjectStyle(specItem);
                var cpvmEnabled = IsCentralVersionsManagementEnabled(specItem);

                // Get base spec
                if (restoreType == ProjectStyle.ProjectJson)
                {
                    result = GetProjectJsonSpec(specItem);
                }
                else
                {
                    // Read msbuild data for both non-nuget and .NET Core
                    result = GetBaseSpec(specItem, restoreType);
                }

                // Applies to all types
                result.RestoreMetadata.ProjectStyle = restoreType;
                result.RestoreMetadata.ProjectPath = specItem.GetProperty("ProjectPath");
                result.RestoreMetadata.ProjectUniqueName = specItem.GetProperty("ProjectUniqueName");

                if (string.IsNullOrEmpty(result.Name))
                {
                    result.Name = result.RestoreMetadata.ProjectName
                        ?? result.RestoreMetadata.ProjectUniqueName
                        ?? Path.GetFileNameWithoutExtension(result.FilePath);
                }

                // Read project references for all
                AddProjectReferences(result, items);

                if (restoreType == ProjectStyle.PackageReference
                    || restoreType == ProjectStyle.Standalone
                    || restoreType == ProjectStyle.DotnetCliTool
                    || restoreType == ProjectStyle.ProjectJson
                    || restoreType == ProjectStyle.DotnetToolReference
                    || restoreType == ProjectStyle.PackagesConfig)
                {

                    foreach (var source in MSBuildStringUtility.Split(specItem.GetProperty("Sources")))
                    {
                        // Fix slashes incorrectly removed by MSBuild 
                        var pkgSource = new PackageSource(FixSourcePath(source));
                        result.RestoreMetadata.Sources.Add(pkgSource);
                    }

                    foreach (var configFilePath in MSBuildStringUtility.Split(specItem.GetProperty("ConfigFilePaths")))
                    {
                        result.RestoreMetadata.ConfigFilePaths.Add(configFilePath);
                    }

                    foreach (var folder in MSBuildStringUtility.Split(specItem.GetProperty("FallbackFolders")))
                    {
                        result.RestoreMetadata.FallbackFolders.Add(folder);
                    }

                    result.RestoreMetadata.PackagesPath = specItem.GetProperty("PackagesPath");
                    result.RestoreMetadata.OutputPath = specItem.GetProperty("OutputPath");
                }

                // Read package references for netcore, tools, and standalone
                if (restoreType == ProjectStyle.PackageReference
                    || restoreType == ProjectStyle.Standalone
                    || restoreType == ProjectStyle.DotnetCliTool
                    || restoreType == ProjectStyle.DotnetToolReference)
                {
                    AddPackageReferences(result, items, cpvmEnabled);
                    AddPackageDownloads(result, items);
                    AddFrameworkReferences(result, items);

                    // Store the original framework strings for msbuild conditionals
                    foreach (var originalFramework in GetFrameworksStrings(specItem))
                    {
                        result.RestoreMetadata.OriginalTargetFrameworks.Add(originalFramework);
                    }
                }

                if (restoreType == ProjectStyle.PackageReference
                    || restoreType == ProjectStyle.Standalone
                    || restoreType == ProjectStyle.DotnetToolReference)
                {
                    // Set project version
                    result.Version = GetVersion(specItem);

                    // Add RIDs and Supports
                    result.RuntimeGraph = GetRuntimeGraph(specItem);

                    // Add Target Framework Specific Properties
                    AddTargetFrameworkSpecificProperties(result, items);

                    // Add PackageTargetFallback
                    AddPackageTargetFallbacks(result, items);

                    // Add CrossTargeting flag
                    result.RestoreMetadata.CrossTargeting = IsPropertyTrue(specItem, "CrossTargeting");

                    // Add RestoreLegacyPackagesDirectory flag
                    result.RestoreMetadata.LegacyPackagesDirectory = IsPropertyTrue(
                        specItem,
                        "RestoreLegacyPackagesDirectory");

                    // ValidateRuntimeAssets compat check
                    result.RestoreMetadata.ValidateRuntimeAssets = IsPropertyTrue(specItem, "ValidateRuntimeAssets");

                    // True for .NETCore projects.
                    result.RestoreMetadata.SkipContentFileWrite = IsPropertyTrue(specItem, "SkipContentFileWrite");

                    // Warning properties
                    result.RestoreMetadata.ProjectWideWarningProperties = GetWarningProperties(specItem);

                    // Packages lock file properties
                    result.RestoreMetadata.RestoreLockProperties = GetRestoreLockProperites(specItem);
                }

                if (restoreType == ProjectStyle.PackagesConfig)
                {
                    var pcRestoreMetadata = (PackagesConfigProjectRestoreMetadata)result.RestoreMetadata;
                    // Packages lock file properties
                    pcRestoreMetadata.PackagesConfigPath = specItem.GetProperty("PackagesConfigPath");
                    pcRestoreMetadata.RepositoryPath = specItem.GetProperty("RepositoryPath");
                    var solutionDir = specItem.GetProperty("SolutionDir");
                    if (string.IsNullOrEmpty(pcRestoreMetadata.RepositoryPath) && !string.IsNullOrEmpty(solutionDir) && solutionDir != "*Undefined*")
                    {
                        pcRestoreMetadata.RepositoryPath = Path.Combine(
                            solutionDir,
                            "packages"
                        );
                    }
                    pcRestoreMetadata.RestoreLockProperties = GetRestoreLockProperites(specItem);

                }

                if (restoreType == ProjectStyle.ProjectJson)
                {
                    // Check runtime assets by default for project.json
                    result.RestoreMetadata.ValidateRuntimeAssets = true;
                }

                result.RestoreMetadata.CentralPackageVersionsEnabled = cpvmEnabled;
            }

            return result;
        }

        /// <summary>
        /// Remove missing project dependencies. These are typically caused by
        /// non-NuGet projects which are missing the targets needed to walk them.
        /// Visual Studio ignores these projects so from the command line we should
        /// also. Build will fail with the appropriate errors for missing projects
        /// restore should not warn or message for this.
        /// </summary>
        public static void RemoveMissingProjects(DependencyGraphSpec graphSpec)
        {
            var existingProjects = new HashSet<string>(
                graphSpec.Projects.Select(e => e.RestoreMetadata.ProjectPath),
                PathUtility.GetStringComparerBasedOnOS());

            foreach (var project in graphSpec.Projects)
            {
                foreach (var framework in project.RestoreMetadata.TargetFrameworks)
                {
                    foreach (var projectReference in framework.ProjectReferences.ToArray())
                    {
                        if (!existingProjects.Contains(projectReference.ProjectPath))
                        {
                            framework.ProjectReferences.Remove(projectReference);
                        }
                    }
                }
            }
        }

        public static void NormalizePathCasings(Dictionary<string, string> paths, DependencyGraphSpec graphSpec)
        {
            NormalizePathCasings((IDictionary<string, string>) paths, graphSpec);
        }

        /// <summary>
        /// Change all project paths to the same casing.
        /// </summary>
        public static void NormalizePathCasings(IDictionary<string, string> paths, DependencyGraphSpec graphSpec)
        {
            if (PathUtility.IsFileSystemCaseInsensitive)
            {
                foreach (var project in graphSpec.Projects)
                {
                    foreach (var framework in project.RestoreMetadata.TargetFrameworks)
                    {
                        foreach (var projectReference in framework.ProjectReferences)
                        {
                            // Check reference unique name
                            var refUniqueName = projectReference.ProjectUniqueName;

                            if (refUniqueName != null
                                && paths.TryGetValue(refUniqueName, out var refUniqueNameCasing)
                                && !StringComparer.Ordinal.Equals(refUniqueNameCasing, refUniqueName))
                            {
                                projectReference.ProjectUniqueName = refUniqueNameCasing;
                            }

                            // Check reference project path
                            var projectRefPath = projectReference.ProjectPath;

                            if (projectRefPath != null
                                && paths.TryGetValue(projectRefPath, out var projectRefPathCasing)
                                && !StringComparer.Ordinal.Equals(projectRefPathCasing, projectRefPath))
                            {
                                projectReference.ProjectPath = projectRefPathCasing;
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// True if the list contains CLEAR.
        /// </summary>
        public static bool ContainsClearKeyword(IEnumerable<string> values)
        {
            return (values?.Contains(Clear, StringComparer.OrdinalIgnoreCase) == true);
        }

        /// <summary>
        /// True if the list contains CLEAR and non-CLEAR keywords.
        /// </summary>
        /// <remarks>CLEAR;CLEAR is considered valid.</remarks>
        public static bool HasInvalidClear(IEnumerable<string> values)
        {
            return ContainsClearKeyword(values)
                    && (values?.Any(e => !StringComparer.OrdinalIgnoreCase.Equals(e, Clear)) == true);
        }

        /// <summary>
        /// Logs an error if CLEAR is used with non-CLEAR entries.
        /// </summary>
        /// <returns>True if an invalid combination exists.</returns>
        public static bool LogErrorForClearIfInvalid(IEnumerable<string> values, string projectPath, ILogger logger)
        {
            if (HasInvalidClear(values))
            {
                var text = string.Format(CultureInfo.CurrentCulture, Strings.CannotBeUsedWithOtherValues, Clear);
                var message = LogMessage.CreateError(NuGetLogCode.NU1002, text);
                message.ProjectPath = projectPath;
                logger.Log(message);

                return true;
            }

            return false;
        }

        /// <summary>
        /// Log warning NU1503
        /// </summary>
        public static RestoreLogMessage GetWarningForUnsupportedProject(string path)
        {
            var text = string.Format(CultureInfo.CurrentCulture, Strings.UnsupportedProject, path);
            var message = RestoreLogMessage.CreateWarning(NuGetLogCode.NU1503, text);
            message.FilePath = path;

            return message;
        }

        private static void AddPackageTargetFallbacks(PackageSpec spec, IEnumerable<IMSBuildItem> items)
        {
            foreach (var item in GetItemByType(items, "TargetFrameworkInformation"))
            {
                var frameworkString = item.GetProperty("TargetFramework");
                var frameworks = new List<TargetFrameworkInformation>();

                if (!string.IsNullOrEmpty(frameworkString))
                {
                    frameworks.Add(spec.GetTargetFramework(NuGetFramework.Parse(frameworkString)));
                }
                else
                {
                    frameworks.AddRange(spec.TargetFrameworks);
                }

                foreach (var targetFrameworkInfo in frameworks)
                {
                    var packageTargetFallback = MSBuildStringUtility.Split(item.GetProperty("PackageTargetFallback"))
                        .Select(NuGetFramework.Parse)
                        .ToList();

                    var assetTargetFallback = MSBuildStringUtility.Split(item.GetProperty(AssetTargetFallbackUtility.AssetTargetFallback))
                        .Select(NuGetFramework.Parse)
                        .ToList();

                    // Throw if an invalid combination was used.
                    AssetTargetFallbackUtility.EnsureValidFallback(packageTargetFallback, assetTargetFallback, spec.FilePath);

                    // Update the framework appropriately
                    AssetTargetFallbackUtility.ApplyFramework(targetFrameworkInfo, packageTargetFallback, assetTargetFallback);
                }
            }
        }

        private static void AddTargetFrameworkSpecificProperties(PackageSpec spec, IEnumerable<IMSBuildItem> items)
        {
            foreach (var item in GetItemByType(items, "TargetFrameworkProperties"))
            {
                var frameworkString = item.GetProperty("TargetFramework");
                var targetFrameworkInformation = spec.GetTargetFramework(NuGetFramework.Parse(frameworkString));
                targetFrameworkInformation.RuntimeIdentifierGraphPath = item.GetProperty("RuntimeIdentifierGraphPath");
            }
        }

        /// <summary>
        /// Remove duplicates and excluded values a set of sources or fallback folders.
        /// </summary>
        /// <remarks>Compares with Ordinal, excludes must be exact matches.</remarks>
        public static IEnumerable<string> AggregateSources(IEnumerable<string> values, IEnumerable<string> excludeValues)
        {
            if (values == null)
            {
                throw new ArgumentNullException(nameof(values));
            }

            if (excludeValues == null)
            {
                throw new ArgumentNullException(nameof(excludeValues));
            }

            var result = new SortedSet<string>(values, StringComparer.Ordinal);

            // Remove excludes
            result.ExceptWith(excludeValues);

            return result;
        }

        private static RuntimeGraph GetRuntimeGraph(IMSBuildItem specItem)
        {
            var runtimes = MSBuildStringUtility.Split(specItem.GetProperty("RuntimeIdentifiers"))
                .Distinct(StringComparer.Ordinal)
                .Select(rid => new RuntimeDescription(rid))
                .ToList();

            var supports = MSBuildStringUtility.Split(specItem.GetProperty("RuntimeSupports"))
                .Distinct(StringComparer.Ordinal)
                .Select(s => new CompatibilityProfile(s))
                .ToList();

            return new RuntimeGraph(runtimes, supports);
        }

        private static void AddProjectReferences(PackageSpec spec, IEnumerable<IMSBuildItem> items)
        {
            // Add groups for each spec framework
            var frameworkGroups = new Dictionary<NuGetFramework, List<ProjectRestoreReference>>();
            foreach (var framework in spec.TargetFrameworks.Select(e => e.FrameworkName).Distinct())
            {
                frameworkGroups.Add(framework, new List<ProjectRestoreReference>());
            }

            var flatReferences = GetItemByType(items, "ProjectReference")
                .Select(GetProjectRestoreReference);

            var comparer = PathUtility.GetStringComparerBasedOnOS();

            // Add project paths
            foreach (var frameworkPair in flatReferences)
            {
                // If no frameworks were given, apply to all
                var addToFrameworks = frameworkPair.Item1.Count == 0
                    ? frameworkGroups.Keys.ToList()
                    : frameworkPair.Item1;

                foreach (var framework in addToFrameworks)
                {
                    List<ProjectRestoreReference> references;
                    if (frameworkGroups.TryGetValue(framework, out references))
                    {
                        // Ensure unique
                        if (!references.Any(e => comparer.Equals(e.ProjectUniqueName, frameworkPair.Item2.ProjectUniqueName)))
                        {
                            references.Add(frameworkPair.Item2);
                        }
                    }
                }
            }

            // Add groups to spec
            foreach (var frameworkPair in frameworkGroups)
            {
                spec.RestoreMetadata.TargetFrameworks.Add(new ProjectRestoreMetadataFrameworkInfo(frameworkPair.Key)
                {
                    ProjectReferences = frameworkPair.Value
                });
            }
        }

        private static Tuple<List<NuGetFramework>, ProjectRestoreReference> GetProjectRestoreReference(IMSBuildItem item)
        {
            var frameworks = GetFrameworks(item).ToList();

            var reference = new ProjectRestoreReference()
            {
                ProjectPath = item.GetProperty("ProjectPath"),
                ProjectUniqueName = item.GetProperty("ProjectReferenceUniqueName"),
            };

            ApplyIncludeFlags(reference, item.GetProperty("IncludeAssets"), item.GetProperty("ExcludeAssets"), item.GetProperty("PrivateAssets"));

            return new Tuple<List<NuGetFramework>, ProjectRestoreReference>(frameworks, reference);
        }

        private static bool AddDownloadDependencyIfNotExist(PackageSpec spec, NuGetFramework framework, DownloadDependency dependency)
        {
            var frameworkInfo = spec.GetTargetFramework(framework);

            if (!frameworkInfo.DownloadDependencies.Contains(dependency))
            {
                frameworkInfo.DownloadDependencies.Add(dependency);

                return true;
            }
            return false;
        }

        private static bool AddDependencyIfNotExist(PackageSpec spec, LibraryDependency dependency)
        {
            foreach (var framework in spec.TargetFrameworks.Select(e => e.FrameworkName))
            {
                AddDependencyIfNotExist(spec, framework, dependency);
            }

            return false;
        }

        private static bool AddDependencyIfNotExist(PackageSpec spec, NuGetFramework framework, LibraryDependency dependency)
        {
            var frameworkInfo = spec.GetTargetFramework(framework);

            if (!spec.Dependencies
                            .Concat(frameworkInfo.Dependencies)
                            .Select(d => d.Name)
                            .Contains(dependency.Name, StringComparer.OrdinalIgnoreCase))
            {
                frameworkInfo.Dependencies.Add(dependency);

                return true;
            }

            return false;
        }

        private static void AddPackageReferences(PackageSpec spec, IEnumerable<IMSBuildItem> items, bool cpvmEnabled)
        {
            var itemsForRestore = GetPackageReferenceItems(items, cpvmEnabled);
            foreach (var item in itemsForRestore)
            {
                var dependency = new LibraryDependency
                {
                    LibraryRange = new LibraryRange(
                        name: item.GetProperty("Id"),
                        versionRange: GetVersionRange(item),
                        typeConstraint: LibraryDependencyTarget.Package),

                    AutoReferenced = IsPropertyTrue(item, "IsImplicitlyDefined"),

                    GeneratePathProperty = IsPropertyTrue(item, "GeneratePathProperty")
                };

                // Add warning suppressions
                foreach (var code in MSBuildStringUtility.GetNuGetLogCodes(item.GetProperty("NoWarn")))
                {
                    dependency.NoWarn.Add(code);
                }

                ApplyIncludeFlags(dependency, item);

                var frameworks = GetFrameworks(item);

                if (frameworks.Count == 0)
                {
                    AddDependencyIfNotExist(spec, dependency);
                }
                else
                {
                    foreach (var framework in frameworks)
                    {
                        AddDependencyIfNotExist(spec, framework, dependency);
                    }
                }
            }

            if (cpvmEnabled)
            {
                AddCentralVersionDependencies(spec, items);
            }
        }

        internal static void AddPackageDownloads(PackageSpec spec, IEnumerable<IMSBuildItem> items)
        {
            var splitChars = new[] { ';' };

            foreach (var item in GetItemByType(items, "DownloadDependency"))
            {
                var id = item.GetProperty("Id");
                var versionString = item.GetProperty("VersionRange");
                var versions = versionString.Split(splitChars, StringSplitOptions.RemoveEmptyEntries);

                foreach (var version in versions)
                {
                    var versionRange = GetVersionRange(version);

                    if (!(versionRange.HasLowerAndUpperBounds && versionRange.MinVersion.Equals(versionRange.MaxVersion)))
                    {
                        throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, Strings.Error_PackageDownload_OnlyExactVersionsAreAllowed, versionRange.OriginalString));
                    }

                    var downloadDependency = new DownloadDependency(id, versionRange);

                    var frameworks = GetFrameworks(item);
                    foreach (var framework in frameworks)
                    {
                        AddDownloadDependencyIfNotExist(spec, framework, downloadDependency);
                    }
                }
            }
        }

        private static void ApplyIncludeFlags(LibraryDependency dependency, IMSBuildItem item)
        {
            ApplyIncludeFlags(dependency, item.GetProperty("IncludeAssets"), item.GetProperty("ExcludeAssets"), item.GetProperty("PrivateAssets"));
        }

        private static LibraryIncludeFlags GetIncludeFlags(string value, LibraryIncludeFlags defaultValue)
        {
            var parts = MSBuildStringUtility.Split(value);

            if (parts.Length > 0)
            {
                return LibraryIncludeFlagUtils.GetFlags(parts);
            }
            else
            {
                return defaultValue;
            }
        }

        private static void AddFrameworkReferences(PackageSpec spec, IEnumerable<IMSBuildItem> items)
        {
            foreach (var item in GetItemByType(items, "FrameworkReference"))
            {
                var frameworkReference = item.GetProperty("Id");
                var frameworks = GetFrameworks(item);

                var privateAssets = item.GetProperty("PrivateAssets");

                foreach (var framework in frameworks)
                {
                    AddFrameworkReferenceIfNotExists(spec, framework, frameworkReference, privateAssets);
                }
            }
        }

        private static bool AddFrameworkReferenceIfNotExists(PackageSpec spec, NuGetFramework framework, string frameworkReference, string privateAssetsValue)
        {
            var frameworkInfo = spec.GetTargetFramework(framework);

            if (!frameworkInfo
                .FrameworkReferences
                .Select(f => f.Name)
                .Contains(frameworkReference, ComparisonUtility.FrameworkReferenceNameComparer))
            {
                var privateAssets = FrameworkDependencyFlagsUtils.GetFlags(MSBuildStringUtility.Split(privateAssetsValue));
                frameworkInfo.FrameworkReferences.Add(new FrameworkDependency(frameworkReference, privateAssets));
                return true;
            }
            return false;
        }

        private static VersionRange GetVersionRange(IMSBuildItem item)
        {
            var rangeString = item.GetProperty("VersionRange");
            return GetVersionRange(rangeString);
        }

        private static VersionRange GetVersionRange(string rangeString)
        {
            if (!string.IsNullOrEmpty(rangeString))
            {
                return VersionRange.Parse(rangeString);
            }

            return VersionRange.All;
        }

        private static PackageSpec GetProjectJsonSpec(IMSBuildItem specItem)
        {
            PackageSpec result;
            var projectPath = specItem.GetProperty("ProjectPath");
            var projectName = Path.GetFileNameWithoutExtension(projectPath);
            var projectJsonPath = specItem.GetProperty("ProjectJsonPath");

            // Read project.json
            result = JsonPackageSpecReader.GetPackageSpec(projectName, projectJsonPath);

            result.RestoreMetadata = new ProjectRestoreMetadata();
            result.RestoreMetadata.ProjectJsonPath = projectJsonPath;
            result.RestoreMetadata.ProjectName = projectName;
            return result;
        }

        private static PackageSpec GetBaseSpec(IMSBuildItem specItem, ProjectStyle projectStyle)
        {
            var frameworkInfo = GetFrameworks(specItem)
                .Select(framework => new TargetFrameworkInformation()
                {
                    FrameworkName = framework
                })
                .ToList();

            var spec = new PackageSpec(frameworkInfo);
            spec.RestoreMetadata = projectStyle == ProjectStyle.PackagesConfig
                ? new PackagesConfigProjectRestoreMetadata()
                : new ProjectRestoreMetadata();
            spec.FilePath = specItem.GetProperty("ProjectPath");
            spec.RestoreMetadata.ProjectName = specItem.GetProperty("ProjectName");

            return spec;
        }

        private static HashSet<NuGetFramework> GetFrameworks(IMSBuildItem item)
        {
            return new HashSet<NuGetFramework>(
                GetFrameworksStrings(item).Select(NuGetFramework.Parse));
        }

        private static HashSet<string> GetFrameworksStrings(IMSBuildItem item)
        {
            var frameworks = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            var frameworksString = item.GetProperty("TargetFrameworks");
            if (!string.IsNullOrEmpty(frameworksString))
            {
                frameworks.UnionWith(MSBuildStringUtility.Split(frameworksString));
            }

            return frameworks;
        }

        private static IEnumerable<IMSBuildItem> GetItemByType(IEnumerable<IMSBuildItem> items, string type)
        {
            return items.Where(e => e.IsType(type));
        }

        private static bool IsType(this IMSBuildItem item, string type)
        {
            return StringComparer.OrdinalIgnoreCase.Equals(type, item?.GetProperty("Type"));
        }

        /// <summary>
        /// Return the parsed version or 1.0.0 if the property does not exist.
        /// </summary>
        private static NuGetVersion GetVersion(IMSBuildItem item)
        {
            var versionString = item.GetProperty("Version");
            NuGetVersion version = null;

            if (string.IsNullOrEmpty(versionString))
            {
                // Default to 1.0.0 if the property does not exist
                version = new NuGetVersion(1, 0, 0);
            }
            else
            {
                // Snapshot versions are not allowed in .NETCore
                version = NuGetVersion.Parse(versionString);
            }

            return version;
        }

        public static void Dump(IEnumerable<IMSBuildItem> items, ILogger log)
        {
            foreach (var item in items)
            {
                log.LogDebug($"Item: {item.Identity}");

                foreach (var key in item.Properties)
                {
                    var val = item.GetProperty(key);

                    if (!string.IsNullOrEmpty(val))
                    {
                        log.LogDebug($"  {key}={val}");
                    }
                }
            }
        }

        private static WarningProperties GetWarningProperties(IMSBuildItem specItem)
        {
            return WarningProperties.GetWarningProperties(
                treatWarningsAsErrors: specItem.GetProperty("TreatWarningsAsErrors"),
                warningsAsErrors: specItem.GetProperty("WarningsAsErrors"),
                noWarn: specItem.GetProperty("NoWarn"));
        }

        private static RestoreLockProperties GetRestoreLockProperites(IMSBuildItem specItem)
        {
            return new RestoreLockProperties(
                specItem.GetProperty("RestorePackagesWithLockFile"),
                specItem.GetProperty("NuGetLockFilePath"),
                IsPropertyTrue(specItem, "RestoreLockedMode"));
        }

        /// <summary>
        /// Convert http:/url to http://url 
        /// If not needed the same path is returned. This is to work around
        /// issues with msbuild dropping slashes from paths on linux and osx.
        /// </summary>
        public static string FixSourcePath(string s)
        {
            var result = s;

            if (result.IndexOf('/') >= -1 && result.IndexOf(DoubleSlash) == -1)
            {
                for (var i = 0; i < HttpPrefixes.Length; i++)
                {
                    result = FixSourcePath(result, HttpPrefixes[i], DoubleSlash);
                }

                // For non-windows machines use file:///
                var fileSlashes = RuntimeEnvironmentHelper.IsWindows ? DoubleSlash : "///";
                result = FixSourcePath(result, "file:", fileSlashes);
            }

            return result;
        }

        private static string FixSourcePath(string s, string prefixWithoutSlashes, string slashes)
        {
            if (s.Length >= (prefixWithoutSlashes.Length + 2)
                && s.StartsWith($"{prefixWithoutSlashes}/", StringComparison.OrdinalIgnoreCase)
                && !s.StartsWith($"{prefixWithoutSlashes}{DoubleSlash}", StringComparison.OrdinalIgnoreCase))
            {
                // original prefix casing + // + rest of the path
                return s.Substring(0, prefixWithoutSlashes.Length) + slashes + s.Substring(prefixWithoutSlashes.Length + 1);
            }

            return s;
        }

        private static bool IsPropertyTrue(IMSBuildItem item, string propertyName)
        {
            return StringComparer.OrdinalIgnoreCase.Equals(item.GetProperty(propertyName), bool.TrueString);
        }

        /// <summary>
        /// Function used to display errors and warnings at the end of restore operation.
        /// The errors and warnings are read from the assets file based on restore result.
        /// </summary>
        /// <param name="lockFile">LockFile generated by preview restore.</param>
        /// <param name="logger">Logger used to display warnings and errors.</param>
        [Obsolete]
        public static Task ReplayWarningsAndErrorsAsync(LockFile lockFile, ILogger logger)
        {
            return ReplayWarningsAndErrorsAsync(lockFile?.LogMessages, logger);
        }

        /// <summary>
        /// Function used to display errors and warnings at the end of restore operation.
        /// The errors and warnings are read from the assets file based on restore result.
        /// </summary>
        /// <param name="messages">Messages to log.</param>
        /// <param name="logger">Logger used to display warnings and errors.</param>
        public static Task ReplayWarningsAndErrorsAsync(IEnumerable<IAssetsLogMessage> messages, ILogger logger)
        {
            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            var logMessages = messages?.Select(m => m.AsRestoreLogMessage()) ??
                              Enumerable.Empty<RestoreLogMessage>();

            return logger.LogMessagesAsync(logMessages);
        }

        private static IEnumerable<IMSBuildItem> MergeCentralPackageVersionsInPackageReferences(IEnumerable<IMSBuildItem> items)
        {
            // Do not touch the IsImplicitlyDefined package references. 
            var explicitlyReferencedPackages = GetItemByType(items, "Dependency")
                   .Where(item => !IsPropertyTrue(item, "IsImplicitlyDefined"))
                   .ToList();

            var implicitlyReferencedPackages = GetItemByType(items, "Dependency")
                   .Where(item => IsPropertyTrue(item, "IsImplicitlyDefined"))
                   .ToList();

            ThrowIfItemsHaveVersion(explicitlyReferencedPackages);

            var centralPackageVersions = GetItemByType(items, "CentralVersionDependency").ToList();

            foreach (var item in explicitlyReferencedPackages)
            {
                var itemTargetedFrameworksStrings = GetFrameworksStrings(item);

                var currentCentralPackageVersions = centralPackageVersions
                    .Where(x => StringComparer.OrdinalIgnoreCase.Equals(x.GetProperty("Id"), item.GetProperty("Id")))
                    .ToList();

                if (centralPackageVersions.Any())
                {
                    // Legacy projects do not have TargetFrameworks.
                    if (itemTargetedFrameworksStrings.Count == 0)
                    {
                        var centralVersionItemOnTFM = currentCentralPackageVersions
                            .Where(gi =>
                            {
                                var globalItemsTFMs = GetFrameworksStrings(gi);
                                return globalItemsTFMs.Count == 0;
                            })
                            .FirstOrDefault();

                        if (centralVersionItemOnTFM != null)
                        {
                            yield return MergeCentralVersionInformation(item, centralVersionItemOnTFM);
                        }
                    }
                    else
                    {
                        foreach (var projectTFM in itemTargetedFrameworksStrings)
                        {
                            var centralVersionItemOnTFM = currentCentralPackageVersions
                                .Where(gi =>
                                {
                                    var globalItemsTFMs = GetFrameworksStrings(gi);
                                    return globalItemsTFMs.Contains(projectTFM);
                                })
                                .FirstOrDefault();

                            if (centralVersionItemOnTFM != null)
                            {
                                yield return MergeCentralVersionInformation(item, centralVersionItemOnTFM);
                            }
                        }
                    }
                }
                else
                {
                    yield return item;
                }           
            }

            foreach (var implicitlyDefinedPackage in implicitlyReferencedPackages)
            {
                yield return implicitlyDefinedPackage;
            }
        }

        private static Dictionary<NuGetFramework, List<CentralVersionDependency>> CreateCentralVersionDependencies(IEnumerable<IMSBuildItem> items,
            IList<TargetFrameworkInformation> specFrameworks)
        {
            var centralVersionDependencies = GetItemByType(items, "CentralVersionDependency").ToList();
            var result = new Dictionary<NuGetFramework, List<CentralVersionDependency>>();

            foreach (var cvd in centralVersionDependencies)
            {
                var tfms = GetFrameworks(cvd);
                var dependency = new CentralVersionDependency(cvd.GetProperty("Id"), GetVersionRange(cvd));

                if (tfms.Count > 0)
                {
                    AddCentralVersionDependency(result, dependency, tfms);
                }
                else
                {
                    AddCentralVersionDependency(result, dependency, specFrameworks.Select(f=>f.FrameworkName));
                }
            }

            return result;
        }

        private static void AddCentralVersionDependency(Dictionary<NuGetFramework, List<CentralVersionDependency>> centralDependencies,
            CentralVersionDependency centralDependency,
            IEnumerable<NuGetFramework> frameworks)
        {
            foreach (var framework in frameworks)
            {
                if (centralDependencies.ContainsKey(framework))
                {
                    centralDependencies[framework].Add(centralDependency);
                }
                else
                {
                    var deps = new List<CentralVersionDependency>() { centralDependency };
                    centralDependencies.Add(framework, deps);
                }
            }
        }

        private static ProjectStyle GetProjectStyle(IMSBuildItem projectSpecItem)
        {
            var typeString = projectSpecItem.GetProperty("ProjectStyle");
            var restoreType = ProjectStyle.Unknown;

            if (!string.IsNullOrEmpty(typeString))
            {
                Enum.TryParse(typeString, ignoreCase: true, result: out restoreType);
            }

            return restoreType;
        }

        internal static bool IsCentralVersionsManagementEnabled(IMSBuildItem projectSpecItem)
        {
            return IsPropertyTrue(projectSpecItem, "CentralPackageVersionsEnabled") && GetProjectStyle(projectSpecItem) == ProjectStyle.PackageReference;
        }

        private static IMSBuildItem MergeCentralVersionInformation(IMSBuildItem item, IMSBuildItem centralVersionItem)
        {
            var metadata = item.Properties
                .Where(p => !StringComparer.Ordinal.Equals("Version") && !StringComparer.Ordinal.Equals("VersionRange"))
                .Select(p => new KeyValuePair<string, string>(p, item.GetProperty(p)))
                .ToDictionary( kvp=>kvp.Key, kvp=>kvp.Value);

            var version = centralVersionItem.GetProperty("Version");
            var versionRange = centralVersionItem.GetProperty("VersionRange");

            if (!string.IsNullOrEmpty(version))
            {
                metadata.Add("Version", version);
            }
            if (!string.IsNullOrEmpty(versionRange))
            {
                metadata.Add("VersionRange", versionRange);
            }

            return new MSBuildItem(item.Identity, metadata);
        }

        private static bool AddCentralVersionDependencyIfNotExist(PackageSpec spec, NuGetFramework framework, CentralVersionDependency centralVersionDependency)
        {
            var frameworkInfo = spec.GetTargetFramework(framework);
            var frameworkDepIds = frameworkInfo.Dependencies.Select(d => d.Name).ToList();
            if (!frameworkInfo.CentralVersionDependencies
                            .Select(d => d.Name)
                            .Contains(centralVersionDependency.Name, StringComparer.OrdinalIgnoreCase))
            {
                // Do not duplicate the item in dependecy and in the central list
                if (!frameworkDepIds.Contains(centralVersionDependency.Name, StringComparer.OrdinalIgnoreCase))
                {
                    frameworkInfo.CentralVersionDependencies.Add(centralVersionDependency);
                }

                return true;
            }

            return false;
        }

        private static void AddCentralVersionDependencies(PackageSpec spec, IEnumerable<IMSBuildItem> items)
        {
            var centralVersionsDependencies = CreateCentralVersionDependencies(items, spec.TargetFrameworks);
            foreach (var framework in centralVersionsDependencies.Keys)
            {
                foreach (var centralVersionsDependency in centralVersionsDependencies[framework])
                {
                    AddCentralVersionDependencyIfNotExist(spec, framework, centralVersionsDependency);
                }
            }
        }

        internal static IEnumerable<IMSBuildItem> GetPackageReferenceItems(IEnumerable<IMSBuildItem> items, bool cpvmEnabled)
        {
            if (cpvmEnabled)
            {        
                return MergeCentralPackageVersionsInPackageReferences(items);
            }

            return GetItemByType(items, "Dependency");
        }

        private static void ThrowIfItemsHaveVersion(IEnumerable<IMSBuildItem> items)
        {
            var builder = new StringBuilder();
            foreach(var item in items)
            {
                var version = item.GetProperty("Version");
                var versionRange = item.GetProperty("VersionRange");
                if (!string.IsNullOrEmpty(version) || !string.IsNullOrEmpty(versionRange))
                {
                    builder.Append(item.GetProperty("Id"));
                }
            }
            if(builder.Length > 0)
            {
                throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, Strings.Error_CentralPackageVersions_VersionsNotAllowed, builder.ToString()));
            }
        }
    }
}
