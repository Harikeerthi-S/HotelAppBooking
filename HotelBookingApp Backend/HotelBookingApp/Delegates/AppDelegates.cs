namespace HotelBookingApp.Delegates
{
    // ── Validation Delegates ──────────────────────────────────────────────
    /// <summary>Validates any single string value — returns error message or null if valid.</summary>
    //public delegate string? StringValidatorDelegate(string value);

    /// <summary>Validates a decimal value — returns error message or null if valid.</summary>
    public delegate string? DecimalValidatorDelegate(decimal value);

    /// <summary>Validates a date range — returns error message or null if valid.</summary>
    public delegate string? DateRangeValidatorDelegate(DateTime checkIn, DateTime checkOut);

    // ── Mapping / Transform Delegates ────────────────────────────────────
    /// <summary>Computes refund amount given total booking amount and hours until check-in.</summary>
    public delegate decimal RefundCalculatorDelegate(decimal totalAmount, double hoursUntilCheckIn);

    /// <summary>Determines payment status string from method and amount.</summary>
    public delegate string PaymentStatusResolverDelegate(string paymentMethod, decimal amount, decimal bookingAmount);

    /// <summary>Formats a log message string given level and details.</summary>
    public delegate string LogFormatterDelegate(string level, string context, string message);

    // ── Pre-built delegate factory methods ────────────────────────────────
    public static class AppDelegateFactory
    {
        /// <summary>
        /// Standard refund policy based on days before check-in:
        ///   ≥ 5 days → 100% full refund
        ///   3–5 days → 50% refund
        ///   1–3 days → 25% refund
        ///   ≤ 1 day  → 100% full refund
        /// </summary>
        public static readonly RefundCalculatorDelegate StandardRefundPolicy =
            (totalAmount, hoursUntilCheckIn) =>
            {
                var days = hoursUntilCheckIn / 24.0;
                if (days >= 5) return Math.Round(totalAmount * 1.00m, 2); // 5+ days  → 100%
                if (days > 3)  return Math.Round(totalAmount * 0.50m, 2); // 3–5 days → 50%
                if (days > 1)  return Math.Round(totalAmount * 0.25m, 2); // 1–3 days → 25%
                return Math.Round(totalAmount * 1.00m, 2);                // ≤ 1 day  → 100%
            };

        /// <summary>
        /// Returns a human-readable refund breakdown string for a given amount and hours.
        /// Used by ChatService to explain refund to users.
        /// </summary>
        public static string DescribeRefund(decimal totalAmount, double hoursUntilCheckIn)
        {
            var days = hoursUntilCheckIn / 24.0;
            if (days >= 5)
            {
                var refund = Math.Round(totalAmount * 1.00m, 2);
                return $"100% refund = ₹{refund:N2} (cancelled 5 or more days before check-in)";
            }
            if (days > 3)
            {
                var refund = Math.Round(totalAmount * 0.50m, 2);
                return $"50% refund = ₹{refund:N2} (cancelled 3–5 days before check-in)";
            }
            if (days > 1)
            {
                var refund = Math.Round(totalAmount * 0.25m, 2);
                return $"25% refund = ₹{refund:N2} (cancelled 1–3 days before check-in)";
            }
            var full = Math.Round(totalAmount * 1.00m, 2);
            return $"100% refund = ₹{full:N2} (cancelled within 1 day of check-in)";
        }

        /// <summary>Resolves payment status based on method and amounts.</summary>
        public static readonly PaymentStatusResolverDelegate DefaultPaymentStatusResolver =
            (method, amount, bookingAmount) =>
            {
                if (amount < bookingAmount) return "Failed";
                return method switch
                {
                    "CreditCard" or "DebitCard" or "UPI" or "Wallet" or "PayPal" or "NetBanking" => "Completed",
                    _ => "Failed"
                };
            };

        /// <summary>Validates that a check-out date is after check-in and check-in is not in the past.</summary>
        public static readonly DateRangeValidatorDelegate StrictDateRangeValidator =
            (checkIn, checkOut) =>
            {
                if (checkIn < DateTime.UtcNow.Date)
                    return "Check-in date cannot be in the past.";
                if (checkOut <= checkIn)
                    return "Check-out date must be after check-in date.";
                return null;
            };

        /// <summary>Log formatter — produces structured single-line log output.</summary>
        public static readonly LogFormatterDelegate StandardLogFormatter =
            (level, context, message) =>
                $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] [{level.ToUpper()}] [{context}] {message}";
    }
}
