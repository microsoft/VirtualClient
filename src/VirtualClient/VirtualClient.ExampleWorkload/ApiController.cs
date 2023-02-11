// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.DependencyInjection;
    using Newtonsoft.Json.Linq;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Rest;
    using VirtualClient.Contracts;
    using VirtualClient.Properties;

    /// <summary>
    /// Example REST API controller for illustring server-side behavior.
    /// </summary>
    /// <remarks>
    /// Introduction to ASP.NET Core
    /// https://docs.microsoft.com/en-us/aspnet/core/?view=aspnetcore-2.1
    ///
    /// ASP.NET Core MVC Controllers
    /// https://docs.microsoft.com/en-us/aspnet/core/mvc/controllers/actions?view=aspnetcore-2.1
    ///
    /// Kestrel Web Server (Self-Hosting)
    /// https://docs.microsoft.com/en-us/aspnet/core/fundamentals/servers/kestrel?view=aspnetcore-2.1
    /// 
    /// Async/Await/ConfigureAwait Overview
    /// https://www.skylinetechnologies.com/Blog/Skyline-Blog/December_2018/async-await-configureawait
    /// </remarks>
    [ApiController]
    public class ApiController : ControllerBase
    {
        private static IDictionary<int, string> responseFiles = new Dictionary<int, string>
        {
            { 0, Resources.Response1 },
            { 1, Resources.Response2 },
            { 2, Resources.Response3 },
            { 3, Resources.Response4 }
        };

        private Random randomGen;
        private List<ExampleApiClient> apiClients;

        /// <summary>
        /// Initializes a new instance of the <see cref="ApiController"/> class.
        /// </summary>
        public ApiController(IEnumerable<Uri> targetServerUris = null)
        {
            this.apiClients = new List<ExampleApiClient>();
            this.randomGen = new Random();

            if (targetServerUris?.Any() == true)
            {
                foreach (Uri serverUri in targetServerUris)
                {
                    IRestClient restClient = new RestClientBuilder()
                        .AlwaysTrustServerCertificate()
                        .AddAcceptedMediaType(MediaType.Json)
                        .Build();

                    this.apiClients.Add(new ExampleApiClient(restClient, serverUri));
                }
            }
        }



        /// <summary>
        /// Returns a response to the caller with something in it.
        /// </summary>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <response code="200">OK. The API is online.</response>
        /// <response code="500">Internal Server Error. An unexpected error occurred on the server.</response>
        [HttpGet("/api/something")]
        [Produces("application/json")]
        [Description("Returns a response to the caller with something in it.")]
        [ProducesResponseType(typeof(JObject), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetSomethingAsync(CancellationToken cancellationToken)
        {
            IActionResult response = null;

            try
            {
                if (this.apiClients?.Any() != true)
                {
                    // API is acting in server-mode where it will serve the requests directly.
                    int responseFile = this.randomGen.Next(0, 4);
                    string responseContent = ApiController.responseFiles[responseFile];
                    response = this.Ok(responseContent.RemoveWhitespace());
                }
                else
                {
                    // API is functioning in proxy-mode where it will proxy the requests to another
                    // server for handling.
                    int roundRobinServer = this.randomGen.Next(0, this.apiClients.Count);
                    ExampleApiClient serverClient = this.apiClients[roundRobinServer];

                    HttpResponseMessage targetServerResponse = await serverClient.GetSomethingAsync(cancellationToken)
                        .ConfigureAwait(false);

                    if (!targetServerResponse.IsSuccessStatusCode)
                    {
                        response = new ObjectResult((await targetServerResponse.Content.ReadAsJsonAsync<ProblemDetails>()
                            .ConfigureAwait(false)));
                    }
                    else
                    {
                        response = this.Ok((await targetServerResponse.Content.ReadAsStreamAsync()
                            .ConfigureAwait(false)));
                    }
                }
            }
            catch (Exception exc)
            {
                response = new ObjectResult(this.CreateErrorDetails(
                    StatusCodes.Status500InternalServerError,
                    exc.Message,
                    exc.ToDisplayFriendlyString(true)));
            }

            return response;
        }

        private ProblemDetails CreateErrorDetails(int statusCode, string title, string details)
        {
            return new ProblemDetails
            {
                Detail = details,
                Instance = $"{this.Request?.Method} {this.Request?.Path.Value}",
                Status = statusCode,
                Title = title
            };
        }
    }
}
