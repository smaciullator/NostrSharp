namespace NostrSharp.Nostr.Enums.Marketplace
{
    public enum PaymentType
    {
        /// <summary>
        /// URL to a payment page, stripe, paypal, btcpayserver, etc
        /// </summary>
        url,
        /// <summary>
        /// onchain bitcoin address
        /// </summary>
        btc,
        /// <summary>
        /// bitcoin lightning invoice
        /// </summary>
        ln,
        lnurl
    }
}
