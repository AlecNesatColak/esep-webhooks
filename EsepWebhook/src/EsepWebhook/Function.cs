using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Newtonsoft.Json;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace EsepWebhook
{
    public class Function
    {
        private static readonly HttpClient client = new HttpClient();

        /// <summary>
        /// A function that processes a GitHub Issues webhook event and posts to Slack.
        /// </summary>
        /// <param name="input"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task<string> FunctionHandler(object input, ILambdaContext context)
        {
            try
            {
                context.Logger.LogInformation($"FunctionHandler received: {input}");

                // Deserialize the input JSON
                dynamic json = JsonConvert.DeserializeObject<dynamic>(input.ToString());
                
                // Extract the issue URL from the JSON payload
                string issueUrl = json?.issue?.html_url;
                if (string.IsNullOrEmpty(issueUrl))
                {
                    throw new Exception("No issue URL found in the payload.");
                }
                
                // Prepare the Slack message payload
                string payload = JsonConvert.SerializeObject(new { text = $"Issue Created: {issueUrl}" });

                // Set up the Slack webhook URL from environment variables
                string slackUrl = Environment.GetEnvironmentVariable("SLACK_URL");
                if (string.IsNullOrEmpty(slackUrl))
                {
                    throw new Exception("SLACK_URL environment variable is not set.");
                }

                // Create the HTTP request to post to Slack
                var webRequest = new HttpRequestMessage(HttpMethod.Post, slackUrl)
                {
                    Content = new StringContent(payload, Encoding.UTF8, "application/json")
                };

                // Send the request asynchronously
                var response = await client.SendAsync(webRequest);
                response.EnsureSuccessStatusCode();

                // Read and return the response content
                return await response.Content.ReadAsStringAsync();
            }
            catch (Exception ex)
            {
                context.Logger.LogError($"Error in FunctionHandler: {ex.Message}");
                throw;
            }
        }
    }
}
