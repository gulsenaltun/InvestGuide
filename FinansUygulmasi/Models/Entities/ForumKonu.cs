using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;

namespace FinansUygulmasi.Models.Entities
{
    [BsonIgnoreExtraElements] // JSON'da olup burada olmayan alanlar hata vermesin
    public class ForumKonu
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [BsonElement("discussion_id")]
        public int DiscussionId { get; set; }

        [BsonElement("asset_tag")]
        public string AssetTag { get; set; }

        [BsonElement("title")]
        public string Title { get; set; }

        [BsonElement("content")]
        public string Content { get; set; }

        [BsonElement("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [BsonElement("stats")]
        public ForumStats Stats { get; set; } = new ForumStats();

        [BsonElement("author")]
        public ForumAuthor Author { get; set; } = new ForumAuthor();

        [BsonElement("comments")]
        public List<ForumComment> Comments { get; set; } = new List<ForumComment>();
    }

    public class ForumAuthor
    {
        [BsonElement("user_id_ref")]
        public int? UserId { get; set; } // JSON'da bazen olmayabilir diye nullable yaptım

        [BsonElement("username")]
        public string Username { get; set; }

        [BsonElement("badge")]
        public string Badge { get; set; }
    }

    public class ForumStats
    {
        [BsonElement("views")]
        public int Views { get; set; }

        [BsonElement("likes")]
        public int Likes { get; set; }
    }

    public class ForumComment
    {
        [BsonElement("comment_id")]
        public int CommentId { get; set; } 

        [BsonElement("user_id_ref")]
        public int? UserId { get; set; }

        [BsonElement("username")]
        public string Username { get; set; }

        [BsonElement("text")]
        public string Text { get; set; }

        [BsonElement("date")]
        public DateTime Date { get; set; } = DateTime.Now;

        [BsonElement("likes")]
        public int Likes { get; set; } = 0;

        // EKSİK OLAN KISIM BURASIYDI:
        [BsonElement("replies")]
        public List<ForumReply> Replies { get; set; } = new List<ForumReply>();
    }

    // JSON'daki 'replies' dizisi için yeni sınıf
    public class ForumReply
    {
        [BsonElement("user_id_ref")]
        public int? UserId { get; set; }

        [BsonElement("username")]
        public string Username { get; set; }

        [BsonElement("text")]
        public string Text { get; set; }

        [BsonElement("date")]
        public DateTime Date { get; set; } = DateTime.Now;
    }
}