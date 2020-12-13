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

        public class LogDataOut
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

        private Func<LogDataOut, string> _logLineFormatter;
        private Func<LogDataOut, string> logLineFormatter
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
        protected Func<LogDataOut, string> DefaultFormatter()
        {
            return (LogDataOut => $"{LogDataOut.DurationMs}ms \r\n {LogDataOut.Payload}");
        }

        /// <summary>
        /// Used to set a custom formatter for this instance
        /// </summary>
        /// <param name="formatter"></param>
        public void SetLogLineFormat(Func<LogDataOut, string> formatter)
        {
            this._logLineFormatter = formatter;
        }

        public async Task Invoke(HttpContext context)
        {

            var now = DateTime.Now;
            var watch = Stopwatch.StartNew();

            // var ResponsePayloadStream = new MemoryStream();
            //-- await context.Response.Body.CopyToAsync(ResponsePayloadStream);
            var bodyStream = context.Response.Body;
            var ResponsePayloadStream = new MemoryStream();
            context.Response.Body = ResponsePayloadStream;

            await _next.Invoke(context);
            watch.Stop();

            var duration = watch.ElapsedMilliseconds;


            ResponsePayloadStream.Seek(0, SeekOrigin.Begin);
            var ResponsePayloadText = new StreamReader(ResponsePayloadStream).ReadToEnd();

            var payload = ResponsePayloadText;  //-- "Response Payload"; 

            var LogDataOut = new LogDataOut
            {
                DurationMs = duration,
                Payload = payload,
            };

            _logger.LogInformation(this.logLineFormatter(LogDataOut));

            ResponsePayloadStream.Seek(0, SeekOrigin.Begin);
            await ResponsePayloadStream.CopyToAsync(bodyStream);

        }
    }
}
