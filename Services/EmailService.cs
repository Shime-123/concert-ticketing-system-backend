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

        public async Task SendTicketEmailAsync(string toEmail, string customerName, string ticketType, int qty, string ticketId, string artist, string venue)
        {
            // 1. Generate QR Code
            using QRCodeGenerator qrGenerator = new QRCodeGenerator();
            using QRCodeData qrCodeData = qrGenerator.CreateQrCode(ticketId, QRCodeGenerator.ECCLevel.Q);
            using PngByteQRCode qrCode = new PngByteQRCode(qrCodeData);
            byte[] qrCodeImage = qrCode.GetGraphic(20);

            // 2. Determine Background Image
            string imageName = artist.ToLower().Contains("teddy") ? "teddy afro.jpg" : "artist.jpg";
            string bgPath = Path.Combine(_env.ContentRootPath, "assets", imageName);

            // 3. Generate PDF
            byte[] pdfBytes = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(new PageSize(500, 300)); // Smaller fit to pull layout tighter
                    page.Margin(0);

                    // Background Layer
                    page.Background().Layers(layers =>
                    {
                        if (File.Exists(bgPath))
                            layers.PrimaryLayer().Image(bgPath).FitArea();
                        else
                            layers.PrimaryLayer().Background(Colors.Black);

                        layers.Layer().Background("#CC000000"); // Dark glass effect
                    });

                    page.Content().Padding(10).Row(row =>
                    {
                        // LEFT: Ticket Details
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

                        // RIGHT: QR Stub
                        row.RelativeItem(1.5f).Padding(10).Column(col =>
                        {
                            col.Item().AlignCenter().MaxWidth(90).Image(qrCodeImage);
                            col.Item().PaddingTop(5).AlignCenter().Text("Scan to verify").FontSize(7).FontColor(Colors.Grey.Lighten1);
                            
                            col.Item().AlignBottom().AlignCenter()
                                .Background(Colors.Yellow.Medium)
                                .PaddingHorizontal(8)
                                .Text(ticketType.ToUpper()).FontSize(10).Black().Bold();
                        });
                    });
                });
            }).GeneratePdf();

            // 4. SMTP Send
            using var smtpClient = new SmtpClient("smtp.gmail.com")
            {
                Port = 587,
                Credentials = new NetworkCredential(_config["EmailSettings:EmailUser"], _config["EmailSettings:EmailPass"]),
                EnableSsl = true,
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(_config["EmailSettings:EmailUser"]!, "Ethio Concert Organization"),
                Subject = $"Your Ticket for {artist}",
                Body = $"Hello {customerName}, your ticket for {artist} at {venue} is attached.",
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