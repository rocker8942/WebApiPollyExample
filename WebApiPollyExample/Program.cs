using System.Net;
using Polly;
using Polly.CircuitBreaker;
using WebApiPollyExample;

// define retry policy using polly for http request
var retryPolicy = Policy.HandleResult<HttpResponseMessage>(result => 
        result.StatusCode == HttpStatusCode.NotFound)
    .WaitAndRetryAsync(2, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));

// define circuit breaker policy using polly for http request
var circuitBreakerPolicy = Policy.HandleResult<HttpResponseMessage>(result => 
        result.StatusCode == HttpStatusCode.NotFound)
    .CircuitBreakerAsync(2, TimeSpan.FromSeconds(10), OnBreak, OnReset, OnHalfOpen);

// define fallback policy using polly for http request
var fallbackPolicy = Policy.HandleResult<HttpResponseMessage>(result => 
        result.StatusCode == HttpStatusCode.NotFound)
    .FallbackAsync(context =>
    {
        Console.WriteLine("Fallback policy executed");
        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
    });

// 3 Polly policies are combined into a policyWrap. The order of the policies is important.
// retry 2 times -> circuit breaker (if 2 times failed, circuit break open for 10 seconds) -> fallback (return OK)
//
var policyWrap = Policy.WrapAsync(fallbackPolicy, circuitBreakerPolicy, retryPolicy);

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddScoped<IApiService, ApiService>();

builder.Services.AddHttpClient("ResilientClient")
    .SetHandlerLifetime(TimeSpan.FromMinutes(5)) // Sample: default lifetime is 2 minutes
    .AddPolicyHandler(policyWrap);

var app = builder.Build();

app.UseHttpsRedirection();

app.MapGet("/", async (IApiService apiService) =>
{
    if (circuitBreakerPolicy.CircuitState == CircuitState.Open)
    {
        return "Circuit breaker is open";
    }

    var httpResponseMessage = await apiService.CallApi();

    return httpResponseMessage.StatusCode switch
    {
        HttpStatusCode.OK => "OK",
        HttpStatusCode.NotFound => "Not Found",
        _ => "Unknown"
    };
});

app.Run();

void OnBreak(DelegateResult<HttpResponseMessage> result, TimeSpan timeSpan)
{
    Console.WriteLine("Circuit breaker opened");
}

void OnReset()
{
    Console.WriteLine("Circuit breaker reset");
}

void OnHalfOpen()
{
    Console.WriteLine("Circuit breaker half open");
}
