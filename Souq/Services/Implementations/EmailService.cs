using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Souq.Services.Interfaces;

namespace Souq.Services.Implementations
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _config;

        public EmailService(IConfiguration config)
        {
            _config = config;
        }
        public async Task SendAsync(string toEmail, string toName, string subject, string htmlBody)
        {
            /*
                MimeMessage is MailKit's email object.
                We build it then send via SmtpClient.
            */
            var message = new MimeMessage();

            message.From.Add(new MailboxAddress(
                _config["Email:FromName"],
                _config["Email:FromEmail"]));

            message.To.Add(new MailboxAddress(toName, toEmail));
            message.Subject = subject;

            var bodyBuilder = new BodyBuilder
            {
                HtmlBody = htmlBody,
                TextBody = HtmlToPlainText(htmlBody)
            };

            message.Body = bodyBuilder.ToMessageBody();

            using var smtp = new SmtpClient();

            // Bypasses SSL certificate revocation check errors common in some local environments
            smtp.ServerCertificateValidationCallback = (s, c, h, e) => true;

            await smtp.ConnectAsync(
                _config["Email:Host"],
                int.Parse(_config["Email:Port"]!),
                SecureSocketOptions.StartTls);

            await smtp.AuthenticateAsync(
                _config["Email:Username"],
                _config["Email:Password"]);

            await smtp.SendAsync(message);
            await smtp.DisconnectAsync(true);
        }

        // ── New sale → Vendor ─────────────────────────────────
        public async Task SendNewSaleAsync(string toEmail, string vendorStoreName, string orderNumber, decimal vendorEarnings, List<string> itemNames)
        {
            var subject = $"New sale — {orderNumber}";
            var html = BuildNewSaleEmail(
                vendorStoreName, orderNumber,
                vendorEarnings, itemNames);

            await SendAsync(toEmail, vendorStoreName, subject, html);
        }

        public async Task SendOrderConfirmedAsync(string toEmail, string customerName, string orderNumber, decimal total, string city, string country)
        {
            var subject = $"Order confirmed — {orderNumber}";
            var html = BuildOrderConfirmedEmail(
                customerName, orderNumber, total, city, country);

            await SendAsync(toEmail, customerName, subject, html);
        }

        public async Task SendVendorApprovedAsync(string toEmail, string storeName)
        {
            var subject = "Your vendor application is approved!";
            var html = BuildVendorApprovedEmail(storeName);

            await SendAsync(toEmail, storeName, subject, html);
        }

        public async Task SendVendorRejectedAsync(string toEmail, string storeName)
        {
            var subject = "Update on your vendor application";
            var html = BuildVendorRejectedEmail(storeName);

            await SendAsync(toEmail, storeName, subject, html);
        }

        public async Task SendEmailVerificationAsync(string toEmail, string firstName, string verificationUrl)
        {
            var subject = "Verify your Souq account";
            var html = BuildVerificationEmail(firstName, verificationUrl);
            await SendAsync(toEmail, firstName, subject, html);
        }

        public async Task SendPasswordResetAsync(string toEmail, string firstName, string resetUrl)
        {
            var subject = "Reset your Souq password";
            var html = BuildPasswordResetEmail(firstName, resetUrl);
            await SendAsync(toEmail, firstName, subject, html);
        }

        // ── Helper: strip HTML for plain text fallback ────────
        private string HtmlToPlainText(string html)
        {
            return System.Text.RegularExpressions.Regex
                .Replace(html, "<[^>]*>", "")
                .Trim();
        }

        private string BuildNewSaleEmail(
            string storeName, string orderNumber,
            decimal earnings, List<string> itemNames)
        {
            var itemsList = string.Join("",
                itemNames.Select(i =>
                    $"<li style='color:#374151;font-size:13px;" +
                    $"padding:4px 0'>{i}</li>"));

            return $@"
<!DOCTYPE html>
<html>
<body style='margin:0;padding:0;background:#f9fafb;
             font-family:Arial,sans-serif'>
<table width='100%' cellpadding='0' cellspacing='0'>
<tr><td align='center' style='padding:40px 20px'>
<table width='600' cellpadding='0' cellspacing='0'
       style='background:#ffffff;border-radius:16px;
              overflow:hidden;border:1px solid #e5e7eb'>

    <tr>
        <td style='background:#0284c7;padding:32px;text-align:center'>
            <h1 style='color:#ffffff;margin:0;font-size:24px'>Souq.</h1>
        </td>
    </tr>

    <tr>
        <td style='padding:32px'>
            <h2 style='color:#111827;font-size:20px;margin-bottom:8px'>
                New sale! 🎉
            </h2>
            <p style='color:#6b7280;font-size:14px;margin-bottom:24px'>
                Hi {storeName}, you have a new order.
                Time to get it packed!
            </p>

            <table width='100%' cellpadding='0' cellspacing='0'
                   style='background:#f9fafb;border-radius:12px;
                          padding:20px;margin-bottom:24px'>
                <tr>
                    <td style='padding:8px 0'>
                        <span style='color:#6b7280;font-size:13px'>
                            Order
                        </span>
                        <span style='color:#111827;font-size:13px;
                                     font-weight:bold;float:right'>
                            {orderNumber}
                        </span>
                    </td>
                </tr>
                <tr>
                    <td style='padding:8px 0;border-top:1px solid #e5e7eb'>
                        <span style='color:#6b7280;font-size:13px'>
                            Your earnings
                        </span>
                        <span style='color:#16a34a;font-size:16px;
                                     font-weight:bold;float:right'>
                            ${earnings:0.00}
                        </span>
                    </td>
                </tr>
            </table>

            <p style='color:#374151;font-size:13px;
                      font-weight:bold;margin-bottom:8px'>
                Items sold:
            </p>
            <ul style='margin:0;padding-left:20px'>
                {itemsList}
            </ul>

            <div style='text-align:center;margin-top:24px'>
                <a href='https://souq.com/vendor/orders'
                   style='background:#0284c7;color:#ffffff;
                          padding:12px 32px;border-radius:12px;
                          text-decoration:none;font-weight:bold;
                          font-size:14px;display:inline-block'>
                    View order details
                </a>
            </div>
        </td>
    </tr>

    <tr>
        <td style='background:#f9fafb;padding:24px;
                   text-align:center;border-top:1px solid #e5e7eb'>
            <p style='color:#9ca3af;font-size:12px;margin:0'>
                © 2025 Souq Marketplace · All rights reserved
            </p>
        </td>
    </tr>
</table>
</td></tr>
</table>
</body>
</html>";
        }

        // ── Email HTML templates ──────────────────────────────
        private string BuildOrderConfirmedEmail(
            string customerName, string orderNumber,
            decimal total, string city, string country)
        {
            /*
                We use inline CSS because many email clients
                (Gmail, Outlook) strip <style> tags.
                Inline styles are the only reliable way
                to style HTML emails.
            */
            return $@"
<!DOCTYPE html>
<html>
<body style='margin:0;padding:0;background:#f9fafb;font-family:Arial,sans-serif'>
<table width='100%' cellpadding='0' cellspacing='0'>
<tr><td align='center' style='padding:40px 20px'>
<table width='600' cellpadding='0' cellspacing='0'
       style='background:#ffffff;border-radius:16px;
              overflow:hidden;border:1px solid #e5e7eb'>

    <!-- Header -->
    <tr>
        <td style='background:#0284c7;padding:32px;text-align:center'>
            <h1 style='color:#ffffff;margin:0;font-size:24px'>Souq.</h1>
        </td>
    </tr>

    <!-- Body -->
    <tr>
        <td style='padding:32px'>
            <div style='text-align:center;margin-bottom:24px'>
                <div style='width:64px;height:64px;background:#dcfce7;
                            border-radius:50%;display:inline-flex;
                            align-items:center;justify-content:center;
                            margin-bottom:16px'>
                    <span style='font-size:32px'>✓</span>
                </div>
                <h2 style='color:#111827;font-size:20px;margin:0'>
                    Payment confirmed!
                </h2>
            </div>

            <p style='color:#6b7280;font-size:14px;margin-bottom:24px'>
                Hi {customerName}, your order has been placed
                and payment confirmed. Thank you for shopping with us!
            </p>

            <!-- Order details box -->
            <table width='100%' cellpadding='0' cellspacing='0'
                   style='background:#f9fafb;border-radius:12px;
                          padding:20px;margin-bottom:24px'>
                <tr>
                    <td style='padding:8px 0'>
                        <span style='color:#6b7280;font-size:13px'>
                            Order number
                        </span>
                        <span style='color:#111827;font-size:13px;
                                     font-weight:bold;float:right'>
                            {orderNumber}
                        </span>
                    </td>
                </tr>
                <tr>
                    <td style='padding:8px 0;border-top:1px solid #e5e7eb'>
                        <span style='color:#6b7280;font-size:13px'>
                            Shipping to
                        </span>
                        <span style='color:#111827;font-size:13px;
                                     font-weight:bold;float:right'>
                            {city}, {country}
                        </span>
                    </td>
                </tr>
                <tr>
                    <td style='padding:8px 0;border-top:1px solid #e5e7eb'>
                        <span style='color:#6b7280;font-size:13px'>
                            Total paid
                        </span>
                        <span style='color:#0284c7;font-size:16px;
                                     font-weight:bold;float:right'>
                            ${total:0.00}
                        </span>
                    </td>
                </tr>
            </table>

            <!-- CTA Button -->
            <div style='text-align:center'>
                <a href='https://souq.com/orders'
                   style='background:#0284c7;color:#ffffff;
                          padding:12px 32px;border-radius:12px;
                          text-decoration:none;font-weight:bold;
                          font-size:14px;display:inline-block'>
                    View my orders
                </a>
            </div>
        </td>
    </tr>

    <!-- Footer -->
    <tr>
        <td style='background:#f9fafb;padding:24px;
                   text-align:center;border-top:1px solid #e5e7eb'>
            <p style='color:#9ca3af;font-size:12px;margin:0'>
                © 2025 Souq Marketplace · All rights reserved
            </p>
        </td>
    </tr>
</table>
</td></tr>
</table>
</body>
</html>";
        }

        private string BuildVendorApprovedEmail(string storeName)
        {
            return $@"
<!DOCTYPE html>
<html>
<body style='margin:0;padding:0;background:#f9fafb;
             font-family:Arial,sans-serif'>
<table width='100%' cellpadding='0' cellspacing='0'>
<tr><td align='center' style='padding:40px 20px'>
<table width='600' cellpadding='0' cellspacing='0'
       style='background:#ffffff;border-radius:16px;
              overflow:hidden;border:1px solid #e5e7eb'>

    <tr>
        <td style='background:#0284c7;padding:32px;text-align:center'>
            <h1 style='color:#ffffff;margin:0;font-size:24px'>Souq.</h1>
        </td>
    </tr>

    <tr>
        <td style='padding:32px;text-align:center'>
            <div style='width:64px;height:64px;background:#dcfce7;
                        border-radius:50%;display:inline-flex;
                        align-items:center;justify-content:center;
                        margin-bottom:16px'>
                <span style='font-size:32px'>✓</span>
            </div>
            <h2 style='color:#111827;font-size:22px;margin-bottom:8px'>
                You're approved!
            </h2>
            <p style='color:#6b7280;font-size:14px;margin-bottom:24px'>
                Congratulations! Your store
                <strong>{storeName}</strong>
                has been approved and is now live on Souq.
                You can start adding products right away.
            </p>
            <a href='https://souq.com/vendor/dashboard'
               style='background:#0284c7;color:#ffffff;
                      padding:14px 40px;border-radius:12px;
                      text-decoration:none;font-weight:bold;
                      font-size:14px;display:inline-block'>
                Go to my dashboard
            </a>
        </td>
    </tr>

    <tr>
        <td style='background:#f9fafb;padding:24px;
                   text-align:center;border-top:1px solid #e5e7eb'>
            <p style='color:#9ca3af;font-size:12px;margin:0'>
                © 2025 Souq Marketplace · All rights reserved
            </p>
        </td>
    </tr>
</table>
</td></tr>
</table>
</body>
</html>";
        }

        private string BuildVendorRejectedEmail(string storeName)
        {
            return $@"
<!DOCTYPE html>
<html>
<body style='margin:0;padding:0;background:#f9fafb;
             font-family:Arial,sans-serif'>
<table width='100%' cellpadding='0' cellspacing='0'>
<tr><td align='center' style='padding:40px 20px'>
<table width='600' cellpadding='0' cellspacing='0'
       style='background:#ffffff;border-radius:16px;
              overflow:hidden;border:1px solid #e5e7eb'>

    <tr>
        <td style='background:#0284c7;padding:32px;text-align:center'>
            <h1 style='color:#ffffff;margin:0;font-size:24px'>Souq.</h1>
        </td>
    </tr>

    <tr>
        <td style='padding:32px;text-align:center'>
            <h2 style='color:#111827;font-size:20px;margin-bottom:8px'>
                Application update
            </h2>
            <p style='color:#6b7280;font-size:14px;margin-bottom:8px'>
                Thank you for applying to sell on Souq.
            </p>
            <p style='color:#6b7280;font-size:14px;margin-bottom:24px'>
                After reviewing your application for
                <strong>{storeName}</strong>,
                we're unable to approve it at this time.
                You're welcome to update your information and reapply.
            </p>
            <a href='https://souq.com/vendor/apply'
               style='background:#0284c7;color:#ffffff;
                      padding:14px 40px;border-radius:12px;
                      text-decoration:none;font-weight:bold;
                      font-size:14px;display:inline-block'>
                Reapply now
            </a>
        </td>
    </tr>

    <tr>
        <td style='background:#f9fafb;padding:24px;
                   text-align:center;border-top:1px solid #e5e7eb'>
            <p style='color:#9ca3af;font-size:12px;margin:0'>
                © 2025 Souq Marketplace · All rights reserved
            </p>
        </td>
    </tr>
</table>
</td></tr>
</table>
</body>
</html>";
        }

        private string BuildVerificationEmail(
            string firstName, string verificationUrl)
        {
            return $@"
<!DOCTYPE html>
<html>
<body style='margin:0;padding:0;background:#f9fafb;
             font-family:Arial,sans-serif'>
<table width='100%' cellpadding='0' cellspacing='0'>
<tr><td align='center' style='padding:40px 20px'>
<table width='600' cellpadding='0' cellspacing='0'
       style='background:#ffffff;border-radius:16px;
              overflow:hidden;border:1px solid #e5e7eb'>

    <!-- Header -->
    <tr>
        <td style='background:#0284c7;padding:32px;text-align:center'>
            <h1 style='color:#ffffff;margin:0;font-size:24px'>Souq.</h1>
        </td>
    </tr>

    <!-- Body -->
    <tr>
        <td style='padding:40px 32px;text-align:center'>

            <div style='width:64px;height:64px;background:#e0f2fe;
                        border-radius:50%;margin:0 auto 20px;
                        display:table-cell;vertical-align:middle;
                        text-align:center'>
                <span style='font-size:28px'>✉</span>
            </div>

            <h2 style='color:#111827;font-size:22px;
                       margin:0 0 12px 0'>
                Verify your email address
            </h2>

            <p style='color:#6b7280;font-size:14px;
                      margin:0 0 8px 0;line-height:1.6'>
                Hi {firstName}, welcome to Souq!
            </p>

            <p style='color:#6b7280;font-size:14px;
                      margin:0 0 32px 0;line-height:1.6'>
                Please click the button below to verify
                your email address and activate your account.
                This link expires in 24 hours.
            </p>

            <a href='{verificationUrl}'
               style='background:#0284c7;color:#ffffff;
                      padding:14px 48px;border-radius:12px;
                      text-decoration:none;font-weight:bold;
                      font-size:15px;display:inline-block;
                      margin-bottom:24px'>
                Verify my email
            </a>

            <p style='color:#9ca3af;font-size:12px;
                      margin:0;line-height:1.6'>
                If you didn't create an account on Souq,
                you can safely ignore this email.
            </p>

            <!-- Fallback link -->
            <div style='margin-top:24px;padding:16px;
                        background:#f9fafb;border-radius:8px'>
                <p style='color:#6b7280;font-size:11px;margin:0 0 8px 0'>
                    Button not working? Copy and paste this link:
                </p>
                <p style='color:#0284c7;font-size:11px;
                          margin:0;word-break:break-all'>
                    {verificationUrl}
                </p>
            </div>
        </td>
    </tr>

    <!-- Footer -->
    <tr>
        <td style='background:#f9fafb;padding:24px;
                   text-align:center;border-top:1px solid #e5e7eb'>
            <p style='color:#9ca3af;font-size:12px;margin:0'>
                © 2025 Souq Marketplace · All rights reserved
            </p>
        </td>
    </tr>
</table>
</td></tr>
</table>
</body>
</html>";
        }

        private string BuildPasswordResetEmail(
            string firstName, string resetUrl)
        {
            return $@"
<!DOCTYPE html>
<html>
<body style='margin:0;padding:0;background:#f9fafb;
             font-family:Arial,sans-serif'>
<table width='100%' cellpadding='0' cellspacing='0'>
<tr><td align='center' style='padding:40px 20px'>
<table width='600' cellpadding='0' cellspacing='0'
       style='background:#ffffff;border-radius:16px;
              overflow:hidden;border:1px solid #e5e7eb'>

    <tr>
        <td style='background:#0284c7;padding:32px;text-align:center'>
            <h1 style='color:#ffffff;margin:0;font-size:24px'>Souq.</h1>
        </td>
    </tr>

    <tr>
        <td style='padding:40px 32px;text-align:center'>
            <h2 style='color:#111827;font-size:22px;margin:0 0 12px 0'>
                Reset your password
            </h2>

            <p style='color:#6b7280;font-size:14px;
                      margin:0 0 8px 0;line-height:1.6'>
                Hi {firstName},
            </p>

            <p style='color:#6b7280;font-size:14px;
                      margin:0 0 32px 0;line-height:1.6'>
                We received a request to reset your password.
                Click the button below to choose a new one.
                This link expires in 1 hour.
            </p>

            <a href='{resetUrl}'
               style='background:#0284c7;color:#ffffff;
                      padding:14px 48px;border-radius:12px;
                      text-decoration:none;font-weight:bold;
                      font-size:15px;display:inline-block;
                      margin-bottom:24px'>
                Reset password
            </a>

            <p style='color:#9ca3af;font-size:12px;
                      margin:0;line-height:1.6'>
                If you didn't request this, you can safely
                ignore this email. Your password won't change.
            </p>
        </td>
    </tr>

    <tr>
        <td style='background:#f9fafb;padding:24px;
                   text-align:center;border-top:1px solid #e5e7eb'>
            <p style='color:#9ca3af;font-size:12px;margin:0'>
                © 2025 Souq Marketplace · All rights reserved
            </p>
        </td>
    </tr>
</table>
</td></tr>
</table>
</body>
</html>";
        }


    }
}
