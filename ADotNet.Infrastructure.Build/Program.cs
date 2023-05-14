// ---------------------------------------------------------------------------
// Copyright (c) Hassan Habib & Shri Humrudha Jagathisun All rights reserved.
// Licensed under the MIT License.
// See License.txt in the project root for license information.
// ---------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using ADotNet.Clients;
using ADotNet.Models.Pipelines.GithubPipelines.DotNets;
using ADotNet.Models.Pipelines.GithubPipelines.DotNets.Tasks;
using ADotNet.Models.Pipelines.GithubPipelines.DotNets.Tasks.SetupDotNetTaskV1s;

namespace ADotNet.Infrastructure.Build
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var aDotNetClient = new ADotNetClient();

            var githubPipeline = new GithubPipeline
            {
                Name = ".Net",

                OnEvents = new Events
                {
                    Push = new PushEvent
                    {
                        Tags = new string[] { "RELEASE" },
                        Branches = new string[] { "master" }
                    },

                    PullRequest = new PullRequestEvent
                    {
                        Branches = new string[] { "master" }
                    }
                },

                EnvironmentVariables = new Dictionary<string, string>
                {
                    { "IS_RELEASE_CANDIDATE", EnvironmentVariables.IsGitHubReleaseCandidate() }
                },

                Jobs = new Jobs
                {
                    Build = new BuildJob
                    {
                        RunsOn = BuildMachines.Windows2019,

                        Steps = new List<GithubTask>
                        {
                            new CheckoutTaskV2
                            {
                                Name = "Check out"
                            },

                            new SetupDotNetTaskV1
                            {
                                Name = "Setup .Net",

                                TargetDotNetVersion = new TargetDotNetVersion
                                {
                                    DotNetVersion = "7.0.100-preview.1.22110.4",
                                    IncludePrerelease = true
                                }
                            },

                            new RestoreTask
                            {
                                Name = "Restore"
                            },

                            new DotNetBuildTask
                            {
                                Name = "Build"
                            },

                            new TestTask
                            {
                                Name = "Test"
                            }
                        }
                    },
                    AddTag = new TagJob
                    {
                        If =
                        "github.event.pull_request.merged &&\r"
                        + "github.event.pull_request.base.ref == 'main' &&\r"
                        + "startsWith(github.event.pull_request.title, 'RELEASES:') &&\r"
                        + "contains(github.event.pull_request.labels.*.name, 'RELEASES')\r",

                        RunsOn = BuildMachines.UbuntuLatest,

                        Steps = new List<GithubTask>
                        {
                            new CheckoutTaskV2
                            {
                                Name = "Checkout code"
                            },

                            new ShellScriptTask
                            {
                                Name = "Extract Version Number",
                                Id = "extract_version",
                                Run = "echo \"::set-output name=version_number::$(grep -oP '(?<=<Version>)[^<]+' BuildTestApp/BuildTestApp.csproj)\""
                            },

                            new ShellScriptTask
                            {
                                Name = "Print Version Number",
                                Run = "echo \"Version number is ${{ steps.extract_version.outputs.version_number }}\""
                            },

                            new ShellScriptTask
                            {
                                Name = "Configure Git",
                                Run =
                                    "git config user.name \"Add Git Release Tag Action\""
                                    + "\r"
                                    + "git config user.email \"github.action@noreply.github.com\""
                            },

                            new CheckoutTaskV2
                            {
                                Name = "Authenticate with GitHub",
                                With = new Dictionary<string, string>
                                {
                                    { "token", "${{ secrets.PAT_FOR_TAGGING }}" }
                                }
                            },

                            new ShellScriptTask
                            {
                                Name = "Add Git Tag - Release",
                                Run =
                                    "git tag -a \"release-${{ steps.extract_version.outputs.version_number }}\" -m \"Release ${{ steps.extract_version.outputs.version_number }}\""
                                    + "\r"
                                    + "git push origin --tags"
                            },
                        }
                    }
                }
            };

            aDotNetClient.SerializeAndWriteToFile(githubPipeline, "../../../../.github/workflows/dotnet.yml");
        }
    }
}
