namespace AdoreFlowerShop.Services
{
    /// <summary>Единые ограничения длины и формата для полей ввода.</summary>
    public static class InputLimits
    {
        public const int UsernameMin = 2;
        public const int UsernameMax = 32;

        public const int PasswordMin = 4;
        public const int PasswordMax = 64;

        public const int PhoneDigits = 12;
        public const int PhoneDisplayMax = 24;

        public const int AddressMin = 7;
        public const int AddressMax = 500;

        public const int ReviewCommentMin = 10;
        public const int ReviewCommentMax = 1000;

        public const int SearchMax = 50;

        public const int FlowerShortNameMax = 80;
        public const int FlowerDescriptionMax = 2000;
        public const int FlowerCountryMax = 60;
        public const int FlowerColorMax = 12;
    }
}
