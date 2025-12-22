using FinansUygulmasi.Models.Entities;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;

namespace FinansUygulmasi.Data
{
    public class MongoDbContext
    {
        private readonly IMongoDatabase _database;

        public MongoDbContext(IConfiguration configuration)
        {
            // appsettings.json dosyasından bağlantı bilgisini alıyoruz
            var connectionString = configuration["MongoDbSettings:ConnectionString"];
            var dbName = configuration["MongoDbSettings:DatabaseName"];

            var client = new MongoClient(connectionString);
            _database = client.GetDatabase(dbName);
        }

        // SQL'deki "DbSet" yerine burada "IMongoCollection" kullanıyoruz
        public IMongoCollection<ForumKonu> Tartismalar => _database.GetCollection<ForumKonu>("comments");

        // İleride başka tablolar eklemek istersen buraya ekleyeceksin:
        // public IMongoCollection<LogKaydi> Loglar => _database.GetCollection<LogKaydi>("Loglar");
    }
}