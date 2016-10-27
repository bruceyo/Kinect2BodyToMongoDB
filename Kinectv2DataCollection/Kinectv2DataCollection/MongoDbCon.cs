//-------------------------------------------﻿﻿﻿
// YU Xinbo  
// bruce.xb.yu@connect.polyu.hk      
// PhD Candidate 
// in the Hong Kong PolyTechnical University   
//-------------------------------------------
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MongoDB.Bson;
using MongoDB.Driver;

namespace Kinectv2DataCollection
{
    class MongoDbCon
    {
        public IMongoClient _client { get; set; }
        public IMongoDatabase _database { get; set; }

        public String connectionString { get; set; }

        public void insert(string jsonString)
        {
            MongoDB.Bson.BsonDocument bsonDoc = MongoDB.Bson.Serialization.BsonSerializer.Deserialize<BsonDocument>(jsonString);
            var collection = _database.GetCollection<BsonDocument>("asdset");
            collection.InsertOne(bsonDoc);
        }

        public void update()
        {

        }

        public void delete()
        {

        }


    }
}
