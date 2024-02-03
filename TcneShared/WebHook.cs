namespace TcneShared.WebHook
{

    public class Attributes
    {
        public Attributes()
        {
            Version = string.Empty;
            Host = string.Empty;
        }

        public string Version { get; set; }
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

        public string Code { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string Region { get; set; }
        public string Address { get; set; }
        public string City { get; set; }
        public string Country { get; set; }
        public string Phone { get; set; }
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
        public string StartDate { get; set; }
        public string EndDate { get; set; }
        public string Sku { get; set; }
        public object Slip { get; set; }
        public string PackageId { get; set; }
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

        public Attributes Attributes { get; set; }
        public string SubTotal { get; set; }
        public string TaxTotal { get; set; }
        public string PaidTotal { get; set; }
        public string Total { get; set; }
        public Tax Taxes { get; set; }
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

        public Attributes Attributes { get; set; }
        public string Status { get; set; }
        public string Code { get; set; }
        public object Tid { get; set; }
        public string CreatedDate { get; set; }
        public string StaffId { get; set; }
        public string SourceIp { get; set; }
        public string StartDate { get; set; }
        public string EndDate { get; set; }
        public Customer Customer { get; set; }
        public Dictionary<string, string> Fields { get; set; }
        public Dictionary<string, object> Meta { get; set; }
        public Order Order { get; set; }
    }


    public class Root
    {
        public Root()
        {
            Attributes = new Attributes();
            Booking = new Booking();
        }

        public Attributes Attributes { get; set; }
        public Booking Booking { get; set; }
    }



}