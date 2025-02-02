﻿using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NGitLab.Models;
using NGitLab.Tests.Docker;
using NUnit.Framework;

namespace NGitLab.Tests
{
    public class LintClientTests
    {
        private const string ValidCIYaml = @"
variables:
  CI_DEBUG_TRACE: ""true""
build:
  script:
    - echo test
";

        private const string InvalidCIYaml = @"
variables:
  CI_DEBUG_TRACE: ""true""
build:
  script:
    - echo test
  this_key_should_not_exist:
    - this should fail the linting
";

        [Test]
        [NGitLabRetry]
        public async Task LintValidCIYaml()
        {
            using var context = await GitLabTestContext.CreateAsync();
            var project = context.CreateProject();
            var lintClient = context.Client.Lint;

            var result = await context.Client.Lint.ValidateCIYamlContentAsync(project.Id.ToString(), ValidCIYaml, new(), CancellationToken.None);

            Assert.True(result.Valid);
            Assert.False(result.Errors.Any());
            Assert.False(result.Warnings.Any());
        }

        [Test]
        [NGitLabRetry]
        public async Task LintInvalidCIYaml()
        {
            using var context = await GitLabTestContext.CreateAsync();
            var project = context.CreateProject();
            var lintClient = context.Client.Lint;

            var result = await context.Client.Lint.ValidateCIYamlContentAsync(project.Id.ToString(), InvalidCIYaml, new(), CancellationToken.None);

            Assert.False(result.Valid);
            Assert.True(result.Errors.Any());
            Assert.False(result.Warnings.Any());
        }

        [Test]
        [NGitLabRetry]
        public async Task LintValidCIProjectYaml()
        {
            using var context = await GitLabTestContext.CreateAsync();
            var project = context.CreateProject();
            var lintClient = context.Client.Lint;

            context.Client.GetRepository(project.Id).Files.Create(new FileUpsert
            {
                Branch = project.DefaultBranch,
                CommitMessage = "test",
                Path = ".gitlab-ci.yml",
                Content = ValidCIYaml,
            });

            var result = await context.Client.Lint.ValidateProjectCIConfigurationAsync(project.Id.ToString(), new(), CancellationToken.None);

            Assert.True(result.Valid);
            Assert.False(result.Errors.Any());
            Assert.False(result.Warnings.Any());
        }

        [Test]
        [NGitLabRetry]
        public async Task LintInvalidProjectCIYaml()
        {
            using var context = await GitLabTestContext.CreateAsync();
            var project = context.CreateProject();
            var lintClient = context.Client.Lint;

            context.Client.GetRepository(project.Id).Files.Create(new FileUpsert
            {
                Branch = project.DefaultBranch,
                CommitMessage = "test",
                Path = ".gitlab-ci.yml",
                Content = InvalidCIYaml,
            });

            var result = await context.Client.Lint.ValidateProjectCIConfigurationAsync(project.Id.ToString(), new(), CancellationToken.None);

            Assert.False(result.Valid);
            Assert.True(result.Errors.Any());
            Assert.False(result.Warnings.Any());
        }
    }
}
