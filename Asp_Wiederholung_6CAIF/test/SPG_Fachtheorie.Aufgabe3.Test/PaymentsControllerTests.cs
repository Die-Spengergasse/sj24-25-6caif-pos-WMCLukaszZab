using Spg.Fachtheorie.Aufgabe3.API.Test;
using SPG_Fachtheorie.Aufgabe1.Model;
using SPG_Fachtheorie.Aufgabe3.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Xunit;

namespace SPG_Fachtheorie.Aufgabe3.Test
{
    public class PaymentsControllerTests
    {
        [Theory]
        [InlineData(1, null, 3)]
        [InlineData(null, "2024-05-13", 3)]
        [InlineData(1, "2024-05-13", 2)]
        public async Task GetAllPaymentsSuccessTest(
            int? cashDesk,
            string? dateFrom,
            int expectedCount)
        {
            var factory = new TestWebApplicationFactory();
            factory.InitializeDatabase(db =>
            {
                var cd1 = new CashDesk(1);
                var cd2 = new CashDesk(2);

                var cashier = new Cashier(
                    1,
                    "Max",
                    "Mustermann",
                    new DateOnly(1990, 1, 1),
                    2500m,
                    null,
                    "Lebensmittel");

                db.AddRange(cd1, cd2, cashier);

                db.Add(new Payment(cd1,
                    new DateTime(2024, 05, 12),
                    cashier,
                    PaymentType.Cash));
                db.Add(new Payment(cd2,
                    new DateTime(2024, 05, 13),
                    cashier,
                    PaymentType.Cash));
                db.Add(new Payment(cd1,
                    new DateTime(2024, 05, 14),
                    cashier,
                    PaymentType.Cash));

                db.SaveChanges();
            });

            var query = new List<string>();
            if (cashDesk.HasValue) query.Add($"cashDesk={cashDesk.Value}");
            if (dateFrom != null) query.Add($"dateFrom={dateFrom}");
            var url = "/api/payments" + (query.Any()
                ? "?" + string.Join("&", query)
                : "");

            var (statusCode, payments) =
                await factory.GetHttpContent<List<PaymentDto>>(url);

            Assert.Equal(HttpStatusCode.OK, statusCode);
            Assert.NotNull(payments);
            Assert.Equal(expectedCount, payments!.Count);

            if (cashDesk.HasValue)
                Assert.All(payments, p =>
                    Assert.Equal(cashDesk.Value, p.CashDeskNumber)
                );

            if (dateFrom != null)
            {
                var fromDate = DateTime.Parse(dateFrom).Date;
                Assert.All(payments, p =>
                    Assert.True(
                        p.PaymentDateTime.Date >= fromDate,
                        $"Payment {p.Id} hat Datum {p.PaymentDateTime.Date:d}, ist aber < {fromDate:d}"
                    )
                );
            }
        }
    }
}
