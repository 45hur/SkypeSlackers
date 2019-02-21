using System.Collections.Generic;
using System.Linq;

using System;
using System.Data.SQLite;

using Newtonsoft.Json;

namespace SkypeBot
{
    class Program
    {
        static void Main(string[] args)
        {
            foreach (var slacker in Slackers())
            {
                Console.WriteLine(slacker);
            }

            Console.ReadLine();
        }

        static IEnumerable<string> Slackers()
        {
            string user = "ashur";
            string dbname = "s4l-ashur.eu.db";
            string convo = "C19:I2FzaHVyLmV1LyRqYW4ubWFuZWs7N2RhOTczY2U1ODliYTFkMQ==%";

            using (var conn = new SQLiteConnection($@"Data Source=c:\Users\{user}\AppData\Local\Packages\Microsoft.SkypeApp_kzf8qxf38zg5c\LocalState\{dbname};Version=3;New=False;Compress=True;"))
            {
                conn.Open();

                var sql = $"SELECT * FROM conversationsv14 WHERE nsp_pk LIKE '{convo}'";
                var contacts = new List<string>();
                using (var cmd = new SQLiteCommand(sql, conn))
                {
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            dynamic desobj = JsonConvert.DeserializeObject(reader["nsp_data"].ToString());
                            foreach (var mem in desobj.conv._threadMembers)
                            {
                                contacts.Add(mem.id.ToString());
                            }
                        }
                    }
                }

                sql = $"SELECT * FROM messagesv12 WHERE nsp_pk LIKE '{convo}'";
                var list = new List<string>();
                var messages = new List<Message>();

                using (var cmd = new SQLiteCommand(sql, conn))
                {
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(reader["nsp_data"].ToString());
                        }
                    }
                }

                foreach (var message in list)
                {
                    dynamic desobj = JsonConvert.DeserializeObject(message);
                    var servermessages = desobj._serverMessages;
                    dynamic desobj2 = JsonConvert.DeserializeObject(servermessages.ToString());

                    var msg = new Message()
                    {
                        Author = desobj.creator.ToString(),
                        Content = desobj.content.ToString(),
                        Created = DateTime.Parse(desobj2[0].composetime.ToString(), null, System.Globalization.DateTimeStyles.RoundtripKind)
                    };

                    messages.Add(msg);
                }

                var users = new Dictionary<string, DateTime>();
                foreach (var m in messages)
                {
                    if (!users.ContainsKey(m.Author))
                    {
                        users.Add(m.Author, m.Created);
                    }
                    else
                    {
                        if (users[m.Author] < m.Created)
                            users[m.Author] = m.Created;
                    }
                }

                //var slackers = users.OrderByDescending(t => t.Value).Select(t => t.Key + " - " + t.Value);
                var posters = users.OrderByDescending(t => t.Value).Select(t => t.Key);
                var missing = contacts.Except(posters);

                return missing.ToArray();
            }
        }
    }
}
