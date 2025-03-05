using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SPG_Fachtheorie.Aufgabe1.Infrastructure;
using SPG_Fachtheorie.Aufgabe1.Model;
using SPG_Fachtheorie.Aufgabe3.Dtos;

namespace SPG_Fachtheorie.Aufgabe3.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PaymentsController : ControllerBase
    {
        private readonly AppointmentContext _db;

        public PaymentsController(AppointmentContext db)
        {
            _db = db;
        }
        [HttpGet("{id}")]
        public ActionResult<PaymentDetailDto> GetPayment(int id)
        {
            var payment = _db.Payments
                .Where(p => p.Id == id)
                .Select(p => new PaymentDetailDto(
                p.Id,
                p.Employee.FirstName,
                p.Employee.LastName,
                p.CashDesk.Number,
                p.PaymentType.ToString(),
                p.PaymentItems.Select(pi => new PaymentItemDto(
                    pi.ArticleName,
                    pi.Amount,
                    pi.Price))
                .ToList()
                ))
                .FirstOrDefault();
            if (payment == null)
            {
                return NotFound();
            }
            return Ok(payment);
        }
        [HttpGet]
       public ActionResult<List<PaymentDto>> GetPayments(
    [FromQuery] int? cashDesk,
    [FromQuery] DateTime? dateFrom
)
        {
            IQueryable<Payment> query = _db.Payments;

            if (cashDesk.HasValue)
            {
                query = query.Where(p => p.CashDesk.Number == cashDesk.Value);
            }

            if (dateFrom.HasValue)
            {
                query = query.Where(p => p.PaymentDateTime >= dateFrom.Value);
            }

            var payments = query
                .Select(p => new PaymentDto(
                    p.Id,
                    p.Employee.FirstName,
                    p.Employee.LastName,
                    p.CashDesk.Number,
                    p.PaymentType.ToString(),
                    (int)p.PaymentItems.Sum(pi => pi.Price * pi.Amount)
                ))
                .ToList();

            return Ok(payments);
        }

    }
}
