// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using NuGet.Configuration;
using NuGet.Frameworks;
using NuGet.LibraryModel;
using NuGet.Test.Utility;
using NuGet.Versioning;
using Xunit;

namespace NuGet.ProjectModel.Test
{
    public class DependencyGraphSpecTests
    {
        [Fact]
        public void DependencyGraphSpec_GetParents()
        {
            // Arrange
            var json = JObject.Parse(ResourceTestUtility.GetResource("NuGet.ProjectModel.Test.compiler.resources.test1.dg", typeof(DependencyGraphSpecTests)));

            // Act
            var dg = DependencyGraphSpec.Load(json);

            var xParents = dg.GetParents("A55205E7-4D08-4672-8011-0925467CC45F");
            var yParents = dg.GetParents("78A6AD3F-9FA5-47F6-A54E-84B46A48CB2F");
            var zParents = dg.GetParents("44B29B8D-8413-42D2-8DF4-72225659619B");

            // Assert
            Assert.Equal(0, xParents.Count);
            Assert.Equal(1, yParents.Count);
            Assert.Equal("A55205E7-4D08-4672-8011-0925467CC45F", yParents.Single());

            Assert.Equal(1, zParents.Count);
            Assert.Equal("A55205E7-4D08-4672-8011-0925467CC45F", zParents.Single());
        }

        [Fact]
        public void DependencyGraphSpec_ReadFileWithProjects_GetClosures()
        {
            // Arrange
            var json = JObject.Parse(ResourceTestUtility.GetResource("NuGet.ProjectModel.Test.compiler.resources.test1.dg", typeof(DependencyGraphSpecTests)));

            // Act
            var dg = DependencyGraphSpec.Load(json);

            var xClosure = dg.GetClosure("A55205E7-4D08-4672-8011-0925467CC45F").OrderBy(e => e.RestoreMetadata.ProjectUniqueName, StringComparer.Ordinal).ToList();
            var yClosure = dg.GetClosure("78A6AD3F-9FA5-47F6-A54E-84B46A48CB2F").OrderBy(e => e.RestoreMetadata.ProjectUniqueName, StringComparer.Ordinal).ToList();
            var zClosure = dg.GetClosure("44B29B8D-8413-42D2-8DF4-72225659619B").OrderBy(e => e.RestoreMetadata.ProjectUniqueName, StringComparer.Ordinal).ToList();

            // Assert
            Assert.Equal(3, xClosure.Count);
            Assert.Equal("44B29B8D-8413-42D2-8DF4-72225659619B", xClosure[0].RestoreMetadata.ProjectUniqueName);
            Assert.Equal("78A6AD3F-9FA5-47F6-A54E-84B46A48CB2F", xClosure[1].RestoreMetadata.ProjectUniqueName);
            Assert.Equal("A55205E7-4D08-4672-8011-0925467CC45F", xClosure[2].RestoreMetadata.ProjectUniqueName);

            Assert.Equal(1, yClosure.Count);
            Assert.Equal("78A6AD3F-9FA5-47F6-A54E-84B46A48CB2F", yClosure.Single().RestoreMetadata.ProjectUniqueName);

            Assert.Equal(1, zClosure.Count);
            Assert.Equal("44B29B8D-8413-42D2-8DF4-72225659619B", zClosure.Single().RestoreMetadata.ProjectUniqueName);
        }

        [PlatformFact(Platform.Windows)]
        public void DependencyGraphSpec_ReadFileWithProjects_CaseInsensitive_GetClosures()
        {
            // Arrange
            var json = JObject.Parse(ResourceTestUtility.GetResource("NuGet.ProjectModel.Test.compiler.resources.test3.dg", typeof(DependencyGraphSpecTests)));

            // Act
            var dg = DependencyGraphSpec.Load(json);

            var xClosure = dg.GetClosure("A55205E7-4D08-4672-8011-0925467CC45F").OrderBy(e => e.RestoreMetadata.ProjectUniqueName, StringComparer.OrdinalIgnoreCase).ToList();
            var yClosure = dg.GetClosure("78A6AD3F-9FA5-47F6-A54E-84B46A48CB2F").OrderBy(e => e.RestoreMetadata.ProjectUniqueName, StringComparer.OrdinalIgnoreCase).ToList();

            // Assert
            Assert.Equal(3, xClosure.Count);
            Assert.Equal("44B29B8D-8413-42D2-8DF4-72225659619B", xClosure[0].RestoreMetadata.ProjectUniqueName);
            Assert.Equal("78A6AD3F-9FA5-47F6-A54E-84B46A48CB2F", xClosure[1].RestoreMetadata.ProjectUniqueName);
            Assert.Equal("A55205E7-4D08-4672-8011-0925467CC45F", xClosure[2].RestoreMetadata.ProjectUniqueName);

            Assert.Equal(1, yClosure.Count);
            Assert.Equal("78A6AD3F-9FA5-47F6-A54E-84B46A48CB2F", yClosure.Single().RestoreMetadata.ProjectUniqueName);
        }

        [Fact]
        public void DependencyGraphSpec_ProjectsWithToolReferences_GetClosures()
        {
            // Arrange
            var json = JObject.Parse(ResourceTestUtility.GetResource("NuGet.ProjectModel.Test.compiler.resources.test2.dg", typeof(DependencyGraphSpecTests)));
            var childProject = @"f:\validation\test\dg\Project.Core\Project.Core\Project.Core.csproj";
            var parentProject = @"f:\validation\test\dg\Project.Core\Project\Project.csproj";
            var tool = @"atool-netcoreapp2.0-[1.0.0, )";

            // Act
            var dg = DependencyGraphSpec.Load(json);

            var childClosure = dg.GetClosure(childProject).OrderBy(e => e.RestoreMetadata.ProjectUniqueName, StringComparer.Ordinal).ToList();
            var parentClosure = dg.GetClosure(parentProject).OrderBy(e => e.RestoreMetadata.ProjectUniqueName, StringComparer.Ordinal).ToList();
            var toolClosure = dg.GetClosure(tool).OrderBy(e => e.RestoreMetadata.ProjectUniqueName, StringComparer.Ordinal).ToList();

            // Assert
            Assert.Equal(2, parentClosure.Count);
            Assert.Equal(childProject, parentClosure[0].RestoreMetadata.ProjectUniqueName);
            Assert.Equal(parentProject, parentClosure[1].RestoreMetadata.ProjectUniqueName);

            Assert.Equal(1, childClosure.Count);
            Assert.Equal(childProject, childClosure.Single().RestoreMetadata.ProjectUniqueName);

            Assert.Equal(1, toolClosure.Count);
            Assert.Equal(tool, toolClosure.Single().RestoreMetadata.ProjectUniqueName);
        }

        [Fact]
        public void DependencyGraphSpec_ReadEmptyJObject()
        {
            // Arrange
            var json = new JObject();

            // Act
            var dg = new DependencyGraphSpec(json);

            // Assert
            Assert.Equal(json, dg.Json);
            Assert.Equal(0, dg.Restore.Count);
            Assert.Equal(0, dg.Projects.Count);
        }

        [Fact]
        public void DependencyGraphSpec_ReadEmpty()
        {
            // Arrange && Act
            var dg = new DependencyGraphSpec();

            // Assert
            Assert.Equal(0, dg.Json.Properties().Count());
            Assert.Equal(0, dg.Restore.Count);
            Assert.Equal(0, dg.Projects.Count);
        }

        [Theory]
        [InlineData("")]
        [InlineData("[]")]
        public void Load_WithPath_WhenJsonIsInvalid_Throws(string json)
        {
            using (var test = Test.Create(json))
            {
                Assert.Throws<InvalidDataException>(() => DependencyGraphSpec.Load(test.FilePath));
            }
        }

        [Fact]
        public void Load_WithPath_WhenJsonStartsWithComment_SkipsComment()
        {
            var json = @"/*
*/
{
}";

            using (var test = Test.Create(json))
            { 
                DependencyGraphSpec dgSpec = DependencyGraphSpec.Load(test.FilePath);

                Assert.NotNull(dgSpec);
            }
        }

        [Fact]
        public void DependencyGraphSpec_ReadMSBuildMetadata()
        {
            // Arrange
            var json = ResourceTestUtility.GetResource("NuGet.ProjectModel.Test.compiler.resources.project1.json", typeof(DependencyGraphSpecTests));

            // Act
            var spec = JsonPackageSpecReader.GetPackageSpec(json, "x", "c:\\fake\\project.json");
            var msbuildMetadata = spec.RestoreMetadata;

            // Assert
            Assert.NotNull(msbuildMetadata);
            Assert.Equal("A55205E7-4D08-4672-8011-0925467CC45F", msbuildMetadata.ProjectUniqueName);
            Assert.Equal("c:\\x\\x.csproj", msbuildMetadata.ProjectPath);
            Assert.Equal("x", msbuildMetadata.ProjectName);
            Assert.Equal("c:\\x\\project.json", msbuildMetadata.ProjectJsonPath);
            Assert.Equal(ProjectStyle.PackageReference, msbuildMetadata.ProjectStyle);
            Assert.Equal("c:\\packages", msbuildMetadata.PackagesPath);
            Assert.Equal("https://api.nuget.org/v3/index.json", string.Join("|", msbuildMetadata.Sources.Select(s => s.Source)));
            Assert.Equal("c:\\fallback1|c:\\fallback2", string.Join("|", msbuildMetadata.FallbackFolders));
            Assert.Equal("c:\\nuget.config|d:\\nuget.config", string.Join("|", msbuildMetadata.ConfigFilePaths));
            Assert.Equal("44B29B8D-8413-42D2-8DF4-72225659619B|c:\\a\\a.csproj|78A6AD3F-9FA5-47F6-A54E-84B46A48CB2F|c:\\b\\b.csproj", string.Join("|", msbuildMetadata.TargetFrameworks.Single().ProjectReferences.Select(e => $"{e.ProjectUniqueName}|{e.ProjectPath}")));
            Assert.True(msbuildMetadata.CrossTargeting);
            Assert.True(msbuildMetadata.LegacyPackagesDirectory);
        }

        [Fact]
        public void DependencyGraphSpec_ReadMSBuildMetadata_WithProperDefaults()
        {
            // Arrange
            var json = ResourceTestUtility.GetResource("NuGet.ProjectModel.Test.compiler.resources.project2.json", typeof(DependencyGraphSpecTests));

            // Act
            var spec = JsonPackageSpecReader.GetPackageSpec(json, "x", "c:\\fake\\project.json");
            var msbuildMetadata = spec.RestoreMetadata;

            // Assert
            Assert.NotNull(msbuildMetadata);
            Assert.Equal("A55205E7-4D08-4672-8011-0925467CC45F", msbuildMetadata.ProjectUniqueName);
            Assert.Equal("c:\\x\\x.csproj", msbuildMetadata.ProjectPath);
            Assert.Equal("x", msbuildMetadata.ProjectName);
            Assert.Equal("c:\\x\\project.json", msbuildMetadata.ProjectJsonPath);
            Assert.Equal(ProjectStyle.PackageReference, msbuildMetadata.ProjectStyle);
            Assert.Equal("c:\\packages", msbuildMetadata.PackagesPath);
            Assert.Equal("https://api.nuget.org/v3/index.json", string.Join("|", msbuildMetadata.Sources.Select(s => s.Source)));
            Assert.Equal("c:\\fallback1|c:\\fallback2", string.Join("|", msbuildMetadata.FallbackFolders));
            Assert.Equal("c:\\nuget.config|e:\\nuget.config", string.Join("|", msbuildMetadata.ConfigFilePaths));
            Assert.Equal("44B29B8D-8413-42D2-8DF4-72225659619B|c:\\a\\a.csproj|78A6AD3F-9FA5-47F6-A54E-84B46A48CB2F|c:\\b\\b.csproj", string.Join("|", msbuildMetadata.TargetFrameworks.Single().ProjectReferences.Select(e => $"{e.ProjectUniqueName}|{e.ProjectPath}")));
            Assert.False(msbuildMetadata.CrossTargeting);
            Assert.False(msbuildMetadata.LegacyPackagesDirectory);
        }

        [Fact]
        public void DependencyGraphSpec_VerifyMSBuildMetadataObject()
        {
            // Arrange && Act
            var msbuildMetadata = new ProjectRestoreMetadata();

            msbuildMetadata.ProjectUniqueName = "A55205E7-4D08-4672-8011-0925467CC45F";
            msbuildMetadata.ProjectPath = "c:\\x\\x.csproj";
            msbuildMetadata.ProjectName = "x";
            msbuildMetadata.ProjectJsonPath = "c:\\x\\project.json";
            msbuildMetadata.ProjectStyle = ProjectStyle.PackageReference;
            msbuildMetadata.PackagesPath = "c:\\packages";
            msbuildMetadata.Sources = new[] { new PackageSource("https://api.nuget.org/v3/index.json") };

            var tfmGroup = new ProjectRestoreMetadataFrameworkInfo(NuGetFramework.Parse("net45"));

            msbuildMetadata.TargetFrameworks.Add(tfmGroup);

            tfmGroup.ProjectReferences.Add(new ProjectRestoreReference()
            {
                ProjectUniqueName = "44B29B8D-8413-42D2-8DF4-72225659619B",
                ProjectPath = "c:\\a\\a.csproj"
            });

            tfmGroup.ProjectReferences.Add(new ProjectRestoreReference()
            {
                ProjectUniqueName = "78A6AD3F-9FA5-47F6-A54E-84B46A48CB2F",
                ProjectPath = "c:\\b\\b.csproj"
            });

            msbuildMetadata.FallbackFolders.Add("c:\\fallback1");
            msbuildMetadata.FallbackFolders.Add("c:\\fallback2");

            msbuildMetadata.ConfigFilePaths.Add("c:\\nuget.config");
            msbuildMetadata.ConfigFilePaths.Add("d:\\nuget.config");


            // Assert
            Assert.NotNull(msbuildMetadata);
            Assert.Equal("A55205E7-4D08-4672-8011-0925467CC45F", msbuildMetadata.ProjectUniqueName);
            Assert.Equal("c:\\x\\x.csproj", msbuildMetadata.ProjectPath);
            Assert.Equal("x", msbuildMetadata.ProjectName);
            Assert.Equal("c:\\x\\project.json", msbuildMetadata.ProjectJsonPath);
            Assert.Equal(ProjectStyle.PackageReference, msbuildMetadata.ProjectStyle);
            Assert.Equal("c:\\packages", msbuildMetadata.PackagesPath);
            Assert.Equal("https://api.nuget.org/v3/index.json", string.Join("|", msbuildMetadata.Sources.Select(s => s.Source)));
            Assert.Equal("c:\\fallback1|c:\\fallback2", string.Join("|", msbuildMetadata.FallbackFolders));
            Assert.Equal("c:\\nuget.config|d:\\nuget.config", string.Join("|", msbuildMetadata.ConfigFilePaths));
            Assert.Equal("44B29B8D-8413-42D2-8DF4-72225659619B|c:\\a\\a.csproj|78A6AD3F-9FA5-47F6-A54E-84B46A48CB2F|c:\\b\\b.csproj", string.Join("|", msbuildMetadata.TargetFrameworks.Single().ProjectReferences.Select(e => $"{e.ProjectUniqueName}|{e.ProjectPath}")));
        }

        [Fact]
        public void DependencyGraphSpec_RoundTripMSBuildMetadata()
        {
            // Arrange
            var frameworks = new List<TargetFrameworkInformation>();
            frameworks.Add(new TargetFrameworkInformation()
            {
                FrameworkName = NuGetFramework.Parse("net45")
            });

            var spec = new PackageSpec(frameworks);
            spec.Version = NuGetVersion.Parse("24.5.1.2-alpha.1.2+a.b.c");
            var msbuildMetadata = new ProjectRestoreMetadata();
            spec.RestoreMetadata = msbuildMetadata;

            msbuildMetadata.ProjectUniqueName = "A55205E7-4D08-4672-8011-0925467CC45F";
            msbuildMetadata.ProjectPath = "c:\\x\\x.csproj";
            msbuildMetadata.ProjectName = "x";
            msbuildMetadata.ProjectJsonPath = "c:\\x\\project.json";
            msbuildMetadata.ProjectStyle = ProjectStyle.PackageReference;
            msbuildMetadata.PackagesPath = "c:\\packages";
            msbuildMetadata.Sources = new[] { new PackageSource("https://api.nuget.org/v3/index.json") };

            var tfmGroup = new ProjectRestoreMetadataFrameworkInfo(NuGetFramework.Parse("net45"));
            msbuildMetadata.TargetFrameworks.Add(tfmGroup);

            tfmGroup.ProjectReferences.Add(new ProjectRestoreReference()
            {
                ProjectUniqueName = "44B29B8D-8413-42D2-8DF4-72225659619B",
                ProjectPath = "c:\\a\\a.csproj"
            });

            tfmGroup.ProjectReferences.Add(new ProjectRestoreReference()
            {
                ProjectUniqueName = "78A6AD3F-9FA5-47F6-A54E-84B46A48CB2F",
                ProjectPath = "c:\\b\\b.csproj"
            });

            msbuildMetadata.FallbackFolders.Add("c:\\fallback1");
            msbuildMetadata.FallbackFolders.Add("c:\\fallback2");


            msbuildMetadata.ConfigFilePaths.Add("c:\\nuget.config");
            msbuildMetadata.ConfigFilePaths.Add("d:\\nuget.config");

            msbuildMetadata.CrossTargeting = true;
            msbuildMetadata.LegacyPackagesDirectory = true;

            // Act
            var writer = new RuntimeModel.JsonObjectWriter();
            PackageSpecWriter.Write(spec, writer);
            var json = writer.GetJson();
            var readSpec = JsonPackageSpecReader.GetPackageSpec(json, "x", "c:\\fake\\project.json");
            var msbuildMetadata2 = readSpec.RestoreMetadata;

            // Assert
            Assert.NotNull(msbuildMetadata2);
            Assert.Equal("A55205E7-4D08-4672-8011-0925467CC45F", msbuildMetadata2.ProjectUniqueName);
            Assert.Equal("c:\\x\\x.csproj", msbuildMetadata2.ProjectPath);
            Assert.Equal("x", msbuildMetadata2.ProjectName);
            Assert.Equal("c:\\x\\project.json", msbuildMetadata2.ProjectJsonPath);
            Assert.Equal(ProjectStyle.PackageReference, msbuildMetadata2.ProjectStyle);
            Assert.Equal("c:\\packages", msbuildMetadata2.PackagesPath);
            Assert.Equal("https://api.nuget.org/v3/index.json", string.Join("|", msbuildMetadata.Sources.Select(s => s.Source)));
            Assert.Equal("c:\\fallback1|c:\\fallback2", string.Join("|", msbuildMetadata2.FallbackFolders));
            Assert.Equal("c:\\nuget.config|d:\\nuget.config", string.Join("|", msbuildMetadata.ConfigFilePaths));
            Assert.Equal("44B29B8D-8413-42D2-8DF4-72225659619B|c:\\a\\a.csproj|78A6AD3F-9FA5-47F6-A54E-84B46A48CB2F|c:\\b\\b.csproj", string.Join("|", msbuildMetadata2.TargetFrameworks.Single().ProjectReferences.Select(e => $"{e.ProjectUniqueName}|{e.ProjectPath}")));
            Assert.True(msbuildMetadata.CrossTargeting);
            Assert.True(msbuildMetadata.LegacyPackagesDirectory);

            // Verify build metadata is not lost.
            Assert.Equal("24.5.1.2-alpha.1.2+a.b.c", readSpec.Version.ToFullString());
        }

        [Fact]
        public void DependencyGraphSpec_RoundTripMSBuildMetadata_ProjectReferenceFlags()
        {
            // Arrange
            var frameworks = new List<TargetFrameworkInformation>();
            frameworks.Add(new TargetFrameworkInformation()
            {
                FrameworkName = NuGetFramework.Parse("net45")
            });

            var spec = new PackageSpec(frameworks);
            var msbuildMetadata = new ProjectRestoreMetadata();
            spec.RestoreMetadata = msbuildMetadata;

            msbuildMetadata.ProjectUniqueName = "A55205E7-4D08-4672-8011-0925467CC45F";
            msbuildMetadata.ProjectPath = "c:\\x\\x.csproj";
            msbuildMetadata.ProjectName = "x";
            msbuildMetadata.ProjectStyle = ProjectStyle.PackageReference;

            var tfmGroup = new ProjectRestoreMetadataFrameworkInfo(NuGetFramework.Parse("net45"));
            var tfmGroup2 = new ProjectRestoreMetadataFrameworkInfo(NuGetFramework.Parse("netstandard1.3"));

            msbuildMetadata.TargetFrameworks.Add(tfmGroup);
            msbuildMetadata.TargetFrameworks.Add(tfmGroup2);

            var ref1 = new ProjectRestoreReference()
            {
                ProjectUniqueName = "44B29B8D-8413-42D2-8DF4-72225659619B",
                ProjectPath = "c:\\a\\a.csproj",
                IncludeAssets = LibraryIncludeFlags.Build,
                ExcludeAssets = LibraryIncludeFlags.Compile,
                PrivateAssets = LibraryIncludeFlags.Runtime
            };

            var ref2 = new ProjectRestoreReference()
            {
                ProjectUniqueName = "78A6AD3F-9FA5-47F6-A54E-84B46A48CB2F",
                ProjectPath = "c:\\b\\b.csproj"
            };

            tfmGroup.ProjectReferences.Add(ref1);
            tfmGroup.ProjectReferences.Add(ref2);

            tfmGroup2.ProjectReferences.Add(ref1);
            tfmGroup2.ProjectReferences.Add(ref2);

            var writer = new RuntimeModel.JsonObjectWriter();

            // Act
            PackageSpecWriter.Write(spec, writer);
            var json = writer.GetJson();
            var readSpec = JsonPackageSpecReader.GetPackageSpec(json, "x", "c:\\fake\\project.json");

            // Assert
            Assert.Equal(2, readSpec.RestoreMetadata.TargetFrameworks.Count);

            foreach (var framework in readSpec.RestoreMetadata.TargetFrameworks)
            {
                var references = framework.ProjectReferences.OrderBy(e => e.ProjectUniqueName).ToArray();
                Assert.Equal("44B29B8D-8413-42D2-8DF4-72225659619B", references[0].ProjectUniqueName);
                Assert.Equal(LibraryIncludeFlags.Build, references[0].IncludeAssets);
                Assert.Equal(LibraryIncludeFlags.Compile, references[0].ExcludeAssets);
                Assert.Equal(LibraryIncludeFlags.Runtime, references[0].PrivateAssets);

                Assert.Equal("78A6AD3F-9FA5-47F6-A54E-84B46A48CB2F", references[1].ProjectUniqueName);
                Assert.Equal(LibraryIncludeFlags.All, references[1].IncludeAssets);
                Assert.Equal(LibraryIncludeFlags.None, references[1].ExcludeAssets);
                Assert.Equal(LibraryIncludeFlagUtils.DefaultSuppressParent, references[1].PrivateAssets);
            }
        }

        [Fact]
        public void DependencyGraphSpec_Save_SerializesMembersAsJson()
        {
            var expectedJson = ResourceTestUtility.GetResource("NuGet.ProjectModel.Test.compiler.resources.DependencyGraphSpec_Save_SerializesMembersAsJson.json", typeof(DependencyGraphSpecTests));
            var dependencyGraphSpec = CreateDependencyGraphSpec();
            var actualJson = GetJson(dependencyGraphSpec);

            Assert.Equal(expectedJson, actualJson);
        }

        [Fact]
        public void DependencyGraphSpec_Save_SerializesMembersAsJson_CentralVersionDependencies()
        {
            // Arrange
            var expectedJson = ResourceTestUtility.GetResource("NuGet.ProjectModel.Test.compiler.resources.DependencyGraphSpec_CentralVersionDependencies.json", typeof(DependencyGraphSpecTests));

            // Act
            var dependencyGraphSpec = CreateDependencyGraphSpecWithCentralDependencies();
            var actualJson = GetJson(dependencyGraphSpec);

            // Assert
            Assert.Equal(expectedJson, actualJson);
        }

        [Fact]
        public void DependencyGraphSpec_AddProjectWithCentralVersionDependencies_DependenciesAreMergedWhenNullVersion()
        {
            // Arrange
            var dependencyFoo = new LibraryDependency(new LibraryRange("foo", versionRange: null, LibraryDependencyTarget.All),
               LibraryDependencyType.Default,
               LibraryIncludeFlags.All,
               LibraryIncludeFlags.All,
               new List<Common.NuGetLogCode>(),
               autoReferenced: false,
               generatePathProperty: true);
            var dependencyBar = new LibraryDependency(new LibraryRange("bar", VersionRange.Parse("3.0.0"), LibraryDependencyTarget.All),
               LibraryDependencyType.Default,
               LibraryIncludeFlags.All,
               LibraryIncludeFlags.All,
               new List<Common.NuGetLogCode>(),
               autoReferenced: true,
               generatePathProperty: true);
            var dependencyBoom = new LibraryDependency(new LibraryRange("boom", versionRange: null, LibraryDependencyTarget.All),
                LibraryDependencyType.Default,
                LibraryIncludeFlags.All,
                LibraryIncludeFlags.All,
                new List<Common.NuGetLogCode>(),
                autoReferenced: true,
                generatePathProperty: true);
            var centralVersionFoo = new CentralPackageVersion("foo", VersionRange.Parse("1.0.0"));
            var centralVersionBar = new CentralPackageVersion("bar", VersionRange.Parse("2.0.0"));
            var centralVersionBoom = new CentralPackageVersion("boom", VersionRange.Parse("4.0.0"));

            var tfi = CreateTargetFrameworkInformation(new List<LibraryDependency>() { dependencyFoo, dependencyBar, dependencyBoom }, new List<CentralPackageVersion>() { centralVersionFoo, centralVersionBar, centralVersionBoom });

            // Act
            var dependencyGraphSpec = CreateDependencyGraphSpecWithCentralDependencies(tfi);

            // Assert
            Assert.Equal(1, dependencyGraphSpec.Projects.Count);
            var packSpec = dependencyGraphSpec.Projects[0];
            var tfms = packSpec.TargetFrameworks;

            Assert.Equal(1, tfms.Count);
            Assert.Equal(3, tfms[0].Dependencies.Count);
            Assert.Equal("[1.0.0, )", tfms[0].Dependencies.Where( d => d.Name == "foo").First().LibraryRange.VersionRange.ToNormalizedString());
            Assert.True(tfms[0].Dependencies.Where(d => d.Name == "foo").First().VersionCentrallyManaged);

            Assert.Equal("[3.0.0, )", tfms[0].Dependencies.Where(d => d.Name == "bar").First().LibraryRange.VersionRange.ToNormalizedString());
            Assert.False(tfms[0].Dependencies.Where(d => d.Name == "bar").First().VersionCentrallyManaged);

            Assert.Null(tfms[0].Dependencies.Where(d => d.Name == "boom").First().LibraryRange.VersionRange);
        }

        [Fact]
        public void DependencyGraphSpec_AddProjectWithCentralVersionDependencies_DependenciesAreVersionAllWhenNotInCentralVersion()
        {
            // Arrange
            var dependencyFoo = new LibraryDependency(new LibraryRange("foo", versionRange: null, LibraryDependencyTarget.All),
               LibraryDependencyType.Default,
               LibraryIncludeFlags.All,
               LibraryIncludeFlags.All,
               new List<Common.NuGetLogCode>(),
               autoReferenced: false,
               generatePathProperty: true);
            var dependencyBar = new LibraryDependency(new LibraryRange("bar", VersionRange.Parse("3.0.0"), LibraryDependencyTarget.All),
               LibraryDependencyType.Default,
               LibraryIncludeFlags.All,
               LibraryIncludeFlags.All,
               new List<Common.NuGetLogCode>(),
               autoReferenced: false,
               generatePathProperty: true);

            // only a central dependency for bar not for foo
            // foo will be set to VersionRange.All
            var centralVersionBar = new CentralPackageVersion("bar", VersionRange.Parse("2.0.0"));

            var tfi = CreateTargetFrameworkInformation(new List<LibraryDependency>() { dependencyFoo, dependencyBar }, new List<CentralPackageVersion>() { centralVersionBar });

            // Act
            var dependencyGraphSpec = CreateDependencyGraphSpecWithCentralDependencies(tfi);

            // Assert
            var packSpec = dependencyGraphSpec.Projects[0];
            var tfms = packSpec.TargetFrameworks;

            Assert.Equal(1, tfms.Count);
            Assert.Equal(2, tfms[0].Dependencies.Count);
            Assert.Equal("(, )", tfms[0].Dependencies.Where(d => d.Name == "foo").First().LibraryRange.VersionRange.ToNormalizedString());
            Assert.True(tfms[0].Dependencies.Where(d => d.Name == "foo").First().VersionCentrallyManaged);
        }

        private static DependencyGraphSpec CreateDependencyGraphSpec()
        {
            var dgSpec = new DependencyGraphSpec();

            dgSpec.AddRestore("b");
            dgSpec.AddRestore("a");
            dgSpec.AddRestore("c");

            dgSpec.AddProject(new PackageSpec() { RestoreMetadata = new ProjectRestoreMetadata() { ProjectUniqueName = "b" } });
            dgSpec.AddProject(new PackageSpec() { RestoreMetadata = new ProjectRestoreMetadata() { ProjectUniqueName = "a" } });
            dgSpec.AddProject(new PackageSpec() { RestoreMetadata = new ProjectRestoreMetadata() { ProjectUniqueName = "c" } });

            return dgSpec;
        }

        private static DependencyGraphSpec CreateDependencyGraphSpecWithCentralDependencies()
        {
            return CreateDependencyGraphSpecWithCentralDependencies( CreateTargetFrameworkInformation() );
        }

        private static DependencyGraphSpec CreateDependencyGraphSpecWithCentralDependencies( params TargetFrameworkInformation[] tfis)
        {
            var packageSpec = new PackageSpec(tfis);
            packageSpec.RestoreMetadata = new ProjectRestoreMetadata() { ProjectUniqueName = "a", CentralPackageVersionsEnabled = true };
            var dgSpec = new DependencyGraphSpec();
            dgSpec.AddRestore("a");
            dgSpec.AddProject(packageSpec);
            return dgSpec;
        }

        private static TargetFrameworkInformation CreateTargetFrameworkInformation()
        {
            NuGetFramework nugetFramework = new NuGetFramework("net40");
            var dependencyFoo = new LibraryDependency(new LibraryRange("foo", null, LibraryDependencyTarget.All),
                LibraryDependencyType.Default,
                LibraryIncludeFlags.All,
                LibraryIncludeFlags.All,
                new List<Common.NuGetLogCode>(),
                autoReferenced: false,
                generatePathProperty: true);

            var centralVersionFoo = new CentralPackageVersion("foo", VersionRange.Parse("1.0.0"));
            var centralVersionBar = new CentralPackageVersion("bar", VersionRange.Parse("2.0.0"));

            var dependencies = new List<LibraryDependency>() { dependencyFoo };
            var assetTargetFallback = true;
            var warn = false;

            TargetFrameworkInformation tfi = new TargetFrameworkInformation()
            {
                AssetTargetFallback = assetTargetFallback,
                Dependencies = dependencies,
                Warn = warn,
                FrameworkName = nugetFramework,
            };

            tfi.CentralPackageVersions.Add(centralVersionFoo.Name, centralVersionFoo);
            tfi.CentralPackageVersions.Add(centralVersionBar.Name, centralVersionBar);

            return tfi;
        }

        private static TargetFrameworkInformation CreateTargetFrameworkInformation(List<LibraryDependency> dependencies, List<CentralPackageVersion> centralVersionsDependencies)
        {
            NuGetFramework nugetFramework = new NuGetFramework("net40");
                   
            TargetFrameworkInformation tfi = new TargetFrameworkInformation()
            {
                AssetTargetFallback = true,
                Warn = false,
                FrameworkName = nugetFramework,
                Dependencies = dependencies,
            };

            foreach (var cvd in centralVersionsDependencies)
            {
                tfi.CentralPackageVersions.Add(cvd.Name, cvd);
            }

            return tfi;
        }

        private static string GetJson(DependencyGraphSpec dgSpec)
        {
            using (var testDirectory = TestDirectory.Create())
            {
                var filePath = Path.Combine(testDirectory.Path, "out.json");

                dgSpec.Save(filePath);

                return File.ReadAllText(filePath);
            }
        }

        private sealed class Test : IDisposable
        {
            private readonly TestDirectory _directory;
            private bool _isDisposed;

            internal string FilePath { get; }

            private Test(TestDirectory directory, string filePath)
            {
                _directory = directory;
                FilePath = filePath;
            }

            internal static Test Create(string json = null)
            {
                TestDirectory directory = TestDirectory.Create();
                string filePath = Path.Combine(directory.Path, "dg.spec");

                File.WriteAllText(filePath, json ?? string.Empty);

                return new Test(directory, filePath);
            }

            public void Dispose()
            {
                if (!_isDisposed)
                {
                    _directory.Dispose();

                    _isDisposed = true;
                }
            }
        }
    }
}
