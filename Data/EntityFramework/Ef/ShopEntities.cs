using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AdoreFlowerShop.Data.EntityFramework
{
    [Table("Users")]
    public sealed class ShopUser
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = "";

        [Required]
        public string Password { get; set; } = "";

        [Required]
        public string Role { get; set; } = "Client";

        public string? Phone { get; set; }

        public ICollection<ShopOrder> Orders { get; set; } = new List<ShopOrder>();
    }

    [Table("Flowers")]
    public sealed class ShopFlower
    {
        public int Id { get; set; }

        [Required]
        public string ShortName { get; set; } = "";

        public string? FullName { get; set; }
        public string? Description { get; set; }
        public string? Category { get; set; }

        public double Price { get; set; }
        public int Quantity { get; set; }
        public double Rating { get; set; }
        public string? Color { get; set; }
        public string? Country { get; set; }
        public double Discount { get; set; }
        public bool InStock { get; set; }
        public int SoldCount { get; set; }
        public int SortOrder { get; set; }

        public ICollection<ShopFlowerImage> FlowerImages { get; set; } = new List<ShopFlowerImage>();
        public ICollection<ShopOrder> Orders { get; set; } = new List<ShopOrder>();
        public ICollection<Tag> Tags { get; set; } = new List<Tag>();
    }

    [Table("FlowerImages")]
    public sealed class ShopFlowerImage
    {
        public int Id { get; set; }

        public int FlowerId { get; set; }
        public ShopFlower Flower { get; set; } = null!;

        [Required]
        public byte[] Image { get; set; } = System.Array.Empty<byte>();

        public bool IsMain { get; set; }
    }

    [Table("Orders")]
    public sealed class ShopOrder
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public ShopUser? User { get; set; }

        public int FlowerId { get; set; }
        public ShopFlower? Flower { get; set; }

        public int Quantity { get; set; }
        public double UnitPrice { get; set; }
        public string Status { get; set; } = "Ожидает обработки";
        public string? DeliveryDate { get; set; }
        public string? Address { get; set; }

        /// <summary>Клиенту в личном кабинете показывается объявление о регистрации заказа.</summary>
        public bool IsRegisteredNotice { get; set; }
    }

    [Table("Reviews")]
    public sealed class ShopReview
    {
        public int Id { get; set; }

        public int FlowerId { get; set; }
        public ShopFlower? Flower { get; set; }

        public int UserId { get; set; }
        public ShopUser? User { get; set; }

        public int Rating { get; set; }

        public string? Comment { get; set; }

        [Required]
        public string CreatedAt { get; set; } = "";
    }

    [Table("Tags")]
    public sealed class Tag
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = "";

        public ICollection<ShopFlower> Flowers { get; set; } = new List<ShopFlower>();
    }
}
