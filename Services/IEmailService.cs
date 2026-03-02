namespace Concert_Backend.Services
{
    public interface IEmailService
    {
        // For Forgot Password / Codes
        Task SendEmailAsync(string toEmail, string subject, string htmlContent);

        // For sending the PDF Ticket after purchase
        Task SendTicketEmailAsync(string toEmail, string customerName, string ticketType, int qty, string ticketId, string artist, string venue);
    }
}