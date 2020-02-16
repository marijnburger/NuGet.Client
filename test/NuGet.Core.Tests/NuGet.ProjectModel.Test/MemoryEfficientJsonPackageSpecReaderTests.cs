// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using NuGet.Common;
using NuGet.Configuration;
using NuGet.Frameworks;
using NuGet.LibraryModel;
using NuGet.Packaging.Core;
using NuGet.RuntimeModel;
using NuGet.Versioning;
using Xunit;

namespace NuGet.ProjectModel.Test
{
    public class MemoryEfficientJsonPackageSpecReaderTests
    {
        [Fact]
        public void GetPackageSpec_WhenAuthorsPropertyIsAbsent_ReturnsEmptyAuthors()
        {
            PackageSpecs packageSpecs = GetPackageSpecs("{}");

            Assert.Empty(packageSpecs.Improved.Authors);
            Assert.Empty(packageSpecs.Baseline.Authors);
        }

        [Fact]
        public void GetPackageSpec_WhenAuthorsValueIsNull_ReturnsEmptyAuthors()
        {
            PackageSpecs packageSpecs = GetPackageSpecs("{\"authors\":null}");

            Assert.Empty(packageSpecs.Improved.Authors);
            Assert.Empty(packageSpecs.Baseline.Authors);
        }

        [Fact]
        public void GetPackageSpec_WhenAuthorsValueIsString_ReturnsEmptyAuthors()
        {
            PackageSpecs packageSpecs = GetPackageSpecs("{\"authors\":\"b\"}");

            Assert.Empty(packageSpecs.Improved.Authors);
            Assert.Empty(packageSpecs.Baseline.Authors);
        }

        [Theory]
        [InlineData("")]
        [InlineData("/**/")]
        public void GetPackageSpec_WhenAuthorsValueIsEmptyArray_ReturnsEmptyAuthors(string value)
        {
            PackageSpecs packageSpecs = GetPackageSpecs($"{{\"authors\":[{value}]}}");

            Assert.Empty(packageSpecs.Improved.Authors);
            Assert.Empty(packageSpecs.Baseline.Authors);
        }

        [Theory]
        [InlineData("{}")]
        [InlineData("[]")]
        public void GetPackageSpec_WhenAuthorsValueElementIsNotConvertibleToString_Throws(string value)
        {
            var json = $"{{\"authors\":[{value}]}}";

            Assert.Throws<InvalidCastException>(() => GetPackageSpecImproved(json));
            Assert.Throws<InvalidCastException>(() => GetPackageSpecBaseline(json));
        }

        [Theory]
        [InlineData("\"a\"", "a")]
        [InlineData("true", "True")]
        [InlineData("-2", "-2")]
        [InlineData("3.14", "3.14")]
        public void GetPackageSpec_WhenAuthorsValueElementIsConvertibleToString_ReturnsAuthor(string value, string expectedValue)
        {
            PackageSpecs packageSpecs = GetPackageSpecs($"{{\"authors\":[{value}]}}");

            Assert.Collection(packageSpecs.Improved.Authors, author => Assert.Equal(expectedValue, author));
            Assert.Equal(packageSpecs.Baseline.Authors, packageSpecs.Improved.Authors);
        }

        [Fact]
        public void GetPackageSpec_WhenBuildOptionsPropertyIsAbsent_ReturnsNullBuildOptions()
        {
            PackageSpecs packageSpecs = GetPackageSpecs("{}");

            Assert.Null(packageSpecs.Improved.BuildOptions);
            Assert.Null(packageSpecs.Baseline.BuildOptions);
        }

        [Fact]
        public void GetPackageSpec_WhenBuildOptionsValueIsEmptyObject_ReturnsBuildOptions()
        {
            PackageSpecs packageSpecs = GetPackageSpecs("{\"buildOptions\":{}}");

            Assert.NotNull(packageSpecs.Improved.BuildOptions);
            Assert.Null(packageSpecs.Improved.BuildOptions.OutputName);
            Assert.Null(packageSpecs.Baseline.BuildOptions.OutputName);
        }

        [Fact]
        public void GetPackageSpec_WhenBuildOptionsValueOutputNameIsNull_ReturnsNullOutputName()
        {
            PackageSpecs packageSpecs = GetPackageSpecs("{\"buildOptions\":{\"outputName\":null}}");

            Assert.Null(packageSpecs.Improved.BuildOptions.OutputName);
            Assert.Null(packageSpecs.Baseline.BuildOptions.OutputName);
        }

        [Fact]
        public void GetPackageSpec_WhenBuildOptionsValueOutputNameIsValid_ReturnsOutputName()
        {
            const string expectedResult = "a";

            var json = $"{{\"buildOptions\":{{\"outputName\":\"{expectedResult}\"}}}}";

            PackageSpecs packageSpecs = GetPackageSpecs(json);

            Assert.Equal(expectedResult, packageSpecs.Improved.BuildOptions.OutputName);
            Assert.Equal(expectedResult, packageSpecs.Baseline.BuildOptions.OutputName);
        }

        [Theory]
        [InlineData("-2", "-2")]
        [InlineData("3.14", "3.14")]
        [InlineData("true", "True")]
        public void GetPackageSpec_WhenBuildOptionsValueOutputNameIsConvertibleToString_ReturnsOutputName(string outputName, string expectedValue)
        {
            PackageSpecs packageSpecs = GetPackageSpecs($"{{\"buildOptions\":{{\"outputName\":{outputName}}}}}");

            Assert.Equal(expectedValue, packageSpecs.Improved.BuildOptions.OutputName);
            Assert.Equal(expectedValue, packageSpecs.Baseline.BuildOptions.OutputName);
        }

        [Fact]
        public void GetPackageSpec_WhenContentFilesPropertyIsAbsent_ReturnsEmptyContentFiles()
        {
            PackageSpecs packageSpecs = GetPackageSpecs("{}");

            Assert.Empty(packageSpecs.Improved.ContentFiles);
            Assert.Empty(packageSpecs.Baseline.ContentFiles);
        }

        [Fact]
        public void GetPackageSpec_WhenContentFilesValueIsNull_ReturnsEmptyContentFiles()
        {
            PackageSpecs packageSpecs = GetPackageSpecs("{\"contentFiles\":null}");

            Assert.Empty(packageSpecs.Improved.ContentFiles);
            Assert.Empty(packageSpecs.Baseline.ContentFiles);
        }

        [Fact]
        public void GetPackageSpec_WhenContentFilesValueIsString_ReturnsEmptyContentFiles()
        {
            PackageSpecs packageSpecs = GetPackageSpecs("{\"contentFiles\":\"a\"}");

            Assert.Empty(packageSpecs.Improved.ContentFiles);
            Assert.Empty(packageSpecs.Baseline.ContentFiles);
        }

        [Theory]
        [InlineData("")]
        [InlineData("/**/")]
        public void GetPackageSpec_WhenContentFilesValueIsEmptyArray_ReturnsEmptyContentFiles(string value)
        {
            PackageSpecs packageSpecs = GetPackageSpecs($"{{\"contentFiles\":[{value}]}}");

            Assert.Empty(packageSpecs.Improved.ContentFiles);
            Assert.Empty(packageSpecs.Baseline.ContentFiles);
        }

        [Theory]
        [InlineData("{}")]
        [InlineData("[]")]
        public void GetPackageSpec_WhenContentFilesValueElementIsNotConvertibleToString_Throws(string value)
        {
            var json = $"{{\"contentFiles\":[{value}]}}";

            AssertEquivalentExceptionTypes<InvalidCastException>(json);
        }

        [Theory]
        [InlineData("\"a\"", "a")]
        [InlineData("true", "True")]
        [InlineData("-2", "-2")]
        [InlineData("3.14", "3.14")]
        public void GetPackageSpec_WhenContentFilesValueElementIsConvertibleToString_ReturnsContentFile(string value, string expectedValue)
        {
            PackageSpecs packageSpecs = GetPackageSpecs($"{{\"contentFiles\":[{value}]}}");

            Assert.Collection(packageSpecs.Improved.ContentFiles, contentFile => Assert.Equal(expectedValue, contentFile));
            Assert.Equal(packageSpecs.Baseline.ContentFiles, packageSpecs.Improved.ContentFiles);
        }

        [Fact]
        public void GetPackageSpec_WhenCopyrightPropertyIsAbsent_ReturnsNullCopyright()
        {
            PackageSpecs packageSpecs = GetPackageSpecs("{}");

            Assert.Null(packageSpecs.Improved.Copyright);
            Assert.Null(packageSpecs.Baseline.Copyright);
        }

        [Fact]
        public void GetPackageSpec_WhenCopyrightValueIsNull_ReturnsNullCopyright()
        {
            PackageSpecs packageSpecs = GetPackageSpecs("{\"copyright\":null}");

            Assert.Null(packageSpecs.Improved.Copyright);
            Assert.Null(packageSpecs.Baseline.Copyright);
        }

        [Fact]
        public void GetPackageSpec_WhenCopyrightValueIsString_ReturnsCopyright()
        {
            const string expectedResult = "a";

            PackageSpecs packageSpecs = GetPackageSpecs($"{{\"copyright\":\"{expectedResult}\"}}");

            Assert.Equal(expectedResult, packageSpecs.Improved.Copyright);
            Assert.Equal(expectedResult, packageSpecs.Baseline.Copyright);
        }

        [Theory]
        [InlineData("\"a\"", "a")]
        [InlineData("true", "True")]
        [InlineData("-2", "-2")]
        [InlineData("3.14", "3.14")]
        public void GetPackageSpec_WhenCopyrightValueIsConvertibleToString_ReturnsCopyright(string value, string expectedValue)
        {
            PackageSpecs packageSpecs = GetPackageSpecs($"{{\"copyright\":{value}}}");

            Assert.Equal(expectedValue, packageSpecs.Improved.Copyright);
            Assert.Equal(expectedValue, packageSpecs.Baseline.Copyright);
        }

        [Fact]
        public void GetPackageSpec_WhenDependenciesPropertyIsAbsent_ReturnsEmptyDependencies()
        {
            PackageSpecs packageSpecs = GetPackageSpecs("{}");

            Assert.Empty(packageSpecs.Improved.Dependencies);
            Assert.Empty(packageSpecs.Baseline.Dependencies);
        }

        [Fact]
        public void GetPackageSpec_WhenDependenciesValueIsNull_ReturnsEmptyDependencies()
        {
            PackageSpecs packageSpecs = GetPackageSpecs("{\"dependencies\":null}");

            Assert.Empty(packageSpecs.Improved.Dependencies);
            Assert.Empty(packageSpecs.Baseline.Dependencies);
        }

        [Fact]
        public void GetPackageSpec_WhenDependenciesDependencyNameIsEmptyString_Throws()
        {
            const string json = "{\"dependencies\":{\"\":{}}}";

            AssertEquivalentFileFormatExceptions(json);
        }

        [Fact]
        public void GetPackageSpec_WhenDependenciesDependencyValueIsVersionString_ReturnsDependencyVersionRange()
        {
            var expectedResult = new LibraryRange(
                name: "a",
                new VersionRange(new NuGetVersion("1.2.3")),
                LibraryDependencyTarget.All & ~LibraryDependencyTarget.Reference);
            var json = $"{{\"dependencies\":{{\"{expectedResult.Name}\":\"{expectedResult.VersionRange.ToLegacyShortString()}\"}}}}";

            Dependencies dependencies = GetDependencies(json);

            Assert.Equal(expectedResult, dependencies.Improved.LibraryRange);
            Assert.Equal(expectedResult, dependencies.Baseline.LibraryRange);
        }

        [Fact]
        public void GetPackageSpec_WhenDependenciesDependencyValueIsVersionRangeString_ReturnsDependencyVersionRange()
        {
            var expectedResult = new LibraryRange(
                name: "a",
                new VersionRange(new NuGetVersion("1.2.3"), includeMinVersion: true, new NuGetVersion("4.5.6"), includeMaxVersion: false),
                LibraryDependencyTarget.All & ~LibraryDependencyTarget.Reference);
            var json = $"{{\"dependencies\":{{\"{expectedResult.Name}\":\"{expectedResult.VersionRange}\"}}}}";

            Dependencies dependencies = GetDependencies(json);

            Assert.Equal(expectedResult, dependencies.Improved.LibraryRange);
            Assert.Equal(expectedResult, dependencies.Baseline.LibraryRange);
        }

        [Theory]
        [InlineData(LibraryDependencyTarget.None)]
        [InlineData(LibraryDependencyTarget.Assembly)]
        [InlineData(LibraryDependencyTarget.Reference)]
        [InlineData(LibraryDependencyTarget.WinMD)]
        [InlineData(LibraryDependencyTarget.All)]
        [InlineData(LibraryDependencyTarget.PackageProjectExternal)]
        public void GetPackageSpec_WhenDependenciesDependencyTargetIsUnsupported_Throws(LibraryDependencyTarget target)
        {
            var json = $"{{\"dependencies\":{{\"a\":{{\"version\":\"1.2.3\",\"target\":\"{target}\"}}}}}}";

            AssertEquivalentFileFormatExceptions(json);
        }

        [Fact]
        public void GetPackageSpec_WhenDependenciesDependencyAutoreferencedPropertyIsAbsent_ReturnsFalseAutoreferenced()
        {
            Dependencies dependencies = GetDependencies($"{{\"dependencies\":{{\"a\":{{\"target\":\"Project\"}}}}}}");

            Assert.False(dependencies.Improved.AutoReferenced);
            Assert.False(dependencies.Baseline.AutoReferenced);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void GetPackageSpec_WhenDependenciesDependencyAutoreferencedValueIsBool_ReturnsBoolAutoreferenced(bool expectedValue)
        {
            var json = $"{{\"dependencies\":{{\"a\":{{\"autoReferenced\":{expectedValue.ToString().ToLower()},\"target\":\"Project\"}}}}}}";

            Dependencies dependencies = GetDependencies(json);

            Assert.Equal(expectedValue, dependencies.Improved.AutoReferenced);
            Assert.Equal(expectedValue, dependencies.Baseline.AutoReferenced);
        }

        [Theory]
        [InlineData("exclude")]
        [InlineData("include")]
        [InlineData("suppressParent")]
        [InlineData("type")]
        public void GetPackageSpec_WhenDependenciesDependencyValueIsArray_Throws(string propertyName)
        {
            var json = $"{{\"dependencies\":{{\"a\":{{\"{propertyName}\":[\"b\"]}}}}}}";

            AssertEquivalentExceptionTypes<InvalidCastException>(json);
        }

        [Fact]
        public void GetPackageSpec_WhenDependenciesDependencyIncludeAndExcludePropertiesAreAbsent_ReturnsAllIncludeType()
        {
            const string json = "{\"dependencies\":{\"a\":{\"version\":\"1.0.0\"}}}";

            Dependencies dependencies = GetDependencies(json);

            Assert.Equal(LibraryIncludeFlags.All, dependencies.Improved.IncludeType);
            Assert.Equal(LibraryIncludeFlags.All, dependencies.Baseline.IncludeType);
        }

        [Theory]
        [InlineData("\"Native\"", LibraryIncludeFlags.Native)]
        [InlineData("\"Analyzers, Native\"", LibraryIncludeFlags.Analyzers | LibraryIncludeFlags.Native)]
        public void GetPackageSpec_WhenDependenciesDependencyExcludeValueIsValid_ReturnsIncludeType(
            string value,
            LibraryIncludeFlags result)
        {
            var json = $"{{\"dependencies\":{{\"a\":{{\"exclude\":{value},\"version\":\"1.0.0\"}}}}}}";

            Dependencies dependencies = GetDependencies(json);

            Assert.Equal(LibraryIncludeFlags.All & ~result, dependencies.Improved.IncludeType);
            Assert.Equal(dependencies.Baseline.IncludeType, dependencies.Improved.IncludeType);
        }

        [Theory]
        [InlineData("\"Native\"", LibraryIncludeFlags.Native)]
        [InlineData("\"Analyzers, Native\"", LibraryIncludeFlags.Analyzers | LibraryIncludeFlags.Native)]
        public void GetPackageSpec_WhenDependenciesDependencyIncludeValueIsValid_ReturnsIncludeType(
            string value,
            LibraryIncludeFlags expectedResult)
        {
            var json = $"{{\"dependencies\":{{\"a\":{{\"include\":{value},\"version\":\"1.0.0\"}}}}}}";

            Dependencies dependencies = GetDependencies(json);

            Assert.Equal(expectedResult, dependencies.Improved.IncludeType);
            Assert.Equal(expectedResult, dependencies.Baseline.IncludeType);
        }

        [Fact]
        public void GetPackageSpec_WhenDependenciesDependencyIncludeValueOverridesTypeValue_ReturnsIncludeType()
        {
            const string json = "{\"dependencies\":{\"a\":{\"include\":\"ContentFiles\",\"type\":\"BecomesNupkgDependency, SharedFramework\",\"version\":\"1.0.0\"}}}";

            Dependencies dependencies = GetDependencies(json);

            Assert.Equal(LibraryIncludeFlags.ContentFiles, dependencies.Improved.IncludeType);
            Assert.Equal(LibraryIncludeFlags.ContentFiles, dependencies.Baseline.IncludeType);
        }

        [Fact]
        public void GetPackageSpec_WhenDependenciesDependencySuppressParentValueOverridesTypeValue_ReturnsSuppressParent()
        {
            const string json = "{\"dependencies\":{\"a\":{\"suppressParent\":\"ContentFiles\",\"type\":\"SharedFramework\",\"version\":\"1.0.0\"}}}";

            Dependencies dependencies = GetDependencies(json);

            Assert.Equal(LibraryIncludeFlags.ContentFiles, dependencies.Improved.SuppressParent);
            Assert.Equal(LibraryIncludeFlags.ContentFiles, dependencies.Baseline.SuppressParent);
        }

        [Fact]
        public void GetPackageSpec_WhenDependenciesDependencySuppressParentPropertyIsAbsent_ReturnsSuppressParent()
        {
            const string json = "{\"dependencies\":{\"a\":{\"version\":\"1.0.0\"}}}";

            Dependencies dependencies = GetDependencies(json);

            Assert.Equal(LibraryIncludeFlagUtils.DefaultSuppressParent, dependencies.Improved.SuppressParent);
            Assert.Equal(LibraryIncludeFlagUtils.DefaultSuppressParent, dependencies.Baseline.SuppressParent);
        }

        [Theory]
        [InlineData("\"Compile\"", LibraryIncludeFlags.Compile)]
        [InlineData("\"Analyzers, Compile\"", LibraryIncludeFlags.Analyzers | LibraryIncludeFlags.Compile)]
        public void GetPackageSpec_WhenDependenciesDependencySuppressParentValueIsValid_ReturnsSuppressParent(
            string value,
            LibraryIncludeFlags expectedResult)
        {
            var json = $"{{\"dependencies\":{{\"a\":{{\"suppressParent\":{value},\"version\":\"1.0.0\"}}}}}}";

            Dependencies dependencies = GetDependencies(json);

            Assert.Equal(expectedResult, dependencies.Improved.SuppressParent);
            Assert.Equal(expectedResult, dependencies.Baseline.SuppressParent);
        }

        [Fact]
        public void GetPackageSpec_WhenDependenciesDependencyVersionValueIsInvalid_Throws()
        {
            const string json = "{\"dependencies\":{\"a\":{\"version\":\"b\"}}}";

            AssertEquivalentFileFormatExceptions(json);
        }

        [Fact]
        public void GetPackageSpec_WhenDependenciesDependencyTargetPropertyIsAbsent_ReturnsTarget()
        {
            const string json = "{\"dependencies\":{\"a\":{\"version\":\"1.0.0\"}}}";

            Dependencies dependencies = GetDependencies(json);

            Assert.Equal(LibraryDependencyTarget.All & ~LibraryDependencyTarget.Reference, dependencies.Improved.LibraryRange.TypeConstraint);
            Assert.Equal(dependencies.Baseline.LibraryRange.TypeConstraint, dependencies.Improved.LibraryRange.TypeConstraint);
        }

        [Fact]
        public void GetPackageSpec_WhenDependenciesDependencyTargetValueIsPackageAndVersionPropertyIsAbsent_Throws()
        {
            const string json = "{\"dependencies\":{\"a\":{\"target\":\"Package\"}}}";

            AssertEquivalentFileFormatExceptions(json);
        }

        [Fact]
        public void GetPackageSpec_WhenDependenciesDependencyTargetValueIsProjectAndVersionPropertyIsAbsent_ReturnsAllVersionRange()
        {
            Dependencies dependencies = GetDependencies("{\"dependencies\":{\"a\":{\"target\":\"Project\"}}}");

            Assert.Equal(VersionRange.All, dependencies.Improved.LibraryRange.VersionRange);
            Assert.Equal(VersionRange.All, dependencies.Baseline.LibraryRange.VersionRange);
        }

        [Fact]
        public void GetPackageSpec_WhenDependenciesDependencyNoWarnPropertyIsAbsent_ReturnsEmptyNoWarns()
        {
            const string json = "{\"dependencies\":{\"a\":{\"version\":\"1.0.0\"}}}";

            Dependencies dependencies = GetDependencies(json);

            Assert.Empty(dependencies.Improved.NoWarn);
            Assert.Empty(dependencies.Baseline.NoWarn);
        }

        [Fact]
        public void GetPackageSpec_WhenDependenciesDependencyNoWarnValueIsValid_ReturnsNoWarns()
        {
            NuGetLogCode[] expectedResults = { NuGetLogCode.NU1000, NuGetLogCode.NU3000 };
            var json = $"{{\"dependencies\":{{\"a\":{{\"noWarn\":[\"{expectedResults[0].ToString()}\",\"{expectedResults[1].ToString()}\"],\"version\":\"1.0.0\"}}}}}}";

            Dependencies dependencies = GetDependencies(json);

            Assert.Collection(
                dependencies.Improved.NoWarn,
                noWarn => Assert.Equal(expectedResults[0], noWarn),
                noWarn => Assert.Equal(expectedResults[1], noWarn));
            Assert.Equal(dependencies.Baseline.NoWarn, dependencies.Improved.NoWarn);
        }

        [Fact]
        public void GetPackageSpec_WhenDependenciesDependencyGeneratePathPropertyPropertyIsAbsent_ReturnsFalseGeneratePathProperty()
        {
            const string json = "{\"dependencies\":{\"a\":{\"version\":\"1.0.0\"}}}";

            Dependencies dependencies = GetDependencies(json);

            Assert.False(dependencies.Improved.GeneratePathProperty);
            Assert.False(dependencies.Baseline.GeneratePathProperty);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void GetPackageSpec_WhenDependenciesDependencyGeneratePathPropertyValueIsValid_ReturnsGeneratePathProperty(bool expectedResult)
        {
            var json = $"{{\"dependencies\":{{\"a\":{{\"generatePathProperty\":{expectedResult.ToString().ToLowerInvariant()},\"version\":\"1.0.0\"}}}}}}";

            Dependencies dependencies = GetDependencies(json);

            Assert.Equal(expectedResult, dependencies.Improved.GeneratePathProperty);
            Assert.Equal(expectedResult, dependencies.Baseline.GeneratePathProperty);
        }

        [Fact]
        public void GetPackageSpec_WhenDependenciesDependencyTypePropertyIsAbsent_ReturnsDefaultTypeConstraint()
        {
            const string json = "{\"dependencies\":{\"a\":{\"version\":\"1.0.0\"}}}";

            Dependencies dependencies = GetDependencies(json);

            Assert.Equal(
                LibraryDependencyTarget.All & ~LibraryDependencyTarget.Reference,
                dependencies.Improved.LibraryRange.TypeConstraint);
            Assert.Equal(
                dependencies.Baseline.LibraryRange.TypeConstraint,
                dependencies.Improved.LibraryRange.TypeConstraint);
        }

        [Fact]
        public void GetPackageSpec_WhenDescriptionPropertyIsAbsent_ReturnsNullDescription()
        {
            PackageSpecs packageSpecs = GetPackageSpecs("{}");

            Assert.Null(packageSpecs.Improved.Description);
            Assert.Null(packageSpecs.Baseline.Description);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("b")]
        public void GetPackageSpec_WhenDescriptionValueIsValid_ReturnsDescription(string expectedResult)
        {
            string description = expectedResult == null ? "null" : $"\"{expectedResult}\"";
            PackageSpecs packageSpecs = GetPackageSpecs($"{{\"description\":{description}}}");

            Assert.Equal(expectedResult, packageSpecs.Improved.Description);
            Assert.Equal(expectedResult, packageSpecs.Baseline.Description);
        }

        [Fact]
        public void GetPackageSpec_WhenLanguagePropertyIsAbsent_ReturnsNullLanguage()
        {
            PackageSpecs packageSpecs = GetPackageSpecs("{}");

            Assert.Null(packageSpecs.Improved.Language);
            Assert.Null(packageSpecs.Baseline.Language);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("b")]
        public void GetPackageSpec_WhenLanguageValueIsValid_ReturnsLanguage(string expectedResult)
        {
            string language = expectedResult == null ? "null" : $"\"{expectedResult}\"";
            PackageSpecs packageSpecs = GetPackageSpecs($"{{\"language\":{language}}}");

            Assert.Equal(expectedResult, packageSpecs.Improved.Language);
            Assert.Equal(expectedResult, packageSpecs.Baseline.Language);
        }

        [Fact]
        public void GetPackageSpec_WhenFrameworksPropertyIsAbsent_ReturnsEmptyFrameworks()
        {
            PackageSpecs packageSpecs = GetPackageSpecs("{}");

            Assert.Empty(packageSpecs.Improved.TargetFrameworks);
            Assert.Empty(packageSpecs.Baseline.TargetFrameworks);
        }

        [Fact]
        public void GetPackageSpec_WhenFrameworksValueIsEmptyObject_ReturnsEmptyFrameworks()
        {
            PackageSpecs packageSpecs = GetPackageSpecs("{\"frameworks\":{}}");

            Assert.Empty(packageSpecs.Improved.TargetFrameworks);
            Assert.Empty(packageSpecs.Baseline.TargetFrameworks);
        }

        [Fact]
        public void GetPackageSpec_WhenFrameworksAssetTargetFallbackPropertyIsAbsent_ReturnsFalseAssetTargetFallback()
        {
            Frameworks frameworks = GetFrameworks("{\"frameworks\":{\"a\":{}}}");

            Assert.False(frameworks.Improved.AssetTargetFallback);
            Assert.False(frameworks.Baseline.AssetTargetFallback);
            Assert.Equal(frameworks.Baseline, frameworks.Improved);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void GetPackageSpec_WhenFrameworksAssetTargetFallbackValueIsValid_ReturnsAssetTargetFallback(bool expectedValue)
        {
            var json = $"{{\"frameworks\":{{\"a\":{{\"assetTargetFallback\":{expectedValue.ToString().ToLowerInvariant()}}}}}}}";

            Frameworks frameworks = GetFrameworks(json);

            Assert.Equal(expectedValue, frameworks.Improved.AssetTargetFallback);
            Assert.Equal(expectedValue, frameworks.Baseline.AssetTargetFallback);
            Assert.Equal(frameworks.Baseline, frameworks.Improved);
        }

        [Fact]
        public void GetPackageSpec_WhenFrameworksDependenciesPropertyIsAbsent_ReturnsEmptyDependencies()
        {
            Frameworks frameworks = GetFrameworks("{\"frameworks\":{\"a\":{}}}");

            Assert.Empty(frameworks.Improved.Dependencies);
            Assert.Empty(frameworks.Baseline.Dependencies);
            Assert.Equal(frameworks.Baseline, frameworks.Improved);
        }

        [Fact]
        public void GetPackageSpec_WhenFrameworksDependenciesValueIsNull_ReturnsEmptyDependencies()
        {
            Frameworks frameworks = GetFrameworks("{\"frameworks\":{\"a\":{\"dependencies\":null}}}");

            Assert.Empty(frameworks.Improved.Dependencies);
            Assert.Empty(frameworks.Baseline.Dependencies);
            Assert.Equal(frameworks.Baseline, frameworks.Improved);
        }

        [Fact]
        public void GetPackageSpec_WhenFrameworksDependenciesDependencyNameIsEmptyString_Throws()
        {
            const string json = "{\"frameworks\":{\"a\":{\"dependencies\":{\"\":{}}}}}";

            AssertEquivalentFileFormatExceptions(json);
        }

        [Fact]
        public void GetPackageSpec_WhenFrameworksDependenciesDependencyValueIsVersionString_ReturnsDependencyVersionRange()
        {
            var expectedResult = new LibraryRange(
                name: "b",
                new VersionRange(new NuGetVersion("1.2.3")),
                LibraryDependencyTarget.All & ~LibraryDependencyTarget.Reference);
            var json = $"{{\"frameworks\":{{\"a\":{{\"dependencies\":{{\"{expectedResult.Name}\":\"{expectedResult.VersionRange.ToLegacyShortString()}\"}}}}}}}}";

            FrameworksDependencies dependencies = GetFrameworksDependencies(json);

            Assert.Equal(expectedResult, dependencies.Improved.LibraryRange);
            Assert.Equal(expectedResult, dependencies.Baseline.LibraryRange);
        }

        [Fact]
        public void GetPackageSpec_WhenFrameworksDependenciesDependencyValueIsVersionRangeString_ReturnsDependencyVersionRange()
        {
            var expectedResult = new LibraryRange(
                name: "b",
                new VersionRange(new NuGetVersion("1.2.3"), includeMinVersion: true, new NuGetVersion("4.5.6"), includeMaxVersion: false),
                LibraryDependencyTarget.All & ~LibraryDependencyTarget.Reference);
            var json = $"{{\"frameworks\":{{\"a\":{{\"dependencies\":{{\"{expectedResult.Name}\":\"{expectedResult.VersionRange}\"}}}}}}}}";

            FrameworksDependencies dependencies = GetFrameworksDependencies(json);

            Assert.Equal(expectedResult, dependencies.Improved.LibraryRange);
            Assert.Equal(expectedResult, dependencies.Baseline.LibraryRange);
        }

        [Theory]
        [InlineData(LibraryDependencyTarget.None)]
        [InlineData(LibraryDependencyTarget.Assembly)]
        [InlineData(LibraryDependencyTarget.Reference)]
        [InlineData(LibraryDependencyTarget.WinMD)]
        [InlineData(LibraryDependencyTarget.All)]
        [InlineData(LibraryDependencyTarget.PackageProjectExternal)]
        public void GetPackageSpec_WhenFrameworksDependenciesDependencyTargetValueIsUnsupported_Throws(LibraryDependencyTarget target)
        {
            var json = $"{{\"frameworks\":{{\"a\":{{\"dependencies\":{{\"b\":{{\"version\":\"1.2.3\",\"target\":\"{target}\"}}}}}}}}}}";

            AssertEquivalentFileFormatExceptions(json);
        }

        [Fact]
        public void GetPackageSpec_WhenFrameworksDependenciesDependencyAutoreferencedPropertyIsAbsent_ReturnsFalseAutoreferenced()
        {
            const string json = "{\"frameworks\":{\"a\":{\"dependencies\":{\"b\":{\"target\":\"Project\"}}}}}";

            FrameworksDependencies dependencies = GetFrameworksDependencies(json);

            Assert.False(dependencies.Improved.AutoReferenced);
            Assert.False(dependencies.Baseline.AutoReferenced);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void GetPackageSpec_WhenFrameworksDependenciesDependencyAutoreferencedValueIsBool_ReturnsBoolAutoreferenced(bool expectedValue)
        {
            var json = $"{{\"frameworks\":{{\"a\":{{\"dependencies\":{{\"b\":{{\"autoReferenced\":{expectedValue.ToString().ToLower()},\"target\":\"Project\"}}}}}}}}}}";

            FrameworksDependencies dependencies = GetFrameworksDependencies(json);

            Assert.Equal(expectedValue, dependencies.Improved.AutoReferenced);
            Assert.Equal(expectedValue, dependencies.Baseline.AutoReferenced);
        }

        [Theory]
        [InlineData("exclude")]
        [InlineData("include")]
        [InlineData("suppressParent")]
        [InlineData("type")]
        public void GetPackageSpec_WhenFrameworksDependenciesDependencyValueIsArray_Throws(string propertyName)
        {
            var json = $"{{\"frameworks\":{{\"a\":{{\"dependencies\":{{\"b\":{{\"{propertyName}\":[\"c\"]}}}}}}}}}}";

            // The exception messages will not be the same because the innermost exception in the baseline
            // is a Newtonsoft.Json exception, while it's a .NET exception in the improved.
            AssertEquivalentFileFormatExceptions(json, verifyExceptionMessages: false);
        }

        [Fact]
        public void GetPackageSpec_WhenFrameworksDependenciesDependencyIncludeAndExcludePropertiesAreAbsent_ReturnsAllIncludeType()
        {
            const string json = "{\"frameworks\":{\"a\":{\"dependencies\":{\"b\":{\"version\":\"1.0.0\"}}}}}";

            FrameworksDependencies dependencies = GetFrameworksDependencies(json);

            Assert.Equal(LibraryIncludeFlags.All, dependencies.Improved.IncludeType);
            Assert.Equal(LibraryIncludeFlags.All, dependencies.Baseline.IncludeType);
        }

        [Fact]
        public void GetPackageSpec_WhenFrameworksDependenciesDependencyExcludeValueIsValid_ReturnsIncludeType()
        {
            const string json = "{\"frameworks\":{\"a\":{\"dependencies\":{\"b\":{\"exclude\":\"Native\",\"version\":\"1.0.0\"}}}}}";

            FrameworksDependencies dependencies = GetFrameworksDependencies(json);

            Assert.Equal(LibraryIncludeFlags.All & ~LibraryIncludeFlags.Native, dependencies.Improved.IncludeType);
            Assert.Equal(dependencies.Baseline.IncludeType, dependencies.Improved.IncludeType);
        }

        [Fact]
        public void GetPackageSpec_WhenFrameworksDependenciesDependencyIncludeValueIsValid_ReturnsIncludeType()
        {
            const string json = "{\"frameworks\":{\"a\":{\"dependencies\":{\"b\":{\"include\":\"ContentFiles\",\"version\":\"1.0.0\"}}}}}";

            FrameworksDependencies dependencies = GetFrameworksDependencies(json);

            Assert.Equal(LibraryIncludeFlags.ContentFiles, dependencies.Improved.IncludeType);
            Assert.Equal(LibraryIncludeFlags.ContentFiles, dependencies.Improved.IncludeType);
        }

        [Fact]
        public void GetPackageSpec_WhenFrameworksDependenciesDependencyIncludeValueOverridesTypeValue_ReturnsIncludeType()
        {
            const string json = "{\"frameworks\":{\"a\":{\"dependencies\":{\"b\":{\"include\":\"ContentFiles\",\"type\":\"BecomesNupkgDependency, SharedFramework\",\"version\":\"1.0.0\"}}}}}";

            FrameworksDependencies dependencies = GetFrameworksDependencies(json);

            Assert.Equal(LibraryIncludeFlags.ContentFiles, dependencies.Improved.IncludeType);
            Assert.Equal(LibraryIncludeFlags.ContentFiles, dependencies.Baseline.IncludeType);
        }

        [Fact]
        public void GetPackageSpec_WhenFrameworksDependenciesDependencySuppressParentValueOverridesTypeValue_ReturnsSuppressParent()
        {
            const string json = "{\"frameworks\":{\"a\":{\"dependencies\":{\"b\":{\"suppressParent\":\"ContentFiles\",\"type\":\"SharedFramework\",\"version\":\"1.0.0\"}}}}}";

            FrameworksDependencies dependencies = GetFrameworksDependencies(json);

            Assert.Equal(LibraryIncludeFlags.ContentFiles, dependencies.Improved.SuppressParent);
            Assert.Equal(LibraryIncludeFlags.ContentFiles, dependencies.Baseline.SuppressParent);
        }

        [Fact]
        public void GetPackageSpec_WhenFrameworksDependenciesDependencySuppressParentPropertyIsAbsent_ReturnsSuppressParent()
        {
            const string json = "{\"frameworks\":{\"a\":{\"dependencies\":{\"b\":{\"version\":\"1.0.0\"}}}}}";

            FrameworksDependencies dependencies = GetFrameworksDependencies(json);

            Assert.Equal(LibraryIncludeFlagUtils.DefaultSuppressParent, dependencies.Improved.SuppressParent);
            Assert.Equal(LibraryIncludeFlagUtils.DefaultSuppressParent, dependencies.Baseline.SuppressParent);
        }

        [Fact]
        public void GetPackageSpec_WhenFrameworksDependenciesDependencySuppressParentValueIsValid_ReturnsSuppressParent()
        {
            const string json = "{\"frameworks\":{\"a\":{\"dependencies\":{\"b\":{\"suppressParent\":\"Compile\",\"version\":\"1.0.0\"}}}}}";

            FrameworksDependencies dependencies = GetFrameworksDependencies(json);

            Assert.Equal(LibraryIncludeFlags.Compile, dependencies.Improved.SuppressParent);
            Assert.Equal(LibraryIncludeFlags.Compile, dependencies.Baseline.SuppressParent);
        }

        [Fact]
        public void GetPackageSpec_WhenFrameworksDependenciesDependencyVersionValueIsInvalid_Throws()
        {
            const string json = "{\"frameworks\":{\"a\":{\"dependencies\":{\"b\":{\"version\":\"c\"}}}}}";

            AssertEquivalentFileFormatExceptions(json);
        }

        [Fact]
        public void GetPackageSpec_WhenFrameworksDependenciesDependencyTargetPropertyIsAbsent_ReturnsTarget()
        {
            const string json = "{\"frameworks\":{\"a\":{\"dependencies\":{\"b\":{\"version\":\"1.0.0\"}}}}}";

            FrameworksDependencies dependencies = GetFrameworksDependencies(json);

            Assert.Equal(
                LibraryDependencyTarget.All & ~LibraryDependencyTarget.Reference,
                dependencies.Improved.LibraryRange.TypeConstraint);
            Assert.Equal(
                dependencies.Baseline.LibraryRange.TypeConstraint,
                dependencies.Improved.LibraryRange.TypeConstraint);
        }

        [Fact]
        public void GetPackageSpec_WhenFrameworksDependenciesDependencyTargetValueIsPackageAndVersionPropertyIsAbsent_Throws()
        {
            const string json = "{\"frameworks\":{\"a\":{\"dependencies\":{\"b\":{\"target\":\"Package\"}}}}}";

            AssertEquivalentFileFormatExceptions(json);
        }

        [Fact]
        public void GetPackageSpec_WhenFrameworksDependenciesDependencyTargetValueIsProjectAndVersionPropertyIsAbsent_ReturnsAllVersionRange()
        {
            const string json = "{\"frameworks\":{\"a\":{\"dependencies\":{\"b\":{\"target\":\"Project\"}}}}}";

            FrameworksDependencies dependencies = GetFrameworksDependencies(json);

            Assert.Equal(VersionRange.All, dependencies.Improved.LibraryRange.VersionRange);
            Assert.Equal(VersionRange.All, dependencies.Baseline.LibraryRange.VersionRange);
        }

        [Fact]
        public void GetPackageSpec_WhenFrameworksDependenciesDependencyNoWarnPropertyIsAbsent_ReturnsEmptyNoWarns()
        {
            const string json = "{\"frameworks\":{\"a\":{\"dependencies\":{\"b\":{\"version\":\"1.0.0\"}}}}}";

            FrameworksDependencies dependencies = GetFrameworksDependencies(json);

            Assert.Empty(dependencies.Improved.NoWarn);
            Assert.Empty(dependencies.Baseline.NoWarn);
        }

        [Fact]
        public void GetPackageSpec_WhenFrameworksDependenciesDependencyNoWarnValueIsValid_ReturnsNoWarns()
        {
            NuGetLogCode[] expectedResults = { NuGetLogCode.NU1000, NuGetLogCode.NU3000 };
            var json = $"{{\"frameworks\":{{\"a\":{{\"dependencies\":{{\"b\":{{\"noWarn\":[\"{expectedResults[0].ToString()}\",\"{expectedResults[1].ToString()}\"],\"version\":\"1.0.0\"}}}}}}}}}}";

            FrameworksDependencies dependencies = GetFrameworksDependencies(json);

            Assert.Collection(
                dependencies.Improved.NoWarn,
                noWarn => Assert.Equal(expectedResults[0], noWarn),
                noWarn => Assert.Equal(expectedResults[1], noWarn));
            Assert.Equal(dependencies.Baseline.NoWarn, dependencies.Improved.NoWarn);
        }

        [Fact]
        public void GetPackageSpec_WhenFrameworksDependenciesDependencyGeneratePathPropertyPropertyIsAbsent_ReturnsFalseGeneratePathProperty()
        {
            const string json = "{\"frameworks\":{\"a\":{\"dependencies\":{\"b\":{\"version\":\"1.0.0\"}}}}}}}";

            FrameworksDependencies dependencies = GetFrameworksDependencies(json);

            Assert.False(dependencies.Improved.GeneratePathProperty);
            Assert.False(dependencies.Baseline.GeneratePathProperty);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void GetPackageSpec_WhenFrameworksDependenciesDependencyGeneratePathPropertyValueIsValid_ReturnsGeneratePathProperty(bool expectedResult)
        {
            var json = $"{{\"frameworks\":{{\"a\":{{\"dependencies\":{{\"b\":{{\"generatePathProperty\":{expectedResult.ToString().ToLowerInvariant()},\"version\":\"1.0.0\"}}}}}}}}}}";

            FrameworksDependencies dependencies = GetFrameworksDependencies(json);

            Assert.Equal(expectedResult, dependencies.Improved.GeneratePathProperty);
            Assert.Equal(expectedResult, dependencies.Baseline.GeneratePathProperty);
        }

        [Fact]
        public void GetPackageSpec_WhenFrameworksDependenciesDependencyTypePropertyIsAbsent_ReturnsDefaultTypeConstraint()
        {
            const string json = "{\"frameworks\":{\"a\":{\"dependencies\":{\"b\":{\"version\":\"1.0.0\"}}}}}";

            FrameworksDependencies dependencies = GetFrameworksDependencies(json);

            Assert.Equal(
                LibraryDependencyTarget.All & ~LibraryDependencyTarget.Reference,
                dependencies.Improved.LibraryRange.TypeConstraint);
            Assert.Equal(
                dependencies.Baseline.LibraryRange.TypeConstraint,
                dependencies.Improved.LibraryRange.TypeConstraint);
        }

        [Fact]
        public void GetPackageSpec_WhenFrameworksDownloadDependenciesPropertyIsAbsent_ReturnsEmptyDownloadDependencies()
        {
            const string json = "{\"frameworks\":{\"a\":{}}}";

            Frameworks frameworks = GetFrameworks(json);

            Assert.Empty(frameworks.Improved.DownloadDependencies);
            Assert.Empty(frameworks.Baseline.DownloadDependencies);
            Assert.Equal(frameworks.Baseline, frameworks.Improved);
        }

        [Fact]
        public void GetPackageSpec_WhenFrameworksDownloadDependenciesValueIsNull_ReturnsEmptyDownloadDependencies()
        {
            const string json = "{\"frameworks\":{\"a\":{\"downloadDependencies\":null}}}";

            Frameworks frameworks = GetFrameworks(json);

            Assert.Empty(frameworks.Improved.DownloadDependencies);
            Assert.Empty(frameworks.Baseline.DownloadDependencies);
            Assert.Equal(frameworks.Baseline, frameworks.Improved);
        }

        [Fact]
        public void GetPackageSpec_WhenFrameworksDownloadDependenciesValueIsNotArray_ReturnsEmptyDownloadDependencies()
        {
            const string json = "{\"frameworks\":{\"a\":{\"downloadDependencies\":\"b\"}}}";

            Frameworks frameworks = GetFrameworks(json);

            Assert.Empty(frameworks.Improved.DownloadDependencies);
            Assert.Empty(frameworks.Baseline.DownloadDependencies);
            Assert.Equal(frameworks.Baseline, frameworks.Improved);
        }

        [Fact]
        public void GetPackageSpec_WhenFrameworksDownloadDependenciesValueIsEmptyArray_ReturnsEmptyDownloadDependencies()
        {
            const string json = "{\"frameworks\":{\"a\":{\"downloadDependencies\":[]}}}";

            Frameworks frameworks = GetFrameworks(json);

            Assert.Empty(frameworks.Improved.DownloadDependencies);
            Assert.Empty(frameworks.Baseline.DownloadDependencies);
            Assert.Equal(frameworks.Baseline, frameworks.Improved);
        }

        [Fact]
        public void GetPackageSpec_WhenFrameworksDownloadDependenciesDependencyNameIsAbsent_Throws()
        {
            const string json = "{\"frameworks\":{\"a\":{\"downloadDependencies\":[{\"version\":\"1.2.3\"}]}}}";

            AssertEquivalentFileFormatExceptions(json);
        }

        [Fact]
        public void GetPackageSpec_WhenFrameworksDownloadDependenciesDependencyNameIsNull_ReturnsDownloadDependencies()
        {
            var expectedResult = new DownloadDependency(name: null, new VersionRange(new NuGetVersion("1.2.3")));
            var json = $"{{\"frameworks\":{{\"a\":{{\"downloadDependencies\":[{{\"name\":null,\"version\":\"{expectedResult.VersionRange.ToLegacyShortString()}\"}}]}}}}}}";

            Frameworks frameworks = GetFrameworks(json);

            AssertEqual(expectedResult, frameworks.Improved.DownloadDependencies.Single());
            AssertEqual(expectedResult, frameworks.Baseline.DownloadDependencies.Single());
        }

        [Fact]
        public void GetPackageSpec_WhenFrameworksDownloadDependenciesDependencyVersionIsAbsent_Throws()
        {
            const string json = "{\"frameworks\":{\"a\":{\"downloadDependencies\":[{\"name\":\"b\"}]}}}";

            AssertEquivalentFileFormatExceptions(json);
        }

        [Theory]
        [InlineData("null")]
        [InlineData("c")]
        public void GetPackageSpec_WhenFrameworksDownloadDependenciesDependencyVersionIsInvalid_Throws(string version)
        {
            var json = $"{{\"frameworks\":{{\"a\":{{\"downloadDependencies\":[{{\"name\":\"b\",\"version\":\"{version}\"}}]}}}}}}";

            AssertEquivalentFileFormatExceptions(json);
        }

        [Fact]
        public void GetPackageSpec_WhenFrameworksDownloadDependenciesValueIsValid_ReturnsDownloadDependencies()
        {
            var expectedResult = new DownloadDependency(name: "b", new VersionRange(new NuGetVersion("1.2.3")));
            var json = $"{{\"frameworks\":{{\"a\":{{\"downloadDependencies\":[{{\"name\":\"{expectedResult.Name}\",\"version\":\"{expectedResult.VersionRange.ToLegacyShortString()}\"}}]}}}}}}";

            Frameworks frameworks = GetFrameworks(json);

            Assert.Equal(expectedResult, frameworks.Improved.DownloadDependencies.Single());
            Assert.Equal(expectedResult, frameworks.Baseline.DownloadDependencies.Single());
            Assert.Equal(frameworks.Baseline, frameworks.Improved);
        }

        [Fact]
        public void GetPackageSpec_WhenFrameworksDownloadDependenciesValueHasDuplicates_PrefersFirstByName()
        {
            var expectedResult = new DownloadDependency(name: "b", new VersionRange(new NuGetVersion("1.2.3")));
            var unexpectedResult = new DownloadDependency(name: "b", new VersionRange(new NuGetVersion("4.5.6")));
            var json = "{\"frameworks\":{\"a\":{\"downloadDependencies\":[" +
                $"{{\"name\":\"{expectedResult.Name}\",\"version\":\"{expectedResult.VersionRange.ToLegacyShortString()}\"}}," +
                $"{{\"name\":\"{unexpectedResult.Name}\",\"version\":\"{unexpectedResult.VersionRange.ToLegacyShortString()}\"}}" +
                "]}}}";

            Frameworks frameworks = GetFrameworks(json);

            Assert.Equal(expectedResult, frameworks.Improved.DownloadDependencies.Single());
            Assert.Equal(expectedResult, frameworks.Baseline.DownloadDependencies.Single());
            Assert.Equal(frameworks.Baseline, frameworks.Improved);
        }

        [Fact]
        public void GetPackageSpec_WhenFrameworksFrameworkAssembliesPropertyIsAbsent_ReturnsEmptyDependencies()
        {
            const string json = "{\"frameworks\":{\"a\":{}}}";

            Frameworks frameworks = GetFrameworks(json);

            Assert.Empty(frameworks.Improved.Dependencies);
            Assert.Empty(frameworks.Baseline.Dependencies);
            Assert.Equal(frameworks.Baseline, frameworks.Improved);
        }

        [Fact]
        public void GetPackageSpec_WhenFrameworksFrameworkAssembliesValueIsNull_ReturnsEmptyDependencies()
        {
            const string json = "{\"frameworks\":{\"a\":{\"frameworkAssemblies\":null}}}";

            Frameworks frameworks = GetFrameworks(json);

            Assert.Empty(frameworks.Improved.Dependencies);
            Assert.Empty(frameworks.Baseline.Dependencies);
            Assert.Equal(frameworks.Baseline, frameworks.Improved);
        }

        [Fact]
        public void GetPackageSpec_WhenFrameworksFrameworkAssembliesValueIsEmptyObject_ReturnsEmptyDependencies()
        {
            const string json = "{\"frameworks\":{\"a\":{\"frameworkAssemblies\":{}}}}";

            Frameworks frameworks = GetFrameworks(json);

            Assert.Empty(frameworks.Improved.Dependencies);
            Assert.Empty(frameworks.Baseline.Dependencies);
            Assert.Equal(frameworks.Baseline, frameworks.Improved);
        }

        [Fact]
        public void GetPackageSpec_WhenFrameworksFrameworkAssembliesDependencyTargetPropertyIsAbsent_ReturnsTarget()
        {
            const string json = "{\"frameworks\":{\"a\":{\"frameworkAssemblies\":{\"b\":{\"version\":\"1.0.0\"}}}}}";

            FrameworksDependencies dependencies = GetFrameworksDependencies(json);

            Assert.Equal(LibraryDependencyTarget.Reference, dependencies.Improved.LibraryRange.TypeConstraint);
            Assert.Equal(
                dependencies.Baseline.LibraryRange.TypeConstraint,
                dependencies.Improved.LibraryRange.TypeConstraint);
        }

        [Fact]
        public void GetPackageSpec_WhenFrameworksFrameworkAssembliesDependencyTargetValueIsPackageAndVersionPropertyIsAbsent_Throws()
        {
            const string json = "{\"frameworks\":{\"a\":{\"frameworkAssemblies\":{\"b\":{\"target\":\"Package\"}}}}}";

            AssertEquivalentFileFormatExceptions(json);
        }

        [Fact]
        public void GetPackageSpec_WhenFrameworksFrameworkAssembliesDependencyTargetValueIsProjectAndVersionPropertyIsAbsent_ReturnsAllVersionRange()
        {
            const string json = "{\"frameworks\":{\"a\":{\"frameworkAssemblies\":{\"b\":{\"target\":\"Project\"}}}}}";

            FrameworksDependencies dependencies = GetFrameworksDependencies(json);

            Assert.Equal(VersionRange.All, dependencies.Improved.LibraryRange.VersionRange);
            Assert.Equal(VersionRange.All, dependencies.Baseline.LibraryRange.VersionRange);
        }

        [Fact]
        public void GetPackageSpec_WhenFrameworksFrameworkReferencesPropertyIsAbsent_ReturnsEmptyFrameworkReferences()
        {
            const string json = "{\"frameworks\":{\"a\":{}}}";

            Frameworks frameworks = GetFrameworks(json);

            Assert.Empty(frameworks.Improved.FrameworkReferences);
            Assert.Empty(frameworks.Baseline.FrameworkReferences);
            Assert.Equal(frameworks.Baseline, frameworks.Improved);
        }

        [Fact]
        public void GetPackageSpec_WhenFrameworksFrameworkReferencesValueIsNull_ReturnsEmptyFrameworkReferences()
        {
            const string json = "{\"frameworks\":{\"a\":{\"frameworkReferences\":null}}}";

            Frameworks frameworks = GetFrameworks(json);

            Assert.Empty(frameworks.Improved.FrameworkReferences);
            Assert.Empty(frameworks.Baseline.FrameworkReferences);
            Assert.Equal(frameworks.Baseline, frameworks.Improved);
        }

        [Fact]
        public void GetPackageSpec_WhenFrameworksFrameworkReferencesValueIsEmptyObject_ReturnsEmptyFrameworkReferences()
        {
            const string json = "{\"frameworks\":{\"a\":{\"frameworkReferences\":{}}}}";

            Frameworks frameworks = GetFrameworks(json);

            Assert.Empty(frameworks.Improved.FrameworkReferences);
            Assert.Empty(frameworks.Baseline.FrameworkReferences);
            Assert.Equal(frameworks.Baseline, frameworks.Improved);
        }

        [Fact]
        public void GetPackageSpec_WhenFrameworksFrameworkReferencesFrameworkNameIsEmptyString_Throws()
        {
            const string json = "{\"frameworks\":{\"a\":{\"frameworkReferences\":{\"\":{}}}}}";

            AssertEquivalentFileFormatExceptions(json);
        }

        [Fact]
        public void GetPackageSpec_WhenFrameworksFrameworkReferencesPrivateAssetsPropertyIsAbsent_ReturnsNonePrivateAssets()
        {
            var expectedResult = new FrameworkDependency(name: "b", FrameworkDependencyFlags.None);
            var json = $"{{\"frameworks\":{{\"a\":{{\"frameworkReferences\":{{\"{expectedResult.Name}\":{{}}}}}}}}}}";

            FrameworksFrameworkReferences references = GetFrameworksFrameworkReferences(json);

            Assert.Equal(expectedResult, references.Improved);
            Assert.Equal(expectedResult, references.Baseline);
        }

        [Theory]
        [InlineData("\"null\"")]
        [InlineData("\"\"")]
        [InlineData("\"c\"")]
        public void GetPackageSpec_WhenFrameworksFrameworkReferencesPrivateAssetsValueIsInvalidValue_ReturnsNonePrivateAssets(string privateAssets)
        {
            var expectedResult = new FrameworkDependency(name: "b", FrameworkDependencyFlags.None);
            var json = $"{{\"frameworks\":{{\"a\":{{\"frameworkReferences\":{{\"{expectedResult.Name}\":{{\"privateAssets\":{privateAssets}}}}}}}}}}}";

            FrameworksFrameworkReferences references = GetFrameworksFrameworkReferences(json);

            Assert.Equal(expectedResult, references.Improved);
            Assert.Equal(expectedResult, references.Baseline);
        }

        [Fact]
        public void GetPackageSpec_WhenFrameworksFrameworkReferencesPrivateAssetsValueIsValidString_ReturnsPrivateAssets()
        {
            var expectedResult = new FrameworkDependency(name: "b", FrameworkDependencyFlags.All);
            var json = $"{{\"frameworks\":{{\"a\":{{\"frameworkReferences\":{{\"{expectedResult.Name}\":{{\"privateAssets\":\"{expectedResult.PrivateAssets.ToString().ToLowerInvariant()}\"}}}}}}}}}}";

            FrameworksFrameworkReferences references = GetFrameworksFrameworkReferences(json);

            Assert.Equal(expectedResult, references.Improved);
            Assert.Equal(expectedResult, references.Baseline);
        }

        [Fact]
        public void GetPackageSpec_WhenFrameworksFrameworkReferencesPrivateAssetsValueIsValidDelimitedString_ReturnsPrivateAssets()
        {
            var expectedResult = new FrameworkDependency(name: "b", FrameworkDependencyFlags.All);
            var json = $"{{\"frameworks\":{{\"a\":{{\"frameworkReferences\":{{\"{expectedResult.Name}\":{{\"privateAssets\":\"none,all\"}}}}}}}}}}";

            FrameworksFrameworkReferences references = GetFrameworksFrameworkReferences(json);

            Assert.Equal(expectedResult, references.Improved);
            Assert.Equal(expectedResult, references.Baseline);
        }

        [Fact]
        public void GetPackageSpec_WhenFrameworksImportsPropertyIsAbsent_ReturnsEmptyImports()
        {
            const string json = "{\"frameworks\":{\"a\":{}}}";

            Frameworks frameworks = GetFrameworks(json);

            Assert.Empty(frameworks.Improved.Imports);
            Assert.Empty(frameworks.Baseline.Imports);
            Assert.Equal(frameworks.Baseline, frameworks.Improved);
        }

        [Theory]
        [InlineData("null")]
        [InlineData("\"\"")]
        public void GetPackageSpec_WhenFrameworksImportsValueIsArrayOfNullOrEmptyString_ImportIsSkipped(string import)
        {
            var json = $"{{\"frameworks\":{{\"a\":{{\"imports\":[{import}]}}}}}}";

            Frameworks frameworks = GetFrameworks(json);

            Assert.Empty(frameworks.Improved.Imports);
            Assert.Empty(frameworks.Baseline.Imports);
            Assert.Equal(frameworks.Baseline, frameworks.Improved);
        }

        [Fact]
        public void GetPackageSpec_WhenFrameworksImportsValueIsNull_ReturnsEmptyList()
        {
            const string json = "{\"frameworks\":{\"a\":{\"imports\":null}}}";

            Frameworks frameworks = GetFrameworks(json);

            Assert.Empty(frameworks.Improved.Imports);
            Assert.Empty(frameworks.Baseline.Imports);
            Assert.Equal(frameworks.Baseline, frameworks.Improved);
        }

        [Theory]
        [InlineData("true")]
        [InlineData("-2")]
        [InlineData("3.14")]
        [InlineData("{}")]
        public void GetPackageSpec_WhenFrameworksImportsValueIsInvalidValue_ReturnsEmptyList(string value)
        {
            var json = $"{{\"frameworks\":{{\"a\":{{\"imports\":{value}}}}}}}";

            Frameworks frameworks = GetFrameworks(json);

            Assert.Empty(frameworks.Improved.Imports);
            Assert.Empty(frameworks.Baseline.Imports);
            Assert.Equal(frameworks.Baseline, frameworks.Improved);
        }

        [Fact]
        public void GetPackageSpec_WhenFrameworksImportsValueContainsInvalidValue_Throws()
        {
            const string expectedImport = "b";

            var json = $"{{\"frameworks\":{{\"a\":{{\"imports\":[\"{expectedImport}\"]}}}}}}";

            FileFormatException exceptionImproved = Assert.Throws<FileFormatException>(() => GetPackageSpecImproved(json));
            FileFormatException exceptionBaseline = Assert.Throws<FileFormatException>(() => GetPackageSpecBaseline(json));

            Assert.Equal(exceptionBaseline.Line, exceptionImproved.Line);
            Assert.Equal(exceptionBaseline.Column, exceptionImproved.Column);

            Assert.Equal(
                $"Error reading '' at line 1 column 20 : Imports contains an invalid framework: '{expectedImport}' in 'project.json'.",
                exceptionImproved.Message);
            Assert.Equal(
                $"Error reading '' at line 1 column 20 : Imports contains an invalid framework: '[  \"{expectedImport}\"]' in 'project.json'.",
                exceptionBaseline.Message);
        }

        [Fact]
        public void GetPackageSpec_WhenFrameworksImportsValueIsString_ReturnsImport()
        {
            NuGetFramework expectedResult = NuGetFramework.Parse("net48");
            var json = $"{{\"frameworks\":{{\"a\":{{\"imports\":\"{expectedResult.GetShortFolderName()}\"}}}}}}";

            Frameworks frameworks = GetFrameworks(json);

            Assert.Collection(
                frameworks.Improved.Imports,
                actualResult => Assert.Equal(expectedResult, actualResult));
            Assert.Equal(frameworks.Baseline.Imports, frameworks.Improved.Imports);
            Assert.Equal(frameworks.Baseline, frameworks.Improved);
        }

        [Fact]
        public void GetPackageSpec_WhenFrameworksImportsValueIsArrayOfStrings_ReturnsImports()
        {
            NuGetFramework[] expectedResults = { NuGetFramework.Parse("net472"), NuGetFramework.Parse("net48") };
            var json = $"{{\"frameworks\":{{\"a\":{{\"imports\":[\"{expectedResults[0].GetShortFolderName()}\",\"{expectedResults[1].GetShortFolderName()}\"]}}}}}}";

            Frameworks frameworks = GetFrameworks(json);

            Assert.Collection(
                frameworks.Improved.Imports,
                actualResult => Assert.Equal(expectedResults[0], actualResult),
                actualResult => Assert.Equal(expectedResults[1], actualResult));
            Assert.Equal(frameworks.Baseline.Imports, frameworks.Improved.Imports);
            Assert.Equal(frameworks.Baseline, frameworks.Improved);
        }

        [Fact]
        public void GetPackageSpec_WhenFrameworksRuntimeIdentifierGraphPathPropertyIsAbsent_ReturnsRuntimeIdentifierGraphPath()
        {
            const string json = "{\"frameworks\":{\"a\":{}}}}";

            Frameworks frameworks = GetFrameworks(json);

            Assert.Null(frameworks.Improved.RuntimeIdentifierGraphPath);
            Assert.Null(frameworks.Baseline.RuntimeIdentifierGraphPath);
            Assert.Equal(frameworks.Baseline, frameworks.Improved);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("b")]
        public void GetPackageSpec_WhenFrameworksRuntimeIdentifierGraphPathValueIsString_ReturnsRuntimeIdentifierGraphPath(string expectedResult)
        {
            string runtimeIdentifierGraphPath = expectedResult == null ? "null" : $"\"{expectedResult}\"";
            var json = $"{{\"frameworks\":{{\"a\":{{\"runtimeIdentifierGraphPath\":{runtimeIdentifierGraphPath}}}}}}}";

            Frameworks frameworks = GetFrameworks(json);

            Assert.Equal(expectedResult, frameworks.Improved.RuntimeIdentifierGraphPath);
            Assert.Equal(expectedResult, frameworks.Baseline.RuntimeIdentifierGraphPath);
            Assert.Equal(frameworks.Baseline, frameworks.Improved);
        }

        [Fact]
        public void GetPackageSpec_WhenFrameworksWarnPropertyIsAbsent_ReturnsWarn()
        {
            const string json = "{\"frameworks\":{\"a\":{}}}}";

            Frameworks frameworks = GetFrameworks(json);

            Assert.False(frameworks.Improved.Warn);
            Assert.False(frameworks.Baseline.Warn);
            Assert.Equal(frameworks.Baseline, frameworks.Improved);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void GetPackageSpec_WhenFrameworksWarnValueIsValid_ReturnsWarn(bool expectedResult)
        {
            var json = $"{{\"frameworks\":{{\"a\":{{\"warn\":{expectedResult.ToString().ToLowerInvariant()}}}}}}}";

            Frameworks frameworks = GetFrameworks(json);

            Assert.Equal(expectedResult, frameworks.Improved.Warn);
            Assert.Equal(expectedResult, frameworks.Baseline.Warn);
            Assert.Equal(frameworks.Baseline, frameworks.Improved);
        }

        [Fact]
        public void GetPackageSpec_WhenPackIncludePropertyIsAbsent_ReturnsEmptyPackInclude()
        {
            PackageSpecs packageSpecs = GetPackageSpecs("{}");

            Assert.Empty(packageSpecs.Improved.PackInclude);
            Assert.Empty(packageSpecs.Baseline.PackInclude);
        }

        [Fact]
        public void GetPackageSpec_WhenPackIncludePropertyIsValid_ReturnsPackInclude()
        {
            var expectedResults = new List<KeyValuePair<string, string>>() { new KeyValuePair<string, string>("a", "b"), new KeyValuePair<string, string>("c", "d") };
            var json = $"{{\"packInclude\":{{\"{expectedResults[0].Key}\":\"{expectedResults[0].Value}\",\"{expectedResults[1].Key}\":\"{expectedResults[1].Value}\"}}}}";

            PackageSpecs packageSpecs = GetPackageSpecs(json);

            Assert.Collection(
                packageSpecs.Improved.PackInclude,
                actualResult => Assert.Equal(expectedResults[0], actualResult),
                actualResult => Assert.Equal(expectedResults[1], actualResult));
            Assert.Equal(packageSpecs.Baseline.PackInclude, packageSpecs.Improved.PackInclude);
        }

        [Theory]
        [InlineData("{}")]
        [InlineData("{\"packOptions\":null}")]
        public void GetPackageSpec_WhenPackOptionsPropertyIsAbsentOrValueIsNull_ReturnsPackOptions(string json)
        {
            PackageSpecs packageSpecs = GetPackageSpecs(json);

            Assert.NotNull(packageSpecs.Improved.PackOptions);
            Assert.NotNull(packageSpecs.Baseline.PackOptions);
            Assert.Null(packageSpecs.Improved.PackOptions.IncludeExcludeFiles);
            Assert.Null(packageSpecs.Baseline.PackOptions.IncludeExcludeFiles);
            Assert.Empty(packageSpecs.Improved.PackOptions.Mappings);
            Assert.Empty(packageSpecs.Baseline.PackOptions.Mappings);
            Assert.Empty(packageSpecs.Improved.PackOptions.PackageType);
            Assert.Empty(packageSpecs.Baseline.PackOptions.PackageType);
            Assert.Equal(packageSpecs.Baseline.PackInclude, packageSpecs.Improved.PackInclude);

            Assert.Null(packageSpecs.Improved.IconUrl);
            Assert.Null(packageSpecs.Baseline.IconUrl);
            Assert.Null(packageSpecs.Improved.LicenseUrl);
            Assert.Null(packageSpecs.Baseline.LicenseUrl);
            Assert.Empty(packageSpecs.Improved.Owners);
            Assert.Empty(packageSpecs.Baseline.Owners);
            Assert.Null(packageSpecs.Improved.ProjectUrl);
            Assert.Null(packageSpecs.Baseline.ProjectUrl);
            Assert.Null(packageSpecs.Improved.ReleaseNotes);
            Assert.Null(packageSpecs.Baseline.ReleaseNotes);
            Assert.False(packageSpecs.Improved.RequireLicenseAcceptance);
            Assert.False(packageSpecs.Baseline.RequireLicenseAcceptance);
            Assert.Null(packageSpecs.Improved.Summary);
            Assert.Null(packageSpecs.Baseline.Summary);
            Assert.Empty(packageSpecs.Improved.Tags);
            Assert.Empty(packageSpecs.Baseline.Tags);
        }

        [Fact]
        public void GetPackageSpec_WhenPackOptionsPropertyIsAbsent_OwnersAndTagsAreEmpty()
        {
            const string json = "{\"owners\":[\"a\"],\"tags\":[\"b\"]}";

            PackageSpecs packageSpecs = GetPackageSpecs(json);

            Assert.Empty(packageSpecs.Improved.Owners);
            Assert.Empty(packageSpecs.Baseline.Owners);
            Assert.Empty(packageSpecs.Improved.Tags);
            Assert.Empty(packageSpecs.Baseline.Tags);
        }

        [Fact]
        public void GetPackageSpec_WhenPackOptionsPropertyIsEmptyObject_ReturnsPackOptions()
        {
            string json = "{\"packOptions\":{}}";

            PackageSpecs packageSpecs = GetPackageSpecs(json);

            Assert.NotNull(packageSpecs.Improved.PackOptions);
            Assert.NotNull(packageSpecs.Baseline.PackOptions);
            Assert.Null(packageSpecs.Improved.PackOptions.IncludeExcludeFiles);
            Assert.Null(packageSpecs.Baseline.PackOptions.IncludeExcludeFiles);
            Assert.Null(packageSpecs.Improved.PackOptions.Mappings);
            Assert.Null(packageSpecs.Baseline.PackOptions.Mappings);
            Assert.Empty(packageSpecs.Improved.PackOptions.PackageType);
            Assert.Empty(packageSpecs.Baseline.PackOptions.PackageType);
            Assert.Equal(packageSpecs.Baseline.PackInclude, packageSpecs.Improved.PackInclude);

            Assert.Null(packageSpecs.Improved.IconUrl);
            Assert.Null(packageSpecs.Baseline.IconUrl);
            Assert.Null(packageSpecs.Improved.LicenseUrl);
            Assert.Null(packageSpecs.Baseline.LicenseUrl);
            Assert.Empty(packageSpecs.Improved.Owners);
            Assert.Empty(packageSpecs.Baseline.Owners);
            Assert.Null(packageSpecs.Improved.ProjectUrl);
            Assert.Null(packageSpecs.Baseline.ProjectUrl);
            Assert.Null(packageSpecs.Improved.ReleaseNotes);
            Assert.Null(packageSpecs.Baseline.ReleaseNotes);
            Assert.False(packageSpecs.Improved.RequireLicenseAcceptance);
            Assert.False(packageSpecs.Baseline.RequireLicenseAcceptance);
            Assert.Null(packageSpecs.Improved.Summary);
            Assert.Null(packageSpecs.Baseline.Summary);
            Assert.Empty(packageSpecs.Improved.Tags);
            Assert.Empty(packageSpecs.Baseline.Tags);
        }

        [Fact]
        public void GetPackageSpec_WhenPackOptionsValueIsValid_ReturnsPackOptions()
        {
            const string iconUrl = "a";
            const string licenseUrl = "b";
            string[] owners = { "c", "d" };
            const string projectUrl = "e";
            const string releaseNotes = "f";
            const bool requireLicenseAcceptance = true;
            const string summary = "g";
            string[] tags = { "h", "i" };

            var json = $"{{\"packOptions\":{{\"iconUrl\":\"{iconUrl}\",\"licenseUrl\":\"{licenseUrl}\",\"owners\":[{string.Join(",", owners.Select(owner => $"\"{owner}\""))}]," +
                $"\"projectUrl\":\"{projectUrl}\",\"releaseNotes\":\"{releaseNotes}\",\"requireLicenseAcceptance\":{requireLicenseAcceptance.ToString().ToLowerInvariant()}," +
                $"\"summary\":\"{summary}\",\"tags\":[{string.Join(",", tags.Select(tag => $"\"{tag}\""))}]}}}}";

            PackageSpecs packageSpecs = GetPackageSpecs(json);

            Assert.NotNull(packageSpecs.Improved.PackOptions);
            Assert.NotNull(packageSpecs.Baseline.PackOptions);
            Assert.Null(packageSpecs.Improved.PackOptions.IncludeExcludeFiles);
            Assert.Null(packageSpecs.Baseline.PackOptions.IncludeExcludeFiles);
            Assert.Null(packageSpecs.Improved.PackOptions.Mappings);
            Assert.Null(packageSpecs.Baseline.PackOptions.Mappings);
            Assert.Empty(packageSpecs.Improved.PackOptions.PackageType);
            Assert.Empty(packageSpecs.Baseline.PackOptions.PackageType);
            Assert.Equal(packageSpecs.Baseline.PackInclude, packageSpecs.Improved.PackInclude);
            Assert.Equal(iconUrl, packageSpecs.Improved.IconUrl);
            Assert.Equal(iconUrl, packageSpecs.Baseline.IconUrl);
            Assert.Equal(licenseUrl, packageSpecs.Improved.LicenseUrl);
            Assert.Equal(licenseUrl, packageSpecs.Baseline.LicenseUrl);
            Assert.Equal(owners, packageSpecs.Improved.Owners);
            Assert.Equal(owners, packageSpecs.Baseline.Owners);
            Assert.Equal(projectUrl, packageSpecs.Improved.ProjectUrl);
            Assert.Equal(projectUrl, packageSpecs.Baseline.ProjectUrl);
            Assert.Equal(releaseNotes, packageSpecs.Improved.ReleaseNotes);
            Assert.Equal(releaseNotes, packageSpecs.Baseline.ReleaseNotes);
            Assert.Equal(requireLicenseAcceptance, packageSpecs.Improved.RequireLicenseAcceptance);
            Assert.Equal(requireLicenseAcceptance, packageSpecs.Baseline.RequireLicenseAcceptance);
            Assert.Equal(summary, packageSpecs.Improved.Summary);
            Assert.Equal(summary, packageSpecs.Baseline.Summary);
            Assert.Equal(tags, packageSpecs.Improved.Tags);
            Assert.Equal(tags, packageSpecs.Baseline.Tags);
        }

        [Fact]
        public void GetPackageSpec_WhenPackOptionsPackageTypeValueIsNull_ReturnsEmptyPackageTypes()
        {
            const string json = "{\"packOptions\":{\"packageType\":null}}";

            PackageSpecs packageSpecs = GetPackageSpecs(json);

            Assert.Empty(packageSpecs.Improved.PackOptions.PackageType);
            Assert.Empty(packageSpecs.Baseline.PackOptions.PackageType);
        }

        [Theory]
        [InlineData("true")]
        [InlineData("-2")]
        [InlineData("3.14")]
        [InlineData("{}")]
        [InlineData("[true]")]
        [InlineData("[-2]")]
        [InlineData("[3.14]")]
        [InlineData("[null]")]
        [InlineData("[{}]")]
        [InlineData("[[]]")]
        public void GetPackageSpec_WhenPackOptionsPackageTypeIsInvalid_Throws(string value)
        {
            var json = $"{{\"packOptions\":{{\"packageType\":{value}}}}}";

            AssertEquivalentFileFormatExceptions(json);
        }

        [Theory]
        [InlineData("\"a\"", "a")]
        [InlineData("\"a,b\"", "a,b")]
        [InlineData("[\"a\"]", "a")]
        [InlineData("[\"a b\"]", "a b")]
        public void GetPackageSpec_WhenPackOptionsPackageTypeValueIsValid_ReturnsPackageTypes(string value, string expectedName)
        {
            var expectedResult = new PackageType(expectedName, PackageType.EmptyVersion);
            var json = $"{{\"packOptions\":{{\"packageType\":{value}}}}}";

            PackageSpecs packageSpecs = GetPackageSpecs(json);

            Assert.Collection(
                packageSpecs.Improved.PackOptions.PackageType,
                actualResult => Assert.Equal(expectedResult, actualResult));
            Assert.Equal(
                packageSpecs.Baseline.PackOptions.PackageType,
                packageSpecs.Improved.PackOptions.PackageType);
        }

        [Fact]
        public void GetPackageSpec_WhenPackOptionsFilesValueIsNull_ReturnsNullInclude()
        {
            const string json = "{\"packOptions\":{\"files\":null}}";

            PackageSpecs packageSpecs = GetPackageSpecs(json);

            Assert.Null(packageSpecs.Improved.PackOptions.IncludeExcludeFiles);
            Assert.Null(packageSpecs.Baseline.PackOptions.IncludeExcludeFiles);
        }

        [Fact]
        public void GetPackageSpec_WhenPackOptionsFilesValueIsEmptyObject_ReturnsNullInclude()
        {
            const string json = "{\"packOptions\":{\"files\":{}}}";

            PackageSpecs packageSpecs = GetPackageSpecs(json);

            Assert.Null(packageSpecs.Improved.PackOptions.IncludeExcludeFiles);
            Assert.Null(packageSpecs.Baseline.PackOptions.IncludeExcludeFiles);
        }

        [Fact]
        public void GetPackageSpec_WhenPackOptionsFilesIncludeValueIsNull_ReturnsNullIncludeExcludeFiles()
        {
            const string json = "{\"packOptions\":{\"files\":{\"include\":null}}}";

            PackageSpecs packageSpecs = GetPackageSpecs(json);

            Assert.Null(packageSpecs.Improved.PackOptions.IncludeExcludeFiles);
            Assert.Null(packageSpecs.Baseline.PackOptions.IncludeExcludeFiles);
        }

        [Fact]
        public void GetPackageSpec_WhenPackOptionsFilesIncludeValueIsEmptyArray_ReturnsEmptyInclude()
        {
            const string json = "{\"packOptions\":{\"files\":{\"include\":[]}}}";

            PackageSpecs packageSpecs = GetPackageSpecs(json);

            Assert.Empty(packageSpecs.Improved.PackOptions.IncludeExcludeFiles.Include);
            Assert.Empty(packageSpecs.Baseline.PackOptions.IncludeExcludeFiles.Include);
        }

        [Theory]
        [InlineData("\"a\"", "a")]
        [InlineData("\"a, b\"", "a, b")]
        [InlineData("[null]", null)]
        [InlineData("[\"\"]", "")]
        [InlineData("[\"a\"]", "a")]
        [InlineData("[\"a, b\"]", "a, b")]
        [InlineData("[\"a\", \"b\"]", "a", "b")]
        public void GetPackageSpec_WhenPackOptionsFilesIncludeValueIsValid_ReturnsInclude(string value, params string[] expectedResults)
        {
            expectedResults = expectedResults ?? new string[] { null };

            var json = $"{{\"packOptions\":{{\"files\":{{\"include\":{value}}}}}}}";

            PackageSpecs packageSpecs = GetPackageSpecs(json);

            Assert.Equal(expectedResults, packageSpecs.Improved.PackOptions.IncludeExcludeFiles.Include);
            Assert.Equal(
                packageSpecs.Baseline.PackOptions.IncludeExcludeFiles.Include,
                packageSpecs.Improved.PackOptions.IncludeExcludeFiles.Include);
        }

        [Fact]
        public void GetPackageSpec_WhenPackOptionsFilesIncludeFilesValueIsNull_ReturnsNullIncludeExcludeFiles()
        {
            const string json = "{\"packOptions\":{\"files\":{\"includeFiles\":null}}}";

            PackageSpecs packageSpecs = GetPackageSpecs(json);

            Assert.Null(packageSpecs.Improved.PackOptions.IncludeExcludeFiles);
            Assert.Null(packageSpecs.Baseline.PackOptions.IncludeExcludeFiles);
        }

        [Fact]
        public void GetPackageSpec_WhenPackOptionsFilesIncludeFilesValueIsEmptyArray_ReturnsEmptyIncludeFiles()
        {
            const string json = "{\"packOptions\":{\"files\":{\"includeFiles\":[]}}}";

            PackageSpecs packageSpecs = GetPackageSpecs(json);

            Assert.Empty(packageSpecs.Improved.PackOptions.IncludeExcludeFiles.IncludeFiles);
            Assert.Empty(packageSpecs.Baseline.PackOptions.IncludeExcludeFiles.IncludeFiles);
        }

        [Theory]
        [InlineData("\"a\"", "a")]
        [InlineData("\"a, b\"", "a, b")]
        [InlineData("[null]", null)]
        [InlineData("[\"\"]", "")]
        [InlineData("[\"a\"]", "a")]
        [InlineData("[\"a, b\"]", "a, b")]
        [InlineData("[\"a\", \"b\"]", "a", "b")]
        public void GetPackageSpec_WhenPackOptionsFilesIncludeFilesValueIsValid_ReturnsIncludeFiles(string value, params string[] expectedResults)
        {
            expectedResults = expectedResults ?? new string[] { null };

            var json = $"{{\"packOptions\":{{\"files\":{{\"includeFiles\":{value}}}}}}}";

            PackageSpecs packageSpecs = GetPackageSpecs(json);

            Assert.Equal(expectedResults, packageSpecs.Improved.PackOptions.IncludeExcludeFiles.IncludeFiles);
            Assert.Equal(
                packageSpecs.Baseline.PackOptions.IncludeExcludeFiles.IncludeFiles,
                packageSpecs.Improved.PackOptions.IncludeExcludeFiles.IncludeFiles);
        }

        [Fact]
        public void GetPackageSpec_WhenPackOptionsFilesExcludeValueIsNull_ReturnsNullIncludeExcludeFiles()
        {
            const string json = "{\"packOptions\":{\"files\":{\"exclude\":null}}}";

            PackageSpecs packageSpecs = GetPackageSpecs(json);

            Assert.Null(packageSpecs.Improved.PackOptions.IncludeExcludeFiles);
            Assert.Null(packageSpecs.Baseline.PackOptions.IncludeExcludeFiles);
        }

        [Fact]
        public void GetPackageSpec_WhenPackOptionsFilesExcludeValueIsEmptyArray_ReturnsEmptyExclude()
        {
            const string json = "{\"packOptions\":{\"files\":{\"exclude\":[]}}}";

            PackageSpecs packageSpecs = GetPackageSpecs(json);

            Assert.Empty(packageSpecs.Improved.PackOptions.IncludeExcludeFiles.Exclude);
            Assert.Empty(packageSpecs.Baseline.PackOptions.IncludeExcludeFiles.Exclude);
        }

        [Theory]
        [InlineData("\"a\"", "a")]
        [InlineData("\"a, b\"", "a, b")]
        [InlineData("[null]", null)]
        [InlineData("[\"\"]", "")]
        [InlineData("[\"a\"]", "a")]
        [InlineData("[\"a, b\"]", "a, b")]
        [InlineData("[\"a\", \"b\"]", "a", "b")]
        public void GetPackageSpec_WhenPackOptionsFilesExcludeValueIsValid_ReturnsExclude(string value, params string[] expectedResults)
        {
            expectedResults = expectedResults ?? new string[] { null };

            var json = $"{{\"packOptions\":{{\"files\":{{\"exclude\":{value}}}}}}}";

            PackageSpecs packageSpecs = GetPackageSpecs(json);

            Assert.Equal(expectedResults, packageSpecs.Improved.PackOptions.IncludeExcludeFiles.Exclude);
            Assert.Equal(
                packageSpecs.Baseline.PackOptions.IncludeExcludeFiles.Exclude,
                packageSpecs.Improved.PackOptions.IncludeExcludeFiles.Exclude);
        }

        [Fact]
        public void GetPackageSpec_WhenPackOptionsFilesExcludeFilesValueIsNull_ReturnsNullIncludeExcludeFiles()
        {
            const string json = "{\"packOptions\":{\"files\":{\"excludeFiles\":null}}}";

            PackageSpecs packageSpecs = GetPackageSpecs(json);

            Assert.Null(packageSpecs.Improved.PackOptions.IncludeExcludeFiles);
            Assert.Null(packageSpecs.Baseline.PackOptions.IncludeExcludeFiles);
        }

        [Fact]
        public void GetPackageSpec_WhenPackOptionsFilesExcludeFilesValueIsEmptyArray_ReturnsEmptyExcludeFiles()
        {
            const string json = "{\"packOptions\":{\"files\":{\"excludeFiles\":[]}}}";

            PackageSpecs packageSpecs = GetPackageSpecs(json);

            Assert.Empty(packageSpecs.Improved.PackOptions.IncludeExcludeFiles.ExcludeFiles);
            Assert.Empty(packageSpecs.Baseline.PackOptions.IncludeExcludeFiles.ExcludeFiles);
        }

        [Theory]
        [InlineData("\"a\"", "a")]
        [InlineData("\"a, b\"", "a, b")]
        [InlineData("[null]", null)]
        [InlineData("[\"\"]", "")]
        [InlineData("[\"a\"]", "a")]
        [InlineData("[\"a, b\"]", "a, b")]
        [InlineData("[\"a\", \"b\"]", "a", "b")]
        public void GetPackageSpec_WhenPackOptionsFilesExcludeFilesValueIsValid_ReturnsExcludeFiles(string value, params string[] expectedResults)
        {
            expectedResults = expectedResults ?? new string[] { null };

            var json = $"{{\"packOptions\":{{\"files\":{{\"excludeFiles\":{value}}}}}}}";

            PackageSpecs packageSpecs = GetPackageSpecs(json);

            Assert.Equal(expectedResults, packageSpecs.Improved.PackOptions.IncludeExcludeFiles.ExcludeFiles);
            Assert.Equal(
                packageSpecs.Baseline.PackOptions.IncludeExcludeFiles.ExcludeFiles,
                packageSpecs.Improved.PackOptions.IncludeExcludeFiles.ExcludeFiles);
        }

        [Fact]
        public void GetPackageSpec_WhenPackOptionsFilesMappingsPropertyIsAbsent_ReturnsNullMappings()
        {
            const string json = "{\"packOptions\":{\"files\":{}}}";

            PackageSpecs packageSpecs = GetPackageSpecs(json);

            Assert.Null(packageSpecs.Improved.PackOptions.Mappings);
            Assert.Null(packageSpecs.Baseline.PackOptions.Mappings);
        }

        [Fact]
        public void GetPackageSpec_WhenPackOptionsFilesMappingsValueIsNull_ReturnsNullMappings()
        {
            const string json = "{\"packOptions\":{\"files\":{\"mappings\":null}}}";

            PackageSpecs packageSpecs = GetPackageSpecs(json);

            Assert.Null(packageSpecs.Improved.PackOptions.Mappings);
            Assert.Null(packageSpecs.Baseline.PackOptions.Mappings);
        }

        [Theory]
        [InlineData("\"b\"", "b")]
        [InlineData("\"b,c\"", "b,c")]
        [InlineData("[\"b\", \"c\"]", "b", "c")]
        public void GetPackageSpec_WhenPackOptionsFilesMappingsValueIsValid_ReturnsMappings(string value, params string[] expectedIncludes)
        {
            var expectedResults = new Dictionary<string, IncludeExcludeFiles>()
            {
                { "a", new IncludeExcludeFiles() { Include = expectedIncludes } }
            };
            var json = $"{{\"packOptions\":{{\"files\":{{\"mappings\":{{\"a\":{value}}}}}}}}}";

            PackageSpecs packageSpecs = GetPackageSpecs(json);

            Assert.Equal(expectedResults, packageSpecs.Improved.PackOptions.Mappings);
            Assert.Equal(expectedResults, packageSpecs.Baseline.PackOptions.Mappings);
        }

        [Fact]
        public void GetPackageSpec_WhenPackOptionsFilesMappingsValueHasMultipleMappings_ReturnsMappings()
        {
            var expectedResults = new Dictionary<string, IncludeExcludeFiles>()
            {
                { "a", new IncludeExcludeFiles() { Include = new[] { "b" } } },
                { "c", new IncludeExcludeFiles() { Include = new[] { "d", "e" } } }
            };
            const string json = "{\"packOptions\":{\"files\":{\"mappings\":{\"a\":\"b\",\"c\":[\"d\", \"e\"]}}}}";

            PackageSpecs packageSpecs = GetPackageSpecs(json);

            Assert.Equal(expectedResults, packageSpecs.Improved.PackOptions.Mappings);
            Assert.Equal(expectedResults, packageSpecs.Baseline.PackOptions.Mappings);
        }

        [Fact]
        public void GetPackageSpec_WhenPackOptionsFilesMappingsValueHasFiles_ReturnsMappings()
        {
            var expectedResults = new Dictionary<string, IncludeExcludeFiles>()
            {
                {
                    "a",
                    new IncludeExcludeFiles()
                    {
                        Include = new [] { "b" },
                        IncludeFiles = new [] { "c" },
                        Exclude = new [] { "d" },
                        ExcludeFiles = new [] { "e" }
                    }
                }
            };
            const string json = "{\"packOptions\":{\"files\":{\"mappings\":{\"a\":{\"include\":\"b\",\"includeFiles\":\"c\",\"exclude\":\"d\",\"excludeFiles\":\"e\"}}}}}";

            PackageSpecs packageSpecs = GetPackageSpecs(json);

            Assert.Equal(expectedResults, packageSpecs.Improved.PackOptions.Mappings);
            Assert.Equal(expectedResults, packageSpecs.Baseline.PackOptions.Mappings);
        }

        [Fact]
        public void GetPackageSpec_WhenRestorePropertyIsAbsent_ReturnsNullRestoreMetadata()
        {
            const string json = "{}";
            PackageSpecs packageSpecs = GetPackageSpecs(json);

            Assert.Null(packageSpecs.Improved.RestoreMetadata);
            Assert.Null(packageSpecs.Baseline.RestoreMetadata);
        }

        [Fact]
        public void GetPackageSpec_WhenRestoreValueIsEmptyObject_ReturnsRestoreMetadata()
        {
            const string json = "{\"restore\":{}}";
            PackageSpecs packageSpecs = GetPackageSpecs(json);

            Assert.NotNull(packageSpecs.Improved.RestoreMetadata);
            Assert.NotNull(packageSpecs.Baseline.RestoreMetadata);
            Assert.Equal(packageSpecs.Baseline.RestoreMetadata, packageSpecs.Improved.RestoreMetadata);
        }

        [Theory]
        [InlineData("null")]
        [InlineData("\"\"")]
        [InlineData("\"a\"")]
        public void GetPackageSpec_WhenRestoreProjectStyleValueIsInvalid_ReturnsProjectStyle(string value)
        {
            var json = $"{{\"restore\":{{\"projectStyle\":{value}}}}}";
            PackageSpecs packageSpecs = GetPackageSpecs(json);

            Assert.Equal(ProjectStyle.Unknown, packageSpecs.Improved.RestoreMetadata.ProjectStyle);
            Assert.Equal(ProjectStyle.Unknown, packageSpecs.Baseline.RestoreMetadata.ProjectStyle);
        }

        [Fact]
        public void GetPackageSpec_WhenRestoreProjectStyleValueIsValid_ReturnsProjectStyle()
        {
            const ProjectStyle expectedResult = ProjectStyle.PackageReference;

            var json = $"{{\"restore\":{{\"projectStyle\":\"{expectedResult.ToString().ToLowerInvariant()}\"}}}}";
            PackageSpecs packageSpecs = GetPackageSpecs(json);

            Assert.Equal(expectedResult, packageSpecs.Improved.RestoreMetadata.ProjectStyle);
            Assert.Equal(expectedResult, packageSpecs.Baseline.RestoreMetadata.ProjectStyle);
        }

        [Theory]
        [InlineData("null", null)]
        [InlineData("\"\"", "")]
        [InlineData("\"a\"", "a")]
        public void GetPackageSpec_WhenRestoreProjectUniqueNameValueIsValid_ReturnsProjectUniqueName(
            string value,
            string expectedValue)
        {
            var json = $"{{\"restore\":{{\"projectUniqueName\":{value}}}}}";
            PackageSpecs packageSpecs = GetPackageSpecs(json);

            Assert.Equal(expectedValue, packageSpecs.Improved.RestoreMetadata.ProjectUniqueName);
            Assert.Equal(expectedValue, packageSpecs.Baseline.RestoreMetadata.ProjectUniqueName);
        }

        [Theory]
        [InlineData("null", null)]
        [InlineData("\"\"", "")]
        [InlineData("\"a\"", "a")]
        public void GetPackageSpec_WhenRestoreOutputPathValueIsValid_ReturnsOutputPath(
            string value,
            string expectedValue)
        {
            var json = $"{{\"restore\":{{\"outputPath\":{value}}}}}";
            PackageSpecs packageSpecs = GetPackageSpecs(json);

            Assert.Equal(expectedValue, packageSpecs.Improved.RestoreMetadata.OutputPath);
            Assert.Equal(expectedValue, packageSpecs.Baseline.RestoreMetadata.OutputPath);
        }

        [Theory]
        [InlineData("null", null)]
        [InlineData("\"\"", "")]
        [InlineData("\"a\"", "a")]
        public void GetPackageSpec_WhenRestorePackagesPathValueIsValid_ReturnsPackagesPath(
            string value,
            string expectedValue)
        {
            var json = $"{{\"restore\":{{\"packagesPath\":{value}}}}}";
            PackageSpecs packageSpecs = GetPackageSpecs(json);

            Assert.Equal(expectedValue, packageSpecs.Improved.RestoreMetadata.PackagesPath);
            Assert.Equal(expectedValue, packageSpecs.Baseline.RestoreMetadata.PackagesPath);
        }

        [Theory]
        [InlineData("null", null)]
        [InlineData("\"\"", "")]
        [InlineData("\"a\"", "a")]
        public void GetPackageSpec_WhenRestoreProjectJsonPathValueIsValid_ReturnsProjectJsonPath(
            string value,
            string expectedValue)
        {
            var json = $"{{\"restore\":{{\"projectJsonPath\":{value}}}}}";
            PackageSpecs packageSpecs = GetPackageSpecs(json);

            Assert.Equal(expectedValue, packageSpecs.Improved.RestoreMetadata.ProjectJsonPath);
            Assert.Equal(expectedValue, packageSpecs.Baseline.RestoreMetadata.ProjectJsonPath);
        }

        [Theory]
        [InlineData("null", null)]
        [InlineData("\"\"", "")]
        [InlineData("\"a\"", "a")]
        public void GetPackageSpec_WhenRestoreProjectNameValueIsValid_ReturnsProjectName(
            string value,
            string expectedValue)
        {
            var json = $"{{\"restore\":{{\"projectName\":{value}}}}}";
            PackageSpecs packageSpecs = GetPackageSpecs(json);

            Assert.Equal(expectedValue, packageSpecs.Improved.RestoreMetadata.ProjectName);
            Assert.Equal(expectedValue, packageSpecs.Baseline.RestoreMetadata.ProjectName);
        }

        [Theory]
        [InlineData("null", null)]
        [InlineData("\"\"", "")]
        [InlineData("\"a\"", "a")]
        public void GetPackageSpec_WhenRestoreProjectPathValueIsValid_ReturnsProjectPath(
            string value,
            string expectedValue)
        {
            var json = $"{{\"restore\":{{\"projectPath\":{value}}}}}";
            PackageSpecs packageSpecs = GetPackageSpecs(json);

            Assert.Equal(expectedValue, packageSpecs.Improved.RestoreMetadata.ProjectPath);
            Assert.Equal(expectedValue, packageSpecs.Baseline.RestoreMetadata.ProjectPath);
        }

        [Theory]
        [InlineData(null, false)]
        [InlineData(true, true)]
        [InlineData(false, false)]
        public void GetPackageSpec_WhenCrossTargetingValueIsValid_ReturnsCrossTargeting(
            bool? value,
            bool expectedValue)
        {
            var json = $"{{\"restore\":{{\"crossTargeting\":{(value.HasValue ? value.ToString().ToLowerInvariant() : "null")}}}}}";
            PackageSpecs packageSpecs = GetPackageSpecs(json);

            Assert.Equal(expectedValue, packageSpecs.Improved.RestoreMetadata.CrossTargeting);
            Assert.Equal(expectedValue, packageSpecs.Baseline.RestoreMetadata.CrossTargeting);
        }

        [Theory]
        [InlineData(null, false)]
        [InlineData(true, true)]
        [InlineData(false, false)]
        public void GetPackageSpec_WhenLegacyPackagesDirectoryValueIsValid_ReturnsLegacyPackagesDirectory(
            bool? value,
            bool expectedValue)
        {
            var json = $"{{\"restore\":{{\"legacyPackagesDirectory\":{(value.HasValue ? value.ToString().ToLowerInvariant() : "null")}}}}}";
            PackageSpecs packageSpecs = GetPackageSpecs(json);

            Assert.Equal(expectedValue, packageSpecs.Improved.RestoreMetadata.LegacyPackagesDirectory);
            Assert.Equal(expectedValue, packageSpecs.Baseline.RestoreMetadata.LegacyPackagesDirectory);
        }

        [Theory]
        [InlineData(null, false)]
        [InlineData(true, true)]
        [InlineData(false, false)]
        public void GetPackageSpec_WhenValidateRuntimeAssetsValueIsValid_ReturnsValidateRuntimeAssets(
            bool? value,
            bool expectedValue)
        {
            var json = $"{{\"restore\":{{\"validateRuntimeAssets\":{(value.HasValue ? value.ToString().ToLowerInvariant() : "null")}}}}}";
            PackageSpecs packageSpecs = GetPackageSpecs(json);

            Assert.Equal(expectedValue, packageSpecs.Improved.RestoreMetadata.ValidateRuntimeAssets);
            Assert.Equal(expectedValue, packageSpecs.Baseline.RestoreMetadata.ValidateRuntimeAssets);
        }

        [Theory]
        [InlineData(null, false)]
        [InlineData(true, true)]
        [InlineData(false, false)]
        public void GetPackageSpec_WhenSkipContentFileWriteValueIsValid_ReturnsSkipContentFileWrite(
            bool? value,
            bool expectedValue)
        {
            var json = $"{{\"restore\":{{\"skipContentFileWrite\":{(value.HasValue ? value.ToString().ToLowerInvariant() : "null")}}}}}";
            PackageSpecs packageSpecs = GetPackageSpecs(json);

            Assert.Equal(expectedValue, packageSpecs.Improved.RestoreMetadata.SkipContentFileWrite);
            Assert.Equal(expectedValue, packageSpecs.Baseline.RestoreMetadata.SkipContentFileWrite);
        }

        [Fact]
        public void GetPackageSpec_WhenSourcesValueIsEmptyObject_ReturnsEmptySources()
        {
            const string json = "{\"restore\":{\"sources\":{}}}";
            PackageSpecs packageSpecs = GetPackageSpecs(json);

            Assert.Empty(packageSpecs.Improved.RestoreMetadata.Sources);
            Assert.Empty(packageSpecs.Baseline.RestoreMetadata.Sources);
        }

        [Fact]
        public void GetPackageSpec_WhenSourcesValueIsValid_ReturnsSources()
        {
            PackageSource[] expectedResults = { new PackageSource(source: "a"), new PackageSource(source: "b") };
            string values = string.Join(",", expectedResults.Select(expectedResult => $"\"{expectedResult.Name}\":{{}}"));
            var json = $"{{\"restore\":{{\"sources\":{{{values}}}}}}}";
            PackageSpecs packageSpecs = GetPackageSpecs(json);

            Assert.Equal(expectedResults, packageSpecs.Improved.RestoreMetadata.Sources);
            Assert.Equal(expectedResults, packageSpecs.Baseline.RestoreMetadata.Sources);
        }

        [Fact]
        public void GetPackageSpec_WhenFilesValueIsEmptyObject_ReturnsEmptyFiles()
        {
            const string json = "{\"restore\":{\"files\":{}}}";
            PackageSpecs packageSpecs = GetPackageSpecs(json);

            Assert.Empty(packageSpecs.Improved.RestoreMetadata.Files);
            Assert.Empty(packageSpecs.Baseline.RestoreMetadata.Files);
        }

        [Fact]
        public void GetPackageSpec_WhenFilesValueIsValid_ReturnsFiles()
        {
            ProjectRestoreMetadataFile[] expectedResults =
            {
                new ProjectRestoreMetadataFile(packagePath: "a", absolutePath: "b"),
                new ProjectRestoreMetadataFile(packagePath: "c", absolutePath:"d")
            };
            string values = string.Join(",", expectedResults.Select(expectedResult => $"\"{expectedResult.PackagePath}\":\"{expectedResult.AbsolutePath}\""));
            var json = $"{{\"restore\":{{\"files\":{{{values}}}}}}}";
            PackageSpecs packageSpecs = GetPackageSpecs(json);

            Assert.Equal(expectedResults, packageSpecs.Improved.RestoreMetadata.Files);
            Assert.Equal(expectedResults, packageSpecs.Baseline.RestoreMetadata.Files);
        }

        [Fact]
        public void GetPackageSpec_WhenRestoreFrameworksValueIsEmptyObject_ReturnsEmptyFrameworks()
        {
            const string json = "{\"restore\":{\"frameworks\":{}}}";
            PackageSpecs packageSpecs = GetPackageSpecs(json);

            Assert.Empty(packageSpecs.Improved.RestoreMetadata.TargetFrameworks);
            Assert.Empty(packageSpecs.Baseline.RestoreMetadata.TargetFrameworks);
        }

        [Fact]
        public void GetPackageSpec_WhenRestoreFrameworksFrameworkNameValueIsValid_ReturnsFrameworks()
        {
            var expectedResult = new ProjectRestoreMetadataFrameworkInfo(NuGetFramework.ParseFolder("net472"));
            var json = $"{{\"restore\":{{\"frameworks\":{{\"{expectedResult.FrameworkName.GetShortFolderName()}\":{{}}}}}}}}";
            PackageSpecs packageSpecs = GetPackageSpecs(json);

            Assert.Collection(
                packageSpecs.Improved.RestoreMetadata.TargetFrameworks,
                actualResult => Assert.Equal(expectedResult, actualResult));
            Assert.Collection(
                packageSpecs.Baseline.RestoreMetadata.TargetFrameworks,
                actualResult => Assert.Equal(expectedResult, actualResult));
        }

        [Fact]
        public void GetPackageSpec_WhenRestoreFrameworksFrameworkValueHasProjectReferenceWithoutAssets_ReturnsFrameworks()
        {
            var projectReference = new ProjectRestoreReference()
            {
                ProjectUniqueName = "a",
                ProjectPath = "b"
            };
            var expectedResult = new ProjectRestoreMetadataFrameworkInfo(NuGetFramework.ParseFolder("net472"));

            expectedResult.ProjectReferences.Add(projectReference);

            var json = $"{{\"restore\":{{\"frameworks\":{{\"{expectedResult.FrameworkName.GetShortFolderName()}\":{{\"projectReferences\":{{" +
                $"\"{projectReference.ProjectUniqueName}\":{{\"projectPath\":\"{projectReference.ProjectPath}\"}}}}}}}}}}}}";
            PackageSpecs packageSpecs = GetPackageSpecs(json);

            Assert.Collection(
                packageSpecs.Improved.RestoreMetadata.TargetFrameworks,
                actualResult => Assert.Equal(expectedResult, actualResult));
            Assert.Collection(
                packageSpecs.Baseline.RestoreMetadata.TargetFrameworks,
                actualResult => Assert.Equal(expectedResult, actualResult));
        }

        [Fact]
        public void GetPackageSpec_WhenRestoreFrameworksFrameworkValueHasProjectReferenceWithAssets_ReturnsFrameworks()
        {
            var projectReference = new ProjectRestoreReference()
            {
                ProjectUniqueName = "a",
                ProjectPath = "b",
                IncludeAssets = LibraryIncludeFlags.Analyzers,
                ExcludeAssets = LibraryIncludeFlags.Native,
                PrivateAssets = LibraryIncludeFlags.Runtime
            };
            var expectedResult = new ProjectRestoreMetadataFrameworkInfo(NuGetFramework.ParseFolder("net472"));

            expectedResult.ProjectReferences.Add(projectReference);

            var json = $"{{\"restore\":{{\"frameworks\":{{\"{expectedResult.FrameworkName.GetShortFolderName()}\":{{\"projectReferences\":{{" +
                $"\"{projectReference.ProjectUniqueName}\":{{\"projectPath\":\"{projectReference.ProjectPath}\"," +
                $"\"includeAssets\":\"{projectReference.IncludeAssets}\",\"excludeAssets\":\"{projectReference.ExcludeAssets}\"," +
                $"\"privateAssets\":\"{projectReference.PrivateAssets}\"}}}}}}}}}}}}";
            PackageSpecs packageSpecs = GetPackageSpecs(json);

            Assert.Collection(
                packageSpecs.Improved.RestoreMetadata.TargetFrameworks,
                actualResult => Assert.Equal(expectedResult, actualResult));
            Assert.Collection(
                packageSpecs.Baseline.RestoreMetadata.TargetFrameworks,
                actualResult => Assert.Equal(expectedResult, actualResult));
        }

        [Fact]
        public void GetPackageSpec_WhenRestoreConfigFilePathsValueIsEmptyArray_ReturnsEmptyConfigFilePaths()
        {
            const string json = "{\"restore\":{\"configFilePaths\":[]}}";
            PackageSpecs packageSpecs = GetPackageSpecs(json);

            Assert.Empty(packageSpecs.Improved.RestoreMetadata.ConfigFilePaths);
            Assert.Empty(packageSpecs.Baseline.RestoreMetadata.ConfigFilePaths);
        }

        [Fact]
        public void GetPackageSpec_WhenRestoreConfigFilePathsValueIsValid_ReturnsConfigFilePaths()
        {
            string[] expectedResults = { "a", "b" };
            string values = string.Join(",", expectedResults.Select(expectedResult => $"\"{expectedResult}\""));
            var json = $"{{\"restore\":{{\"configFilePaths\":[{values}]}}}}";
            PackageSpecs packageSpecs = GetPackageSpecs(json);

            Assert.Equal(expectedResults, packageSpecs.Improved.RestoreMetadata.ConfigFilePaths);
            Assert.Equal(expectedResults, packageSpecs.Baseline.RestoreMetadata.ConfigFilePaths);
        }

        [Fact]
        public void GetPackageSpec_WhenRestoreFallbackFoldersValueIsEmptyArray_ReturnsEmptyFallbackFolders()
        {
            const string json = "{\"restore\":{\"fallbackFolders\":[]}}";
            PackageSpecs packageSpecs = GetPackageSpecs(json);

            Assert.Empty(packageSpecs.Improved.RestoreMetadata.FallbackFolders);
            Assert.Empty(packageSpecs.Baseline.RestoreMetadata.FallbackFolders);
        }

        [Fact]
        public void GetPackageSpec_WhenRestoreFallbackFoldersValueIsValid_ReturnsConfigFilePaths()
        {
            string[] expectedResults = { "a", "b" };
            string values = string.Join(",", expectedResults.Select(expectedResult => $"\"{expectedResult}\""));
            var json = $"{{\"restore\":{{\"fallbackFolders\":[{values}]}}}}";
            PackageSpecs packageSpecs = GetPackageSpecs(json);

            Assert.Equal(expectedResults, packageSpecs.Improved.RestoreMetadata.FallbackFolders);
            Assert.Equal(expectedResults, packageSpecs.Baseline.RestoreMetadata.FallbackFolders);
        }

        [Fact]
        public void GetPackageSpec_WhenRestoreOriginalTargetFrameworksValueIsEmptyArray_ReturnsEmptyOriginalTargetFrameworks()
        {
            const string json = "{\"restore\":{\"originalTargetFrameworks\":[]}}";
            PackageSpecs packageSpecs = GetPackageSpecs(json);

            Assert.Empty(packageSpecs.Improved.RestoreMetadata.OriginalTargetFrameworks);
            Assert.Empty(packageSpecs.Baseline.RestoreMetadata.OriginalTargetFrameworks);
        }

        [Fact]
        public void GetPackageSpec_WhenRestoreOriginalTargetFrameworksValueIsValid_ReturnsOriginalTargetFrameworks()
        {
            string[] expectedResults = { "a", "b" };
            string values = string.Join(",", expectedResults.Select(expectedResult => $"\"{expectedResult}\""));
            var json = $"{{\"restore\":{{\"originalTargetFrameworks\":[{values}]}}}}";
            PackageSpecs packageSpecs = GetPackageSpecs(json);

            Assert.Equal(expectedResults, packageSpecs.Improved.RestoreMetadata.OriginalTargetFrameworks);
            Assert.Equal(expectedResults, packageSpecs.Baseline.RestoreMetadata.OriginalTargetFrameworks);
        }

        [Fact]
        public void GetPackageSpec_WhenRestoreWarningPropertiesValueIsEmptyObject_ReturnsWarningProperties()
        {
            var expectedResult = new WarningProperties();
            const string json = "{\"restore\":{\"warningProperties\":{}}}";
            PackageSpecs packageSpecs = GetPackageSpecs(json);

            Assert.Equal(expectedResult, packageSpecs.Improved.RestoreMetadata.ProjectWideWarningProperties);
            Assert.Equal(expectedResult, packageSpecs.Baseline.RestoreMetadata.ProjectWideWarningProperties);
        }

        [Fact]
        public void GetPackageSpec_WhenRestoreWarningPropertiesValueIsValid_ReturnsWarningProperties()
        {
            var expectedResult = new WarningProperties(
                new HashSet<NuGetLogCode>() { NuGetLogCode.NU3000 },
                new HashSet<NuGetLogCode>() { NuGetLogCode.NU3001 },
                allWarningsAsErrors: true);
            var json = $"{{\"restore\":{{\"warningProperties\":{{\"allWarningsAsErrors\":{expectedResult.AllWarningsAsErrors.ToString().ToLowerInvariant()}," +
                $"\"warnAsError\":[\"{expectedResult.WarningsAsErrors.Single()}\"],\"noWarn\":[\"{expectedResult.NoWarn.Single()}\"]}}}}}}";
            PackageSpecs packageSpecs = GetPackageSpecs(json);

            Assert.Equal(expectedResult, packageSpecs.Improved.RestoreMetadata.ProjectWideWarningProperties);
            Assert.Equal(expectedResult, packageSpecs.Baseline.RestoreMetadata.ProjectWideWarningProperties);
        }

        [Fact]
        public void GetPackageSpec_WhenRestoreRestoreLockPropertiesValueIsEmptyObject_ReturnsRestoreLockProperties()
        {
            var expectedResult = new RestoreLockProperties();
            const string json = "{\"restore\":{\"restoreLockProperties\":{}}}";
            PackageSpecs packageSpecs = GetPackageSpecs(json);

            Assert.Equal(expectedResult, packageSpecs.Improved.RestoreMetadata.RestoreLockProperties);
            Assert.Equal(expectedResult, packageSpecs.Baseline.RestoreMetadata.RestoreLockProperties);
        }

        [Fact]
        public void GetPackageSpec_WhenRestoreRestoreLockPropertiesValueIsValid_ReturnsRestoreLockProperties()
        {
            var expectedResult = new RestoreLockProperties(
                restorePackagesWithLockFile: "a",
                nuGetLockFilePath: "b",
                restoreLockedMode: true);;
            var json = $"{{\"restore\":{{\"restoreLockProperties\":{{\"restoreLockedMode\":{expectedResult.RestoreLockedMode.ToString().ToLowerInvariant()}," +
                $"\"restorePackagesWithLockFile\":\"{expectedResult.RestorePackagesWithLockFile}\"," +
                $"\"nuGetLockFilePath\":\"{expectedResult.NuGetLockFilePath}\"}}}}}}";
            PackageSpecs packageSpecs = GetPackageSpecs(json);

            Assert.Equal(expectedResult, packageSpecs.Improved.RestoreMetadata.RestoreLockProperties);
            Assert.Equal(expectedResult, packageSpecs.Baseline.RestoreMetadata.RestoreLockProperties);
        }

        [Theory]
        [InlineData("null")]
        [InlineData("\"\"")]
        [InlineData("\"a\"")]
        public void GetPackageSpec_WhenRestorePackagesConfigPathValueIsValidAndProjectStyleValueIsNotPackagesConfig_DoesNotReturnPackagesConfigPath(
            string value)
        {
            var json = $"{{\"restore\":{{\"projectStyle\":\"PackageReference\",\"packagesConfigPath\":{value}}}}}";
            PackageSpecs packageSpecs = GetPackageSpecs(json);

            Assert.IsNotType<PackagesConfigProjectRestoreMetadata>(packageSpecs.Improved.RestoreMetadata);
            Assert.IsNotType<PackagesConfigProjectRestoreMetadata>(packageSpecs.Baseline.RestoreMetadata);
        }

        [Theory]
        [InlineData("null", null)]
        [InlineData("\"\"", "")]
        [InlineData("\"a\"", "a")]
        public void GetPackageSpec_WhenRestorePackagesConfigPathValueIsValidAndProjectStyleValueIsPackagesConfig_ReturnsPackagesConfigPath(
            string value,
            string expectedValue)
        {
            var json = $"{{\"restore\":{{\"projectStyle\":\"PackagesConfig\",\"packagesConfigPath\":{value}}}}}";
            PackageSpecs packageSpecs = GetPackageSpecs(json);

            Assert.IsType<PackagesConfigProjectRestoreMetadata>(packageSpecs.Improved.RestoreMetadata);
            Assert.Equal(expectedValue, ((PackagesConfigProjectRestoreMetadata)packageSpecs.Improved.RestoreMetadata).PackagesConfigPath);
            Assert.IsType<PackagesConfigProjectRestoreMetadata>(packageSpecs.Baseline.RestoreMetadata);
            Assert.Equal(expectedValue, ((PackagesConfigProjectRestoreMetadata)packageSpecs.Baseline.RestoreMetadata).PackagesConfigPath);
        }

        [Fact]
        public void GetPackageSpec_WhenRestoreSettingsValueIsEmptyObject_ReturnsRestoreSettings()
        {
            var expectedResult = new ProjectRestoreSettings();
            const string json = "{\"restoreSettings\":{}}";
            PackageSpecs packageSpecs = GetPackageSpecs(json);

            Assert.Equal(expectedResult, packageSpecs.Improved.RestoreSettings);
            Assert.Equal(expectedResult, packageSpecs.Baseline.RestoreSettings);
        }

        [Theory]
        [InlineData(null, false)]
        [InlineData(true, true)]
        [InlineData(false, false)]
        public void GetPackageSpec_WhenRestoreSettingsValueIsValid_ReturnsRestoreSettings(
            bool? value,
            bool expectedHide)
        {
            var expectedResult = new ProjectRestoreSettings() { HideWarningsAndErrors = expectedHide };
            var json = $"{{\"restoreSettings\":{{\"hideWarningsAndErrors\":{(value == null ? "null" : value.ToString().ToLowerInvariant())}}}}}";
            PackageSpecs packageSpecs = GetPackageSpecs(json);

            Assert.Equal(expectedResult, packageSpecs.Improved.RestoreSettings);
            Assert.Equal(expectedResult, packageSpecs.Baseline.RestoreSettings);
        }

        [Fact]
        public void GetPackageSpec_WhenRuntimesValueIsEmptyObject_ReturnsRuntimes()
        {
            var expectedResult = new RuntimeGraph();
            const string json = "{\"runtimes\":{}}";
            PackageSpecs packageSpecs = GetPackageSpecs(json);

            Assert.Equal(expectedResult, packageSpecs.Improved.RuntimeGraph);
            Assert.Equal(expectedResult, packageSpecs.Baseline.RuntimeGraph);
        }

        [Fact]
        public void GetPackageSpec_WhenRuntimesValueIsValidWithImports_ReturnsRuntimes()
        {
            var runtimeDescription = new RuntimeDescription(
                runtimeIdentifier: "a",
                inheritedRuntimes: new[] { "b", "c" },
                Enumerable.Empty<RuntimeDependencySet>());
            var expectedResult = new RuntimeGraph(new[] { runtimeDescription });
            var json = $"{{\"runtimes\":{{\"{runtimeDescription.RuntimeIdentifier}\":{{\"#import\":[" +
                $"{string.Join(",", runtimeDescription.InheritedRuntimes.Select(runtime => $"\"{runtime}\""))}]}}}}}}";
            PackageSpecs packageSpecs = GetPackageSpecs(json);

            Assert.Equal(expectedResult, packageSpecs.Improved.RuntimeGraph);
            Assert.Equal(expectedResult, packageSpecs.Baseline.RuntimeGraph);
        }

        [Fact]
        public void GetPackageSpec_WhenRuntimesValueIsValidWithDependencySet_ReturnsRuntimes()
        {
            var dependencySet = new RuntimeDependencySet(id: "b");
            var runtimeDescription = new RuntimeDescription(
                runtimeIdentifier: "a",
                inheritedRuntimes: Enumerable.Empty<string>(),
                runtimeDependencySets: new[] { dependencySet });
            var expectedResult = new RuntimeGraph(new[] { runtimeDescription });
            var json = $"{{\"runtimes\":{{\"{runtimeDescription.RuntimeIdentifier}\":{{\"{dependencySet.Id}\":{{}}}}}}}}";
            PackageSpecs packageSpecs = GetPackageSpecs(json);

            Assert.Equal(expectedResult, packageSpecs.Improved.RuntimeGraph);
            Assert.Equal(expectedResult, packageSpecs.Baseline.RuntimeGraph);
        }

        [Fact]
        public void GetPackageSpec_WhenRuntimesValueIsValidWithDependencySetWithDependency_ReturnsRuntimes()
        {
            var dependency = new RuntimePackageDependency("c", VersionRange.Parse("[1.2.3,4.5.6)"));
            var dependencySet = new RuntimeDependencySet(id: "b", new[] { dependency });
            var runtimeDescription = new RuntimeDescription(
                runtimeIdentifier: "a",
                inheritedRuntimes: Enumerable.Empty<string>(),
                runtimeDependencySets: new[] { dependencySet });
            var expectedResult = new RuntimeGraph(new[] { runtimeDescription });
            var json = $"{{\"runtimes\":{{\"{runtimeDescription.RuntimeIdentifier}\":{{\"{dependencySet.Id}\":{{" +
                $"\"{dependency.Id}\":\"{dependency.VersionRange.ToLegacyString()}\"}}}}}}}}";
            PackageSpecs packageSpecs = GetPackageSpecs(json);

            Assert.Equal(expectedResult, packageSpecs.Improved.RuntimeGraph);
            Assert.Equal(expectedResult, packageSpecs.Baseline.RuntimeGraph);
        }

        [Fact]
        public void GetPackageSpec_WhenSupportsValueIsEmptyObject_ReturnsSupports()
        {
            var expectedResult = new RuntimeGraph();
            const string json = "{\"supports\":{}}";
            PackageSpecs packageSpecs = GetPackageSpecs(json);

            Assert.Equal(expectedResult, packageSpecs.Improved.RuntimeGraph);
            Assert.Equal(expectedResult, packageSpecs.Baseline.RuntimeGraph);
        }

        [Fact]
        public void GetPackageSpec_WhenSupportsValueIsValidWithCompatibilityProfiles_ReturnsSupports()
        {
            var profile = new CompatibilityProfile(name: "a");
            var expectedResult = new RuntimeGraph(new[] { profile });
            var json = $"{{\"supports\":{{\"{profile.Name}\":{{}}}}}}";
            PackageSpecs packageSpecs = GetPackageSpecs(json);

            Assert.Equal(expectedResult, packageSpecs.Improved.RuntimeGraph);
            Assert.Equal(expectedResult, packageSpecs.Baseline.RuntimeGraph);
        }

        [Fact]
        public void GetPackageSpec_WhenSupportsValueIsValidWithCompatibilityProfilesAndFrameworkRuntimePairs_ReturnsSupports()
        {
            FrameworkRuntimePair[] restoreContexts = new[]
            {
                new FrameworkRuntimePair(NuGetFramework.Parse("net472"), "b"),
                new FrameworkRuntimePair(NuGetFramework.Parse("net48"), "c")
            };
            var profile = new CompatibilityProfile(name: "a", restoreContexts);
            var expectedResult = new RuntimeGraph(new[] { profile });
            var json = $"{{\"supports\":{{\"{profile.Name}\":{{" +
                $"\"{restoreContexts[0].Framework.GetShortFolderName()}\":\"{restoreContexts[0].RuntimeIdentifier}\"," +
                $"\"{restoreContexts[1].Framework.GetShortFolderName()}\":[\"{restoreContexts[1].RuntimeIdentifier}\"]}}}}}}";
            PackageSpecs packageSpecs = GetPackageSpecs(json);

            Assert.Equal(expectedResult, packageSpecs.Improved.RuntimeGraph);
            Assert.Equal(expectedResult, packageSpecs.Baseline.RuntimeGraph);
        }

        [Fact]
        public void GetPackageSpec_WhenScriptsValueIsEmptyObject_ReturnsScripts()
        {
            const string json = "{\"scripts\":{}}";
            PackageSpecs packageSpecs = GetPackageSpecs(json);

            Assert.Empty(packageSpecs.Improved.Scripts);
            Assert.Empty(packageSpecs.Baseline.Scripts);
        }

        [Fact]
        public void GetPackageSpec_WhenScriptsValueIsInvalid_Throws()
        {
            var json = "{\"scripts\":{\"a\":0}}";

            AssertEquivalentFileFormatExceptions(json);
        }

        [Fact]
        public void GetPackageSpec_WhenScriptsValueIsValid_ReturnsScripts()
        {
            const string name0 = "a";
            const string name1 = "b";
            const string script0 = "c";
            const string script1 = "d";
            const string script2 = "e";

            var json = $"{{\"scripts\":{{\"{name0}\":\"{script0}\",\"{name1}\":[\"{script1}\",\"{script2}\"]}}}}";
            PackageSpecs packageSpecs = GetPackageSpecs(json);

            Assert.Collection(
                packageSpecs.Improved.Scripts,
                actualResult =>
                {
                    Assert.Equal(name0, actualResult.Key);
                    Assert.Collection(
                        actualResult.Value,
                        actualScript => Assert.Equal(script0, actualScript));
                },
                actualResult =>
                {
                    Assert.Equal(name1, actualResult.Key);
                    Assert.Collection(
                        actualResult.Value,
                        actualScript => Assert.Equal(script1, actualScript),
                        actualScript => Assert.Equal(script2, actualScript));
                });
            Assert.Equal(packageSpecs.Baseline.Scripts, packageSpecs.Improved.Scripts);
        }

        [Theory]
        [InlineData("null", null)]
        [InlineData("\"\"", "")]
        [InlineData("\"a\"", "a")]
        public void GetPackageSpec_WhenTitleValueIsValid_ReturnsTitle(string value, string expectedResult)
        {
            var json = $"{{\"title\":{value}}}";

            PackageSpecs packageSpecs = GetPackageSpecs(json);

            Assert.Equal(expectedResult, packageSpecs.Improved.Title);
            Assert.Equal(expectedResult, packageSpecs.Baseline.Title);
        }

        [Fact]
        public void GetPackageSpec_WhenNameIsNull_RestoreMetadataProvidesFallbackName()
        {
            const string expectedResult = "a";
            var json = $"{{\"restore\":{{\"projectName\":\"{expectedResult}\"}}}}";

            PackageSpecs packageSpecs = GetPackageSpecs(json);

            Assert.Equal(expectedResult, packageSpecs.Improved.Name);
            Assert.Equal(expectedResult, packageSpecs.Baseline.Name);
        }

        [Theory]
        [InlineData("{\"restore\":{\"projectJsonPath\":\"a\"}}")]
        [InlineData("{\"restore\":{\"projectPath\":\"a\"}}")]
        [InlineData("{\"restore\":{\"projectJsonPath\":\"a\",\"projectPath\":\"b\"}}")]
        public void GetPackageSpec_WhenFilePathIsNull_RestoreMetadataProvidesFallbackFilePath(string json)
        {
            const string expectedResult = "a";

            PackageSpecs packageSpecs = GetPackageSpecs(json);

            Assert.Equal(expectedResult, packageSpecs.Improved.FilePath);
            Assert.Equal(expectedResult, packageSpecs.Baseline.FilePath);
        }

        private static void AssertEquivalentExceptionTypes<T>(string json) where T : Exception
        {
            Assert.Throws<T>(() => GetPackageSpecImproved(json));
            Assert.Throws<T>(() => GetPackageSpecBaseline(json));
        }

        private static void AssertEquivalentFileFormatExceptions(string json, bool verifyExceptionMessages = true)
        {
            FileFormatException exceptionImproved = Assert.Throws<FileFormatException>(() => GetPackageSpecImproved(json));
            FileFormatException exceptionBaseline = Assert.Throws<FileFormatException>(() => GetPackageSpecBaseline(json));

            Exception innermostBaselineException = GetInnermostException(exceptionBaseline);

            AssertEquivalentFileFormatExceptions(exceptionBaseline, exceptionImproved, verifyExceptionMessages);
        }

        private static void AssertEquivalentFileFormatExceptions(FileFormatException exceptionBaseline, FileFormatException exceptionImproved, bool verifyExceptionMessages)
        {
            if (verifyExceptionMessages)
            {
                Assert.Equal(exceptionBaseline.Message, exceptionImproved.Message);
            }

            Assert.Equal(exceptionBaseline.Line, exceptionImproved.Line);
            Assert.Equal(exceptionBaseline.Column, exceptionImproved.Column);

            AssertEquivalentExceptions(exceptionBaseline, exceptionImproved, verifyExceptionMessages);
        }

        private static void AssertEquivalentExceptions(Exception exceptionBaseline, Exception exceptionImproved, bool verifyMessage)
        {
            if (exceptionBaseline.InnerException == null)
            {
                Assert.Null(exceptionImproved.InnerException);
            }
            else
            {
                Assert.NotNull(exceptionImproved.InnerException);

                Assert.Equal(exceptionBaseline.InnerException.GetType().FullName, exceptionImproved.InnerException.GetType().FullName);

                if (exceptionBaseline.InnerException is FileFormatException)
                {
                    Assert.IsType<FileFormatException>(exceptionImproved.InnerException);

                    AssertEquivalentFileFormatExceptions(
                        (FileFormatException)exceptionBaseline.InnerException,
                        (FileFormatException)exceptionImproved.InnerException,
                        verifyMessage);
                }
                else
                {
                    AssertEquivalentExceptions(
                        exceptionBaseline.InnerException,
                        exceptionImproved.InnerException,
                        verifyMessage);
                }
            }
        }

        private static void AssertEqual(DownloadDependency expectedResult, DownloadDependency actualResult)
        {
            Assert.NotNull(expectedResult);
            Assert.NotNull(actualResult);

            Assert.Equal(expectedResult.Name, actualResult.Name);
            Assert.Equal(expectedResult.VersionRange, actualResult.VersionRange);
        }

        private static Exception GetInnermostException(Exception exception)
        {
            Exception innermostException = exception;

            while (innermostException.InnerException != null)
            {
                innermostException = innermostException.InnerException;
            }

            return innermostException;
        }

        private static PackageSpec GetPackageSpecBaseline(string json)
        {
            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(json)))
            {
                return JsonPackageSpecReader.GetPackageSpec(stream, name: null, packageSpecPath: null, snapshotValue: null);
            }
        }

        private static PackageSpec GetPackageSpecImproved(string json)
        {
            using (var stringReader = new StringReader(json))
            using (var jsonReader = new JsonTextReader(stringReader))
            {
                return MemoryEfficientJsonPackageSpecReader.GetPackageSpec(jsonReader, packageSpecPath: null);
            }
        }

        private static PackageSpecs GetPackageSpecs(string json)
        {
            PackageSpec baselinePackageSpec = GetPackageSpecBaseline(json);
            PackageSpec fastPackageSpec = GetPackageSpecImproved(json);

            return new PackageSpecs(baselinePackageSpec, fastPackageSpec);
        }

        private static Dependencies GetDependencies(string json)
        {
            PackageSpecs packageSpecs = GetPackageSpecs(json);

            return new Dependencies(
                packageSpecs.Baseline.Dependencies.Single(),
                packageSpecs.Improved.Dependencies.Single());
        }

        private static Frameworks GetFrameworks(string json)
        {
            PackageSpecs packageSpecs = GetPackageSpecs(json);

            return new Frameworks(
                packageSpecs.Baseline.TargetFrameworks.Single(),
                packageSpecs.Improved.TargetFrameworks.Single());
        }

        private static FrameworksDependencies GetFrameworksDependencies(string json)
        {
            Frameworks frameworks = GetFrameworks(json);

            return new FrameworksDependencies(
                frameworks.Baseline.Dependencies.Single(),
                frameworks.Improved.Dependencies.Single());
        }

        private static FrameworksFrameworkReferences GetFrameworksFrameworkReferences(string json)
        {
            Frameworks frameworks = GetFrameworks(json);

            return new FrameworksFrameworkReferences(
                frameworks.Baseline.FrameworkReferences.Single(),
                frameworks.Improved.FrameworkReferences.Single());
        }

        private sealed class Exceptions<T> where T : Exception
        {
            internal T Baseline { get; }
            internal T Improved { get; }

            internal Exceptions(T baseline, T improved)
            {
                Baseline = baseline;
                Improved = improved;
            }
        }

        private sealed class Dependencies
        {
            internal LibraryDependency Baseline { get; }
            internal LibraryDependency Improved { get; }

            internal Dependencies(LibraryDependency baseline, LibraryDependency improved)
            {
                Baseline = baseline;
                Improved = improved;
            }
        }

        private sealed class Frameworks
        {
            internal TargetFrameworkInformation Baseline { get; }
            internal TargetFrameworkInformation Improved { get; }

            internal Frameworks(TargetFrameworkInformation baseline, TargetFrameworkInformation improved)
            {
                Baseline = baseline;
                Improved = improved;
            }
        }

        private sealed class FrameworksDependencies
        {
            internal LibraryDependency Baseline { get; }
            internal LibraryDependency Improved { get; }

            internal FrameworksDependencies(LibraryDependency baseline, LibraryDependency improved)
            {
                Baseline = baseline;
                Improved = improved;
            }
        }

        private sealed class FrameworksFrameworkReferences
        {
            internal FrameworkDependency Baseline { get; }
            internal FrameworkDependency Improved { get; }

            internal FrameworksFrameworkReferences(FrameworkDependency baseline, FrameworkDependency improved)
            {
                Baseline = baseline;
                Improved = improved;
            }
        }

        private sealed class PackageSpecs
        {
            internal PackageSpec Baseline { get; }
            internal PackageSpec Improved { get; }

            internal PackageSpecs(PackageSpec baseline, PackageSpec improved)
            {
                Baseline = baseline;
                Improved = improved;
            }
        }
    }
}
