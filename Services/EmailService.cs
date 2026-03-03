using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using QRCoder;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Hosting;
using System.IO;

namespace Concert_Backend.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _config;
        private readonly IWebHostEnvironment _env;

        public EmailService(IConfiguration config, IWebHostEnvironment env)
        {
            _config = config;
            _env = env;
            QuestPDF.Settings.License = LicenseType.Community;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string htmlContent)
        {
            var email = new MimeMessage();
            email.From.Add(new MailboxAddress("Ethio Concert", _config["EmailSettings:EmailUser"]));
            email.To.Add(MailboxAddress.Parse(toEmail));
            email.Subject = subject;

            var builder = new BodyBuilder { HtmlBody = htmlContent };
            email.Body = builder.ToMessageBody();

            using var smtp = new SmtpClient();
            // Added explicit timeout of 10 seconds
            smtp.Timeout = 20000; 
            
            await smtp.ConnectAsync(_config["EmailSettings:Host"], 587, SecureSocketOptions.StartTls);
            await smtp.AuthenticateAsync(_config["EmailSettings:EmailUser"]!, _config["EmailSettings:EmailPass"]!);
            await smtp.SendAsync(email);
            await smtp.DisconnectAsync(true);
        }

        public async Task SendTicketEmailAsync(string toEmail, string customerName, string ticketType, int qty, string ticketId, string artist, string venue)
        {
            // 1. Generate QR Code
            using QRCodeGenerator qrGenerator = new QRCodeGenerator();
            using QRCodeData qrCodeData = qrGenerator.CreateQrCode(ticketId, QRCodeGenerator.ECCLevel.Q);
            using PngByteQRCode qrCode = new PngByteQRCode(qrCodeData);
            byte[] qrCodeImage = qrCode.GetGraphic(20);

            // 2. Path for background image - Added safety check
            string imageName = artist.ToLower().Contains("teddy") ? "teddy afro.jpg" : "artist.jpg";
            // Ensure path works on Linux (Render)
            string bgPath = Path.Combine(_env.ContentRootPath, "assets", imageName);

            // 3. Generate PDF
            byte[] pdfBytes = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(new PageSize(500, 300));
                    page.Margin(0);
                    page.Background().Layers(layers =>
                    {
                        // Safely check for background image
                        if (File.Exists(bgPath)) 
                        {
                            layers.PrimaryLayer().Image(bgPath).FitArea();
                        }
                        else 
                        {
                            layers.PrimaryLayer().Background(Colors.Black);
                        }
                        layers.Layer().Background("#CC000000"); 
                    });

                    page.Content().Padding(10).Row(row =>
                    {
                        row.RelativeItem(3).Padding(15).Column(col =>
                        {
                            col.Item().Text(artist.ToUpper()).FontSize(24).ExtraBold().FontColor(Colors.White);
                            col.Item().Text($"{venue} • {DateTime.Now:MMMM dd, yyyy}").FontSize(9).FontColor(Colors.Cyan.Lighten3);
                            col.Spacing(10);
                            col.Item().Table(table =>
                            {
                                table.ColumnsDefinition(c => { c.ConstantColumn(60); c.RelativeColumn(); });
                                void AddRow(string label, string value) {
                                    table.Cell().PaddingVertical(1).Text(label).FontColor(Colors.Grey.Lighten2).FontSize(9).Bold();
                                    table.Cell().PaddingVertical(1).Text(value).FontColor(Colors.White).FontSize(9);
                                }
                                AddRow("Ticket ID:", ticketId.Length > 15 ? ticketId.Substring(0, 15) : ticketId);
                                AddRow("Name:", customerName);
                                AddRow("Type:", ticketType);
                                AddRow("Qty:", qty.ToString());
                            });
                        });

                        row.RelativeItem(1.5f).Padding(10).Column(col =>
                        {
                            col.Item().AlignCenter().MaxWidth(90).Image(qrCodeImage);
                            col.Item().PaddingTop(5).AlignCenter().Text("Scan to verify").FontSize(7).FontColor(Colors.Grey.Lighten1);
                            col.Item().AlignBottom().AlignCenter().Background(Colors.Yellow.Medium).PaddingHorizontal(8).Text(ticketType.ToUpper()).FontSize(10).Black().Bold();
                        });
                    });
                });
            }).GeneratePdf();

            // 4. Send Email
            var email = new MimeMessage();
            email.From.Add(new MailboxAddress("Ethio Concert", _config["EmailSettings:EmailUser"]!));
            email.To.Add(MailboxAddress.Parse(toEmail));
            email.Subject = $"Your Ticket for {artist}";

            var builder = new BodyBuilder 
            { 
                HtmlBody = $"<h3>Hello {customerName}</h3><p>Your ticket for {artist} at {venue} is attached.</p>" 
            };
            builder.Attachments.Add("Ticket.pdf", pdfBytes, ContentType.Parse("application/pdf"));
            email.Body = builder.ToMessageBody();

            using var smtp = new SmtpClient();
            smtp.Timeout = 15000; // 15 second timeout for ticket emails
            
            await smtp.ConnectAsync(_config["EmailSettings:Host"], 587, SecureSocketOptions.StartTls);
            await smtp.AuthenticateAsync(_config["EmailSettings:EmailUser"]!, _config["EmailSettings:EmailPass"]!);
            await smtp.SendAsync(email);
            await smtp.DisconnectAsync(true);
        }
    }
}