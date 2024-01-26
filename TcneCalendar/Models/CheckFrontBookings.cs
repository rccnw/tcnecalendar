using System.Text.Json.Serialization;

namespace TcneCalendar.Models
{
    public class CheckFrontBookings
    {
    }


    public class Root
    {
        [JsonPropertyName("version")]
        public string Version { get; set; }

        [JsonPropertyName("account_id")]
        public int AccountId { get; set; }

        [JsonPropertyName("host_id")]
        public string HostId { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("locale")]
        public Locale Locale { get; set; }

        [JsonPropertyName("request")]
        public Request Request { get; set; }

        [JsonPropertyName("booking/index")]
        public Dictionary<string, Booking> BookingIndex { get; set; }
    }

    public class Locale
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("lang")]
        public string Lang { get; set; }

        [JsonPropertyName("currency")]
        public string Currency { get; set; }
    }

    public class Request
    {
        [JsonPropertyName("path")]
        public List<string> Path { get; set; }

        [JsonPropertyName("resource")]
        public string Resource { get; set; }

        [JsonPropertyName("records")]
        public int Records { get; set; }

        [JsonPropertyName("total_records")]
        public int TotalRecords { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; }

        [JsonPropertyName("limit")]
        public int Limit { get; set; }

        [JsonPropertyName("page")]
        public int Page { get; set; }

        [JsonPropertyName("pages")]
        public int Pages { get; set; }

        [JsonPropertyName("time")]
        public double Time { get; set; }

        [JsonPropertyName("timestamp")]
        public double Timestamp { get; set; }

        [JsonPropertyName("method")]
        public string Method { get; set; }
    }

    public class Booking
    {
        [JsonPropertyName("booking_id")]
        public int BookingId { get; set; }

        [JsonPropertyName("code")]
        public string Code { get; set; }

        [JsonPropertyName("status_id")]
        public string StatusId { get; set; }

        [JsonPropertyName("status_name")]
        public string StatusName { get; set; }

        [JsonPropertyName("created_date")]
        public long CreatedDate { get; set; }

        [JsonPropertyName("total")]
        public string Total { get; set; }

        [JsonPropertyName("tax_total")]
        public string TaxTotal { get; set; }

        [JsonPropertyName("paid_total")]
        public string PaidTotal { get; set; }

        [JsonPropertyName("customer_id")]
        public int CustomerId { get; set; }

        [JsonPropertyName("customer_name")]
        public string CustomerName { get; set; }

        [JsonPropertyName("customer_email")]
        public string CustomerEmail { get; set; }

        [JsonPropertyName("summary")]
        public string Summary { get; set; }

        [JsonPropertyName("date_desc")]
        public string DateDescription { get; set; }

        [JsonPropertyName("tid")]
        public string Tid { get; set; }
        [JsonPropertyName("token")]
        public string Token { get; set; }


        //  These properties are not part of the CheckFront BookingDetail API payload, they come from booking/booking_id API,
        //  and are copied here for convenience from the detail data fetched for each booking.
        public long StartDate { get; set; }

        public long EndDate { get; set; }

        public int Studio { get; set; }

    }

}
