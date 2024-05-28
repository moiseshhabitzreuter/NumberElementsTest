using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace TestBenner
{
    public class NumberElement(int number)
    {
        public ObjectId Id { get; set; }
        [BsonElement("number")]
        public int Number { get; set; } = number;
        [BsonElement("isconected")]
        public bool isConected { get; set; }
        [BsonElement("conectedto")]
        public List<int> ConectedTo { get; set; }
    }
}
