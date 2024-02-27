// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Api
{
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using Newtonsoft.Json.Linq;
    using VirtualClient.Common.Contracts;
    using VirtualClient.Contracts;

    /// <summary>
    /// Class for producing EventHub channels
    /// </summary>
    public static class ApiHostingExtensions
    {
        /// <summary>
        /// Polls the target Virtual Client API until it returns a heartbeat.
        /// </summary>
        /// <param name="client">A client to the Virtual Client REST API.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        public static Task PollForHeartbeatAsync(this VirtualClientApiClient client, CancellationToken cancellationToken)
        {
            return Task.Run(() =>
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    HttpResponseMessage response = null;

                    try
                    {
                        response = client.GetHeartbeatAsync(CancellationToken.None).GetAwaiter().GetResult();

                        if (response.IsSuccessStatusCode)
                        {
                            break;
                        }
                    }
                    catch
                    {
                        // Expected when the API is not online.
                    }
                    finally
                    {
                        Task.Delay(5000).GetAwaiter().GetResult();
                    }
                }
            });
        }

        /// <summary>
        /// Creates a background monitoring task that calls the Virtual Client REST API
        /// to check for heartbeats.
        /// </summary>
        /// <param name="client">A client to the Virtual Client REST API.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        public static Task RunHeartbeatTest(this VirtualClientApiClient client, CancellationToken cancellationToken)
        {
            return Task.Run(() =>
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        HttpResponseMessage response = null;

                        try
                        {
                            response = client.GetHeartbeatAsync(CancellationToken.None).GetAwaiter().GetResult();
                        }
                        catch (Exception exc)
                        {
                            Console.WriteLine($"{Environment.NewLine}API Offline{Environment.NewLine}{exc.Message}");
                            if (exc.InnerException != null)
                            {
                                Console.WriteLine($"{Environment.NewLine}Inner Exception{Environment.NewLine}{exc.InnerException.Message}");
                            }
                        }
                        finally
                        {
                            if (response != null)
                            {
                                Console.WriteLine(
                                    $"{Environment.NewLine}{(response != null && response.IsSuccessStatusCode ? "API Online" : "API Offline")} " +
                                    $"(status code={response.StatusCode.ToString()})");
                            }
                        }
                    }
                    catch
                    {
                        // Do not crash the heartbeat monitor.
                    }
                    finally
                    {
                        Task.Delay(5000).GetAwaiter().GetResult();
                    }
                }
            });
        }

        /// <summary>
        /// Creates a background monitoring task that calls the Virtual Client REST API
        /// to check for heartbeats.
        /// </summary>
        /// <param name="client">A client to the Virtual Client REST API.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        public static Task RunStateTest(this VirtualClientApiClient client, CancellationToken cancellationToken)
        {
            return Task.Run(() =>
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        // Wait for the server-side instance to come online.
                        ApiHostingExtensions.PollForHeartbeatAsync(client, cancellationToken).GetAwaiter().GetResult();

                        client.DeleteStateAsync("TestState", CancellationToken.None)
                           .GetAwaiter().GetResult();

                        State state = new State(new Dictionary<string, IConvertible>
                        {
                            ["property1"] = "value1",
                            ["property2"] = 12345,
                            ["property3"] = true
                        });

                        // POST
                        // -----------------------------------------------------------------
                        HttpResponseMessage response = client.CreateStateAsync("TestState", JObject.FromObject(state), CancellationToken.None)
                            .GetAwaiter().GetResult();

                        Console.WriteLine();
                        Console.WriteLine($"POST Response: {response.StatusCode.ToString()}");
                        Console.WriteLine($"BODY:");

                        // Ensure deserialization works.
                        string responseContent = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                        Console.WriteLine(response.Content.ReadAsStringAsync().GetAwaiter().GetResult());
                        responseContent.FromJson<Item<State>>(); 

                        Task.Delay(10000).GetAwaiter().GetResult();

                        // GET
                        // -----------------------------------------------------------------
                        response = client.GetStateAsync("TestState", CancellationToken.None)
                            .GetAwaiter().GetResult();

                        Console.WriteLine();
                        Console.WriteLine($"GET Response: {response.StatusCode.ToString()}");
                        Console.WriteLine($"BODY:");

                        // Ensure deserialization works.
                        responseContent = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                        Item<State> stateInstance = responseContent.FromJson<Item<State>>();
                        Console.WriteLine(stateInstance.ToJson());

                        Task.Delay(10000).GetAwaiter().GetResult();

                        // PUT
                        // -----------------------------------------------------------------
                        stateInstance.Definition.Properties["property1"] = "value2";
                        response = client.UpdateStateAsync("TestState", JObject.FromObject(stateInstance), CancellationToken.None)
                            .GetAwaiter().GetResult();

                        Console.WriteLine();
                        Console.WriteLine($"PUT Response: {response.StatusCode.ToString()}");
                        Console.WriteLine($"BODY:");

                        // Ensure deserialization works.
                        responseContent = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                        stateInstance = responseContent.FromJson<Item<State>>();
                        Console.WriteLine(stateInstance.ToJson());

                        Task.Delay(10000).GetAwaiter().GetResult();

                        // DELETE
                        // -----------------------------------------------------------------
                        response = client.DeleteStateAsync("TestState", CancellationToken.None)
                            .GetAwaiter().GetResult();

                        Console.WriteLine();
                        Console.WriteLine($"DELETE Response: {response.StatusCode.ToString()}");
                    }
                    catch (Exception exc)
                    {
                        // Do not crash the test.
                        Console.WriteLine($"{Environment.NewLine}API Error{Environment.NewLine}{exc.Message}");
                    }
                    finally
                    {
                        Task.Delay(10000).GetAwaiter().GetResult();
                    }
                }
            });
        }
    }
}
