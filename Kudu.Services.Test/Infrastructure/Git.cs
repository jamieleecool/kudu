using System;
using System.IO;
using Kudu.Core.Infrastructure;
using Kudu.Core.SourceControl.Git;

namespace Kudu.Services.Test.Infrastructure {
    public static class Git {
        public static void Push(string repositoryName, string url, string branchName = "master") {
            Executable gitExe = GetGitExe(repositoryName);
            gitExe.Execute("push {0} {1}", url, branchName);
        }

        public static IDisposable Prepare(string repositoryName, string initialCommitMessage = "Initial commit") {
            Executable gitExe = GetGitExe(repositoryName);
            var repo = new GitExeRepository(gitExe.WorkingDirectory);

            gitExe.Execute("init");
            gitExe.Execute("add -A");
            repo.Commit("Test <test@test.com>", initialCommitMessage);

            return new DisposableAction(() => {
                string gitFolder = Path.Combine(gitExe.WorkingDirectory, ".git");
                FileSystemHelpers.DeleteDirectorySafe(gitFolder);
            });
        }

        private static Executable GetGitExe(string repositoryName) {
            string repositoryPath = Path.Combine(TestPathHelper.TestRepositoryRootPath, repositoryName);
            return new Executable(GitUtility.ResolveGitPath(), repositoryPath);
        }
    }
}
