namespace VirtualClient
{
    using System;
    using System.IO.Abstractions;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Text.Json.Nodes;
    using System.Threading;
    using System.Threading.Tasks;
    using VirtualClient.Common;
    using VirtualClient.Contracts;

    /// <summary>
    /// Provides features for managing requirements for docker images.
    /// </summary>
    public static class DockerUtility
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="imageName"></param>
        /// <param name="platformSpecifics"></param>
        /// <param name="processManager"></param>
        /// <param name="fileSystem"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        public static async Task<DependencyContainerStore> CreateDockerContainerReference(string imageName, PlatformSpecifics platformSpecifics, ProcessManager processManager, IFileSystem fileSystem, CancellationToken token)
        {
            // step 1: verify the image exists locally or remotely.
            string tempImageName = "vcImage01";
            string containerName = Guid.NewGuid().ToString();
            string imageDirectory = platformSpecifics.Combine(platformSpecifics.CurrentDirectory, "Images");
            string imagePath = fileSystem.Directory.GetFiles(imageDirectory).ToList().Where(x => x.StartsWith(imageName, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
            PlatformSpecifics platformSpecificsNew = null;

            if (string.IsNullOrEmpty(imagePath))
            {
                // todo: better error.
                throw new Exception("Image not found");
            }

            // step 2: verify docker is installed.
            // docker -v
            {
                var process = processManager.CreateProcess("docker.exe", "-v", "C:\\Program Files\\Docker\\Docker\\resources\\bin");
                await process.StartAndWaitAsync(token).ConfigureAwait(false);
                Console.WriteLine(process.StandardOutput);
            }

            // step3: build the image or use the existing build from ps1.
            // docker build -t {tempName} {imagePath}
            {
                var process = processManager.CreateProcess("docker.exe", $"build -t {tempImageName} {imagePath}", "C:\\Program Files\\Docker\\Docker\\resources\\bin");
                await process.StartAndWaitAsync(token).ConfigureAwait(false);
                Console.WriteLine(process.StandardOutput);
            }

            // step3.5 get full platform details from the image build. and verify it is valid and save it.
            // docker image inspect vc-ubuntu:22.04 --format '{{.Os}}/{{.Architecture}}'
            {
                var process = processManager.CreateProcess("docker.exe", $"image inspect {tempImageName}");
                await process.StartAndWaitAsync(token).ConfigureAwait(false);
                // Console.WriteLine(process.StandardOutput);
                platformSpecificsNew = DockerUtility.GetPlatform(process.StandardOutput.ToString());
                fileSystem.File.WriteAllText(platformSpecifics.Combine(platformSpecifics.StateDirectory, "ImageInspectOutput.txt"), process.StandardOutput.ToString());
            }

            // step4: build the container
            // docker run --name {containerName} {tempName}
            {
                var process = processManager.CreateProcess("docker.exe", $"build -name {containerName} {tempImageName}", "C:\\Program Files\\Docker\\Docker\\resources\\bin");
                await process.StartAndWaitAsync(token).ConfigureAwait(false);
                Console.WriteLine(process.StandardOutput);
            }

            // step5: save container inspect file in state.
            {
                var process = processManager.CreateProcess("docker.exe", $"build -name {containerName} {tempImageName}", "C:\\Program Files\\Docker\\Docker\\resources\\bin");
                await process.StartAndWaitAsync(token).ConfigureAwait(false);
                fileSystem.File.WriteAllText(platformSpecifics.Combine(platformSpecifics.StateDirectory, "ContainerInspectOutput.txt"), process.StandardOutput.ToString());
                // Console.WriteLine(process.StandardOutput);
            }

            return new DependencyContainerStore(imageName, containerName, platformSpecificsNew);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dockerInspectJson"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>

    }
}