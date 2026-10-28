using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Infrastructure.Utilities
{
    public static class NewRegistrationEmailTemplate
    {
        public static (string Subject, string Html, string Text) Build(
            string env, string email, string phoneNumber, string adminConsoleUrl)
        {
            // Encode everything user-supplied so nothing can inject markup into the email
            var safeEnv = WebUtility.HtmlEncode(env ?? "Unknown");
            var safeEmail = WebUtility.HtmlEncode(email ?? "-");
            var safePhone = WebUtility.HtmlEncode(phoneNumber ?? "-");
            var safeUrl = WebUtility.HtmlEncode(adminConsoleUrl ?? "#");

            var isProd = string.Equals(env, "production", StringComparison.OrdinalIgnoreCase)
                      || string.Equals(env, "prod", StringComparison.OrdinalIgnoreCase);
            var envColor = isProd ? "#1B7F3B" : "#B7791F";
            var envBg = isProd ? "#E6F4EA" : "#FEF3C7";

            var receivedAt = DateTime.UtcNow.ToString("dd MMM yyyy, HH:mm") + " UTC";
            var year = DateTime.UtcNow.Year;

            var subject = $"New registration on NomaOS ({env}): {email}";

            var html = $@"<!DOCTYPE html>
<html lang=""en"">
<head>
  <meta charset=""utf-8"">
  <meta name=""viewport"" content=""width=device-width, initial-scale=1"">
  <title>New Registration</title>
</head>
<body style=""margin:0;padding:0;background-color:#F3F4F6;font-family:Segoe UI,Helvetica,Arial,sans-serif;"">
  <table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""background-color:#F3F4F6;padding:24px 12px;"">
    <tr>
      <td align=""center"">
        <table role=""presentation"" width=""560"" cellpadding=""0"" cellspacing=""0"" style=""max-width:560px;width:100%;background-color:#FFFFFF;border-radius:10px;overflow:hidden;"">

          <!-- Header -->
          <tr>
            <td style=""background-color:#1B7F3B;padding:24px 32px;"">
              <div style=""font-size:20px;font-weight:700;color:#FFFFFF;"">NomaOS</div>
              <div style=""font-size:13px;color:#D1FAE5;margin-top:2px;"">Admin Notification</div>
            </td>
          </tr>

          <!-- Body -->
          <tr>
            <td style=""padding:32px;"">
              <span style=""display:inline-block;background-color:{envBg};color:{envColor};font-size:12px;font-weight:700;padding:4px 10px;border-radius:999px;text-transform:uppercase;letter-spacing:0.5px;"">{safeEnv}</span>

              <h1 style=""margin:16px 0 8px 0;font-size:22px;color:#111827;"">New user registration</h1>
              <p style=""margin:0 0 24px 0;font-size:15px;line-height:22px;color:#4B5563;"">
                A new user has just signed up on the platform. Here are the details:
              </p>

              <table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""border:1px solid #E5E7EB;border-radius:8px;"">
                <tr>
                  <td style=""padding:12px 16px;font-size:13px;color:#6B7280;width:35%;border-bottom:1px solid #E5E7EB;"">Email</td>
                  <td style=""padding:12px 16px;font-size:14px;color:#111827;font-weight:600;border-bottom:1px solid #E5E7EB;"">{safeEmail}</td>
                </tr>
                <tr>
                  <td style=""padding:12px 16px;font-size:13px;color:#6B7280;border-bottom:1px solid #E5E7EB;"">Phone</td>
                  <td style=""padding:12px 16px;font-size:14px;color:#111827;font-weight:600;border-bottom:1px solid #E5E7EB;"">{safePhone}</td>
                </tr>
                <tr>
                  <td style=""padding:12px 16px;font-size:13px;color:#6B7280;"">Received</td>
                  <td style=""padding:12px 16px;font-size:14px;color:#111827;font-weight:600;"">{receivedAt}</td>
                </tr>
              </table>

              <table role=""presentation"" cellpadding=""0"" cellspacing=""0"" style=""margin-top:28px;"">
                <tr>
                  <td style=""background-color:#1B7F3B;border-radius:6px;"">
                    <a href=""{safeUrl}"" style=""display:inline-block;padding:12px 24px;font-size:14px;font-weight:600;color:#FFFFFF;text-decoration:none;"">Open Admin Console</a>
                  </td>
                </tr>
              </table>
            </td>
          </tr>

          <!-- Footer -->
          <tr>
            <td style=""background-color:#F9FAFB;padding:20px 32px;border-top:1px solid #E5E7EB;"">
              <p style=""margin:0;font-size:12px;line-height:18px;color:#9CA3AF;"">
                This is an automated message from NomaOS. Please do not reply to this email.<br>
                &copy; {year} Agrovesto Enterprise Limited
              </p>
            </td>
          </tr>

        </table>
      </td>
    </tr>
  </table>
</body>
</html>";

            var text =
    $@"NomaOS - New user registration ({env})

A new user has just signed up:

Email:    {email}
Phone:    {phoneNumber}
Received: {receivedAt}

Open the Admin Console: {adminConsoleUrl}

This is an automated message from NomaOS. Please do not reply.";

            return (subject, html, text);
        }
    }
}
