using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using QRCoder;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Hosting;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace Concert_Backend.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _config;
        private readonly IWebHostEnvironment _env;
        private readonly IHttpClientFactory _httpClientFactory;

        public EmailService(IConfiguration config, IWebHostEnvironment env, IHttpClientFactory httpClientFactory)
        {
            _config = config;
            _env = env;
            _httpClientFactory = httpClientFactory;
            QuestPDF.Settings.License = LicenseType.Community;
        }

        // Generic Email (Forgot Password, etc.)
        public async Task SendEmailAsync(string toEmail, string subject, string htmlContent)
        {
            using var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Add("api-key", _config["EmailSettings:EmailPass"]);

            var payload = new
            {
                sender = new { name = "Ethio Concert", email = "shimelisgetachew11@gmail.com" },
                to = new[] { new { email = toEmail } },
                subject = subject,
                htmlContent = htmlContent
            };

            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            var response = await client.PostAsync("https://api.brevo.com/v3/smtp/email", content);

            if (response.IsSuccessStatusCode)
                Console.WriteLine($"🚀 API SUCCESS: Email delivered to {toEmail}");
            else
                Console.WriteLine($"❌ API ERROR: {await response.Content.ReadAsStringAsync()}");
        }

        // Dynamic Ticket Email with Poster Background
        public async Task SendTicketEmailAsync(string toEmail, string customerName, string ticketType, int qty, string ticketId, string artist, string venue, string imageUrl)
        {
            try
            {
                // 1. Get the Background Image (From Web or Local)
                // 1. Get the Background Image (With Debugging)
       byte[] bgImageBytes = null;
       try 
         {
         if (!string.IsNullOrEmpty(imageUrl))
        {
        if (imageUrl.StartsWith("http"))
        {
            Console.WriteLine($"🌐 Attempting to download external image: {imageUrl}");
            using var client = _httpClientFactory.CreateClient();
            // Set a timeout so a slow image doesn't hang the task
            client.Timeout = TimeSpan.FromSeconds(10); 
            bgImageBytes = await client.GetByteArrayAsync(imageUrl);
        }
        else
        {
            // Fix path for Linux (Render) servers
            string cleanPath = imageUrl.Replace("\\", "/").TrimStart('/');
            string localPath = Path.Combine(_env.ContentRootPath, cleanPath);
            
            Console.WriteLine($"📂 Checking local path: {localPath}");
            
            if (File.Exists(localPath)) 
            {
                bgImageBytes = await File.ReadAllBytesAsync(localPath);
            }
            else 
            {
                Console.WriteLine("⚠️ Local file not found at path.");
            }
        }
        }
        }
         catch (Exception imgEx)
        {
         Console.WriteLine($"❌ IMAGE LOAD ERROR: {imgEx.Message}");
           }

                // 2. Generate QR Code
                using QRCodeGenerator qrGenerator = new QRCodeGenerator();
                using QRCodeData qrCodeData = qrGenerator.CreateQrCode(ticketId, QRCodeGenerator.ECCLevel.Q);
                using PngByteQRCode qrCode = new PngByteQRCode(qrCodeData);
                byte[] qrCodeImage = qrCode.GetGraphic(20);

                // 3. Generate PDF
                byte[] pdfBytes = Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(new PageSize(500, 300));
                        page.Margin(0);
                        page.Background().Layers(layers =>
                        {
                            if (bgImageBytes != null)
                                layers.PrimaryLayer().Image(bgImageBytes).FitArea();
                            else
                                layers.PrimaryLayer().Background(Colors.Black);
                            
                            layers.Layer().Background("#CC000000"); // Dark overlay for text clarity
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
                                    AddRow("ID:", ticketId.Length > 15 ? ticketId.Substring(0, 15) : ticketId);
                                    AddRow("Name:", customerName);
                                    AddRow("Type:", ticketType);
                                    AddRow("Qty:", qty.ToString());
                                });
                            });

                            row.RelativeItem(1.5f).Padding(10).Column(col =>
                            {
                                col.Item().AlignCenter().MaxWidth(85).Image(qrCodeImage);
                                col.Item().PaddingTop(5).AlignCenter().Text("Scan to verify").FontSize(7).FontColor(Colors.Grey.Lighten1);
                                col.Item().AlignBottom().AlignCenter().Background(Colors.Yellow.Medium).PaddingHorizontal(8).Text(ticketType.ToUpper()).FontSize(10).Black().Bold();
                            });
                        });
                    });
                }).GeneratePdf();

                // 4. Send via Brevo API
                using var apiClient = _httpClientFactory.CreateClient();
                apiClient.DefaultRequestHeaders.Add("api-key", _config["EmailSettings:EmailPass"]);

                var payload = new
                {
                    sender = new { name = "Ethio Concert", email = "shimelisgetachew11@gmail.com" },
                    to = new[] { new { email = toEmail } },
                    subject = $"Ticket: {artist}",
                    htmlContent = $"<h3>Hello {customerName}</h3><p>Your ticket for <b>{artist}</b> is attached.</p>",
                    attachment = new[] 
                    {
                        new { content = Convert.ToBase64String(pdfBytes), name = "Ticket.pdf" }
                    }
                };

                var response = await apiClient.PostAsync("https://api.brevo.com/v3/smtp/email", 
                    new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    Console.WriteLine($"🚀 TICKET API SUCCESS: Sent to {toEmail}");
                else
                    Console.WriteLine($"❌ TICKET API ERROR: {await response.Content.ReadAsStringAsync()}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ TICKET SYSTEM ERROR: {ex.Message}");
            }
        }
    }
}