using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace DATN_API.ViewModels.GHTK
{
    public class GHTKCreateOrderRequest
    {
        [JsonProperty("products")]
        public List<GHTKProduct> Products { get; set; }

        [JsonProperty("order")]
        public GHTKOrder Order { get; set; }
    }

    public class GHTKProduct
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("weight")]
        public decimal Weight { get; set; }

        [JsonProperty("quantity")]
        public int Quantity { get; set; }

        [JsonProperty("product_code", NullValueHandling = NullValueHandling.Ignore)]
        public string ProductCode { get; set; }
    }

    public class GHTKOrder
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("pick_name")]
        public string PickName { get; set; }

        [JsonProperty("pick_address")]
        public string PickAddress { get; set; }

        [JsonProperty("pick_province")]
        public string PickProvince { get; set; }

        [JsonProperty("pick_district")]
        public string PickDistrict { get; set; }

        [JsonProperty("pick_ward")]
        public string PickWard { get; set; }

        [JsonProperty("pick_tel")]
        public string PickTel { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("address")]
        public string Address { get; set; }

        [JsonProperty("province")]
        public string Province { get; set; }

        [JsonProperty("district")]
        public string District { get; set; }

        [JsonProperty("ward")]
        public string Ward { get; set; }
        [JsonProperty("hamlet")]
        public string Hamlet { get; set; } = "Khác";
        [JsonProperty("tel")]
        public string Tel { get; set; }

        [JsonProperty("pick_money")]
        public decimal PickMoney { get; set; }

        [JsonProperty("note")]
        public string Note { get; set; }

        [JsonProperty("value")]
        public decimal Value { get; set; }

        [JsonProperty("transport")]
        public string Transport { get; set; } = "road";

        [JsonProperty("deliver_option")]
        public string DeliverOption { get; set; } = "none";

        [JsonProperty("is_freeship", NullValueHandling = NullValueHandling.Ignore)]
        public string IsFreeShip { get; set; }
    }


    public class GHTKOrderStatusViewModel
    {
        public string OrderCode { get; set; }
        public string PartnerId { get; set; }
        public int Status { get; set; }
        public string StatusText { get; set; }
        public DateTime Created { get; set; }
        public DateTime Modified { get; set; }
        public DateTime? PickDate { get; set; }
        public DateTime? DeliverDate { get; set; }
        public decimal ShipMoney { get; set; }
        public decimal Insurance { get; set; }
        public decimal Value { get; set; }
        public decimal Weight { get; set; }
        public decimal PickMoney { get; set; }
        public bool IsFreeship { get; set; }
        public List<GHTKProductStatusViewModel> Products { get; set; } = new();
    }

    public class GHTKProductStatusViewModel
    {
        public string FullName { get; set; }
        public string ProductCode { get; set; }
        public decimal Weight { get; set; }
        public int Quantity { get; set; }
        public decimal Cost { get; set; }
    }

}