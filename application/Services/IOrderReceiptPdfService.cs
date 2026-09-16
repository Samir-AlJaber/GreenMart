using GreenMart.Models;

namespace GreenMart.Services
{
    public interface IOrderReceiptPdfService
    {
        byte[] Create(Order order, IReadOnlyCollection<DeliveryAssignment> assignments);
    }
}
