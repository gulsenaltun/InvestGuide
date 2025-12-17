using FinansUygulmasi.Models.Entities; // Entity burada
using FinansUygulmasi.Models.ViewModels; // ViewModel burada
using Microsoft.Extensions.Configuration;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public class MesajService : IMesajService
{
    private readonly IMongoCollection<ForumKonu> _konular;

    public MesajService(IConfiguration config)
    {
        var client = new MongoClient(config["MongoDbSettings:ConnectionString"]);
        var database = client.GetDatabase(config["MongoDbSettings:DatabaseName"]);
        _konular = database.GetCollection<ForumKonu>("ForumKonular"); // Koleksiyon adın neyse
    }

    public async Task<List<MesajViewModel>> GetSonYorumlarAsync(int adet)
    {
        // NOT: Comments dizisi boş olan konular hata vermesin diye preserveNullAndEmptyArrays: false diyoruz.

        var pipeline = _konular.Aggregate()
            // 1. Adım: Yorumları diziden çıkarıp düzleştir (Unwind)
            .Unwind(x => x.Comments)
            // 2. Adım: Yorum tarihine göre tersten sırala (En yeni en üstte)
            .Sort(Builders<BsonDocument>.Sort.Descending("Comments.date"))
            // 3. Adım: İstenen adet kadar al (Limit)
            .Limit(adet);

        // Sorguyu çalıştırıp BsonDocument listesi olarak alıyoruz
        var sonYorumlarBson = await pipeline.ToListAsync();

        // 4. Adım: Bson verisini ViewModel'e manuel map ediyoruz
        var viewModelListesi = new List<MesajViewModel>();

        foreach (var doc in sonYorumlarBson)
        {
            // Unwind işleminden sonra ana döküman (root) içine "Comments" objesi yerleşir.
            var commentDoc = doc["Comments"].AsBsonDocument;

            // AvatarUrl entity'de yok, bu yüzden baş harfinden bir görsel uyduruyoruz veya default atıyoruz.
            string username = commentDoc.Contains("username") ? commentDoc["username"].AsString : "Anonim";
            string firstLetter = !string.IsNullOrEmpty(username) ? username.Substring(0, 1).ToUpper() : "U";

            viewModelListesi.Add(new MesajViewModel
            {
                Id = commentDoc.Contains("comment_id") ? commentDoc["comment_id"].AsString : Guid.NewGuid().ToString(),
                KullaniciAdi = username,
                Icerik = commentDoc.Contains("text") ? commentDoc["text"].AsString : "",
                Tarih = commentDoc.Contains("date") ? commentDoc["date"].ToLocalTime() : DateTime.Now,
                KonuBasligi = doc.Contains("title") ? doc["title"].AsString : "Genel",
                AvatarUrl = firstLetter // View'da CSS ile yuvarlak içine harf basıyoruz zaten
            });
        }

        return viewModelListesi;
    }
}