using AventStack.ExtentReports;
using AventStack.ExtentReports.Reporter;
using System;
using System.IO;

namespace MovieDBTests.Utils
{
    public static class ReportManager
    {
        private static ExtentReports? _extent;
        private static ExtentTest? _currentTest;

        public static void Init(string reportName = "TestReport")
        {
            var dir = Path.Combine(AppContext.BaseDirectory, "TestReports");
            Directory.CreateDirectory(dir);
            var htmlReporter = new ExtentHtmlReporter(Path.Combine(dir, $"{reportName}_{DateTime.Now:yyyyMMdd_HHmmss}.html"));
            _extent = new ExtentReports();
            _extent.AttachReporter(htmlReporter);
        }

        public static void CreateTest(string testName)
        {
            if (_extent == null) throw new InvalidOperationException("ExtentReports not initialized");
            _currentTest = _extent.CreateTest(testName);
        }

        public static void LogInfo(string message) => _currentTest?.Info(message);
        public static void LogPass(string message) => _currentTest?.Pass(message);
        public static void LogFail(string message) => _currentTest?.Fail(message);

        public static void Flush() => _extent?.Flush();
    }
}
