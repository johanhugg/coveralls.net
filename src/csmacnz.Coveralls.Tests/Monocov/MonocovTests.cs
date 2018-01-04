﻿using System;
using System.IO;
using csmacnz.Coveralls.Tests.TestAdapters;
using csmacnz.Coveralls.Tests.TestHelpers;
using Newtonsoft.Json.Linq;
using Xunit;

namespace csmacnz.Coveralls.Tests.Monocov
{
    public class MonocovTests
    {
        [Fact]
        public void EmptyReport_GivenAnOutput_OutputsSamplePost()
        {
            var outfile = "TestingOutput.xml";
            var (fileSystem, coverageFolderPath) = BuildMonocovReportFolder();

            var results = CoverallsTestRunner.RunCoveralls(
                $"--monocov -i {coverageFolderPath} --dryrun --output {outfile} --repoToken MYTESTREPOTOKEN",
                fileSystem);

            CoverallsAssert.RanSuccessfully(results);
            var savedFile = fileSystem.TryLoadFile(outfile);
            Assert.True(savedFile.HasValue, "Expected file to exist in fileSystem");
            var savedFileData = savedFile.ValueOr(" ");
            Assert.Contains(@"""repo_token"":""MYTESTREPOTOKEN""", savedFileData, StringComparison.Ordinal);
            Assert.Contains(@"""service_job_id"":""0""", savedFileData, StringComparison.Ordinal);
            Assert.Contains(@"""service_name"":""coveralls.net""", savedFileData, StringComparison.Ordinal);
            Assert.Contains(@"""parallel"":false", savedFileData, StringComparison.Ordinal);
            var jObject = AssertValidJson(savedFileData);
            Assert.Equal("MYTESTREPOTOKEN", TryGetValue(jObject, "repo_token"));
            Assert.Equal("0", TryGetValue(jObject, "service_job_id"));
            Assert.Equal("coveralls.net", TryGetValue(jObject, "service_name"));
            Assert.False(jObject.TryGetValue("parallel", out var isParallel) ? (bool?)isParallel : null);

            Assert.Collection(
                jObject.TryGetValue("source_files", out var sourceFiles) ? ((JArray)sourceFiles) : null,
                i => AssertIsValidCoverageFileData(i, "GameOfLife/Game.cs"),
                i => AssertIsValidCoverageFileData(i, "GameOfLife/Program.cs"),
                i => AssertIsValidCoverageFileData(i, "GameOfLife/World.cs"),
                i => AssertIsValidCoverageFileData(i, "GameOfLife/WorldBuilder.cs"),
                i => AssertIsValidCoverageFileData(i, "GameOfLife.xUnit.Tests/WorldTests.cs"));
        }

        [Fact]
        public void ReportWithOneFile_RunsSuccessfully()
        {
            var (fileSystem, coverageFolderPath) = BuildMonocovReportFolder();

            var results = DryRunCoverallsWithInputFile(coverageFolderPath, fileSystem);

            CoverallsAssert.RanSuccessfully(results);
        }

        [Fact]
        public void ReportWithOneFile_MultipleMode_RunsSuccessfully()
        {
            var (fileSystem, coverageFolderPath) = BuildMonocovReportFolder();

            var results = DryRunCoverallsMultiModeWithInputFile(coverageFolderPath, fileSystem);

            CoverallsAssert.RanSuccessfully(results);
        }

        private static (TestFileSystem fileSystem, string basePath) BuildMonocovReportFolder()
        {
            var fileSystem = new TestFileSystem();
            var filesDir = TestFileSystem.GenerateRandomAbsolutePath("monocov", "Sample1");

            fileSystem.AddFile(Path.Combine(filesDir, "class-GameOfLife.Game.xml"), Reports.MonoCovSamples.Sample1.Class_GameOfLife_Game);
            fileSystem.AddFile(Path.Combine(filesDir, "class-GameOfLife.Program.xml"), Reports.MonoCovSamples.Sample1.Class_GameOfLife_Program);
            fileSystem.AddFile(Path.Combine(filesDir, "class-GameOfLife.World.xml"), Reports.MonoCovSamples.Sample1.Class_GameOfLife_World);
            fileSystem.AddFile(Path.Combine(filesDir, "class-GameOfLife.WorldBuilder.xml"), Reports.MonoCovSamples.Sample1.Class_GameOfLife_WorldBuilder);
            fileSystem.AddFile(Path.Combine(filesDir, "class-GameOfLife.xUnit.Tests.WorldTests.xml"), Reports.MonoCovSamples.Sample1.Class_GameOfLife_Xunit_Tests_WorldTests);
            fileSystem.AddFile(Path.Combine(filesDir, "namespace-GameOfLife.xml"), Reports.MonoCovSamples.Sample1.Namespace_GameOfLife);
            fileSystem.AddFile(Path.Combine(filesDir, "namespace-GameOfLife.xUnit.Tests.xml"), Reports.MonoCovSamples.Sample1.Namespace_GameOfLife_Xunit);
            fileSystem.AddFile(Path.Combine(filesDir, "namespace-GameOfLife.xUnit.xml"), Reports.MonoCovSamples.Sample1.Namespace_GameOfLife_Xunit_Tests);
            fileSystem.AddFile(Path.Combine(filesDir, "project.xml"), Reports.MonoCovSamples.Sample1.Project);
            fileSystem.AddFile(Path.Combine(filesDir, "style.xsl"), Reports.MonoCovSamples.Sample1.Style);

            return (fileSystem, filesDir);
        }

        private static CoverallsRunResults DryRunCoverallsWithInputFile(
            string inputFolderPath,
            TestFileSystem testFileSystem)
        {
            return CoverallsTestRunner.RunCoveralls(
                $"--monocov -i {inputFolderPath} --dryrun --repoToken MYTESTREPOTOKEN",
                testFileSystem);
        }

        private static CoverallsRunResults DryRunCoverallsMultiModeWithInputFile(
            string inputFolderPath,
            TestFileSystem testFileSystem)
        {
            return CoverallsTestRunner.RunCoveralls(
                $"--multiple -i monocov={inputFolderPath} --dryrun --repoToken MYTESTREPOTOKEN",
                testFileSystem);
        }

        private static string TryGetValue(JObject jObject, string name)
        {
            return jObject.TryGetValue(name, out var token) ? (string)token : "<MISSING>";
        }

        private static void AssertIsValidCoverageFileData(JToken i, string fileName)
        {
            Assert.NotNull(i);
            Assert.Equal($"/home/travis/build/csMACnz/Coveralls.net-Samples/src/{fileName}", ((JObject)i).TryGetValue("name", out var value) ? (string)value : null);
        }

        private static JObject AssertValidJson(string savedFileData)
        {
            var result = JObject.Parse(savedFileData);
            Assert.NotNull(result);
            return result;
        }
    }
}
