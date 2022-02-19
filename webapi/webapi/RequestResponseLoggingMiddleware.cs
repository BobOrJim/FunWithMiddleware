using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using System.Diagnostics;

namespace webapi
{
    public class RequestResponseLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private string _left;
        private string _right;
        public static long _initTime = DateTime.Now.Ticks;

        public RequestResponseLoggingMiddleware(RequestDelegate next, string Left, string Right)
        {
            _right = Right;
            _left = Left;
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {

            //First, get the incoming request
            var request = await FormatRequest(context.Request);

            var requestMethod = context.Request.Method;
            var requestPath = context.Request.Path;
            var requestProtocol = context.Request.Protocol;
            var requestHost = context.Request.Host;
            var requestBodyStream = context.Request.Body;
            StreamReader reader = new StreamReader(requestBodyStream);
            string requestBody = reader.ReadToEnd();
            Log.Information($"IN({(DateTime.Now.Ticks - _initTime)/10000}):  LEFT= {_left} RIGHT = {_right} HTTP-request = {requestMethod} {requestPath} {requestProtocol} \n {requestHost} \n {requestBody}");

            //Copy a pointer to the original response body stream
            var originalBodyStream = context.Response.Body;
            //Create a new memory stream...
            //using var responseBody = new MemoryStream(); //orginal
            var responseBody = new MemoryStream();
            //...and use that for the temporary response body
            context.Response.Body = responseBody;
            //Continue down the Middleware pipeline, eventually returning to this class
            await _next(context);
            //Format the response from the server
            var response = await FormatResponse(context.Response);
            //Copy the contents of the new memory stream (which contains the response) to the original stream, which is then returned to the client.
            await responseBody.CopyToAsync(originalBodyStream);

            var responseStatusCode = context.Response.StatusCode;
            var responseDate = context.Response.Headers.Date.ToString();
            var responseContentLength = context.Response.ContentLength;
            var responseContentType = context.Response.ContentType;
            var responseBody2 = response;
            Log.Information($"OUT({(DateTime.Now.Ticks - _initTime)/10000}): LEFT= {_left} RIGHT = {_right} HTTP-response = {responseStatusCode} {responseDate} {responseContentLength} {responseContentType} \n {responseBody2} \n\n");

        }




        private static async Task<string> FormatRequest(HttpRequest request)
        {
            var body = request.Body;

            //This line allows us to set the reader for the request back at the beginning of its stream.
            request.EnableBuffering();

            //We now need to read the request stream.  First, we create a new byte[] with the same length as the request stream...
            var buffer = new byte[Convert.ToInt32(request.ContentLength)];

            //...Then we copy the entire request stream into the new buffer.
            await request.Body.ReadAsync(buffer.AsMemory(0, buffer.Length)).ConfigureAwait(false);

            //We convert the byte[] into a string using UTF8 encoding...
            var bodyAsText = Encoding.UTF8.GetString(buffer);

            // reset the stream position to 0, which is allowed because of EnableBuffering()
            request.Body.Seek(0, SeekOrigin.Begin);

            return $"{request.Scheme} {request.Host}{request.Path} {request.QueryString} {bodyAsText}";
        }

        private static async Task<string> FormatResponse(HttpResponse response)
        {
            //We need to read the response stream from the beginning...
            response.Body.Seek(0, SeekOrigin.Begin);

            //...and copy it into a string
            string text = await new StreamReader(response.Body).ReadToEndAsync();

            //We need to reset the reader for the response so that the client can read it.
            response.Body.Seek(0, SeekOrigin.Begin);

            //Return the string for the response, including the status code (e.g. 200, 404, 401, etc.)
            return $"{text}";
            //return $"{response.StatusCode}: {text}";
        }
    }
}