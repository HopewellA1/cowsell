using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using AgriChoice.Models;
using Microsoft.AspNetCore.Authorization;
using AgriChoice.Data;
using Microsoft.EntityFrameworkCore;
using iText.IO.Font.Constants;
using iText.Kernel.Font;


namespace AgriChoice.Controllers
{
    [Authorize(Roles = "Customer")]
    public class InvoiceController : Controller
    {
        private readonly AgriChoiceContext _context;


        public InvoiceController(AgriChoiceContext context)
        {
            // UserManager<IdentityUser> userManager
            _context = context;
            //_userManager = userManager;
            //_braintreeService = braintreeService;
        }

        [HttpGet]
        public IActionResult DownloadInvoice1(int PurchaseId)
        {
            var purchase = _context.Purchases
                .Where(p => p.PurchaseId == PurchaseId)
                .Include(p => p.PurchaseCows)
                    .ThenInclude(pc => pc.Cow)
                .Include(p => p.User)
                .FirstOrDefault();

            if (purchase == null)
                return NotFound("Purchase not found.");

            using var memoryStream = new MemoryStream();
            var writerProperties = new WriterProperties().UseSmartMode();
            using var writer = new PdfWriter(memoryStream, writerProperties);
            using var pdf = new PdfDocument(writer);
            var document = new iText.Layout.Document(pdf);

            var boldFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);

            document.Add(new Paragraph("AgriChoice Invoice")
                .SetFont(boldFont)
                .SetFontSize(20)
                .SetTextAlignment(TextAlignment.CENTER));

            document.Add(new Paragraph($"Order ID: #{purchase.PurchaseId}"));
            document.Add(new Paragraph($"Purchase Date: {purchase.PurchaseDate.ToString("MMMM dd, yyyy")}"));
            document.Add(new Paragraph($"Customer Email: {purchase.User.Email}"));
            document.Add(new Paragraph($"Delivery Address: {purchase.DeliveryAddress}"));
            document.Add(new Paragraph($"Delivery Status: {purchase.DeliveryStatus}"));
            document.Add(new Paragraph($"Payment Status: {purchase.PaymentStatus}"));

            document.Add(new Paragraph(" ")); // space

            // Table for Cows
            var table = new Table(UnitValue.CreatePercentArray(new float[] { 3, 2, 2, 2, 2 }))
                .UseAllAvailableWidth();

            table.AddHeaderCell("Cow Name & ID");
            table.AddHeaderCell("Age");
            table.AddHeaderCell("Weight (kg)");
            table.AddHeaderCell("Breed");
            table.AddHeaderCell("Price (R)");

            foreach (var pc in purchase.PurchaseCows)
            {
                var cow = pc.Cow;
                table.AddCell($"{cow.Name} (#{cow.CowId})");
                table.AddCell($"{cow.Age} months");
                table.AddCell($"{cow.Weight} kg");
                table.AddCell($"{cow.Breed}");
                table.AddCell($"R {cow.Price:N2}");
            }

            document.Add(table);

            document.Add(new Paragraph(" ")); // space

            document.Add(new Paragraph($"Subtotal: R {purchase.PurchaseCows.Sum(pc => pc.Cow.Price):N2}"));
            document.Add(new Paragraph($"Delivery Fee: R {purchase.ShippingCost:N2}"));
            document.Add(new Paragraph($"Total: R {purchase.TotalPrice:N2}"));

            if (purchase.DeliveryStatus == Purchase.Deliverystatus.Canceled)
            {
                document.Add(new Paragraph("⚠️ This order has been canceled.").SetFontColor(iText.Kernel.Colors.ColorConstants.RED));
            }

            document.Close();
            var fileName = $"Invoice_Order_{purchase.PurchaseId}.pdf";
            var content = memoryStream.ToArray();

            return File(content, "application/pdf", fileName);
        }
   



        public IActionResult DownloadInvoice(int PurchaseId)
        {
            var purchase = _context.Purchases
                .Where(p => p.PurchaseId == PurchaseId)
                .Include(p => p.PurchaseCows)
                    .ThenInclude(pc => pc.Cow)
                .Include(p => p.User)
                .FirstOrDefault();

            if (purchase == null)
                return NotFound("Purchase not found.");

            using var memoryStream = new MemoryStream();
   

            var writerProperties = new WriterProperties().UseSmartMode();
            using var writer = new PdfWriter(memoryStream, writerProperties);


            using var pdf = new PdfDocument(writer);
            var document = new iText.Layout.Document(pdf);

            var boldFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);

            document.Add(new Paragraph("AgriChoice Invoice")
                .SetFont(boldFont)
                .SetFontSize(20)
                .SetTextAlignment(TextAlignment.CENTER));

            document.Add(new Paragraph($"Order ID: #{purchase.PurchaseId}"));
            document.Add(new Paragraph($"Purchase Date: {purchase.PurchaseDate:MMMM dd, yyyy}"));
            document.Add(new Paragraph($"Customer Email: {purchase.User.Email}"));
            document.Add(new Paragraph($"Delivery Address: {purchase.DeliveryAddress}"));
            document.Add(new Paragraph($"Delivery Status: {purchase.DeliveryStatus}"));
            document.Add(new Paragraph($"Payment Status: {purchase.PaymentStatus}"));

            document.Add(new Paragraph(" ")); // space

            // Table for Cows
            var table = new Table(UnitValue.CreatePercentArray(new float[] { 3, 2, 2, 2, 2 }))
                .UseAllAvailableWidth();

            table.AddHeaderCell("Cow Name & ID");
            table.AddHeaderCell("Age");
            table.AddHeaderCell("Weight (kg)");
            table.AddHeaderCell("Breed");
            table.AddHeaderCell("Price (R)");

            foreach (var pc in purchase.PurchaseCows)
            {
                var cow = pc.Cow;
                table.AddCell($"{cow.Name} (#{cow.CowId})");
                table.AddCell($"{cow.Age} months");
                table.AddCell($"{cow.Weight} kg");
                table.AddCell($"{cow.Breed}");
                table.AddCell($"R {cow.Price:N2}");
            }

            document.Add(table);
            document.Add(new Paragraph(" ")); // space

            decimal subtotal = purchase.PurchaseCows.Sum(pc => pc.Cow.Price);
            decimal delivery = purchase.ShippingCost;
            decimal total = purchase.TotalPrice;

            document.Add(new Paragraph($"Subtotal: R {subtotal:N2}"));
            document.Add(new Paragraph($"Delivery Fee: R {delivery:N2}"));
            document.Add(new Paragraph($"Total: R {total:N2}"));

            if (purchase.DeliveryStatus == Purchase.Deliverystatus.Canceled)
            {
                document.Add(new Paragraph("⚠️ This order has been canceled.")
                    .SetFontColor(iText.Kernel.Colors.ColorConstants.RED));

                decimal deduction = total * 0.05m;
                decimal refund = total - deduction;

                document.Add(new Paragraph($"Deduction (5%): R {deduction:N2}"));
                document.Add(new Paragraph($"Refund Amount: R {refund:N2}")
                    .SetFontColor(iText.Kernel.Colors.ColorConstants.GREEN));
            }

            document.Close();
            var fileName = $"Invoice_Order_{purchase.PurchaseId}.pdf";
            var content = memoryStream.ToArray();

            return File(content, "application/pdf", fileName);
        }
   
    }

}
