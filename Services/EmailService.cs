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

        // A professional, neutral dark texture (Base64) to use if the poster is missing
        private const string DefaultTicketBgBase64 = "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8/5+hHgAHggJ/PchI7wAAAABJRU5ErkJggg=="; // Replace this with a real high-res Base64 string if desired

        public EmailService(IConfiguration config, IWebHostEnvironment env, IHttpClientFactory httpClientFactory)
        {
            _config = config;
            _env = env;
            _httpClientFactory = httpClientFactory;
            QuestPDF.Settings.License = LicenseType.Community;
        }

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
            await client.PostAsync("https://api.brevo.com/v3/smtp/email", content);
        }

        public async Task SendTicketEmailAsync(string toEmail, string customerName, string ticketType, int qty, string ticketId, string artist, string venue, string imageUrl)
        {
            try
            {
                byte[] bgImageBytes = null;

                // 1. Image Retrieval Logic
                try
                {
                    if (!string.IsNullOrEmpty(imageUrl))
                    {
                        if (imageUrl.StartsWith("http"))
                        {
                            using var client = _httpClientFactory.CreateClient();
                            client.Timeout = TimeSpan.FromSeconds(10);
                            bgImageBytes = await client.GetByteArrayAsync(imageUrl);
                        }
                        else
                        {
                            // Extracts 'artist.jpg' regardless of whether path is D:\ or /shime/
                            string fileName = Path.GetFileName(imageUrl);
                            string localPath = Path.Combine(_env.ContentRootPath, "assets", fileName);

                            if (File.Exists(localPath))
                                bgImageBytes = await File.ReadAllBytesAsync(localPath);
                        }
                    }
                }
                catch (Exception imgEx)
                {
                    Console.WriteLine($"⚠️ Image loading failed, using professional fallback: {imgEx.Message}");
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
                        page.Size(new PageSize(500, 280));
                        page.Margin(0);
                        
                        // BACKGROUND LAYER
                        page.Background().Layers(layers =>
                        {
                            if (bgImageBytes != null)
                            {
                                layers.PrimaryLayer().Image(bgImageBytes).FitArea();
                            }
                            else
                            {
                                // Professional Fallback: Gradient-like Dark Blue/Black
                                layers.PrimaryLayer().Background("#1a1a2e"); 
                            }
                            
                            // Aesthetic Overlay
                            layers.Layer().Background("#CC000000"); // 80% Black Overlay
                            layers.Layer().Padding(10).BorderRight(3).BorderColor(Colors.Yellow.Medium).ExtendVertical();
                        });

                        // CONTENT LAYER
                        page.Content().Padding(20).Row(row =>
                        {
                            // Left Side: Concert Details
                            row.RelativeItem(3).Column(col =>
                            {
                                col.Item().Text("ETHIO CONCERT").FontSize(10).SemiBold().FontColor(Colors.Yellow.Medium).LetterSpacing(0.2f);
                                col.Item().PaddingBottom(5).Text(artist.ToUpper()).FontSize(26).ExtraBold().FontColor(Colors.White);
                                col.Item().Text($"{venue}").FontSize(11).FontColor(Colors.Grey.Lighten2);
                                col.Item().Text($"{DateTime.Now:dddd, MMMM dd, yyyy}").FontSize(10).Italic().FontColor(Colors.Cyan.Lighten3);
                                
                                col.Spacing(12);

                                col.Item().Table(table =>
                                {
                                    table.ColumnsDefinition(c => { c.ConstantColumn(70); c.RelativeColumn(); });
                                    
                                    void AddDetail(string label, string value) {
                                        table.Cell().Text(label).FontColor(Colors.Grey.Medium).FontSize(9).Bold();
                                        table.Cell().Text(value).FontColor(Colors.White).FontSize(9);
                                    }
                                    
                                    AddDetail("ADMIT:", customerName);
                                    AddDetail("TICKET ID:", ticketId.Length > 12 ? ticketId.Substring(0, 12) : ticketId);
                                    AddDetail("QUANTITY:", qty.ToString());
                                });
                            });

                            // Right Side: QR & Type
                            row.RelativeItem(1.5f).Column(col =>
                            {
                                col.Item().AlignCenter().Background(Colors.White).Padding(5).MaxWidth(90).Image(qrCodeImage);
                                col.Item().PaddingTop(5).AlignCenter().Text("SCAN TO VERIFY").FontSize(7).FontColor(Colors.Grey.Lighten1).LetterSpacing(0.1f);
                                
                                col.Item().AlignBottom().PaddingBottom(5).AlignCenter()
                                   .Background(Colors.Yellow.Medium).PaddingVertical(4).PaddingHorizontal(10)
                                   .Text(ticketType.ToUpper()).FontSize(12).ExtraBold().FontColor(Colors.Black);
                            });
                        });
                    });
                }).GeneratePdf();

                // 4. Send via Brevo
                using var apiClient = _httpClientFactory.CreateClient();
                apiClient.DefaultRequestHeaders.Add("api-key", _config["EmailSettings:EmailPass"]);

                var payload = new
                {
                    sender = new { name = "Ethio Concert", email = "shimelisgetachew11@gmail.com" },
                    to = new[] { new { email = toEmail } },
                    subject = $"Your Ticket: {artist} - {ticketId.Substring(0,5)}",
                    htmlContent = $"<h3>Enjoy the show, {customerName}!</h3><p>Attached is your <b>{ticketType}</b> ticket for {artist}. See you at {venue}!</p>",
                    attachment = new[] { new { content = Convert.ToBase64String(pdfBytes), name = "EthioConcert_Ticket.pdf" } }
                };

                await apiClient.PostAsync("https://api.brevo.com/v3/smtp/email", 
                    new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"));

                Console.WriteLine($"🚀 TICKET SENT SUCCESSFULLY TO {toEmail}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ CRITICAL ERROR: {ex.Message}");
            }
        }
    }
}