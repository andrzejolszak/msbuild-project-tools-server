using System.IO;
using Xunit.Abstractions;

#pragma warning disable xUnit2013 // Do not use equality check to check for collection size.

namespace MSBuildProjectTools.LanguageServer.Tests
{
    using Utilities;

    /// <summary>
    ///     Tests for <see cref="MSBuildTaskScanner"/>.
    /// </summary>
    public class TaskScannerTests
    {
        /// <summary>
        ///     Create a new <see cref="MSBuildTaskScanner"/> test suite.
        /// </summary>
        /// <param name="testOutput">
        ///     Output for the current test.
        /// </param>
        public TaskScannerTests(ITestOutputHelper testOutput)
        {
            if (testOutput == null)
                throw new System.ArgumentNullException(nameof(testOutput));

            TestOutput = testOutput;
        }

        /// <summary>
        ///     Output for the current test.
        /// </summary>
        private ITestOutputHelper TestOutput { get; }

        /// <summary>
        ///     Retrieve the full path of a task assembly supplied as part of the current framework.
        /// </summary>
        /// <param name="assemblyFileName">
        ///     The relative filename of the task assembly.
        /// </param>
        /// <returns>
        ///     The full path of the task assembly.
        /// </returns>
        private static string GetFrameworkTaskAssemblyFile(string assemblyFileName)
        {
            if (string.IsNullOrWhiteSpace(assemblyFileName))
                throw new System.ArgumentException($"Argument cannot be null, empty, or entirely composed of whitespace: {nameof(assemblyFileName)}.", nameof(assemblyFileName));

            var runtimeInfo = DotNetRuntimeInfo.GetCurrent();

            return Path.Combine(runtimeInfo.BaseDirectory,
                assemblyFileName.Replace('/', Path.DirectorySeparatorChar)
            );
        }
    }
}
