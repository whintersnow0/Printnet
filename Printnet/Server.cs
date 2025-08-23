using System;
using System.Net;
using System.Text;

class Server
{
    static void Main()
    {
        HttpListener listener = new HttpListener();
        listener.Prefixes.Add("http://localhost:5000/");
        listener.Start();
        Console.WriteLine("Server started. Waiting for requests...");

        while (true)
        {
            HttpListenerContext context = listener.GetContext();
            HttpListenerRequest request = context.Request;

            Console.WriteLine($"Received request: {request.HttpMethod} {request.Url}");

            string responseString = "Hello from the console server!";
            byte[] buffer = Encoding.UTF8.GetBytes(responseString);

            HttpListenerResponse response = context.Response;
            response.ContentLength64 = buffer.Length;
            response.OutputStream.Write(buffer, 0, buffer.Length);
            response.OutputStream.Close();
        }
    }
}
