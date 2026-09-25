using System.Globalization;
using System.Text;
using GreenMart.Models;

namespace GreenMart.Services
{
    public class OrderReceiptPdfService : IOrderReceiptPdfService
    {
        public byte[] Create(Order order, IReadOnlyCollection<DeliveryAssignment> assignments)
        {
            var lines = BuildReceipt(order, assignments);
            return BuildPdf(Paginate(lines));
        }

        private static List<List<ReceiptLine>> Paginate(IEnumerable<ReceiptLine> lines)
        {
            const int availableHeight = 650;
            var pages = new List<List<ReceiptLine>> { new() };
            var usedHeight = 0;

            foreach (var line in lines)
            {
                var height = LineHeight(line.Kind);
                if (usedHeight + height > availableHeight && pages[^1].Count > 0)
                {
                    pages.Add(new List<ReceiptLine>());
                    usedHeight = 0;
                }

                pages[^1].Add(line);
                usedHeight += height;
            }

            return pages;
        }

        private static int LineHeight(string kind) => kind switch
        {
            "title" => 25,
            "subtitle" => 17,
            "section" => 29,
            "tableHeader" => 16,
            "tableRow" => 15,
            "small" => 14,
            "total" => 20,
            "divider" => 12,
            "space" => 10,
            _ => 16
        };

        private static List<ReceiptLine> BuildReceipt(
            Order order,
            IReadOnlyCollection<DeliveryAssignment> assignments)
        {
            var lines = new List<ReceiptLine>
            {
                new("title", "ORDER CONFIRMATION RECEIPT"),
                new("subtitle", $"Receipt reference: GM-{order.OrderId:D6}-{order.CreatedAt:yyyyMMdd}"),
                new("space", string.Empty),
                new("section", "ORDER DETAILS"),
                new("normal", $"Order number: #{order.OrderId}"),
                new("normal", $"Order status: {ReadableOrderStatus(order.Status)}"),
                new("normal", $"Payment method: {ReadablePaymentMethod(order.PaymentMethod)}"),
                new("normal", $"Payment status: {ReadablePaymentStatus(order.PaymentStatus)}"),
                new("normal", $"Order date: {order.CreatedAt:dd MMM yyyy, hh:mm tt}"),
                new("space", string.Empty),
                new("section", "CUSTOMER AND DELIVERY"),
                new("normal", $"Customer: {order.User.FullName}"),
                new("normal", $"Email: {order.User.Email}"),
                new("normal", $"Phone: {order.User.PhoneNumber}")
            };

            AddWrapped(lines, "Delivery address: ", order.ShippingAddress ?? "Not provided", 78);
            lines.Add(new("space", string.Empty));
            lines.Add(new("section", "ORDER ITEMS"));
            lines.Add(new("tableHeader", "Item                         Qty      Unit        Total"));
            lines.Add(new("divider", string.Empty));

            foreach (var item in order.OrderItems ?? Enumerable.Empty<OrderItem>())
            {
                var name = Fit(item.Product.ProductName, 27);
                var row = string.Format(
                    CultureInfo.InvariantCulture,
                    "{0,-28} {1,3}  {2,10}  {3,11}",
                    name,
                    item.Quantity,
                    Money(item.Price),
                    Money(item.Price * item.Quantity)
                );
                lines.Add(new("tableRow", row));
                lines.Add(new("small", $"Seller: {item.Product.User?.FullName ?? "Unknown seller"}"));
            }

            lines.Add(new("divider", string.Empty));
            lines.Add(new("total", $"ORDER TOTAL: {Money(order.TotalAmount)}"));
            lines.Add(new("space", string.Empty));
            lines.Add(new("section", "DELIVERY PARTNER"));

            if (assignments.Count == 0)
            {
                lines.Add(new("normal", "A delivery partner has not been assigned yet."));
            }
            else
            {
                foreach (var assignment in assignments)
                {
                    lines.Add(new("normal", $"Seller: {assignment.Seller.FullName}"));
                    AddWrapped(lines, "Pickup address: ", assignment.PickupAddress, 78);
                    lines.Add(new("normal", $"Pickup phone: {assignment.PickupPhone}"));
                    if (!string.IsNullOrWhiteSpace(assignment.PickupInstructions))
                    {
                        AddWrapped(lines, "Pickup instructions: ", assignment.PickupInstructions, 78);
                    }
                    lines.Add(new("normal", $"Delivery partner: {assignment.DeliveryMan.FullName}"));
                    lines.Add(new("normal", $"Delivery status: {ReadableStatus(assignment.Status)}"));
                    lines.Add(new("space", string.Empty));
                }
            }

            // Keep the unassigned-delivery message visually separate from the
            // following green section heading.
            lines.Add(new("space", string.Empty));
            lines.Add(new("section", "RECEIPT VERIFICATION"));
            AddWrapped(
                lines,
                string.Empty,
                "Show this receipt to the assigned delivery partner. The delivery partner should match the order number and customer details with the assignment shown in their GreenMart dashboard.",
                84
            );
            lines.Add(new("space", string.Empty));
            lines.Add(new("small", "This receipt was generated electronically by GreenMart."));

            return lines;
        }

        private static string ReadablePaymentMethod(string method) => method switch
        {
            "CashOnDelivery" => "Cash on Delivery",
            "Online" => "Online payment",
            _ => method
        };

        private static string ReadablePaymentStatus(string status) => status switch
        {
            "CashOnDelivery" => "Pay on delivery",
            _ => status
        };

        private static byte[] BuildPdf(IReadOnlyList<List<ReceiptLine>> pages)
        {
            const int catalogId = 1;
            const int pagesId = 2;
            const int regularFontId = 3;
            const int boldFontId = 4;
            const int monoFontId = 5;
            var pageIds = Enumerable.Range(0, pages.Count).Select(i => 6 + (i * 2)).ToList();
            var objects = new SortedDictionary<int, byte[]>();

            objects[catalogId] = Bytes("<< /Type /Catalog /Pages 2 0 R >>");
            objects[pagesId] = Bytes($"<< /Type /Pages /Kids [{string.Join(" ", pageIds.Select(id => $"{id} 0 R"))}] /Count {pages.Count} >>");
            objects[regularFontId] = Bytes("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>");
            objects[boldFontId] = Bytes("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold >>");
            objects[monoFontId] = Bytes("<< /Type /Font /Subtype /Type1 /BaseFont /Courier >>");

            for (var index = 0; index < pages.Count; index++)
            {
                var pageId = pageIds[index];
                var contentId = pageId + 1;
                var content = BuildPageContent(pages[index], index + 1, pages.Count);
                var contentBytes = Bytes(content);

                objects[pageId] = Bytes(
                    $"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] " +
                    $"/Resources << /Font << /F1 {regularFontId} 0 R /F2 {boldFontId} 0 R /F3 {monoFontId} 0 R >> >> " +
                    $"/Contents {contentId} 0 R >>"
                );
                objects[contentId] = Combine(
                    Bytes($"<< /Length {contentBytes.Length} >>\nstream\n"),
                    contentBytes,
                    Bytes("\nendstream")
                );
            }

            using var output = new MemoryStream();
            Write(output, "%PDF-1.4\n%GreenMart\n");
            var offsets = new Dictionary<int, long>();

            foreach (var entry in objects)
            {
                offsets[entry.Key] = output.Position;
                Write(output, $"{entry.Key} 0 obj\n");
                output.Write(entry.Value);
                Write(output, "\nendobj\n");
            }

            var xrefOffset = output.Position;
            var objectCount = objects.Keys.Max() + 1;
            Write(output, $"xref\n0 {objectCount}\n");
            Write(output, "0000000000 65535 f \n");
            for (var id = 1; id < objectCount; id++)
            {
                Write(output, $"{offsets[id]:D10} 00000 n \n");
            }

            Write(output, $"trailer\n<< /Size {objectCount} /Root 1 0 R >>\nstartxref\n{xrefOffset}\n%%EOF");
            return output.ToArray();
        }

        private static string BuildPageContent(
            IReadOnlyList<ReceiptLine> lines,
            int pageNumber,
            int totalPages)
        {
            var content = new StringBuilder();
            content.AppendLine("0.965 0.985 0.970 rg 0 0 595 842 re f");
            content.AppendLine("0.035 0.305 0.210 rg 0 766 595 76 re f");
            AddText(content, "F2", 22, 42, 802, "GreenMart", "1 1 1");
            AddText(content, "F1", 9, 43, 784, "Reuse more. Waste less.", "0.82 0.95 0.87");

            var y = 742;
            foreach (var line in lines)
            {
                switch (line.Kind)
                {
                    case "title":
                        AddText(content, "F2", 18, 42, y, line.Text, "0.03 0.24 0.16");
                        y -= 25;
                        break;
                    case "subtitle":
                        AddText(content, "F1", 9, 42, y, line.Text, "0.35 0.45 0.40");
                        y -= 17;
                        break;
                    case "section":
                        content.AppendLine($"0.88 0.96 0.90 rg 36 {y - 6} 523 22 re f");
                        AddText(content, "F2", 10, 43, y, line.Text, "0.03 0.36 0.23");
                        y -= 29;
                        break;
                    case "tableHeader":
                        AddText(content, "F3", 9, 43, y, line.Text, "0.12 0.25 0.20");
                        y -= 16;
                        break;
                    case "tableRow":
                        AddText(content, "F3", 9, 43, y, line.Text, "0.10 0.18 0.15");
                        y -= 15;
                        break;
                    case "small":
                        AddText(content, "F1", 8, 50, y, line.Text, "0.40 0.48 0.44");
                        y -= 14;
                        break;
                    case "total":
                        AddText(content, "F2", 12, 358, y, line.Text, "0.03 0.30 0.20");
                        y -= 20;
                        break;
                    case "divider":
                        content.AppendLine($"0.78 0.86 0.81 RG 0.7 w 42 {y} m 553 {y} l S");
                        y -= 12;
                        break;
                    case "space":
                        y -= 10;
                        break;
                    default:
                        AddText(content, "F1", 10, 43, y, line.Text, "0.13 0.22 0.18");
                        y -= 16;
                        break;
                }
            }

            content.AppendLine("0.78 0.86 0.81 RG 0.7 w 36 38 m 559 38 l S");
            AddText(content, "F1", 8, 42, 23, $"GreenMart order receipt | Page {pageNumber} of {totalPages}", "0.42 0.50 0.46");
            return content.ToString();
        }

        private static void AddText(
            StringBuilder content,
            string font,
            int size,
            int x,
            int y,
            string text,
            string color)
        {
            content.AppendLine($"BT /{font} {size} Tf {color} rg 1 0 0 1 {x} {y} Tm ({Escape(text)}) Tj ET");
        }

        private static void AddWrapped(
            ICollection<ReceiptLine> lines,
            string prefix,
            string value,
            int width)
        {
            var words = $"{prefix}{value}".Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var current = new StringBuilder();
            foreach (var word in words)
            {
                if (current.Length > 0 && current.Length + word.Length + 1 > width)
                {
                    lines.Add(new("normal", current.ToString()));
                    current.Clear();
                }
                if (current.Length > 0) current.Append(' ');
                current.Append(word);
            }
            if (current.Length > 0) lines.Add(new("normal", current.ToString()));
        }

        private static string Money(decimal amount) =>
            $"BDT {amount.ToString("N2", CultureInfo.InvariantCulture)}";

        private static string ReadableStatus(string status) => status switch
        {
            "PickedUp" => "Picked up",
            "OnTheWay" => "On the way",
            _ => status
        };

        private static string ReadableOrderStatus(string status) => status switch
        {
            "Pending" => "Order placed - awaiting seller confirmation",
            "Confirmed" => "Confirmed by seller",
            _ => status
        };

        private static string Fit(string value, int length) =>
            value.Length <= length ? value : value[..(length - 3)] + "...";

        private static string Escape(string value)
        {
            var safe = new string(value.Select(character =>
                character is >= ' ' and <= '~' ? character : '?').ToArray());
            return safe.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");
        }

        private static byte[] Bytes(string value) => Encoding.ASCII.GetBytes(value);

        private static byte[] Combine(params byte[][] blocks)
        {
            using var stream = new MemoryStream();
            foreach (var block in blocks) stream.Write(block);
            return stream.ToArray();
        }

        private static void Write(Stream stream, string value) => stream.Write(Bytes(value));

        private sealed record ReceiptLine(string Kind, string Text);
    }
}
