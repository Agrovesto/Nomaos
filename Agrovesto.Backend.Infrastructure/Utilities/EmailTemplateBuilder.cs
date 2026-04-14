using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Infrastructure.Utilities
{
    public static class EmailTemplateBuilder
    {
        public static (string Subject, string BodyHtml, string BodyText) BuildAccountCreatedEmail(
            string password,
            string frontendUrl,
            int year)
        {
            var subject = "Your NomaOS Account Has Been Created";

            var bodyHtml = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ font-size: 20px; font-weight: bold; margin-bottom: 20px; }}
        .password-box {{
            background-color: #f4f4f4;
            padding: 12px;
            border-radius: 5px;
            font-weight: bold;
            display: inline-block;
            margin: 10px 0;
        }}
        .btn {{
            display: inline-block;
            padding: 12px 20px;
            margin-top: 20px;
            font-size: 14px;
            color: #ffffff;
            background-color: #1E3A8A;
            text-decoration: none;
            border-radius: 5px;
        }}
        .footer {{ margin-top: 30px; font-size: 12px; color: #777; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>Welcome to NomaOS</div>

        <p>An account has been created for you by your organization.</p>

        <p>You can log in using your registered email address and the temporary password below:</p>

        <div class='password-box'>{password}</div>

        <p>For security reasons, please log in as soon as possible and change your password.</p>

        <a href='{frontendUrl}' class='btn' target=''>Login to NomaOS</a>

        <p style='margin-top:15px;'>If the button above does not work, copy and paste this link into your browser:</p>
        <p><a href='{frontendUrl}'>{frontendUrl}</a></p>

        <p>If you experience any issues accessing your account, please contact your organization’s administrator.</p>

        <div class='footer'>
            &copy; {year} NomaOS. All rights reserved.
        </div>
    </div>
</body>
</html>";

            var bodyText = $@"
Welcome to NomaOS

An account has been created for you by your organization.

Login using your email address and the temporary password below:
Temporary Password: {password}

Login here: {frontendUrl}

For security reasons, please log in as soon as possible and change your password.

If you experience any issues, please contact your organization’s administrator.
";

            return (subject, bodyHtml, bodyText);
        }
    }
}
