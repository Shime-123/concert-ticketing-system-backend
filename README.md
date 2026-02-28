# Ethio Concert Ticketing System - Backend 🎫

This is the power behind the Ethio Concert platform. Built with **ASP.NET Core**, it handles secure payments, database management, and automated ticket delivery.

### 🚀 Key Features
* **Stripe Integration**: Secure checkout sessions and webhook processing.
* **Dynamic PDF Generation**: Uses **QuestPDF** to create high-fidelity, artist-branded tickets with QR codes.
* **Automated Mailing**: Integrated **SMTP Service** to deliver tickets directly to fans' inboxes.
* **Database**: Managed with **Entity Framework Core** and **SQL Server**.

### 📂 Folder Structure
* `/Controllers`: API endpoints for Stripe and Webhooks.
* `/Services`: Core logic for Emailing and PDF generation.
* `/Models`: Data schemas for Users, Tickets, and Purchases.
* `/assets`: High-resolution artist images for the PDF backgrounds.

### 🛠️ Tech Stack
* .NET 8.0 / ASP.NET Core
* Stripe.net
* QuestPDF & QRCoder
* Entity Framework Core
