using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Xml.Linq;
using Kudu.Core.SourceControl;
using Kudu.Web.Infrastructure;

namespace Kudu.Services.Test.Infrastructure {
    public class ApplicationManager : IDisposable {
        private readonly ISiteManager _siteManager;
        private readonly Site _site;
        private readonly string _appName;

        private ApplicationManager(ISiteManager siteManager, Site site, string appName) {
            _siteManager = siteManager;
            _site = site;
            _appName = appName;
        }

        public string GitUrl {
            get {
                return GetCloneUrl(_site, RepositoryType.Git);
            }
        }

        public string HgUrl {
            get {
                return GetCloneUrl(_site, RepositoryType.Mercurial);
            }
        }

        public string RepositoryServiceUrl {
            get {
                return _site.ServiceUrl + "scm";
            }
        }

        public string RepositoryPath {
            get {
                return Path.Combine(TestPathHelper.AppsPath, _appName, "repository");
            }
        }

        public string WebRootPath {
            get {
                return Path.Combine(TestPathHelper.AppsPath, _appName, "wwwroot");
            }
        }

        public string DeploymentCachePath {
            get {
                return Path.Combine(TestPathHelper.AppsPath, _appName, "deployments");
            }
        }

        void IDisposable.Dispose() {
            _siteManager.DeleteSite(_site.SiteName, _site.ServiceAppName);
            SetSiteAppSetting("enableAuthentication", "true");
        }

        public static ApplicationManager CreateApplication() {
            string applicationName = Guid.NewGuid().ToString().Substring(0, 4);
            ISiteManager siteManager = CreateSiteManager();
            Site site = siteManager.CreateSite(applicationName);
            SetSiteAppSetting("enableAuthentication", "false");

            // Sleep so IIS has a little time to start the site
            Thread.Sleep(1000);

            return new ApplicationManager(siteManager, site, applicationName);
        }

        private static void SetSiteAppSetting(string key, string value) {
            string path = Path.Combine(TestPathHelper.ServicesSitePath, "web.config");

            XDocument document = null;
            using (var stream = File.OpenRead(path)) {
                document = XDocument.Load(stream);
            }

            // We're assuming the app settings is there
            var appSettings = document.Descendants().Single(e => e.Name == "appSettings");

            var keyElement = (from entry in appSettings.Elements("add")
                             let keyAttr = entry.Attribute("key")
                             where keyAttr != null && keyAttr.Value.Equals(key)
                             select entry).Single();

            keyElement.SetAttributeValue("value", value);

            document.Save(path);
        }

        private static SiteManager CreateSiteManager() {
            return new SiteManager(TestPathHelper.ServicesSitePath);
        }

        private string GetCloneUrl(Site site, RepositoryType type) {
            string prefix = site.ServiceUrl + _appName;
            return prefix + (type == RepositoryType.Git ? ".git" : String.Empty);
        }
    }
}
