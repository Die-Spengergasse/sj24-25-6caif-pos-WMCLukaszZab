using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SPG_Fachtheorie.Aufgabe1.Commands
{
    // Sie soll die Felder ArticleName (string), Amount (int), Price (decimal) und PaymentId (int) besitzen.
    public record NewPaymentItemCommand(
        [StringLength(255, MinimumLength = 1)]
        string ArticleName,
        [Range(1, int.MaxValue)]
        int Amount,
        [Range(0, 1_000_000)]
        decimal Price,
        [Range(1, int.MaxValue)]
        int PaymentId);
}
