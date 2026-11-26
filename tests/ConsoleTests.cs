// Licensed under the MIT License
// https://github.com/sator-imaging/DotnetTool-StaticImport

namespace SatorImaging.DotnetTool.StaticImport.Core
{
    public class ConsoleTests
    {
        [Test]
        public static void ReadKeyTest()
        {
            if (!Console.CanReadKey)
            {
                Console.WriteWarning("cannot read key. skipped.");
                return;
            }

            var key = Console.ReadKey("ReadKey timeout test (1,000ms): ", 1000);
            Console.WriteLine($"Modifiers: '{key.Modifiers}'  Key: '{key.Key}'  KeyChar: '{key.KeyChar}'");
        }
    }
}
