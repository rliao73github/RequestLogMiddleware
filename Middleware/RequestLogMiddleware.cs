using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using System.Net;
using System.Diagnostics;
using System.Globalization;

namespace RequestLogMiddleware.Middleware
{
    public class RequestLogMiddleware
    {

        public class LogData
        {
            public IPAddress RemoteAddr { get; set; }
            public string User { get; set; }
            public int ResponseStatus { get; set; }

            public string RequestMethod { get; set; }
            public string RequestTimestamp { get; set; }
            public string RequestPath { get; set; }

            public string RequestProtocol { get; set; }
            public string UserAgent { get; set; }

            public string Payload { get; set; }

            public long DurationMs { get; set; }
        }

        private readonly RequestDelegate _next;
        private readonly ILogger _logger;

        public RequestLogMiddleware(RequestDelegate next, ILoggerFactory factory)
        {
            _next = next;
            _logger = factory.CreateLogger("RequestLog");

        }

        private Func<LogData, string> _logLineFormatter;
        private Func<LogData, string> logLineFormatter
        {
            get
            {

                if (this._logLineFormatter != null)
                {
                    return this._logLineFormatter;
                }
                return this.DefaultFormatter();
            }
            set
            {
                this._logLineFormatter = value;
            }
        }

        /// <summary>
        /// Override this to set the default formatter if none was supplied
        /// </summary>
        /// <returns></returns>
        protected Func<LogData, string> DefaultFormatter()
        {
            return (logData => $"{logData.RemoteAddr} - {logData.User} {logData.RequestTimestamp} \"{logData.RequestMethod} {logData.RequestPath} {logData.RequestProtocol}\" {logData.ResponseStatus} \"{logData.UserAgent}\" {logData.DurationMs}ms \r\n {logData.Payload}");
        }

        /// <summary>
        /// Used to set a custom formatter for this instance
        /// </summary>
        /// <param name="formatter"></param>
        public void SetLogLineFormat(Func<LogData, string> formatter)
        {
            this._logLineFormatter = formatter;
        }

        public async Task Invoke(HttpContext context)
        {

            var now = DateTime.Now;
            var watch = Stopwatch.StartNew();
            await _next.Invoke(context);
            watch.Stop();

            var nowString = now.ToString("yyyy-MM-dd'T'HH:mm:ss.fffzzz", DateTimeFormatInfo.InvariantInfo);
            var user = context.User.Identity.Name ?? "-";
            var request = context.Request.Path + (string.IsNullOrEmpty(context.Request.QueryString.ToString()) ? "" : context.Request.QueryString.ToString());
            var responseStatus = context.Response.StatusCode;
            var userAgent = context.Request.Headers.ContainsKey("User-Agent") ? context.Request.Headers["User-Agent"].ToString() : "-";
            var protocol = context.Request.Protocol;
            var duration = watch.ElapsedMilliseconds;
            var remoteAddr = context.Connection.RemoteIpAddress;
            var method = context.Request.Method;

            var requestPayloadStream = new MemoryStream();
            await context.Request.Body.CopyToAsync(requestPayloadStream);
            requestPayloadStream.Seek(0, SeekOrigin.Begin);
            var requestPayloadText = new StreamReader(requestPayloadStream).ReadToEnd();
            requestPayloadStream.Seek(0, SeekOrigin.Begin);
            context.Request.Body = requestPayloadStream;

            var payload = requestPayloadText;

            var logData = new LogData
            {
                RemoteAddr = remoteAddr,
                RequestMethod = method,
                RequestPath = request,
                RequestProtocol = protocol,
                RequestTimestamp = nowString,
                ResponseStatus = responseStatus,
                User = user,
                UserAgent = userAgent,
                DurationMs = duration,
                Payload = payload,
            };

            _logger.LogInformation(this.logLineFormatter(logData));

        }
    }
}
