using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Timers;
using WindowsServiceMentorshipTest.Classes;
using System.Net.Http;

namespace WindowsServiceMentorshipTest
{
    public partial class Service1 : ServiceBase
    {

        Timer timer = new Timer(); // name space(using System.Timers;)  

        public Service1()
        {

            InitializeComponent();

        }

        protected override void OnStart(string[] args)
        {
            //Create db
            WindowsServiceMentorshipTest.Database.InitializeDB.CreateDBTable();

            //Read from db
            string selectQuery = @"SELECT * FROM MyTable";
            using (System.Data.SQLite.SQLiteConnection conn = new System.Data.SQLite.SQLiteConnection("data source=|DataDirectory|/databaseFile1.db3"))
            {
                using (System.Data.SQLite.SQLiteCommand com = new System.Data.SQLite.SQLiteCommand(conn))
                {
                    conn.Open();                             // Open the connection to the database

                    com.CommandText = selectQuery;     // Set CommandText to our query that will select all rows from the table
                    com.ExecuteNonQuery();                  // Execute the query

                    using (System.Data.SQLite.SQLiteDataReader reader = com.ExecuteReader())
                    {
                        //string postJSON;
                        DataClass001 dataClass001 = new DataClass001();
                        while (reader.Read())
                        {
                            dataClass001.Key = reader["Key"].ToString();
                            dataClass001.Value = reader["Value"].ToString();
                            //postJSON = JsonSerializer.Serialize(dataClass001);
                            System.IO.File.AppendAllText(@"C:\Users\Isaiah\Documents\Mentorship Result Files\Result_Post_Keys.txt", dataClass001.Key);
                            System.IO.File.AppendAllText(@"C:\Users\Isaiah\Documents\Mentorship Result Files\Result_Post_Values.txt", dataClass001.Value);


                            using (var client = new HttpClient())
                            {
                                client.BaseAddress = new Uri("http://webapi.local/api/");

                                //HTTP POST
                                //var postTask = client.PostAsJsonAsync<DataClass001>("values", dataClass001);
                                var postTask = client.PostAsJsonAsync<string>("values", @"{"+"\"key\":\""+dataClass001.Key+"\","+"\"value\":\""+dataClass001.Value+"\"}");
                                postTask.Wait();

                                var result = postTask.Result;
                                if (result.IsSuccessStatusCode)
                                {
                                    //return RedirectToAction("Index");
                                    System.IO.File.AppendAllText(@"C:\Users\Isaiah\Documents\Mentorship Result Files\Result_Post.txt", "Success" + result.StatusCode);
                                }
                                else
                                {
                                    System.IO.File.AppendAllText(@"C:\Users\Isaiah\Documents\Mentorship Result Files\Result_Post.txt", "Fail" + result.StatusCode);
                                }

                            }
                        }
                        using (var client = new HttpClient())
                        {
                            client.BaseAddress = new Uri("http://webapi.local/api/");
                            //HTTP GET
                            var responseTask = client.GetAsync("values");
                            responseTask.Wait();

                            var result = responseTask.Result;
                            if (result.IsSuccessStatusCode)
                            {
                                System.IO.File.WriteAllText(@"C:\Users\Isaiah\Documents\Mentorship Result Files\Result.txt", "Success");
                                var readTask = result.Content.ReadAsAsync<IList<string>>();
                                readTask.Wait();

                                IList<string> results = readTask.Result;

                                foreach (var s in results)
                                {
                                    //Console.WriteLine(d);
                                    System.IO.File.AppendAllText(@"C:\Users\Isaiah\Documents\Mentorship Result Files\ResultBody.txt", s);
                                }
                                System.IO.File.AppendAllText(@"C:\Users\Isaiah\Documents\Mentorship Result Files\ResultBody.txt", Environment.NewLine);
                            }
                            else
                            {
                                System.IO.File.WriteAllText(@"C:\Users\Isaiah\Documents\Mentorship Result Files\Result.txt", "FAIL");
                            }
                        }
                        conn.Close();        // Close the connection to the database
                    }
                }

                WriteToFile("Service is started at " + DateTime.Now);

                timer.Elapsed += new ElapsedEventHandler(OnElapsedTime);

                timer.Interval = 5000; //number in milisecinds  

                timer.Enabled = true;
            }

        }

        protected override void OnStop()
        {

            WriteToFile("Service is stopped at " + DateTime.Now);

        }

        private void OnElapsedTime(object source, ElapsedEventArgs e)
        {

            WriteToFile("Service is recall at " + DateTime.Now);

        }

        public void WriteToFile(string Message)
        {

            string path = AppDomain.CurrentDomain.BaseDirectory + "\\Logs";

            if (!Directory.Exists(path))
            {

                Directory.CreateDirectory(path);

            }

            string filepath = AppDomain.CurrentDomain.BaseDirectory + "\\Logs\\ServiceLog_" + DateTime.Now.Date.ToShortDateString().Replace('/', '_') + ".txt";

            if (!File.Exists(filepath))
            {

                // Create a file to write to.   

                using (StreamWriter sw = File.CreateText(filepath))
                {

                    sw.WriteLine(Message);

                }

            }
            else
            {

                using (StreamWriter sw = File.AppendText(filepath))
                {

                    sw.WriteLine(Message);

                }

            }

        }

    }
}
