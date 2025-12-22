using FinansUygulmasi.Data;
using FinansUygulmasi.Models.Entities;
using FinansUygulmasi.Repositories.Interfaces;
using MongoDB.Driver;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FinansUygulmasi.Repositories
{
    public class ForumRepository : IForumRepository
    {
        private readonly MongoDbContext _context;

        public ForumRepository(MongoDbContext context)
        {
            _context = context;
        }

        public async Task<List<ForumKonu>> TumKonulariGetirAsync()
        {
            return await _context.Tartismalar.Find(k => true)
                                 .SortByDescending(x => x.CreatedAt)
                                 .ToListAsync();
        }

        public async Task<ForumKonu> KonuGetirIdIleAsync(string id)
        {
            return await _context.Tartismalar.Find(k => k.Id == id).FirstOrDefaultAsync();
        }

        public async Task KonuEkleAsync(ForumKonu konu)
        {
            await _context.Tartismalar.InsertOneAsync(konu);
        }

        public async Task YorumEkleAsync(string konuId, ForumComment yorum)
        {
            var filter = Builders<ForumKonu>.Filter.Eq(k => k.Id, konuId);
            var update = Builders<ForumKonu>.Update.Push(k => k.Comments, yorum);
            await _context.Tartismalar.UpdateOneAsync(filter, update);
        }

        public async Task YanitEkleAsync(string konuId, int parentCommentId, ForumReply yanit)
        {
            var filter = Builders<ForumKonu>.Filter.And(
                  Builders<ForumKonu>.Filter.Eq(x => x.Id, konuId),
                  Builders<ForumKonu>.Filter.ElemMatch(x => x.Comments, c => c.CommentId == parentCommentId)
            );
            var update = Builders<ForumKonu>.Update.Push("Comments.$.Replies", yanit);
            await _context.Tartismalar.UpdateOneAsync(filter, update);
        }

        public async Task YorumSilAsync(string konuId, int yorumId, int userId)
        {
             var filter = Builders<ForumKonu>.Filter.And(
                Builders<ForumKonu>.Filter.Eq(k => k.Id, konuId),
                Builders<ForumKonu>.Filter.ElemMatch(x => x.Comments, c => c.CommentId == yorumId && c.UserId == userId)
            );
            var update = Builders<ForumKonu>.Update.PullFilter(k => k.Comments, c => c.CommentId == yorumId);
            await _context.Tartismalar.UpdateOneAsync(filter, update);
        }
    }
}