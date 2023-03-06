using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebScraper
{
    public class DealModel
    {
        public string DealNumber { get; set; }
        public string SellerName { get; set; }
        public string SellerInn { get; set; }
        public string BuyerName { get; set; }
        public string BuyerInn { get; set; }
        public DateTime DealDate { get; set; }
        public decimal WoodVolumeSeller { get; set; }
        public decimal WoodVolumeBuyer { get; set; }
    }
}
