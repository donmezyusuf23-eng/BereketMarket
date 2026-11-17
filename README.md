# Bereket Market

Köy bakkalları ve küçük ölçekli marketler için geliştirdiğim masaüstü satış ve veresiye takip programı. Karışık POS cihazları veya internet bağlantısı gerektirmeden, tamamen yerel veritabanı (SQLite) üzerinde çalışır.

## Özellikler

- **Hızlı Satış & Kasa:** Barkod okutarak veya barkodsuz satılan ürünler (ekmek, su, poşet, yumurta vb.) için tek tıkla sepete ekleme.
- **Nakit ve Veresiye:** Pos cihazı ve kart karmaşası olmadan, sadece nakit satış tamamlama veya tek tıkla veresiyeye yazma.
- **Veresiye Defteri:** Müşteri hesapları, güncel borç bakiyeleri, borç ödemesi (tahsilat) alma ve müşterinin hangi gün saat kaçta ne aldığını gösteren detaylı hesap ekstresi.
- **Günlük Kasa Defteri:** Gün boyunca yapılan tüm satışların saat saat akışı, kasaya giren nakit, çıkan veresiye ve tahmini kâr özeti.
- **Ürün ve Stok Takibi:** Alış ve satış fiyatları, kâr marjı hesabı ve stoğu azalan ürünler için kritik stok uyarısı.

## Kullanılan Teknolojiler

- C# / WPF (.NET 8.0)
- SQLite (Microsoft.Data.Sqlite)
- ClosedXML (Excel dışa aktarım)

## Kurulum ve Çalıştırma

Projeyi bilgisayarınızda çalıştırmak için:

1. Bu depoyu klonlayın:
   ```bash
   git clone https://github.com/donmezyusuf23-eng/BereketMarket.git
   ```
2. Visual Studio ile `BereketMarket.sln` dosyasını açın.
3. Çözümü derleyip (Build) çalıştırın (F5).
   veya komut satırından:
   ```bash
   dotnet run --project BereketMarket.csproj
   ```

Veritabanı ilk açılışta `AppData/Local/BereketMarket/bereketmarket.db` konumunda otomatik olarak oluşturulur.
