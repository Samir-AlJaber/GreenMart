# GreenMart payment setup

GreenMart uses the SSLCOMMERZ hosted payment page. Cash on Delivery remains available without gateway configuration. Online payments are verified on the server before an order becomes visible to sellers.

## First-time database setup

Open SQL Server Management Studio, select `GreenMartDB`, open `database/PaymentFeature.sql`, and execute the complete script once. The script is idempotent, so teammates can safely execute it even if the payment columns already exist.

## Local sandbox configuration

The application reads gateway settings from `application/appsettings.Payment.json`. This file is intentionally ignored by Git so live credentials are never committed.

For local sandbox testing, copy `application/appsettings.Payment.example.json` to `application/appsettings.Payment.json`. The example uses SSLCOMMERZ's public `testbox` / `qwerty` test credentials and must never be used as a live merchant account.

Each teammate needs their own ignored `appsettings.Payment.json`. For a real merchant account, replace the two credential values with the sandbox or live credentials provided by SSLCOMMERZ. Keep `Sandbox` set to `true` during development.

## IPN during local development

The browser success callback can return to localhost, but the SSLCOMMERZ server cannot send IPN messages to a private localhost address. For complete IPN testing, expose the HTTPS application through a secure development tunnel and configure this public URL in the SSLCOMMERZ sandbox merchant panel:

`https://your-public-test-host/Payment/Ipn`

In production, the application must be publicly reachable over HTTPS on port 443. Set the production IPN URL in the merchant panel, use your live credentials, and change `Sandbox` to `false`.

## Payment behavior

- The payable amount is recalculated from the database, never accepted from the browser.
- Product stock is reserved while an online payment is pending.
- A verified payment changes the order to `Pending`, removes the purchased cart items, and allows the seller workflow to begin.
- A failed or cancelled payment releases reserved stock and keeps the cart available.
- Repeated success/IPN callbacks are handled without charging or completing an order twice.
- The callback is accepted only when SSLCOMMERZ validates the transaction ID, amount, currency, and safe risk level.

## Seller payout workflow

Run `database/SellerPayoutFeature.sql` once in `GreenMartDB`. Sellers can then add a bKash or bank destination from **Seller Wallet**. An admin must verify a new or changed destination before it can receive payouts.

When a delivery assignment becomes `Delivered`, GreenMart automatically creates an earning for that assignment's seller. The default release delay is 24 hours. Online payments enter the release period immediately; Cash on Delivery earnings first wait for the admin to confirm that GreenMart received the collected cash.

The application automatically builds the payout queue, but it does not claim that real money was transferred without a provider response. Until GreenMart receives approved bKash B2C or bank-disbursement API credentials, the admin must make the transfer and record its real transaction reference in **Admin > Seller payouts**. Duplicate transaction references are rejected.

Payout timing is configured in the ignored `application/appsettings.Payout.json`. Teammates can copy `application/appsettings.Payout.example.json`. Never commit live payout credentials.
