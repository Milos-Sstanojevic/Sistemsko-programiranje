using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Threading;

namespace WebServer
{
    class Program
    {
        static readonly Dictionary<string, byte[]> cache = new Dictionary<string, byte[]>();
        static readonly string rootFolder = "C:\\Users\\Milos\\OneDrive\\Radna površina\\fax\\3. godina\\sistemsko\\projekat\\RGB_Converter_to_Black-White\\RGB_Converter_to_Black-White\\bin\\Debug";

        static void Main(string[] args)
        {
            TcpListener listener = new TcpListener(IPAddress.Any, 8083);
            listener.Start();
            Console.WriteLine("Cekam zahtev sa porta 8083...");

            while (true)
            {
                TcpClient client = listener.AcceptTcpClient();
                ThreadPool.QueueUserWorkItem(ProcessRequest, client);
            }
        }

        static void ProcessRequest(object state)
        {
            TcpClient client = (TcpClient)state;
            NetworkStream stream = client.GetStream();
            StreamReader reader = new StreamReader(stream);
            StreamWriter writer = new StreamWriter(stream);

            try
            {
                string request = reader.ReadLine();

                if (request != null)
                {
                    Console.WriteLine("Primljeni zahtev: " + request);

                    string[] parts = Regex.Split(request, @"\s+");

                    if (parts.Length == 3 && parts[0] == "GET")
                    {
                        string filename = parts[1].Substring(1);
                        string filepath = Path.Combine(rootFolder, filename);

                        Console.WriteLine("Putanja to fajla je: "+filepath);

                        lock (cache)
                        {
                            if (cache.ContainsKey(filepath))
                            {
                                Console.WriteLine("Fajl se trazi u kesu je: " + filename);

                                Console.WriteLine("Sadrzaj kesa: ");

                                foreach(KeyValuePair<string, byte[]> keyValues in cache)
                                {
                                    Console.WriteLine("Key: {0}, value: {1}",keyValues.Key,keyValues.Value);
                                }

                                byte[] response = cache[filepath];
                                Console.WriteLine(filepath);
                                writer.Write("HTTP/1.1 200 OK\r\n");
                                writer.Write("Content-Type: image/jpeg\r\n");
                                writer.Write("Content-Length: " + response.Length + "\r\n");
                                writer.Write("\r\n");
                                writer.Flush();
                                stream.Write(response, 0, response.Length);
                            }
                            else if (File.Exists(filepath))
                            {
                                Console.WriteLine("Prevodim sliku: " + filename + ", u crno - belu sliku");

                                using (Bitmap bmp = new Bitmap(filepath))
                                using (MemoryStream ms = new MemoryStream())
                                {
                                    bmp.Save(ms, ImageFormat.Jpeg);
                                    byte[] bytes = ms.ToArray();
                                    byte[] converted = ConvertToBlackAndWhite(bytes);
                                    cache[filepath] = converted;

                                    writer.Write("HTTP/1.1 200 OK\r\n");
                                    writer.Write("Content-Type: image/jpeg\r\n");
                                    writer.Write("Content-Length: " + converted.Length + "\r\n");
                                    writer.Write("\r\n");
                                    writer.Flush();
                                    stream.Write(converted, 0, converted.Length);
                                }
                            }
                            else
                            {
                                Console.WriteLine("Nema fajla: " + filename);

                                writer.Write("HTTP/1.1 404 Not Found\r\n");
                                writer.Write("\r\n");
                                writer.Flush();
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine("Los zahtev: " + request);

                        writer.Write("HTTP/1.1 400 Bad Request\r\n");
                        writer.Write("\r\n");
                        writer.Flush();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Gresak sa obsluzivanjem zahteva: " + ex.Message);
            }
            finally
            {
                writer.Close();
                reader.Close();
                stream.Close();
                client.Close();
            }
        }

        static byte[] ConvertToBlackAndWhite(byte[] input)
        {
            Console.WriteLine("Pozvana funkcija za konverziju u crno - belu sliku");
            using (MemoryStream ms = new MemoryStream(input))
            using (Bitmap bmp = new Bitmap(ms))
            {
                for (int i = 0; i < bmp.Width; i++)
                {
                    for (int j = 0; j < bmp.Height; j++)
                    {
                        Color color = bmp.GetPixel(i, j);
                        int average = (color.R + color.G + color.B) / 3;
                        bmp.SetPixel(i, j, Color.FromArgb(average, average, average));
                    }
                }
                using(MemoryStream ms1= new MemoryStream())
                {
                    bmp.Save(ms1, ImageFormat.Jpeg);
                    return ms1.ToArray();
                }
                 
                
            }

        }
        }
    }
