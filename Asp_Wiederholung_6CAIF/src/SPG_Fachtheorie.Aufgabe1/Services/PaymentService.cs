using System;
using System.Linq;
using System.Collections.Generic;
using SPG_Fachtheorie.Aufgabe1.Commands;
using SPG_Fachtheorie.Aufgabe1.Model;
using SPG_Fachtheorie.Aufgabe1.Infrastructure;

public class PaymentServiceException : Exception
{
    public PaymentServiceException(string message) : base(message) { }
}

public class PaymentService
{
    private readonly AppointmentContext _context;

    public IQueryable<Payment> Payments => _context.Payments;

    public PaymentService(AppointmentContext context)
    {
        _context = context;
    }

    public Payment CreatePayment(NewPaymentCommand cmd)
    {
        var existingPayment = _context.Payments
            .FirstOrDefault(p => p.CashDesk.Number == cmd.CashDeskNumber && p.Confirmed == null);

        if (existingPayment != null)
            throw new PaymentServiceException("Open payment for cashdesk.");

        var employee = _context.Employees.Find(cmd.EmployeeRegistrationNumber);
        if (cmd.PaymentType == PaymentType.CreditCard && employee?.Type != "Manager")
            throw new PaymentServiceException("Insufficient rights to create a credit card payment.");

        var payment = new Payment
        {
            CashDesk = _context.CashDesks.Find(cmd.CashDeskNumber),
            Employee = employee,
            PaymentType = cmd.PaymentType
            PaymentDateTime = DateTime.UtcNow,
            Confirmed = null
        };

        _context.Payments.Add(payment);
        _context.SaveChanges();
        return payment;
    }

    public void ConfirmPayment(int paymentId)
    {
        var payment = _context.Payments.Find(paymentId);
        if (payment == null)
            throw new PaymentServiceException("Payment not found.");

        if (payment.Confirmed != null)
            throw new PaymentServiceException("Payment already confirmed.");

        payment.Confirmed = DateTime.UtcNow;
        _context.SaveChanges();
    }

    public void AddPaymentItem(NewPaymentItemCommand cmd)
    {
        var payment = _context.Payments.Find(cmd.PaymentId);
        if (payment == null)
            throw new PaymentServiceException("Payment not found.");

        if (payment.Confirmed != null)
            throw new PaymentServiceException("Payment already confirmed.");

        var paymentItem = new PaymentItem
        {
            ArticleName = cmd.ArticleName,
            Amount = cmd.Amount,
            Price = cmd.Price,
            Payment = payment
        };

        _context.PaymentItems.Add(paymentItem);
        _context.SaveChanges();
    }

    public void DeletePayment(int paymentId, bool deleteItems)
    {
        var payment = _context.Payments.Find(paymentId);
        if (payment == null)
            throw new PaymentServiceException("Payment not found.");

        if (deleteItems)
        {
            var items = _context.PaymentItems.Where(p => p.Payment.Id == paymentId).ToList();
            _context.PaymentItems.RemoveRange(items);
        }

        _context.Payments.Remove(payment);
        _context.SaveChanges();
    }
}
