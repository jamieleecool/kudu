using System.IO;
using System.Linq;
using Kudu.Core.Infrastructure;
using Kudu.Core.SourceControl;
using Kudu.Services.Test.Infrastructure;
using Xunit;

using Hg = Kudu.Services.Test.Infrastructure.Mercurial;

namespace Kudu.Services.Test {
    public class ScmIntegrationTests {
        [Fact]
        public void CreateRepository() {
            RunCreateRepository(RepositoryType.Git);
            RunCreateRepository(RepositoryType.Mercurial);
        }

        [Fact]
        public void ScmWriteActions() {
            const string gitWorking = @"{""FilesChanged"":2,""Deletions"":0,""Insertions"":2,""ChangeSet"":null,""Files"":{""foo.txt"":{""Deletions"":0,""Insertions"":0,""Binary"":false,""Status"":1,""DiffLines"":[{""Type"":1,""Text"":""+This is a test"",""LeftLine"":null,""RightLine"":null}]},""foo2.txt"":{""Deletions"":0,""Insertions"":0,""Binary"":false,""Status"":1,""DiffLines"":[{""Type"":1,""Text"":""+This is another file"",""LeftLine"":null,""RightLine"":null}]}}}";
            RunRepositoryWriteActions(RepositoryType.Git, gitWorking);
            const string hgWorking = @"{""FilesChanged"":2,""Deletions"":0,""Insertions"":2,""ChangeSet"":null,""Files"":{""foo.txt"":{""Deletions"":0,""Insertions"":0,""Binary"":false,""Status"":1,""DiffLines"":[{""Type"":0,""Text"":""@@ -0,0 +1,1 @@\n"",""LeftLine"":null,""RightLine"":null},{""Type"":1,""Text"":""+This is a test\n"",""LeftLine"":null,""RightLine"":1},{""Type"":0,""Text"":""\\ No newline at end of file\n"",""LeftLine"":0,""RightLine"":2}]},""foo2.txt"":{""Deletions"":0,""Insertions"":0,""Binary"":false,""Status"":1,""DiffLines"":[{""Type"":0,""Text"":""@@ -0,0 +1,1 @@\n"",""LeftLine"":null,""RightLine"":null},{""Type"":1,""Text"":""+This is another file\n"",""LeftLine"":null,""RightLine"":1},{""Type"":0,""Text"":""\\ No newline at end of file\n"",""LeftLine"":0,""RightLine"":2}]}}}";
            RunRepositoryWriteActions(RepositoryType.Mercurial, hgWorking);
        }

        [Fact]
        public void GitPushSimpleRepository() {            
            using (var app = ApplicationManager.CreateApplication()) {
                using (Git.Prepare("SimpleRepository")) {
                    // Arrange
                    var repository = new RemoteRepository(app.RepositoryServiceUrl);

                    // Act
                    Git.Push("SimpleRepository", app.GitUrl);

                    // Assert
                    Assert.NotNull(repository.CurrentId);
                    var branches = repository.GetBranches().ToList();
                    Assert.Equal(1, branches.Count);
                    Assert.Equal("master", branches[0].Name);
                    var changes = repository.GetChanges().ToList();
                    Assert.Equal(1, changes.Count);
                    Assert.Equal("Initial commit", changes[0].Message.Trim());
                    Assert.True(File.Exists(Path.Combine(app.RepositoryPath, "default.cshtml")));
                }
            }
        }

        [Fact]
        public void HgPushSimpleRepository() {            
            using (var app = ApplicationManager.CreateApplication()) {
                using (Hg.Prepare("SimpleRepository")) {
                    try {
                        // Arrange
                        var repository = new RemoteRepository(app.RepositoryServiceUrl);
                        
                        // Act
                        Hg.Push("SimpleRepository", app.HgUrl);

                        // Assert
                        Assert.NotNull(repository.CurrentId);
                        var branches = repository.GetBranches().ToList();
                        Assert.Equal(1, branches.Count);
                        Assert.Equal("default", branches[0].Name);
                        var changes = repository.GetChanges().ToList();
                        Assert.Equal(1, changes.Count);
                        Assert.Equal("Initial commit", changes[0].Message.Trim());
                        Assert.True(File.Exists(Path.Combine(app.RepositoryPath, "default.cshtml")));
                    }
                    finally {
                        new RemoteRepositoryManager(app.RepositoryServiceUrl).Delete();
                    }
                }
            }
        }

        private static void RunRepositoryWriteActions(RepositoryType repositoryType, string working) {
            using (var app = ApplicationManager.CreateApplication()) {
                var repositoryManager = new RemoteRepositoryManager(app.RepositoryServiceUrl);
                var repository = new RemoteRepository(app.RepositoryServiceUrl);
                var client = HttpClientHelper.Create(app.RepositoryServiceUrl);

                repositoryManager.CreateRepository(repositoryType);

                string f1 = Path.Combine(app.RepositoryPath, "foo.txt");
                string f2 = Path.Combine(app.RepositoryPath, "foo2.txt");
                File.WriteAllText(f1, "This is a test");
                File.WriteAllText(f2, "This is another file");

                var status = repository.GetStatus().ToList();
                Assert.Equal(2, status.Count);
                Assert.Equal("foo.txt", status[0].Path);
                Assert.Equal(ChangeType.Untracked, status[0].Status);
                Assert.Equal("foo2.txt", status[1].Path);
                Assert.Equal(ChangeType.Untracked, status[1].Status);

                repository.AddFile("foo.txt");
                repository.AddFile("foo2.txt");
                status = repository.GetStatus().ToList();

                Assert.Equal(2, status.Count);
                Assert.Equal("foo.txt", status[0].Path);
                Assert.Equal(ChangeType.Added, status[0].Status);
                Assert.Equal("foo2.txt", status[1].Path);
                Assert.Equal(ChangeType.Added, status[1].Status);
                Assert.Equal(working, client.Get("working").ReadAsString());

                repository.RevertFile("foo2.txt");
                status = repository.GetStatus().ToList();
                Assert.Equal(1, status.Count);
                Assert.Equal("foo.txt", status[0].Path);
                Assert.Equal(ChangeType.Added, status[0].Status);


                var commit = repository.Commit("John Doe <johndoe@hotmail.com>", "Initial commit");
                Assert.NotNull(commit);
                Assert.Equal("johndoe@hotmail.com", commit.AuthorEmail);
                Assert.Equal("John Doe", commit.AuthorName);
                Assert.Equal("Initial commit", commit.Message.Trim());
                Assert.NotNull(commit.Id);

                var log = repository.GetChanges().ToList();
                Assert.Equal(1, log.Count);
                Assert.Equal(commit.Id, log[0].Id);
                Assert.Equal(commit.Message.Trim(), log[0].Message.Trim());
            }
        }

        private static void RunCreateRepository(RepositoryType repositoryType) {
            using (var app = ApplicationManager.CreateApplication()) {
                RemoteRepositoryManager repositoryManager = null;
                try {
                    repositoryManager = new RemoteRepositoryManager(app.RepositoryServiceUrl);

                    Assert.Equal(RepositoryType.None, repositoryManager.GetRepositoryType());

                    repositoryManager.CreateRepository(repositoryType);

                    Assert.Equal(repositoryType, repositoryManager.GetRepositoryType());
                }
                finally {
                    if (repositoryManager != null) {
                        repositoryManager.Delete();
                    }
                }
            }
        }
    }
}
