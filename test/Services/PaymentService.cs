using Microsoft.Extensions.Options;
using Stripe;
using Stripe.Checkout;

namespace test.Services;

public class PaymentService
{
    private readonly StripeOptions _options;

    public PaymentService(IOptions<StripeOptions> options)
    {
        _options = options.Value;
        StripeConfiguration.ApiKey = _options.SecretKey;
    }

    public async Task<string> CreateCheckoutSessionAsync(List<SessionLineItemOptions> lineItems, string successUrl, string cancelUrl)
    {
        var options = new SessionCreateOptions
        {
            PaymentMethodTypes = new List<string> { "card" },
            LineItems = lineItems,
            Mode = "payment",
            SuccessUrl = successUrl,
            CancelUrl = cancelUrl,
        };

        var service = new SessionService();
        var session = await service.CreateAsync(options);

        return session.Url;
    }
}