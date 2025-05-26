namespace SatışProject.Entities
{
    // Sepeti temsil eden ana sınıf
    public class Basket
    {
        public int BasketId { get; set; } // Sepetin benzersiz kimliği (Primary Key)

        // Eğer kullanıcı kimlik doğrulama sisteminiz varsa, buraya UserId ekleyebilirsiniz:
        // public string? UserId { get; set; } 

        // Sepetteki ürünlerin listesi (BasketItem koleksiyonu)
        // Bir sepetin içinde birden fazla sepet öğesi bulunabilir.
        public virtual ICollection<BasketItem> BasketItems { get; set; } = new List<BasketItem>();
    }

    // Sepetteki her bir ürünü (öğeyi) temsil eden sınıf
    public class BasketItem
    {
        public int BasketItemId { get; set; } // Sepet öğesinin benzersiz kimliği (Primary Key)

        // Hangi ürüne ait olduğunu belirten ProductId (Foreign Key)
        public int ProductId { get; set; }
        public virtual Product Product { get; set; } = null!; // İlişkili Ürün nesnesi

        // Hangi sepete ait olduğunu belirten BasketId (Foreign Key)
        public int BasketId { get; set; }
        public virtual Basket Basket { get; set; } = null!; // İlişkili Sepet nesnesi

        // Üründen kaç adet olduğunu tutar
        public int Quantity { get; set; }

        // Ürün sepete eklendiği andaki birim fiyatı (Fiyat değişimi olasılığına karşı önemli)
        public decimal UnitPriceAtTimeOfAddition { get; set; }
    }
}