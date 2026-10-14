using CsvHelper;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Infrastructure.Logic.Services
{
    public record RequestClientInfo(
        string? IpAddress, string? Browser, string? OperatingSystem,
        string? DeviceType, string? DeviceName, string? UserAgent);

    public interface IClientInfoProvider { RequestClientInfo Get(); }

    public class ClientInfoProvider : IClientInfoProvider
    {
        private static readonly UAParser.Parser Parser = UAParser.Parser.GetDefault();   // build once, reuse
        private readonly IHttpContextAccessor _http;
        public ClientInfoProvider(IHttpContextAccessor http) => _http = http;

        public RequestClientInfo Get()
        {
            var ctx = _http.HttpContext;
            var ip = ctx?.Connection.RemoteIpAddress?.ToString();
            var ua = ctx?.Request.Headers.UserAgent.ToString();

            if (string.IsNullOrWhiteSpace(ua))
                return new(ip, null, null, null, null, null);

            var c = Parser.Parse(ua);

            static string? Label(string? family, string? major) =>
                string.IsNullOrWhiteSpace(family) || family == "Other" ? null
                : string.IsNullOrWhiteSpace(major) ? family : $"{family} {major}";

            var lower = ua.ToLowerInvariant();
            var deviceType =
                c.Device.Family == "Spider" ? "Bot"
                : lower.Contains("ipad") || lower.Contains("tablet") ? "Tablet"
                : lower.Contains("mobi") || lower.Contains("iphone") ? "Mobile"
                : lower.Contains("android") ? "Tablet"   // Android without "Mobile" is usually a tablet
                : "Desktop";

            var deviceName = !string.IsNullOrWhiteSpace(c.Device.Brand) || !string.IsNullOrWhiteSpace(c.Device.Model)
                ? $"{c.Device.Brand} {c.Device.Model}".Trim()
                : (c.Device.Family == "Other" ? null : c.Device.Family);

            return new(ip, Label(c.UA.Family, c.UA.Major), Label(c.OS.Family, c.OS.Major),
                       deviceType, deviceName, ua.Length > 512 ? ua[..512] : ua);
        }
    }
}
