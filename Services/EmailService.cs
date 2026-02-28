using System.Net;
using System.Net.Mail;
using QRCoder;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Concert_Backend.Services
{
    public class EmailService
    {
        private readonly IConfiguration _config;

        public EmailService(IConfiguration config)
        {
            _config = config;
            QuestPDF.Settings.License = LicenseType.Community;
        }

        public async Task SendTicketEmailAsync(string toEmail, string customerName, string ticketDetails)
        {
            // 1. Generate QR Code
            using QRCodeGenerator qrGenerator = new QRCodeGenerator();
            using QRCodeData qrCodeData = qrGenerator.CreateQrCode(ticketDetails, QRCodeGenerator.ECCLevel.Q);
            using PngByteQRCode qrCode = new PngByteQRCode(qrCodeData);
            byte[] qrCodeImage = qrCode.GetGraphic(20);

            // 2. Generate PDF
            byte[] pdfBytes = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A5);
                    page.Margin(1, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    // FIXED: Using string "Helvetica" instead of Fonts.Helvetica
                    page.DefaultTextStyle(x => x.FontSize(12).FontFamily("Helvetica"));

                    page.Header().Text("OFFICIAL CONCERT TICKET")
                        .SemiBold().FontSize(20).FontColor(Colors.Blue.Medium);

                    page.Content().PaddingVertical(10).Column(col =>
                    {
                        col.Item().Text($"Customer: {customerName}").FontSize(14);
                        col.Item().PaddingVertical(5).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                        
                        col.Item().AlignCenter().MaxWidth(150).Image(qrCodeImage); // QR Code
                        
                        col.Item().AlignCenter().PaddingTop(10).Text(ticketDetails).FontSize(10).Italic();
                        
                        col.Item().PaddingTop(10).AlignCenter().Text("Please present this QR code at the entrance.")
                            .FontSize(10).FontColor(Colors.Grey.Medium);
                    });

                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("Ticket Generated on: ");
                        x.Span(DateTime.Now.ToString("f")).SemiBold();
                    });
                });
            }).GeneratePdf();

            // 3. Configure SMTP
            using var smtpClient = new SmtpClient("smtp.gmail.com")
            {
                Port = 587,
                Credentials = new NetworkCredential(
                    _config["EmailSettings:EmailUser"],
                    _config["EmailSettings:EmailPass"]
                ),
                EnableSsl = true,
            };

            // 4. Create Message
            var mailMessage = new MailMessage
            {
                From = new MailAddress(_config["EmailSettings:EmailUser"]!),
                Subject = "Your Concert Ticket + QR Code",
                Body = $"<h1>Hello {customerName}</h1><p>Thank you for your purchase. Your official ticket is attached as a PDF.</p>",
                IsBodyHtml = true,
            };

            mailMessage.To.Add(toEmail);

            // 5. Attach PDF (using MemoryStream)
            using (var ms = new MemoryStream(pdfBytes))
            {
                var attachment = new Attachment(ms, "ConcertTicket.pdf", "application/pdf");
                mailMessage.Attachments.Add(attachment);
                
                await smtpClient.SendMailAsync(mailMessage);
            }
        }
    }
}