namespace AdoreFlowerShop.Data
{
    /// <summary>Строка списка заказов для UI (админка).</summary>
    public sealed class OrderTableRow
    {
        public int Id { get; set; }
        public string UserName { get; set; } = "";
        public string FlowerName { get; set; } = "";
        public string Status { get; set; } = "";
        public int Quantity { get; set; }
        public double UnitPrice { get; set; }
        public double TotalPrice => UnitPrice * Quantity;
        public string? DeliveryDate { get; set; }
        public string? Address { get; set; }

        /// <summary>Показывать клиенту объявление «Заказ зарегистрирован» в личном кабинете.</summary>
        public bool IsRegisteredNotice { get; set; }
    }
}
