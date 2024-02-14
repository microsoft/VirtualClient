// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Abstractions;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;
    using Polly;
    using VirtualClient.Common.Extensions;

    /// <summary>
    /// Methods for extending the functionality of the 
    /// file system class, and related classes. (i.e. IFile, IPath, etc.)
    /// </summary>
    public static class FileSystemExtensions
    {
        private const string SettingsBeginComment = "# VC Settings Begin";
        private const string SettingsEndComment = "# VC Settings End";

        /// <summary>
        /// Evaluates if the given fully qualified file can be found in the file system, throws if can not be found.
        /// </summary>
        /// <param name="fileHandler">Interface to interact with files in the file system.</param>
        /// <param name="file">A fully qualified path to a file (i.e C:\App\Tools\MyTool\mytool.exe)</param>
        /// <param name="errorMessage">An error message to use instead of the default message.</param>
        public static void ThrowIfFileDoesNotExist(this IFile fileHandler, string file, string errorMessage = null)
        {
            fileHandler.ThrowIfNull(nameof(fileHandler));
            file.ThrowIfNullOrWhiteSpace(nameof(file));

            if (!fileHandler.Exists(file))
            {
                throw new FileNotFoundException(errorMessage ?? $"The file '{file}' could not be found.");
            }
        }

        /// <summary>
        /// Attempts to delete a file with transient issue retry handling.
        /// </summary>
        /// <param name="fileHandler">An interface to the filesystem to interact on a per file basis.</param>
        /// <param name="file">the fully qualified path to the file to delete.</param>
        /// <param name="retryPolicy">The retry policy to apply to the deletion.</param>
        public static Task DeleteAsync(this IFile fileHandler, string file, IAsyncPolicy retryPolicy = null)
        {
            fileHandler.ThrowIfNull(nameof(fileHandler));
            file.ThrowIfNullOrWhiteSpace(nameof(file));

            // IOException is thrown when a process still has a descriptor open on file. This is retryable
            // when a process is closing.
            // https://docs.microsoft.com/en-us/dotnet/api/system.io.file.delete?view=net-5.0
            return (retryPolicy ?? Policy.Handle<IOException>().WaitAndRetryAsync(10, (attempts) => TimeSpan.FromSeconds(Math.Pow(attempts, 2)))).ExecuteAsync(() =>
            {
                try
                {
                    fileHandler.Delete(file);
                }
                catch (FileNotFoundException)
                {
                    // This can happen in certain scenarios. The outcome is the same as the one
                    // expected...the file no longer exists!
                }
                catch (DirectoryNotFoundException)
                {
                    // This can happen in certain scenarios. The outcome is the same as the one
                    // expected...the file no longer exists!
                }

                return Task.CompletedTask;

            });
        }

        /// <summary>
        /// Attempts to delete a directory with transient issue retry handling.
        /// </summary>
        /// <param name="directoryHandler">An interface for file system interactions.</param>
        /// <param name="directory">the fully qualified path to the directory to delete.</param>
        /// <param name="recursive">True if subdirectories of the parent should be deleted. Default = true.</param>
        /// <param name="retryPolicy">The retry policy to apply to the deletion.</param>
        public static Task DeleteAsync(this IDirectory directoryHandler, string directory, bool recursive = true, IAsyncPolicy retryPolicy = null)
        {
            directoryHandler.ThrowIfNull(nameof(directoryHandler));
            directory.ThrowIfNullOrWhiteSpace(nameof(directory));

            // IOException is thrown when a process still has a descriptor open on file. This is retryable
            // when a process is closing.
            // https://docs.microsoft.com/en-us/dotnet/api/system.io.file.delete?view=net-5.0
            return (retryPolicy ?? Policy.Handle<IOException>().WaitAndRetryAsync(10, (attempts) => TimeSpan.FromSeconds(Math.Pow(attempts, 2)))).ExecuteAsync(() =>
            {
                try
                {
                    directoryHandler.Delete(directory, recursive);
                }
                catch (FileNotFoundException)
                {
                    // This can happen in certain scenarios. The outcome is the same as the one
                    // expected...the file no longer exists!
                }
                catch (DirectoryNotFoundException)
                {
                    // This can happen in certain scenarios. The outcome is the same as the one
                    // expected...the file no longer exists!
                }

                return Task.CompletedTask;

            });
        }

        /// <summary>
        /// Replaces the pattern from the file contents.
        /// </summary>
        /// <param name="fileHandler">An interface to the filesystem to interact on a per file basis.</param>
        /// <param name="file">The fully qualified path to the file in which we want to replace the pattern.</param>
        /// <param name="pattern">Pattern to replace from the file contents.</param>
        /// <param name="replacement">Replacement text for the pattern.</param>
        /// <param name="options">RegexOptions: A bitwise combination of the enumeration values that provide options for matching.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        public static async Task ReplaceInFileAsync(this IFile fileHandler, string file, string pattern, string replacement, CancellationToken cancellationToken, RegexOptions options = RegexOptions.None)
        {
            FileSystemExtensions.ThrowIfFileDoesNotExist(fileHandler, file);
            string fileContent = await fileHandler.ReadAllTextAsync(file, cancellationToken)
                .ConfigureAwait(false);

            fileContent = Regex.Replace(fileContent, pattern, replacement, options);

            await fileHandler.WriteAllTextAsync(file, fileContent, cancellationToken)
                .ConfigureAwait(false);
        }
    }
}
