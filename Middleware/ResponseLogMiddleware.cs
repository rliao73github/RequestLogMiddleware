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
    public class ResponseLogMiddleware
    {

        public class LogData
        {

            public string Payload { get; set; }

            public long DurationMs { get; set; }
        }

        private readonly RequestDelegate _next;
        private readonly ILogger _logger;

        public ResponseLogMiddleware(RequestDelegate next, ILoggerFactory factory)
        {
            _next = next;
            _logger = factory.CreateLogger("ResponseLog");

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
            return (logData => $"{logData.DurationMs}ms \r\n {logData.Payload}");
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

            var duration = watch.ElapsedMilliseconds;

            var ResponsePayloadStream = new MemoryStream();
            //-- await context.Response.Body.CopyToAsync(ResponsePayloadStream);
            //-- ResponsePayloadStream = context.Response.Body;

            ResponsePayloadStream.Seek(0, SeekOrigin.Begin);
            var ResponsePayloadText = new StreamReader(ResponsePayloadStream).ReadToEnd();
            ResponsePayloadStream.Seek(0, SeekOrigin.Begin);
            context.Response.Body = ResponsePayloadStream;

            var payload = ResponsePayloadText;

            var logData = new LogData
            {
                DurationMs = duration,
                Payload = payload,
            };

            _logger.LogInformation(this.logLineFormatter(logData));

        }
    }
}
