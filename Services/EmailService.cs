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

        public async Task SendTicketEmailAsync(string toEmail, string customerName, string ticketType, int qty, string ticketId, string artist, string venue, string imageUrl)
        {
            try
            {
                // 1. Get Background Image
                byte[] bgImageBytes = null;
                try 
                {
                    if (!string.IsNullOrEmpty(imageUrl))
                    {
                        string finalUrl = imageUrl;
                        if (!imageUrl.StartsWith("http"))
                        {
                            string baseUrl = "https://concert-ticketing-system-backend.onrender.com";
                            finalUrl = $"{baseUrl}/{imageUrl.TrimStart('/')}";
                            Console.WriteLine($"🔗 Converting local path to Full URL: {finalUrl}");
                        }

                        using var client = _httpClientFactory.CreateClient();
                        client.Timeout = TimeSpan.FromSeconds(10); 
                        bgImageBytes = await client.GetByteArrayAsync(finalUrl);
                        Console.WriteLine("✅ IMAGE SUCCESSFULLY RETRIEVED!");
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
                        page.Size(new PageSize(500, 310)); 
                        page.Margin(0);
                        
                        page.Background().Layers(layers =>
                        {
                            if (bgImageBytes != null)
                                layers.PrimaryLayer().Image(bgImageBytes).FitArea();
                            else
                                layers.PrimaryLayer().Background(Colors.Black);
                            
                            layers.Layer().Background("#DD000000"); 
                        });

                        page.Content().Column(mainCol => 
                        {
                            mainCol.Item().Row(row =>
                            {
                                // Left Side: Details
                                row.RelativeItem(3).Padding(20).Column(col =>
                                {
                                    col.Item().Text(artist.ToUpper()).FontSize(26).ExtraBold().FontColor(Colors.White).LetterSpacing(0.05f);
                                    col.Item().Text($"{venue.ToUpper()}").FontSize(10).SemiBold().FontColor(Colors.Yellow.Medium);
                                    col.Item().Text($"{DateTime.Now:MMMM dd, yyyy} • Doors open at 6:00 PM").FontSize(8).FontColor(Colors.Grey.Lighten2);
                                    
                                    col.Item().PaddingVertical(10).LineHorizontal(1).LineColor(Colors.Grey.Darken3);

                                    col.Item().Table(table =>
                                    {
                                        table.ColumnsDefinition(c => { c.ConstantColumn(70); c.RelativeColumn(); });
                                        void AddRow(string label, string value) {
                                            table.Cell().PaddingVertical(2).Text(label).FontColor(Colors.Grey.Lighten1).FontSize(8).Bold();
                                            table.Cell().PaddingVertical(2).Text(value).FontColor(Colors.White).FontSize(9).Medium();
                                        }
                                        AddRow("TICKET ID:", ticketId.Length > 15 ? ticketId.Substring(0, 15) : ticketId);
                                        AddRow("HOLDER:", customerName.ToUpper());
                                        AddRow("ACCESS:", ticketType);
                                        AddRow("QUANTITY:", qty.ToString());
                                    });
                                });

                                // Right Side: QR & Badge
                                row.RelativeItem(1.5f).Background(Colors.Grey.Darken4).Padding(15).Column(col =>
                                {
                                    col.Item().AlignCenter().Text("ETHIO CONCERT").FontSize(8).ExtraBold().FontColor(Colors.White);
                                    col.Item().PaddingVertical(5).AlignCenter().MaxWidth(90).Image(qrCodeImage);
                                    col.Item().AlignCenter().Text("SCAN TO VERIFY").FontSize(6).FontColor(Colors.Grey.Lighten1);
                                    
                                    // FIXED: .Spacing() called on the Column, not .Item()
                                    col.Spacing(10); 

                                    col.Item().AlignBottom().AlignCenter().Background(Colors.Yellow.Medium).PaddingVertical(4).PaddingHorizontal(8).Text(ticketType.ToUpper()).FontSize(11).Black().ExtraBold();
                                });
                            });

                            // Footer
                            mainCol.Item().AlignBottom().Background(Colors.Black).PaddingHorizontal(20).PaddingVertical(5).Row(footerRow => 
                            {
                                footerRow.RelativeItem().Text($"© {DateTime.Now.Year} ETHIO CONCERT ORGANIZATION | ALL RIGHTS RESERVED").FontSize(7).FontColor(Colors.Grey.Darken1).Italic();
                                footerRow.RelativeItem().AlignRight().Text("NON-TRANSFERABLE • VALID FOR ONE ENTRY").FontSize(7).FontColor(Colors.Grey.Darken1).Bold();
                            });
                        });
                    });
                }).GeneratePdf();

                // 4. Send Email
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