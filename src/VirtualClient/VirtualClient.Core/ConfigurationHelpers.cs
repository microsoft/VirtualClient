// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System.Collections.Generic;
    using System.IO;

    /// <summary>
    /// Static utilities for working with configuration files.
    /// </summary>
    public class ConfigurationHelpers
    {
        /// <summary>
        /// Replaces strings in a file with other strings.
        /// Used to replace placeholders in a configuration file.
        /// Not intended for use with large files.
        /// </summary>
        /// <param name="pathToFile">The path to the file which will be changed.</param>
        /// <param name="valuesToReplace">A dictionary where each key is a string to replace, and the value is what to replace the string with.</param>
        /// <param name="enableResets">Enables the same file to have placeholders replaced multiple times. Creates a duplicate file.</param>
        public static void ReplacePlaceholders(string pathToFile, Dictionary<string, string> valuesToReplace, bool enableResets = true)
        {
            if (pathToFile == null || !File.Exists(pathToFile))
            {
                throw new FileNotFoundException(pathToFile);
            }

            string pathToTempFile = pathToFile + ".vccopy";

            if (enableResets && !File.Exists(pathToTempFile))
            {
                File.Copy(pathToFile, pathToTempFile);
            }

            // This would likely need to be replaced if we started to use large files (500MB+)
            string fileText = File.ReadAllText(pathToTempFile);
            foreach ((string placeholder, string replacement) in valuesToReplace)
            {
                fileText = fileText.Replace(placeholder, replacement);
            }

            File.WriteAllText(pathToFile, fileText);
        }
    }
}
