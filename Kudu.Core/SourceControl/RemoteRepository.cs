﻿using System.Collections.Generic;
using System.Net.Http;
using Kudu.Core.Infrastructure;
using Newtonsoft.Json;

namespace Kudu.Core.SourceControl {
    public class RemoteRepository : IRepository {
        private readonly HttpClient _client;

        public RemoteRepository(string serviceUrl) {
            _client = HttpClientHelper.Create(serviceUrl);
        }

        public string CurrentId {
            get {
                return _client.Get("id").EnsureSuccessful()
                              .Content
                              .ReadAsString();
            }
        }

        public void Initialize() {
            _client.Post("init", new StringContent(null))
                   .EnsureSuccessStatusCode();
        }

        public IEnumerable<Branch> GetBranches() {
            return _client.GetJson<IEnumerable<Branch>>("branches");
        }

        public IEnumerable<FileStatus> GetStatus() {
            return _client.GetJson<IEnumerable<FileStatus>>("status");
        }

        public IEnumerable<ChangeSet> GetChanges() {
            return _client.GetJson<IEnumerable<ChangeSet>>("log");
        }

        public IEnumerable<ChangeSet> GetChanges(int index, int limit) {
            return _client.GetJson<IEnumerable<ChangeSet>>("log?index=" + index + "&limit=" + limit);
        }

        public ChangeSetDetail GetDetails(string id) {
            return _client.GetJson<ChangeSetDetail>("details/" + id);
        }

        public ChangeSetDetail GetWorkingChanges() {
            return _client.GetJson<ChangeSetDetail>("working");
        }

        public void AddFile(string path) {
            _client.Post("add", new FormUrlEncodedContent(new Dictionary<string, string> {
                { "path", path }
            })).EnsureSuccessful();
        }

        public void RevertFile(string path) {
            _client.Post("remove", new FormUrlEncodedContent(new Dictionary<string, string> {
                { "path", path }
            })).EnsureSuccessful();
        }

        public ChangeSet Commit(string authorName, string message) {
            string json = _client.Post("commit", new FormUrlEncodedContent(new Dictionary<string, string> {
                { "name", authorName },
                { "message", message }
            })).EnsureSuccessful().Content.ReadAsString();

            return JsonConvert.DeserializeObject<ChangeSet>(json);
        }

        public void Update(string id) {
            _client.Post("update", new FormUrlEncodedContent(new Dictionary<string, string> {
                { "id", id }
            })).EnsureSuccessful();
        }        
    }
}
