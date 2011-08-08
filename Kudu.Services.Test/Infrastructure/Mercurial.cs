using System;
using System.IO;
using Kudu.Core.Infrastructure;
using Mercurial;

namespace Kudu.Services.Test.Infrastructure {
    public static class Mercurial {
        public static IDisposable Prepare(string repositoryName, string initialCommitMessage = "Initial commit") {
            string repositoryPath = Path.Combine(TestPathHelper.TestRepositoryRootPath, repositoryName);
            var repo = new Repository(repositoryPath);
            repo.Init();
            repo.AddRemove();
            repo.Commit(initialCommitMessage);

            return new DisposableAction(() => {
                string hgFolder = Path.Combine(repositoryPath, ".hg");
                FileSystemHelpers.DeleteDirectorySafe(hgFolder);
            });
        }

        public static void Push(string repositoryName, string url) {
            string repositoryPath = Path.Combine(TestPathHelper.TestRepositoryRootPath, repositoryName);
            var repository = new Repository(repositoryPath);
            repository.Push(url);
        }
    }
}
