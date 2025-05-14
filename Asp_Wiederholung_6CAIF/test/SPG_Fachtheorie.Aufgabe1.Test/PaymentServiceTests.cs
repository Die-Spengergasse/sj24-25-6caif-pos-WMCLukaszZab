using Microsoft.EntityFrameworkCore;
using SPG_Fachtheorie.Aufgabe1.Commands;
using SPG_Fachtheorie.Aufgabe1.Infrastructure;
using SPG_Fachtheorie.Aufgabe1.Model;
using SPG_Fachtheorie.Aufgabe1.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace SPG_Fachtheorie.Aufgabe1.Test
{
    public class PaymentServiceTests
    {
        private AppointmentContext GetEmptyDbContext()
        {
            var options = new DbContextOptionsBuilder()
                .UseSqlite(@"Data Source=cash.db")
                .Options;

            var db = new AppointmentContext(options);
            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();
            return db;
        }
        /// <summary>
        /// Ausgänge:
        ///     1. "Invalid cashdesk"
        ///     2. "Invalid employee"
        ///     3. "Open payment for cashdesk."
        ///     4. "Insufficient rights to create a credit card payment."
        /// </summary>
        [Theory]
        [InlineData(4, "Cash", 1, "Invalid cashdesk")]
        [InlineData(2, "Cash", 3, "Invalid employee")]
        [InlineData(1, "Cash", 1, "Open payment for cashdesk.")]
        [InlineData(3, "CreditCard", 1, "Insufficient rights to create a credit card payment.")]
        public void CreatePaymentThorwsPaymentServiceExceptionTest(
            int cashDeskNumber, string paymentType, int employeeRegistrationNumber, string errorMessage)
        {
            using var db = GetEmptyDbContext();
            var service = new PaymentService(db);
            // Arrange
            var cashdesk1 = new CashDesk(1);  // Kassa mit offenem payment
            var cashdesk2 = new CashDesk(2);  // Kassa mit confirmed payment
            var cashdesk3 = new CashDesk(3);  // Kassa mit confirmed payment
            var cashier = new Cashier(1, "FN", "LN", new DateOnly(2002, 2, 1), 3000M, null, "Wurst");
            // Confirmed ist null, das Payment, das hier angelegt wird, ist also noch nicht "confirmed".
            var payment1 = new Payment(
                cashdesk1, new DateTime(2025, 5, 7, 12, 0, 0),
                cashier, PaymentType.Cash);

            // Dieses Payment ist OK
            var payment2 = new Payment(
                cashdesk2, new DateTime(2025, 5, 7, 12, 0, 0),
                cashier, PaymentType.Cash)
            { Confirmed = new DateTime(2025, 5, 7, 11, 0, 0) };

            db.AddRange(payment1, payment2, cashdesk3);
            db.SaveChanges();

            // ACT
            var cmd = new NewPaymentCommand(cashDeskNumber, paymentType, employeeRegistrationNumber);
            var e = Assert.Throws<PaymentServiceException>(() => service.CreatePayment(cmd));
            Assert.True(e.Message == errorMessage);
        }
        [Fact]
        public void CreatePaymentSuccessTest()
        {
            using var db = GetEmptyDbContext();
            var service = new PaymentService(db);
            var cashdesk = new CashDesk(1);
            var cashier = new Cashier(1, "FN", "LN", new DateOnly(2002, 2, 1), 3000M, null, "Wurst");
            db.Cashiers.Add(cashier);
            db.CashDesks.Add(cashdesk);
            db.SaveChanges();

            // ACT
            var cmd = new NewPaymentCommand(1, "Cash", 1);
            db.ChangeTracker.Clear();
            service.CreatePayment(cmd);

            // ASSERT
            var paymentFromDb = db.Payments.First();
            Assert.True(paymentFromDb.Id != 0);
        }
    }
}
