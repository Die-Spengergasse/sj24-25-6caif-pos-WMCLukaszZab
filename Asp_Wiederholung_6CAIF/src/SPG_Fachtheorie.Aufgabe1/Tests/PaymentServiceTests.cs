using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using SPG_Fachtheorie.Aufgabe1.Infrastructure;
using SPG_Fachtheorie.Aufgabe1.Model;
using SPG_Fachtheorie.Aufgabe1.Commands;
using Xunit;


namespace SPG_Fachtheorie.Aufgabe1.Commands
{

    public record NewPaymentItemCommand(
        string ArticleName,
        int Amount,
        decimal Price,
        int PaymentId);
}


namespace SPG_Fachtheorie.Aufgabe1.Test
{

    [Collection("Sequential")]
    public class PaymentServiceTests
    {

        private AppointmentContext GetEmptyDbContext()
        {
            var options = new DbContextOptionsBuilder()
                .UseSqlite("Data Source=cash_test.db")
                .Options;

            var db = new AppointmentContext(options);
            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();
            return db;
        }


        public static IEnumerable<object[]> CreatePaymentExceptionData =>
            new[]
            {
                new object[]
                {
                    (Action<AppointmentContext>)(db =>
                    {
                        var cashDesk  = new CashDesk(1);
                        var cashier   = new Cashier(1001,"Max","Muster",
                                                     new DateOnly(1990,1,1),3000,null,"Kassier");
                        var payment   = new Payment(cashDesk, DateTime.UtcNow,
                                                     cashier, PaymentType.Cash);

                        db.CashDesks.Add(cashDesk);
                        db.Cashiers.Add(cashier);
                        db.Payments.Add(payment);
                    }),
                    new NewPaymentCommand(1, DateTime.UtcNow,
                                          PaymentType.Cash.ToString(), 1001),
                    "Open payment for cashdesk."
                },

                new object[]
                {
                    (Action<AppointmentContext>)(db =>
                    {
                        var cashDesk = new CashDesk(2);
                        var cashier  = new Cashier(1002,"Julia","Doe",
                                                    new DateOnly(1995,5,5),2800,null,"Kassier");

                        db.CashDesks.Add(cashDesk);
                        db.Cashiers.Add(cashier);
                    }),
                    new NewPaymentCommand(2, DateTime.UtcNow,
                                          PaymentType.CreditCard.ToString(), 1002),
                    "Insufficient rights to create a credit card payment."
                }
            };

        [Theory]
        [MemberData(nameof(CreatePaymentExceptionData))]
        public void CreatePaymentExceptionsTest(
            Action<AppointmentContext> arrange,
            NewPaymentCommand cmd,
            string expectedMessage)
        {
            using var db = GetEmptyDbContext();
            arrange(db);
            db.SaveChanges();

            var service = new PaymentService(db);

            var ex = Assert.Throws<PaymentServiceException>(() => service.CreatePayment(cmd));
            Assert.Equal(expectedMessage, ex.Message);
        }

        [Fact]
        public void CreatePaymentSuccessTest()
        {
            using var db = GetEmptyDbContext();

            var cashDesk = new CashDesk(3);
            var manager = new Manager(2001, "Anna", "Huber",
                                       new DateOnly(1982, 2, 2), 5000, null, "Audi A6");

            db.CashDesks.Add(cashDesk);
            db.Managers.Add(manager);
            db.SaveChanges();

            var cmd = new NewPaymentCommand(3, DateTime.UtcNow,
                                            PaymentType.Cash.ToString(), 2001);

            var service = new PaymentService(db);
            var payment = service.CreatePayment(cmd);

            db.ChangeTracker.Clear();
            Assert.Equal(1, db.Payments.Count());
            Assert.NotEqual(0, payment.Id);
        }

        [Fact]
        public void ConfirmPayment_NotFound_Throws()
        {
            using var db = GetEmptyDbContext();
            var service = new PaymentService(db);

            var ex = Assert.Throws<PaymentServiceException>(() => service.ConfirmPayment(999));
            Assert.Equal("Payment not found.", ex.Message);
        }

        [Fact]
        public void ConfirmPayment_AlreadyConfirmed_Throws()
        {
            using var db = GetEmptyDbContext();

            var cashDesk = new CashDesk(4);
            var manager = new Manager(3001, "Tom", "Tester",
                                       new DateOnly(1980, 3, 3), 5000, null, "Tesla");
            var payment = new Payment(cashDesk, DateTime.UtcNow, manager, PaymentType.Cash)
            {
                Confirmed = DateTime.UtcNow
            };

            db.AddRange(cashDesk, manager, payment);
            db.SaveChanges();

            var service = new PaymentService(db);

            var ex = Assert.Throws<PaymentServiceException>(() => service.ConfirmPayment(payment.Id));
            Assert.Equal("Payment already confirmed.", ex.Message);
        }

        [Fact]
        public void ConfirmPayment_Success()
        {
            using var db = GetEmptyDbContext();

            var cashDesk = new CashDesk(5);
            var manager = new Manager(4001, "Lisa", "Lang",
                                       new DateOnly(1985, 4, 4), 5200, null, "VW ID.7");
            var payment = new Payment(cashDesk, DateTime.UtcNow, manager, PaymentType.Cash);

            db.AddRange(cashDesk, manager, payment);
            db.SaveChanges();

            var service = new PaymentService(db);
            service.ConfirmPayment(payment.Id);

            db.ChangeTracker.Clear();
            var confirm = db.Payments.Single();
            Assert.NotNull(confirm.Confirmed);
        }

        [Fact]
        public void AddPaymentItem_PaymentNotFound_Throws()
        {
            using var db = GetEmptyDbContext();
            var service = new PaymentService(db);
            var cmd = new NewPaymentItemCommand("Cola", 1, 2.5m, 42);

            var ex = Assert.Throws<PaymentServiceException>(() => service.AddPaymentItem(cmd));
            Assert.Equal("Payment not found.", ex.Message);
        }

        [Fact]
        public void AddPaymentItem_AlreadyConfirmed_Throws()
        {
            using var db = GetEmptyDbContext();

            var cashDesk = new CashDesk(6);
            var manager = new Manager(5001, "Peter", "Parker",
                                       new DateOnly(1978, 7, 7), 5100, null, "BMW X5");
            var payment = new Payment(cashDesk, DateTime.UtcNow, manager, PaymentType.Cash)
            {
                Confirmed = DateTime.UtcNow
            };

            db.AddRange(cashDesk, manager, payment);
            db.SaveChanges();

            var cmd = new NewPaymentItemCommand("Cola", 1, 2.5m, payment.Id);
            var service = new PaymentService(db);

            var ex = Assert.Throws<PaymentServiceException>(() => service.AddPaymentItem(cmd));
            Assert.Equal("Payment already confirmed.", ex.Message);
        }

        [Fact]
        public void AddPaymentItem_Success()
        {
            using var db = GetEmptyDbContext();

            var cashDesk = new CashDesk(7);
            var manager = new Manager(6001, "Clara", "Cool",
                                       new DateOnly(1992, 9, 9), 4800, null, "Mercedes");
            var payment = new Payment(cashDesk, DateTime.UtcNow, manager, PaymentType.Cash);

            db.AddRange(cashDesk, manager, payment);
            db.SaveChanges();

            var cmd = new NewPaymentItemCommand("Cola", 2, 2.5m, payment.Id);
            var service = new PaymentService(db);

            service.AddPaymentItem(cmd);

            db.ChangeTracker.Clear();
            Assert.Single(db.PaymentItems);
        }

        [Fact]
        public void DeletePayment_NotFound_Throws()
        {
            using var db = GetEmptyDbContext();
            var service = new PaymentService(db);

            var ex = Assert.Throws<PaymentServiceException>(() => service.DeletePayment(123, true));
            Assert.Equal("Payment not found.", ex.Message);
        }

        [Fact]
        public void DeletePayment_WithItems_Success()
        {
            using var db = GetEmptyDbContext();

            var cashDesk = new CashDesk(8);
            var manager = new Manager(7001, "Mario", "Muster",
                                       new DateOnly(1975, 5, 5), 5500, null, "Volvo XC90");
            var payment = new Payment(cashDesk, DateTime.UtcNow, manager, PaymentType.Cash);
            var item = new PaymentItem("Hot-Dog", 1, 3.0m, payment);

            db.AddRange(cashDesk, manager, payment, item);
            db.SaveChanges();

            var service = new PaymentService(db);
            service.DeletePayment(payment.Id, deleteItems: true);

            db.ChangeTracker.Clear();
            Assert.False(db.Payments.Any());
            Assert.False(db.PaymentItems.Any());
        }
    }
}
