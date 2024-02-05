using System.Text.Json.Serialization;

namespace TcneShared.WebHook
{

    public class Attributes
    {
        public Attributes()
        {
            Version = string.Empty;
            Host = string.Empty;
        }

        [JsonPropertyName("version")]
        public string Version { get; set; }

        [JsonPropertyName("host")]
        public string Host { get; set; }
    }


    public class Customer
    {
        public Customer()
        {
            Code = string.Empty;
            Name = string.Empty;
            Email = string.Empty;
            Region = string.Empty;
            Address = string.Empty;
            City = string.Empty;
            Country = string.Empty;
            Phone = string.Empty;
            PostalZip = string.Empty;
        }

        [JsonPropertyName("code")]
        public string Code { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("email")]
        public string Email { get; set; }

        [JsonPropertyName("region")]
        public string Region { get; set; }

        [JsonPropertyName("address")]
        public string Address { get; set; }

        [JsonPropertyName("city")]
        public string City { get; set; }

        [JsonPropertyName("country")]
        public string Country { get; set; }

        [JsonPropertyName("phone")]
        public string Phone { get; set; }

        [JsonPropertyName("postal_zip")]
        public string PostalZip { get; set; }
    }


    public class Tax
    {
        public Tax()
        {
            TaxId = string.Empty;
            Name = string.Empty;
            Amount = string.Empty;
        }

        public string TaxId { get; set; }
        public string Name { get; set; }
        public string Amount { get; set; }
    }


    public class Item
    {
        public Item()
        {
            LineId = string.Empty;
            ItemId = string.Empty;
            StartDate = string.Empty;
            EndDate = string.Empty;
            Sku = string.Empty;
            Slip = new object();
            PackageId = string.Empty;
            Status = string.Empty;
            Total = string.Empty;
            TaxTotal = string.Empty;
            Qty = string.Empty;
        }

        public string LineId { get; set; }
        public string ItemId { get; set; }

        [JsonPropertyName("start_date")]
        public string StartDate { get; set; }

        [JsonPropertyName("end_date")]
        public string EndDate { get; set; }
        public string Sku { get; set; }
        public object Slip { get; set; }

        [JsonPropertyName("package_id")]
        public string PackageId { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; }
        public string Total { get; set; }
        public string TaxTotal { get; set; }
        public string Qty { get; set; }
    }


    public class Order
    {
        public Order()
        {
            Attributes = new Attributes();
            SubTotal = string.Empty;
            TaxTotal = string.Empty;
            PaidTotal = string.Empty;
            Total = string.Empty;
            Taxes = new Tax();
            Items = new List<Item>();
        }

        [JsonPropertyName("@attributes")]
        public Attributes Attributes { get; set; }

        [JsonPropertyName("sub_total")]
        public string SubTotal { get; set; }

        [JsonPropertyName("tax_total")]
        public string TaxTotal { get; set; }

        [JsonPropertyName("paid_total")]
        public string PaidTotal { get; set; }

        [JsonPropertyName("total")]
        public string Total { get; set; }

        [JsonPropertyName("taxes")]
        public Tax Taxes { get; set; }

        [JsonPropertyName("items")]
        public List<Item> Items { get; set; }
    }


    public class Booking
    {
        public Booking()
        {
            Attributes = new Attributes();
            Status = string.Empty;
            Code = string.Empty;
            Tid = new object();
            CreatedDate = string.Empty;
            StaffId = string.Empty;
            SourceIp = string.Empty;
            StartDate = string.Empty;
            EndDate = string.Empty;
            Customer = new Customer();
            Fields = new Dictionary<string, string>();
            Meta = new Dictionary<string, object>();
            Order = new Order();
        }

        [JsonPropertyName("@attributes")]
        public Attributes Attributes { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; }

        [JsonPropertyName("code")]
        public string Code { get; set; }

        [JsonPropertyName("tid")]
        public object Tid { get; set; }

        [JsonPropertyName("created_date")]
        public string CreatedDate { get; set; }

        [JsonPropertyName("staff_id")]
        public string StaffId { get; set; }

        [JsonPropertyName("source_ip")]
        public string SourceIp { get; set; }

        [JsonPropertyName("start_date")]
        public string StartDate { get; set; }

        [JsonPropertyName("end_date")]
        public string EndDate { get; set; }

        [JsonPropertyName("customer")]
        public Customer Customer { get; set; }

        [JsonPropertyName("fields")]
        public Dictionary<string, string> Fields { get; set; }

        [JsonPropertyName("meta")]
        public Dictionary<string, object> Meta { get; set; }

        [JsonPropertyName("order")]
        public Order Order { get; set; }
    }


    public class Root
    {
        public Root()
        {
            Attributes = new Attributes();
            Booking = new Booking();
        }
        [JsonPropertyName("@attributes")]
        public Attributes Attributes { get; set; }

        [JsonPropertyName("booking")]
        public Booking Booking { get; set; }
    }



}