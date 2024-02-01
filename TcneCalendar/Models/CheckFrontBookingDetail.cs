using System.Text.Json.Serialization;

//namespace TcneCalendar.Models
//{
//    /// <summary>
//    /// This class is used to model the data we care about from the booking detail.
//    /// Only one item is expected!!!!!
//    /// 
//    /// </summary>
//    public class CheckFrontBookingDetail
//    {
//        public class RootObject
//        {
//            [JsonPropertyName("booking")]
//            public BookingDetail? BookingDetail { get; set; }
//        }

//        public class BookingDetail
//        {
//            [JsonPropertyName("items")]
//            public Dictionary<string, Item>? Items { get; set; }
//        }

//        public class Item   // in the JSON there is an 'root/booking/items' collection, this is an item in that collection
//        {
//            [JsonPropertyName("start_date")]
//            public long StartDate { get; set; }

//            [JsonPropertyName("end_date")]
//            public long EndDate { get; set; }

//            [JsonPropertyName("category_id")]
//            public int Studio { get; set; }
//        }
//    }
//}
