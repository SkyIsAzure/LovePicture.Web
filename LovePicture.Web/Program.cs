using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using System.Text;
using LovePicture.Com;

namespace LovePicture.Web
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            Console.OutputEncoding = Encoding.GetEncoding("GB2312");

            Console.WriteLine("请输入域名：");
            var yuming = Console.ReadLine();
            yuming = string.IsNullOrWhiteSpace(yuming) ? "*" : yuming;

            Console.WriteLine("请输入端口号：");
            var port = Console.ReadLine();
            port = string.IsNullOrWhiteSpace(port) ? "5000" : port;

            var host = new WebHostBuilder()
                .UseUrls($"http://{yuming}:{port}")
                .UseKestrel()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseIISIntegration()
                .UseStartup<Startup>()
                .UseApplicationInsights()
                .Build();

            host.Run();
        }
    }
}
