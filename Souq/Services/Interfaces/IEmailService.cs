namespace Souq.Services.Interfaces
{
    public interface IEmailService
    {

        Task SendAsync(string toEmail, string toName,
                       string subject, string htmlBody);

        Task SendOrderConfirmedAsync(string toEmail,
                                     string customerName,
                                     string orderNumber,
                                     decimal total,
                                     string city,
                                     string country);

        Task SendNewSaleAsync(string toEmail,
                              string vendorStoreName,
                              string orderNumber,
                              decimal vendorEarnings,
                              List<string> itemNames);

        Task SendVendorApprovedAsync(string toEmail,
                                     string storeName);

        Task SendVendorRejectedAsync(string toEmail,
                                     string storeName);

        Task SendEmailVerificationAsync(string toEmail,
                                 string firstName,
                                 string verificationUrl);

        Task SendPasswordResetAsync(string toEmail,
                                     string firstName,
                                     string resetUrl);
    }
}
