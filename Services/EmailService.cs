using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using QRCoder;
using System.Net;
using System.Net.Mail;

namespace Concert_Backend.Services
{
    public class EmailService
    {
        private readonly IConfiguration _config;
        private readonly IWebHostEnvironment _env;

        public EmailService(IConfiguration config, IWebHostEnvironment env)
        {
            _config = config;
            _env = env;
            QuestPDF.Settings.License = LicenseType.Community;
        }

        public async Task SendTicketEmailAsync(string toEmail, string customerName, string ticketDetails, string ticketType, int qty, string ticketId)
        {
            // 1. Generate QR Code
            using QRCodeGenerator qrGenerator = new QRCodeGenerator();
            using QRCodeData qrCodeData = qrGenerator.CreateQrCode($"ID:{ticketId}|Name:{customerName}", QRCodeGenerator.ECCLevel.Q);
            using PngByteQRCode qrCode = new PngByteQRCode(qrCodeData);
            byte[] qrCodeImage = qrCode.GetGraphic(20);

            // 2. Define Assets
            string bgPath = Path.Combine(_env.WebRootPath, "assets", "artist.jpg");

            // 3. Generate PDF (600x300 Landscape)
            byte[] pdfBytes = Document.Create(container =>
            {
                container.Page(page =>
                {
                    // Set custom size: 600pt x 300pt (Landscape)
                    page.Size(new PageSize(600, 300));
                    page.Margin(0);

                    // --- LAYER 1: Background Image ---
                    page.Background().Stack(stack =>
                    {
                        if (File.Exists(bgPath))
                            stack.Item().Image(bgPath).FitArea();
                        else
                            stack.Item().Placeholder().Background(Colors.Black);

                        // Dark Overlay for readability (52% opacity)
                        stack.Item().AlignCenter().AlignMiddle().Background(Colors.Black.Medium).Opacity(0.52f);
                    });

                    // --- LAYER 2: Content ---
                    page.Content().Padding(10).Layers(layers =>
                    {
                        // Neon Border Effect
                        layers.Layer().Canvas((canvas, size) =>
                        {
                            canvas.DrawRoundRect(0, 0, size.Width, size.Height, 12, Paint.Stroke(Colors.Pink.Medium, 2));
                            canvas.DrawRoundRect(5, 5, size.Width - 10, size.Height - 10, 10, Paint.Stroke(Colors.Cyan.Medium, 1));
                        });

                        layers.PrimaryLayer().Row(row =>
                        {
                            // Left Section: Tear-off Stub
                            row.RelativeItem(1.5f).BorderRight(1).DashArray(new[] { 5f, 5f }).BorderColor(Colors.Grey.Lighten2).Padding(15).Column(col =>
                            {
                                col.Item().RotateLeft().Text("TEAR HERE").FontSize(9).FontColor(Colors.White).SemiBold();
                                col.Spacing(10);
                                col.Item().AlignCenter().Text("SCAN ME").FontSize(8).FontColor(Colors.Cyan.Lighten3);
                                col.Item().AlignCenter().MaxWidth(80).Image(qrCodeImage);
                            });

                            // Right Section: Main Info
                            row.RelativeItem(3.5f).Padding(20).Column(col =>
                            {
                                col.Item().Text("SONIC RESONANCE 2026").FontSize(28).ExtraBold().FontColor(Colors.White);
                                col.Item().Text("Main Arena • " + DateTime.Now.ToString("f")).FontSize(10).FontColor(Colors.Cyan.Lighten3);
                                
                                col.Spacing(15);

                                col.Item().Table(table =>
                                {
                                    table.ColumnsDefinition(c => { c.RelativeColumn(); c.RelativeColumn(); });
                                    
                                    table.Cell().Text("NAME:").FontColor(Colors.Pink.Lighten3).FontSize(10).Bold();
                                    table.Cell().Text(customerName).FontColor(Colors.White).FontSize(10);

                                    table.Cell().Text("TYPE:").FontColor(Colors.Pink.Lighten3).FontSize(10).Bold();
                                    table.Cell().Text(ticketType).FontColor(Colors.White).FontSize(10);

                                    table.Cell().Text("QTY:").FontColor(Colors.Pink.Lighten3).FontSize(10).Bold();
                                    table.Cell().Text(qty.ToString()).FontColor(Colors.White).FontSize(10);
                                });

                                col.Item().AlignBottom().AlignCenter().Text("Non-transferable • Powered by Live Concert").FontSize(7).FontColor(Colors.Grey.Lighten1);
                            });
                        });
                    });
                });
            }).GeneratePdf();

            // 4. SMTP Send (Same as your previous working version)
            using var smtpClient = new SmtpClient("smtp.gmail.com")
            {
                Port = 587,
                Credentials = new NetworkCredential(_config["EmailSettings:EmailUser"], _config["EmailSettings:EmailPass"]),
                EnableSsl = true,
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(_config["EmailSettings:EmailUser"]!),
                Subject = "Your Exclusive Concert Ticket",
                Body = "<h1>Get Ready!</h1><p>Your ticket is attached below.</p>",
                IsBodyHtml = true,
            };
            mailMessage.To.Add(toEmail);

            using (var ms = new MemoryStream(pdfBytes))
            {
                mailMessage.Attachments.Add(new Attachment(ms, "Ticket.pdf", "application/pdf"));
                await smtpClient.SendMailAsync(mailMessage);
            }
        }
    }
}