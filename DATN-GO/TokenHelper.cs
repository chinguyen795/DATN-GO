namespace DATN_GO
{
    public static class TokenHelper
    {
        public static string GetToken(this IHttpContextAccessor context)
        {
            var token = context.HttpContext.Request.Headers["Authorization"].ToString();
            if (string.IsNullOrEmpty(token))
                token = context.HttpContext.Session.GetString("JwtToken");

            if (token == null) return string.Empty;
            // Remove "Bearer " prefix
            if (token.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                return token.Substring("Bearer ".Length).Trim();
            }
            return token;
        }

        public static string FormatTime(this string inputTime)
        {
            DateTime time;
            if (DateTime.TryParseExact(inputTime, "H:m", null, System.Globalization.DateTimeStyles.None, out time))
                return time.ToString("HH:mm");

            return inputTime;
        }
    }
}
