using System.IO;

namespace Kudu.Services.Test.Infrastructure {
    public static class TestPathHelper {
        private static readonly string KuduRoot = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..");
        public static readonly string ServicesSitePath = Path.Combine(KuduRoot, "Kudu.Services.Web");
        public static readonly string TestRepositoryRootPath = Path.Combine(KuduRoot, "tests", "repositories");
        public static readonly string AppsPath = Path.Combine(KuduRoot, "apps");
    }
}
