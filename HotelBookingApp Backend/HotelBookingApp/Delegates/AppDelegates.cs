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
        /// Standard refund policy:
        ///   >= 24 hours before check-in → 80% refund
        ///   <  24 hours before check-in → 0% refund
        /// </summary>
        public static readonly RefundCalculatorDelegate StandardRefundPolicy =
            (totalAmount, hoursUntilCheckIn) =>
                hoursUntilCheckIn >= 24 ? Math.Round(totalAmount * 0.8m, 2) : 0m;

        /// <summary>
        /// Returns a human-readable refund breakdown string for a given amount and hours.
        /// Used by ChatService to explain refund to users.
        /// </summary>
        public static string DescribeRefund(decimal totalAmount, double hoursUntilCheckIn)
        {
            if (hoursUntilCheckIn >= 24)
            {
                var refund = Math.Round(totalAmount * 0.8m, 2);
                return $"80% refund = ₹{refund:N2} (cancelled {hoursUntilCheckIn:F0}h before check-in)";
            }
            return $"No refund (cancelled less than 24h before check-in)";
        }

        /// <summary>Resolves payment status based on method and amounts.</summary>
        public static readonly PaymentStatusResolverDelegate DefaultPaymentStatusResolver =
            (method, amount, bookingAmount) =>
            {
                if (amount < bookingAmount) return "Failed";
                return method switch
                {
                    "CreditCard" or "DebitCard" => "Completed",
                    "UPI" or "Wallet" or "PayPal" or "NetBanking" => "Pending",
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
