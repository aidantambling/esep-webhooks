using System.Text;
using Amazon.Lambda.Core;
using Newtonsoft.Json;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace EsepWebhook;

public class Function
{
    
    /// <summary>
    /// A simple function that takes a string and does a ToUpper
    /// </summary>
    /// <param name="input"></param>
    /// <param name="context"></param>
    /// <returns></returns>
    public string FunctionHandler(object input, ILambdaContext context)
    {
        context.Logger.LogInformation($"FunctionHandler received: {input}");

        if (input == null) 
        {
            context.Logger.LogError("Input payload is null.");
            throw new ArgumentNullException(nameof(input), "Input payload cannot be null.");
        }

        dynamic json = JsonConvert.DeserializeObject<dynamic>(input.ToString());
        
        string issueUrl = json?.issue?.html_url;
        if (string.IsNullOrEmpty(issueUrl))
        {
            context.Logger.LogError("Issue URL not found in the input payload.");
            throw new Exception("Issue URL not found in the input payload.");
        }
        context.Logger.LogInformation($"Extracted issue URL: {issueUrl}");

        string slackUrl = Environment.GetEnvironmentVariable("SLACK_URL");
        if (string.IsNullOrEmpty(slackUrl))
        {
            context.Logger.LogError("SLACK_URL environment variable is not set.");
            throw new Exception("SLACK_URL environment variable is not set.");
        }
        context.Logger.LogInformation($"Slack URL retrieved: {slackUrl}");

        string payload = JsonConvert.SerializeObject(new { text = $"Issue Created: {issueUrl}" });
        context.Logger.LogInformation($"Payload for Slack: {payload}");
        
        var client = new HttpClient();
        var webRequest = new HttpRequestMessage(HttpMethod.Post, Environment.GetEnvironmentVariable("SLACK_URL"))
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json")
        };
    
        var response = client.Send(webRequest);
        using var reader = new StreamReader(response.Content.ReadAsStream());
            
        return reader.ReadToEnd();
    }
}