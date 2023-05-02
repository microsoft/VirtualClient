// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using VirtualClient.Contracts;

    /// <summary>
    /// 
    /// </summary>
    public static class DefaultFileBlobDescriptorBuilder
    {
        private const string Default = nameof(Default);

        /// <summary>
        /// 
        /// </summary>
        public static ConcurrentDictionary<string, Func<VirtualClientComponent, string, FileUploadNotification>> CreateMethods { get; }
            = new ConcurrentDictionary<string, Func<VirtualClientComponent, string, FileUploadNotification>>(StringComparer.OrdinalIgnoreCase)
            {
                [DefaultFileBlobDescriptorBuilder.Default] = DefaultFileBlobDescriptorBuilder.CreateDefaultCreationMethod();
            };

        /// <summary>
        /// 
        /// </summary>
        /// <param name="component"></param>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public static FileUploadNotification CreateNotification(VirtualClientComponent component, string filePath)
        {
            FileUploadNotification notification = null;

            string structure = "Default";
            if (!string.IsNullOrWhiteSpace(component.ContentLogStructure))
            {
                structure = component.ContentLogStructure;
            }

            if (!notificationBuilders.TryGetValue(structure, out Func<VirtualClientComponent, FileUploadNotification> notificationBuilder))
            {
                throw new DependencyException(
                    $"Content log structure '{structure}' defined in the component parameters is not registered. The content log structure must must be " +
                    $"registered before attempting to upload logs/content to target storage resources.",
                    ErrorReason.DependencyNotFound);
            }

            notification = notificationBuilder.Invoke(component);

            return notification;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="logStructure"></param>
        /// <param name="notificationCreateMethod"></param>
        /// <param name="replace"></param>
        public static void Register(string logStructure, Func<VirtualClientComponent, string, FileUploadNotification> notificationCreateMethod, bool replace = false)
        {
            if (!DefaultFileBlobDescriptorBuilder.CreateMethods.ContainsKey(logStructure) || replace)
            {
                DefaultFileBlobDescriptorBuilder.CreateMethods[logStructure] = notificationCreateMethod;
            }
        }

        private static Func<VirtualClientComponent, string, FileUploadNotification> CreateDefaultCreationMethod()
        {
            return (component, filePath) =>
            {
                List<string> parts = new List<string>();
                parts.Add(component.AgentId);

                if (component.AgentId != null)
                {
                }

                FileUploadNotification notification = new FileUploadNotification
                {
                    ContainerName = component.ExperimentId,
                    BlobName = null,
                    ContentEncoding = null,
                    ContentType = 
                };

                return notification;
            };
        }
    }
}
