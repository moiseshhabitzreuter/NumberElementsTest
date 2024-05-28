using MongoDB.Bson;
using MongoDB.Driver;
using System.Diagnostics;

namespace TestBenner
{
    internal class Program
    {
        static void Main(string[] args)
        {
            const string connectionUri = "myconnectionstring";
            var settings = MongoClientSettings.FromConnectionString(connectionUri);
            settings.ServerApi = new ServerApi(ServerApiVersion.V1);
            var client = new MongoClient(settings);
            var collection = client.GetDatabase("mydatabase").GetCollection<NumberElement>("numberElements");

            try
            {
                var result = client.GetDatabase("admin").RunCommand<BsonDocument>(new BsonDocument("ping", 1));
                Console.WriteLine("Pinged your deployment. You successfully connected to MongoDB!");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            var numberElementRepository = new NumberElementRepository(collection);
            Console.WriteLine("How many elements do you want in your collection");
            var numbers = Int32.Parse(Console.ReadLine());

            var response = numberElementRepository.GetLatestNumber().Result;
            if (response.HasErrors)
            {
                foreach (var error in response.Errors)
                {
                    Console.WriteLine(error);
                }
            }

            var numberElementsToInsert = new List<NumberElement>();
            for (int i = response.Number + 1; i <= numbers; i++)
            {
                numberElementsToInsert.Add(new NumberElement(i));
            }

            if(numberElementsToInsert.Count > 0)
            {
                try
                {
                    collection.InsertMany(numberElementsToInsert);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }

            Console.WriteLine("Whats the first number that you want to connect? (0 for none)");

            var firstNumberToConnect = Int32.Parse(Console.ReadLine());
            if (firstNumberToConnect != 0)
            {
                Console.WriteLine("Whats the second number that you want to connect?");
                var secondNumberToConnect = Int32.Parse(Console.ReadLine());

                var methodResponse = numberElementRepository.Connect(firstNumberToConnect, secondNumberToConnect).GetAwaiter().GetResult();
                if (methodResponse.HasErrors)
                {
                    foreach (var error in methodResponse.Errors)
                    {
                        Console.WriteLine(error);
                    }
                }
                else
                {
                    Console.WriteLine("Both numbers were connected succesfully!");
                }
            }

            Console.WriteLine("Whats the first number that you want to query (0 for none)");
            var firstNumberToQuery = Int32.Parse(Console.ReadLine());
            if (firstNumberToQuery != 0)
            {
                Console.WriteLine("Whats the second number that you want to query?");
                var secondNumberToQuery = Int32.Parse(Console.ReadLine());

                var methodResponse = numberElementRepository.Query(firstNumberToQuery, secondNumberToQuery).GetAwaiter().GetResult();
                if (methodResponse.Success)
                {
                    Console.WriteLine("Both Numbers are connected");
                }
                else
                {
                    Console.WriteLine("Numbers are not connected");
                }
            }
        }
    }

    public class NumberElementRepository(IMongoCollection<NumberElement> collection)
    {
        public async Task<Response> Connect(int firstNumber, int secondNumber)
        {
            var response = new Response();
            var firstNumberToUpdate = new NumberElement(0);
            var secondNumberToUpdate = new NumberElement(0);
            try
            {
                firstNumberToUpdate = collection.Find(Builders<NumberElement>.Filter.Eq(number => number.Number, firstNumber))
                                                .SingleOrDefault();

                secondNumberToUpdate = collection.Find(Builders<NumberElement>.Filter.Eq(number => number.Number, secondNumber))
                                                 .SingleOrDefault();
            }
            catch (Exception ex)
            {
                response.AddError(ex.Message);
                response.Success = false;
                return response;
            }

            response = ValidateNumbers(firstNumberToUpdate, secondNumberToUpdate, response);
            if (response.HasErrors) return response;

            firstNumberToUpdate.ConectedTo = firstNumberToUpdate.ConectedTo is null ? new List<int>() : firstNumberToUpdate.ConectedTo;
            secondNumberToUpdate.ConectedTo = secondNumberToUpdate.ConectedTo is null ? new List<int>() : secondNumberToUpdate.ConectedTo;

            firstNumberToUpdate.ConectedTo.Add(secondNumber);
            secondNumberToUpdate.ConectedTo.Add(firstNumber);

            try
            {
                var updateFirstNumber = collection
                   .UpdateOneAsync(Builders<NumberElement>.Filter.Eq(number => number.Id, firstNumberToUpdate.Id),
                                   Builders<NumberElement>.Update.Set(number => number.ConectedTo, firstNumberToUpdate.ConectedTo).Set(number => number.isConected, true)).Result;

                var updateSecondNumber = collection
                    .UpdateOneAsync(Builders<NumberElement>.Filter.Eq(number => number.Id, secondNumberToUpdate.Id),
                                    Builders<NumberElement>.Update.Set(number => number.ConectedTo, secondNumberToUpdate.ConectedTo).Set(number => number.isConected, true)).Result;
            }
            catch (Exception ex)
            {
                response.AddError(ex.Message);
                return response;
            }
            return response;
        }

        public async Task<Response> Query(int firstNumber, int secondNumber)
        {
            var response = new Response();
            var result = new NumberElement(0);
            var result2 = new NumberElement(0);

            try
            {
                result = collection.Find(Builders<NumberElement>.Filter.Where(number => number.Number == firstNumber && number.isConected && number.ConectedTo.Contains(secondNumber)))
                                   .SingleOrDefault();
            }
            catch (Exception ex)
            {
                response.AddError(ex.Message);
                response.Success = false;
                return response;
            }

            if (result != null)
            {
                return response;
            };

            try
            {
                result2 = collection.Find(Builders<NumberElement>.Filter.Where(number => number.isConected && number.ConectedTo.Contains(secondNumber) && number.ConectedTo.Contains(secondNumber)))
                                    .SingleOrDefault();
            }
            catch (Exception ex)
            {
                response.AddError(ex.Message);
                response.Success = false;
                return response;
            }

            if (result2 == null) response.Success = false;
            return response;
        }

        public async Task<Response> GetLatestNumber()
        {
            var response = new Response();
            var latestNumber = new NumberElement(0);
            var builder = Builders<NumberElement>.Sort;
            var sort = builder.Descending("number");

            try
            {
                latestNumber = collection.Find(Builders<NumberElement>.Filter.Empty).Sort(sort).FirstOrDefault();
            }
            catch (Exception ex)
            {
                response.AddError(ex.Message);
                response.Success = false;
            }

            if (latestNumber == null)
            {
                response.Number = 0;
                return response;
            }

            response.Number = latestNumber.Number;
            return response;
        }

        private Response ValidateNumbers(NumberElement firstNumber, NumberElement secondNumber, Response response)
        {
            if (firstNumber is null || secondNumber is null)
            {
                response.AddError("A number wasnt found in the databank.");
                return response;
            }

            if (firstNumber.ConectedTo is null || secondNumber.ConectedTo is null)
            {
                return response;
            }

            if (firstNumber.ConectedTo.Contains(secondNumber.Number))
            {
                response.AddError("A number wasnt found in the databank.");
                return response;
            }

            return response;
        }
    }
}
