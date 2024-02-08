using System.Text.Json.Serialization;

namespace TcneShared.Models
{
    public class Root
    {
        public Root()
        {
            Version = string.Empty;
            AccountId = 0;
            HostId = string.Empty;
            Name = string.Empty;
            Locale = new Locale();
            Request = new Request();
            BookingIndex = new Dictionary<string, Booking>();
        }

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
        public Locale()
        {
            Id = string.Empty;
            Lang = string.Empty;
            Currency = string.Empty;
        }

        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("lang")]
        public string Lang { get; set; }

        [JsonPropertyName("currency")]
        public string Currency { get; set; }
    }

    public class Request
    {
        public Request()
        {
            Path = new List<string>();
            Resource = string.Empty;
            Records = 0;
            TotalRecords = 0;
            Status = string.Empty;
            Limit = 0;
            Page = 0;
            Pages = 0;
            Time = 0.0;
            Timestamp = 0.0;
            Method = string.Empty;
        }

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
        public Booking()
        {
            BookingId = 0;
            Code = string.Empty;
            StatusId = string.Empty;
            StatusName = string.Empty;
            CreatedDate = 0;
            Total = string.Empty;
            TaxTotal = string.Empty;
            PaidTotal = string.Empty;
            CustomerId = 0;
            CustomerName = string.Empty;
            CustomerEmail = string.Empty;
            Summary = string.Empty;
            DateDescription = string.Empty;
            Tid = string.Empty;
            Token = string.Empty;
            StartDate = 0;
            EndDate = 0;
            Studio = 0;
        }

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

        public Booking(Booking booking)
        {
            BookingId = booking.BookingId;
            Code = booking.Code;
            StatusId = booking.StatusId;
            StatusName = booking.StatusName;
            CreatedDate = booking.CreatedDate;
            Total = booking.Total;
            TaxTotal = booking.TaxTotal;
            PaidTotal = booking.PaidTotal;
            CustomerId = booking.CustomerId;
            CustomerName = booking.CustomerName;
            CustomerEmail = booking.CustomerEmail;
            Summary = booking.Summary;
            DateDescription = booking.DateDescription;
            Tid = booking.Tid;
            Token = booking.Token;
            StartDate = booking.StartDate;
            EndDate = booking.EndDate;
            Studio = booking.Studio;
        }
    }
}
