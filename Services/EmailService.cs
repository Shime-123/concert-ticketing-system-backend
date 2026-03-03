using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using QRCoder;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Hosting;
using System.IO;
using System.Net.Http.Json;

namespace Concert_Backend.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _config;
        private readonly IWebHostEnvironment _env;
        private readonly HttpClient _httpClient;

        public EmailService(IConfiguration config, IWebHostEnvironment env)
        {
            _config = config;
            _env = env;
            _httpClient = new HttpClient();
            QuestPDF.Settings.License = LicenseType.Community;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string htmlContent)
        {
            var apiKey = _config["EmailSettings:ApiKey"]; // You need to add this to Render
            var senderEmail = _config["EmailSettings:EmailUser"];

            var payload = new
            {
                sender = new { name = "Ethio Concert", email = senderEmail },
                to = new[] { new { email = toEmail } },
                subject = subject,
                htmlContent = htmlContent
            };

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("api-key", apiKey);

            var response = await _httpClient.PostAsJsonAsync("https://api.brevo.com/v3/smtp/email", payload);

            if (response.IsSuccessStatusCode)
                Console.WriteLine("📧 API Reset Email Sent to: " + toEmail);
            else
                Console.WriteLine("❌ API Email Failed: " + await response.Content.ReadAsStringAsync());
        }

        public async Task SendTicketEmailAsync(string toEmail, string customerName, string ticketType, int qty, string ticketId, string artist, string venue)
        {
            // 1. Generate QR & PDF (Same as your logic)
            using QRCodeGenerator qrGenerator = new QRCodeGenerator();
            using QRCodeData qrCodeData = qrGenerator.CreateQrCode(ticketId, QRCodeGenerator.ECCLevel.Q);
            using PngByteQRCode qrCode = new PngByteQRCode(qrCodeData);
            byte[] qrCodeImage = qrCode.GetGraphic(20);

            string imageName = artist.ToLower().Contains("teddy") ? "teddy afro.jpg" : "artist.jpg";
            string bgPath = Path.Combine(_env.ContentRootPath, "assets", imageName);

            byte[] pdfBytes = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(new PageSize(500, 300));
                    page.Margin(0);
                    page.Background().Layers(layers => {
                        if (File.Exists(bgPath)) layers.PrimaryLayer().Image(bgPath).FitArea();
                        else layers.PrimaryLayer().Background(Colors.Black);
                        layers.Layer().Background("#CC000000"); 
                    });
                    page.Content().Padding(10).Row(row => {
                        row.RelativeItem(3).Padding(15).Column(col => {
                            col.Item().Text(artist.ToUpper()).FontSize(24).ExtraBold().FontColor(Colors.White);
                            col.Item().Text($"{venue} • {DateTime.Now:MMMM dd, yyyy}").FontSize(9).FontColor(Colors.Cyan.Lighten3);
                            col.Spacing(10);
                            col.Item().Table(table => {
                                table.ColumnsDefinition(c => { c.ConstantColumn(60); c.RelativeColumn(); });
                                void AddRow(string l, string v) {
                                    table.Cell().Text(l).FontColor(Colors.Grey.Lighten2).FontSize(9).Bold();
                                    table.Cell().Text(v).FontColor(Colors.White).FontSize(9);
                                }
                                AddRow("ID:", ticketId.Length > 15 ? ticketId.Substring(0, 15) : ticketId);
                                AddRow("Name:", customerName);
                                AddRow("Type:", ticketType);
                                AddRow("Qty:", qty.ToString());
                            });
                        });
                        row.RelativeItem(1.5f).Padding(10).Column(col => {
                            col.Item().AlignCenter().MaxWidth(90).Image(qrCodeImage);
                            col.Item().AlignBottom().AlignCenter().Background(Colors.Yellow.Medium).PaddingHorizontal(8).Text(ticketType.ToUpper()).FontSize(10).Black().Bold();
                        });
                    });
                });
            }).GeneratePdf();

            // 2. Send via API with Attachment
            var apiKey = _config["EmailSettings:ApiKey"];
            var senderEmail = _config["EmailSettings:EmailUser"];

            var payload = new
            {
                sender = new { name = "Ethio Concert", email = senderEmail },
                to = new[] { new { email = toEmail } },
                subject = $"Your Ticket for {artist}",
                htmlContent = $"<h3>Hello {customerName}</h3><p>Your ticket is attached.</p>",
                attachment = new[] {
                    new { 
                        content = Convert.ToBase64String(pdfBytes), 
                        name = "Ticket.pdf" 
                    }
                }
            };

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("api-key", apiKey);

            var response = await _httpClient.PostAsJsonAsync("https://api.brevo.com/v3/smtp/email", payload);
            
            if (response.IsSuccessStatusCode)
                Console.WriteLine("📧 API Ticket Sent to: " + toEmail);
            else
                Console.WriteLine("❌ API Ticket Failed: " + await response.Content.ReadAsStringAsync());
        }
    }
}