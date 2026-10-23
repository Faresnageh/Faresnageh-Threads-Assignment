using OrderProcessing.Shared.Logging;
using OrderProcessing.Shared.Models;
using OrderProcessing.Shared.Storage;

namespace OrderProcessing.Phase3
{
    public class EmailService
    {
        JsonFileStore jsonFileStore = new();

        public async Task SendAsync(EmailMessage email, CancellationToken ct)
        {
            Log.Info($"EmailService -> Sending email for Order #{email.OrderId}...");

            await Task.Delay(500, ct);

            email.Attempts++;

            if (email.OrderId == 3 && email.Attempts <= 2)
            {
                email.LastError = "Temporary email failure";
                throw new InvalidOperationException(email.LastError);
            }

            if (email.OrderId == 17)
            {
                email.LastError = "Email provider unavailable";
                throw new InvalidOperationException(email.LastError);
            }

            email.Id = Guid.NewGuid();
            email.SentAt = DateTime.Now;
            email.LastError = null;

            await jsonFileStore.UpdateAsync<EmailMessage>("emails.json", emails =>
            {
                emails.Add(email);
            }, ct);

            Log.Success($"EmailService -> Email sent for Order #{email.OrderId}");
        }


        public async Task AddToRetryAsync(EmailMessage email, CancellationToken ct)
        {
            await jsonFileStore.UpdateAsync<EmailMessage>("email_retry.json", emails =>
            {
                emails.Add(email);
            }, ct);

            Log.Info($"EmailService -> Order #{email.OrderId} added to email retry");
        }
    }
}