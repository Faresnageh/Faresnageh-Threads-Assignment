using OrderProcessing.Exceptions;
using OrderProcessing.Shared.Logging;
using OrderProcessing.Shared.Models;
using OrderProcessing.Shared.Storage;
using System.Collections.Concurrent;

namespace OrderProcessing.Phase3
{
    public class EmailService : IEmailService
    {
        JsonFileStore jsonFileStore = new();

        Dictionary<int, int> failAttempts;
        ConcurrentDictionary<int, int> attempts = new();
        ConcurrentDictionary<int, string> lastErrors = new();

        public EmailService(Dictionary<int, int>? failAttempts = null)
        {
            this.failAttempts = failAttempts ?? new();
        }

        public async Task SendConfirmationAsync(Order order, Customer customer, CancellationToken ct)
        {
            int attempt = attempts.AddOrUpdate(order.Id, 1, (id, current) => current + 1);

            Log.Info($"EmailService -> Rendering email for Order #{order.Id}...");
            await Task.Delay(200, ct);

            Log.Info($"EmailService -> Connecting to SMTP for Order #{order.Id}...");
            await Task.Delay(300, ct);

            Log.Info($"EmailService -> Sending email for Order #{order.Id}...");
            await Task.Delay(500, ct);

            if (failAttempts.TryGetValue(order.Id, out int failures) && attempt <= failures)
            {
                string error = "SMTP unavailable";

                lastErrors[order.Id] = error;

                throw new SmtpUnavailableException(error);
            }

            EmailMessage email = new()
            {
                Id = Guid.NewGuid(),
                OrderId = order.Id,
                To = customer.Email,
                Subject = $"Order #{order.Id} Confirmation",
                Body = $"Your order #{order.Id} has been completed. Total: {order.TotalAmount:F2}",
                SentAt = DateTime.Now,
                Attempts = attempt,
                LastError = null
            };

            await jsonFileStore.UpdateAsync<EmailMessage>("emails.json", emails =>
            {
                emails.Add(email);
            }, ct);

            Log.Success($"EmailService -> Email sent for Order #{order.Id} on attempt {attempt}");
        }

        public async Task AddToRetryAsync(Order order, Customer customer, CancellationToken ct)
        {
            attempts.TryGetValue(order.Id, out int attempt);
            lastErrors.TryGetValue(order.Id, out string? lastError);

            EmailMessage email = new()
            {
                Id = Guid.NewGuid(),
                OrderId = order.Id,
                To = customer.Email,
                Subject = $"Order #{order.Id} Confirmation",
                Body = $"Your order #{order.Id} has been completed. Total: {order.TotalAmount:F2}",
                SentAt = DateTime.Now,
                Attempts = attempt,
                LastError = lastError
            };

            await jsonFileStore.UpdateAsync<EmailMessage>("email_retry.json", emails =>
            {
                emails.Add(email);
            }, ct);

            Log.Info($"Email for Order #{order.Id} queued for retry later");
        }
    }
}