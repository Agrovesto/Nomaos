using Agrovesto.Backend.Core.Interfaces.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Serilog;
using Serilog.Context;
using Serilog.Core;
using Serilog.Sinks.Elasticsearch;

namespace Agrovesto.Backend.Infrastructure.ExternalServices.LoggingService
{
    public class SerilogService : ILogService
    {
        private readonly Logger _log;

        public SerilogService(SerilogServiceOptions options)
        {
            var elasticUrls = (options.ElasticUrls ?? "").Split(',').Select(s => new Uri(s));
            var elasticOptions = new ElasticsearchSinkOptions(elasticUrls)
            {
                IndexFormat = options.ElasticIndex
            };

            LogContext.PushProperty("App", "AgrovestoBackend");

            var logConfig = new LoggerConfiguration()
                .Enrich.FromLogContext()
                //.WriteTo.File();
             .WriteTo.Elasticsearch(elasticOptions); //TODO: rewrite to file

            if (options.LogToFile)
            {
                logConfig = logConfig.WriteTo.File(@"logs/log-{Date}.txt", rollingInterval: RollingInterval.Day);
            }

            _log = logConfig.CreateLogger();
        }

        public void Debug(string messageTemplate) => _log.Debug(messageTemplate);

        public void Debug<T>(string messageTemplate, T propertyValue) => _log.Debug(messageTemplate, propertyValue);

        public void Debug(string messageTemplate, params object[] propertyValues) => _log.Debug(messageTemplate, propertyValues);

        public void Debug(Exception exception, string messageTemplate) => _log.Debug(exception, messageTemplate);

        public void Debug<T>(Exception exception, string messageTemplate, T propertyValue) => _log.Debug(exception, messageTemplate, propertyValue);

        public void Debug(Exception exception, string messageTemplate, params object[] propertyValues) => _log.Debug(exception, messageTemplate, propertyValues);

        public void Error(string messageTemplate) => _log.Error(messageTemplate);

        public void Error<T>(string messageTemplate, T propertyValue) => _log.Error(messageTemplate, propertyValue);

        public void Error(string messageTemplate, params object[] propertyValues) => _log.Error(messageTemplate, propertyValues);

        public void Error(Exception exception, string messageTemplate) => _log.Error(exception, messageTemplate);

        public void Error<T>(Exception exception, string messageTemplate, T propertyValue) => _log.Error(exception, messageTemplate, propertyValue);

        public void Error(Exception exception, string messageTemplate, params object[] propertyValues) => _log.Error(exception, messageTemplate, propertyValues);

        public void Information(string messageTemplate) => _log.Information(messageTemplate);

        public void Information<T>(string messageTemplate, T propertyValue) => _log.Information(messageTemplate, propertyValue);

        public void Information(string messageTemplate, params object[] propertyValues) => _log.Information(messageTemplate, propertyValues);

        public void Information(Exception exception, string messageTemplate) => _log.Information(exception, messageTemplate);

        public void Information<T>(Exception exception, string messageTemplate, T propertyValue) => _log.Information(exception, messageTemplate, propertyValue);

        public void Information(Exception exception, string messageTemplate, params object[] propertyValues) => _log.Information(exception, messageTemplate, propertyValues);

        public void Warning(string messageTemplate) => _log.Warning(messageTemplate);

        public void Warning<T>(string messageTemplate, T propertyValue) => _log.Warning(messageTemplate, propertyValue);

        public void Warning(string messageTemplate, params object[] propertyValues) => _log.Warning(messageTemplate, propertyValues);

        public void Warning(Exception exception, string messageTemplate) => _log.Warning(exception, messageTemplate);

        public void Warning<T>(Exception exception, string messageTemplate, T propertyValue) => _log.Warning(exception, messageTemplate, propertyValue);

        public void Warning(Exception exception, string messageTemplate, params object[] propertyValues) => _log.Warning(exception, messageTemplate, propertyValues);
    }

    public class SerilogServiceOptions
    {
        public string ElasticUrls { get; set; } = "http://elastic.cyberpay.ng";
        public string ElasticIndex { get; set; } = "agrovesto-trade";
        public bool LogToFile { get; set; } = true;
    }
}
