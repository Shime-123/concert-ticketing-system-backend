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
using System.Net.Http;
using System.Text;
using System.Text.Json;

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
    using var client = new HttpClient();
    // Use the API KEY you just generated
    client.DefaultRequestHeaders.Add("api-key", _config["EmailSettings:EmailPass"]); 

    var payload = new
    {
        // This MUST be the email verified in your Brevo 'Senders' tab
        sender = new { name = "Ethio Concert", email = "shimelisgetachew11@gmail.com" },
        to = new[] { new { email = toEmail } },
        subject = subject,
        htmlContent = htmlContent
    };

    var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
    var response = await client.PostAsync("https://api.brevo.com/v3/smtp/email", content);

    if (response.IsSuccessStatusCode) {
        Console.WriteLine($"🚀 API SUCCESS: Email delivered to {toEmail}");
    } else {
        var error = await response.Content.ReadAsStringAsync();
        Console.WriteLine($"❌ API ERROR: {error}");
    }
}
public async Task SendTicketEmailAsync(string toEmail, string customerName, string ticketType, int qty, string ticketId, string artist, string venue)
{
    try
    {
        // 1. Generate QR Code
        using QRCodeGenerator qrGenerator = new QRCodeGenerator();
        using QRCodeData qrCodeData = qrGenerator.CreateQrCode(ticketId, QRCodeGenerator.ECCLevel.Q);
        using PngByteQRCode qrCode = new PngByteQRCode(qrCodeData);
        byte[] qrCodeImage = qrCode.GetGraphic(20);

        // 2. Path for background image
        string imageName = artist.ToLower().Contains("teddy") ? "teddy afro.jpg" : "artist.jpg";
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
                    if (File.Exists(bgPath)) 
                        layers.PrimaryLayer().Image(bgPath).FitArea();
                    else 
                        layers.PrimaryLayer().Background(Colors.Black);
                    
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

        // 4. Send via Brevo API (NOT SMTP)
        using var client = new HttpClient();
        client.DefaultRequestHeaders.Add("api-key", _config["EmailSettings:EmailPass"]);

        var payload = new
        {
            sender = new { name = "Ethio Concert", email = "shimelisgetachew11@gmail.com" },
            to = new[] { new { email = toEmail } },
            subject = $"Your Ticket for {artist}",
            htmlContent = $"<h3>Hello {customerName}</h3><p>Your ticket for {artist} at {venue} is attached.</p>",
            attachment = new[] 
            {
                new 
                {
                    content = Convert.ToBase64String(pdfBytes), // PDF converted to Base64
                    name = "Ticket.pdf"
                }
            }
        };

        var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        var response = await client.PostAsync("https://api.brevo.com/v3/smtp/email", content);

        if (response.IsSuccessStatusCode)
        {
            Console.WriteLine("🚀 TICKET SENT SUCCESSFULLY VIA API TO: " + toEmail);
        }
        else
        {
            var error = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"❌ TICKET API ERROR: {error}");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ TICKET SYSTEM ERROR: {ex.Message}");
    }
}
    }
}