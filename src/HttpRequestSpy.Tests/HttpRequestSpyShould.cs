﻿using System.Net.Http.Json;

namespace HttpRequestSpy.Tests;

public partial class HttpRequestSpyShould
{
    private const string AbsoluteRoute = "http://domain/path/to/resource";
        
    [Fact]
    public async Task Record_a_clone_of_an_HttpRequestMessage_which_content_can_be_read_multiple_times()
    {
        var payload = JsonContent.Create(new { Property = "P" });

        var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, $"{AbsoluteRoute}")
        {
            Content = payload,
        };
            
        var record = await RecordedHttpRequest.From(httpRequestMessage);

        Check.That(record.Request).Not.IsSameReferenceAs(httpRequestMessage);
        Check.That(record.Request.Content).Not.IsSameReferenceAs(httpRequestMessage.Content);
    }

    [Fact]
    public async Task Record_Many_Requests()
    {
        var spy = new HttpRequestSpy();

        await RegisterGetRequest();
        await HttpRequestSpyShould.RegisterPostWithJsonPayloadRequest();

        spy.HasRecordedRequests(2);
    }

    [Fact]
    public async Task Ensure_that_a_get_request_is_sent()
    {
        var spy = new HttpRequestSpy();

        await RegisterGetRequest();

        spy.AGetRequestTo("/path/to/resource").OccurredOnce();
    }
        
    [Fact]
    public async Task Ensure_that_a_get_request_is_not_sent()
    {
        var spy = new HttpRequestSpy();

        await RegisterGetRequest();

        spy.AGetRequestTo("/path/to/other/resource").NeverOccurred();
    }
        
    [Fact]
    public void Fail_When_No_Request_Recorded()
    {
        var spy = new HttpRequestSpy();

        Check.ThatCode(() => spy.AGetRequestTo("/path/to/other/resource").OccurredOnce())
            .Throws<HttpRequestSpyException>();
    }
        
    [Fact]
    public async Task Ensure_that_a_get_request_is_sent_to_an_absolute_uri()
    {
        var spy = new HttpRequestSpy();

        await RegisterGetRequest();

        spy.AGetRequestTo(AbsoluteRoute).OccurredOnce();
    }

    [Fact]
    public async Task Fails_when_an_expected_request_is_not_sent()
    {
        var spy = new HttpRequestSpy();

        await RegisterGetRequest();
        await HttpRequestSpyShould.RegisterPostWithJsonPayloadRequest();

        Check.ThatCode(() =>
                spy.AGetRequestTo("/path/to/other/resource").OccurredOnce())
            .Throws<HttpRequestSpyException>();
    }

    [Fact]
    public async Task Ensure_that_a_get_request_is_sent_twice()
    {
        var spy = new HttpRequestSpy();

        await RegisterGetRequest();
        await RegisterGetRequest();

        spy.AGetRequestTo("/path/to/resource").OccurredTwice();
    }

    [Fact]
    public async Task Ensure_that_a_get_request_never_occured()
    {
        var spy = new HttpRequestSpy();

        await RegisterGetRequest();

        spy.AGetRequestTo("/path/to/unknown/resource").NeverOccurred();
    }

    [Fact]
    public async Task Ensure_that_a_get_request_with_a_query_string_is_sent()
    {
        var spy = new HttpRequestSpy();

        await RegisterGetRequest("param=1&param2=value");

        spy.AGetRequestTo("/path/to/resource")
            .WithQuery("param2=value&param=1").OccurredOnce();
    }
        
    [Fact]
    public async Task Ensure_that_a_get_request_with_a_query_is_sent()
    {
        var spy = new HttpRequestSpy();

        await RegisterGetRequest("param=1&param2=value&list=a&list=b");

        spy.AGetRequestTo("/path/to/resource")
            .WithQuery(new
            {
                param = "1",
                param2 = "value",
                list = "a,b"
            }).OccurredOnce();
    }

    [Fact]
    public async Task Ensure_that_a_post_request_is_sent()
    {
        var spy = new HttpRequestSpy();

        await HttpRequestSpyShould.RegisterPostWithJsonPayloadRequest();

        spy.APostRequestTo("/path/to/resource")
            .OccurredOnce();
    }
    
    [Fact]
    public async Task Ensure_that_a_post_is_recorded_without_heading_slash()
    {
        var spy = new HttpRequestSpy();

        await HttpRequestSpyShould.RegisterPostWithJsonPayloadRequest();

        spy.APostRequestTo("path/to/resource")
            .OccurredOnce();
    }

    [Fact]
    public async Task Ensure_that_a_delete_request_is_sent()
    {
        var spy = new HttpRequestSpy();

        await RegisterRequest(HttpMethod.Delete);

        spy.ADeleteRequestTo("/path/to/resource").OccurredOnce();
    }
    [Fact]
    public async Task Fails_when_query_string_does_not_match()
    {
        var spy = new HttpRequestSpy();
            
        await RegisterGetRequest(query:"param=1&param2=hello");

        Check.ThatCode(() =>
            spy.AGetRequestTo("/path/to/resource")
                .WithQuery(new
                {
                    param = "2"
                })
                .OccurredOnce()
        ).Throws<HttpRequestSpyException>();
    }
        
    [Fact]
    public async Task Ensure_that_a_get_request_with_query_string_parameter_was_sent()
    {
        var spy = new HttpRequestSpy();
            
        await RegisterGetRequest(query:"param=1&param2=hello");

        spy.AGetRequestTo("/path/to/resource")
            .WithQueryParam("param2", "hello")
            .OccurredOnce();
    }
        
    [Fact]
    public async Task Ensure_that_a_get_request_containing_a_query_string_parameter_was_sent()
    {
        var spy = new HttpRequestSpy();
            
        await RegisterGetRequest(query:"param=1&param2=hello");

        spy.AGetRequestTo("/path/to/resource")
            .WithQueryParam("param")
            .OccurredOnce();
    }
        
    [Fact]
    public async Task Fails_when_query_string_parameter_does_not_match()
    {
        var spy = new HttpRequestSpy();
            
        await RegisterGetRequest(query:"param=1&param2=hello");

        Check.ThatCode(() =>
            spy.AGetRequestTo("/path/to/resource")
                .WithQueryParam("param", "2")
                .OccurredOnce()
        ).Throws<HttpRequestSpyException>();
    }
    
    private static async Task RegisterGetRequest(string? query = null)
    {
        string? SanitizedQuery()
        {
            if (query is null)
            {
                return null;
            }

            return query.StartsWith("?") ? query : $"?{query}";
        }
            
        await RegisterRequest(HttpMethod.Get, $"{AbsoluteRoute}{SanitizedQuery()}");
    }

    private static async Task RegisterRequest(HttpMethod method, string route = AbsoluteRoute, object? payload = null)
    {
        var httpRequestMessage = new HttpRequestMessage(method, route);

        if (payload != null)
        {
            httpRequestMessage.Content = JsonContent.Create(payload);
        }

        var record = await RecordedHttpRequest.From(httpRequestMessage);
        HttpRequestSpy.Current?.RecordRequest(record);
    }
}