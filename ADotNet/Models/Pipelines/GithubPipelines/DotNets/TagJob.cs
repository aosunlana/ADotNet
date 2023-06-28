// ---------------------------------------------------------------------------
// Copyright (c) Hassan Habib & Shri Humrudha Jagathisun All rights reserved.
// Licensed under the MIT License.
// See License.txt in the project root for license information.
// ---------------------------------------------------------------------------

using System.Collections.Generic;
using System.ComponentModel;
using ADotNet.Models.Pipelines.GithubPipelines.DotNets.Tasks;
using YamlDotNet.Serialization;

namespace ADotNet.Models.Pipelines.GithubPipelines.DotNets
{
    public sealed class TagJob : Job
    {
        public TagJob(
            string runsOn,
            string dependsOn,
            string projectRelativePath,
            string githubToken,
            string branchName)
        {
            RunsOn = runsOn;
            Needs = new string[] { dependsOn };

            If =
                $"needs.{dependsOn}.result == 'success' && {System.Environment.NewLine}"
                + $"github.event.pull_request.merged && {System.Environment.NewLine}"
                + $"github.event.pull_request.base.ref == '{branchName}' && {System.Environment.NewLine}"
                + $"startsWith(github.event.pull_request.title, 'RELEASES:') && {System.Environment.NewLine}"
                + "contains(github.event.pull_request.labels.*.name, 'RELEASES')";

            Steps = new List<GithubTask>
                {
                    new CheckoutTaskV3
                    {
                        Name = "Checkout code",
                        With = new Dictionary<string, string>
                        {
                            { "token", githubToken }
                        }
                    },

                    new ConfigureGitTask()
                    {
                        Name = "Configure Git",
                    },

                    new ExtractProjectPropertyTask(
                        name: "Extract Version",
                        id: "extract_version",
                        projectRelativePath,
                        propertyName: "Version",
                        stepVariableName: "version_number",
                        runsOn),

                    new GithubTask()
                    {
                        Name = "Display Version",
                        Run = "echo \"Version number: ${{ steps.extract_version.outputs.version_number }}\""
                    },

                    new ExtractProjectPropertyTask(
                        name: $"Extract Package Release Notes",
                        id: "extract_package_release_notes",
                        projectRelativePath,
                        propertyName: "PackageReleaseNotes",
                        stepVariableName: "package_release_notes",
                        runsOn),

                    new GithubTask()
                    {
                        Name = "Display Package Release Notes",
                        Run =
                            "echo \"Package Release Notes: "
                            + "${{ steps.extract_package_release_notes.outputs.package_release_notes }}\""
                    },

                    new CreateGitHubTagTask(
                        tagName: "v${{ steps.extract_version.outputs.version_number }}",
                        tagMessage: "Release - v${{ steps.extract_version.outputs.version_number }}")
                    {
                        Name = "Create GitHub Tag",
                    },

                    new CreateGitHubReleaseTask(
                        releaseName: "Release - v${{ steps.extract_version.outputs.version_number }}",
                        tagName: "v${{ steps.extract_version.outputs.version_number }}",
                        releaseNotes: "${{ steps.extract_package_release_notes.outputs.package_release_notes }}",
                        githubToken)
                    {
                        Name = "Create GitHub Release",
                        Uses = "actions/create-release@v1",
                    },
                };
        }

        [YamlMember(Order = 0, DefaultValuesHandling = DefaultValuesHandling.OmitDefaults)]
        public new string Name { get; set; }

        [YamlMember(Order = 1, Alias = "runs-on")]
        public new string RunsOn { get; set; }

        [YamlMember(Order = 2, Alias = "needs", DefaultValuesHandling = DefaultValuesHandling.OmitDefaults)]
        public new string[] Needs { get; set; }

        [YamlMember(Order = 3, Alias = "if", DefaultValuesHandling = DefaultValuesHandling.OmitDefaults)]
        public new string If { get; set; }

        [YamlMember(Order = 4, DefaultValuesHandling = DefaultValuesHandling.OmitDefaults)]
        public new string Environment { get; set; }

        [YamlMember(Order = 5, DefaultValuesHandling = DefaultValuesHandling.OmitDefaults)]
        public new DefaultValues Defaults { get; set; }

        [YamlMember(Order = 6)]
        public new List<GithubTask> Steps { get; set; }

        [DefaultValue(0)]
        [YamlMember(Order = 7, Alias = "timeout-minutes", DefaultValuesHandling = DefaultValuesHandling.OmitDefaults)]
        public new int TimeoutInMinutes { get; set; }

        [YamlMember(Order = 8, DefaultValuesHandling = DefaultValuesHandling.OmitDefaults)]
        public new Strategy Strategy { get; set; }

        [YamlMember(Order = 9, Alias = "env", DefaultValuesHandling = DefaultValuesHandling.OmitDefaults)]
        public new Dictionary<string, string> EnvironmentVariables { get; set; }

        [YamlMember(Order = 10, DefaultValuesHandling = DefaultValuesHandling.OmitDefaults)]
        public new Dictionary<string, string> Outputs { get; set; }
    }
}
