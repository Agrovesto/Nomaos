using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Infrastructure.Utilities
{
    public static class AdminInvitemail
    {

        public static string BuildInviteEmailHtml(
       string? firstName, string role, string? adminCode, string link, string expiresText)
        {
            string E(string? v) => WebHtml(v);
            static string WebHtml(string? v) => WebUtility.HtmlEncode(v ?? string.Empty);

            return InviteEmailTemplate
                .Replace("{{Preheader}}", E($"{firstName}, you've been invited to the NomaOS Admin Panel as {role}."))
                .Replace("{{FirstName}}", E(firstName))
                .Replace("{{Role}}", E(role))
                .Replace("{{AdminCode}}", E(string.IsNullOrWhiteSpace(adminCode) ? "Assigned on activation" : adminCode))
                .Replace("{{Expires}}", E(expiresText))
                .Replace("{{Link}}", E(link))                 // encodes & as &amp; so the href is valid HTML
                .Replace("{{Year}}", DateTime.UtcNow.Year.ToString(CultureInfo.InvariantCulture));
        }

        // All styles are inline and layout uses tables, because email clients ignore most modern CSS.
        // Single quotes are used for attributes so this verbatim string needs no escaping.
        private const string InviteEmailTemplate = @"<!DOCTYPE html>
<html lang='en'>
<head>
<meta charset='utf-8'>
<meta name='viewport' content='width=device-width, initial-scale=1'>
<meta name='color-scheme' content='light'>
<title>NomaOS Admin Invitation</title>
</head>
<body style='margin:0;padding:0;background-color:#f1f5f2;'>

<div style='display:none;max-height:0;overflow:hidden;opacity:0;color:#f1f5f2;font-size:1px;line-height:1px;'>{{Preheader}}</div>

<table role='presentation' width='100%' cellpadding='0' cellspacing='0' border='0' style='background-color:#f1f5f2;'>
<tr><td align='center' style='padding:32px 16px;'>

  <table role='presentation' width='600' cellpadding='0' cellspacing='0' border='0' style='width:100%;max-width:600px;'>

    <tr><td align='center' style='padding-bottom:20px;font-family:Arial,Helvetica,sans-serif;font-size:24px;font-weight:bold;color:#14532d;letter-spacing:0.5px;'>
      Noma<span style='color:#16a34a;'>OS</span>
    </td></tr>

    <tr><td style='background-color:#ffffff;border:1px solid #e2e8e4;border-radius:12px;'>
      <table role='presentation' width='100%' cellpadding='0' cellspacing='0' border='0'>

        <tr><td style='background-color:#14532d;border-radius:12px 12px 0 0;padding:32px 40px;'>
          <p style='margin:0 0 8px 0;font-family:Arial,Helvetica,sans-serif;font-size:12px;font-weight:bold;letter-spacing:2px;color:#86efac;'>ADMIN PANEL INVITATION</p>
          <h1 style='margin:0;font-family:Arial,Helvetica,sans-serif;font-size:26px;line-height:34px;color:#ffffff;'>You're invited to join the team</h1>
        </td></tr>

        <tr><td style='padding:36px 40px 8px 40px;font-family:Arial,Helvetica,sans-serif;font-size:16px;line-height:26px;color:#334155;'>
          <p style='margin:0 0 16px 0;'>Hello {{FirstName}},</p>
          <p style='margin:0;'>You have been invited to join the NomaOS Admin Panel. Accept the invitation and set your password to activate your account.</p>
        </td></tr>

        <tr><td style='padding:24px 40px 8px 40px;'>
          <table role='presentation' width='100%' cellpadding='0' cellspacing='0' border='0' style='background-color:#f0fdf4;border:1px solid #bbf7d0;border-radius:8px;'>
            <tr><td style='padding:20px 24px;font-family:Arial,Helvetica,sans-serif;font-size:14px;line-height:22px;color:#334155;'>
              <table role='presentation' width='100%' cellpadding='0' cellspacing='0' border='0'>
                <!--<tr>
                  <td style='padding:4px 0;color:#64748b;width:120px;'>Your role</td>
                  <td style='padding:4px 0;font-weight:bold;color:#14532d;'>{{Role}}</td>
                </tr> -->
                <tr>
                  <td style='padding:4px 0;color:#64748b;'>Admin ID</td>
                  <td style='padding:4px 0;font-weight:bold;color:#14532d;'>{{AdminCode}}</td>
                </tr>
                <tr>
                  <td style='padding:4px 0;color:#64748b;'>Link expires</td>
                  <td style='padding:4px 0;font-weight:bold;color:#14532d;'>{{Expires}}</td>
                </tr>
              </table>
            </td></tr>
          </table>
        </td></tr>

        <tr><td align='center' style='padding:28px 40px 8px 40px;'>
          <table role='presentation' cellpadding='0' cellspacing='0' border='0'>
            <tr><td align='center' bgcolor='#16a34a' style='border-radius:8px;'>
              <a href='{{Link}}' target='_blank' style='display:inline-block;padding:15px 36px;font-family:Arial,Helvetica,sans-serif;font-size:16px;font-weight:bold;color:#ffffff;text-decoration:none;border-radius:8px;'>Accept invitation &amp; set password</a>
            </td></tr>
          </table>
        </td></tr>

        <tr><td style='padding:20px 40px 0 40px;font-family:Arial,Helvetica,sans-serif;font-size:13px;line-height:20px;color:#64748b;'>
          Button not working? Copy and paste this link into your browser:<br>
          <a href='{{Link}}' target='_blank' style='color:#15803d;word-break:break-all;'>{{Link}}</a>
        </td></tr>

        <tr><td style='padding:28px 40px 36px 40px;'>
          <table role='presentation' width='100%' cellpadding='0' cellspacing='0' border='0'>
            <tr><td style='border-top:1px solid #e2e8e4;padding-top:20px;font-family:Arial,Helvetica,sans-serif;font-size:13px;line-height:20px;color:#64748b;'>
              For your security, this link can only be used once. If you weren't expecting this invitation, you can safely ignore this email and no account changes will be made.
            </td></tr>
          </table>
        </td></tr>

      </table>
    </td></tr>

    <tr><td align='center' style='padding:24px 16px 0 16px;font-family:Arial,Helvetica,sans-serif;font-size:12px;line-height:18px;color:#94a3b8;'>
      &copy; {{Year}} NomaOS by Agrovesto. All rights reserved.<br>
      This is an automated message, please do not reply.
    </td></tr>

  </table>

</td></tr>
</table>

</body>
</html>";

    }
}
