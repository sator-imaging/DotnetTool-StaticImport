// Licensed under the MIT License
// https://github.com/sator-imaging/DotnetTool-StaticImport

using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SatorImaging.DotnetTool.StaticImport.Core
{
    public class FileProviderTests
    {
        // A dummy provider for a custom scheme
        private class DummyProvider : IFileProvider
        {
            public ValueTask<byte[]?> TryGetContentAsync(string uri, CancellationToken ct = default) => new(Encoding.UTF8.GetBytes("dummy content"));
            public ValueTask<DateTimeOffset?> TryGetLastModifiedDateAsync(string uri, CancellationToken ct = default) => new(DateTimeOffset.Now);
            public string GetOutputFilePath(Uri uri, string outputDirOrFilePath, string? outputFilePrefix) => "dummy_path";
        }

        [Test]
        public static void FileProviderRegistrationTest()
        {
            AppCore.RegisterFileProvider("dummy", new DummyProvider());
            Console.WriteImportantLine("\nFileProviderRegistrationTest was done\n");
        }

        [Test]
        public static void FileProviderTest()
        {
            // Create a dummy file for testing
            const string testFilePath = "test.txt";
            const string testFileContent = "Hello, World!";
            File.WriteAllText(testFilePath, testFileContent);
            var fileProvider = LocalFileProvider.Instance;

            // Test GetContent
            var content = fileProvider.TryGetContentAsync(testFilePath).GetAwaiter().GetResult();
            Debug.Assert(content != null);
            Debug.Assert(Encoding.UTF8.GetString(content) == testFileContent);

            // Test GetLastModifiedDate
            var lastModified = fileProvider.TryGetLastModifiedDateAsync(testFilePath).GetAwaiter().GetResult();
            Debug.Assert(lastModified != null);

            // Test non-existent file
            var nonExistentPath = "nonexistent.txt";
            content = fileProvider.TryGetContentAsync(nonExistentPath).GetAwaiter().GetResult();
            Debug.Assert(content == null);
            lastModified = fileProvider.TryGetLastModifiedDateAsync(nonExistentPath).GetAwaiter().GetResult();
            Debug.Assert(lastModified == null);

            File.Delete(testFilePath);
            Console.WriteImportantLine("\nFileProviderTest was done\n");
        }
    }
}
