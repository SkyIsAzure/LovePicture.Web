using MailKit.Net.Smtp;
using MimeKit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace LovePicture.Com
{
    public class PublicClass
    {

        #region _ExcuteTask 批量任务执行器 +int

        /// <summary>
        /// 批次任务执行方法
        /// </summary>
        /// <typeparam name="T">参数类型</typeparam>
        /// <param name="func">func方法</param>
        /// <param name="list">待执行数据</param>
        /// <param name="taskLen">任务量</param>
        /// <param name="timeOut">任务超时时间 默认30s</param>
        /// <returns></returns>
        public static int _ExcuteTask<T>(Func<List<T>, int> func, List<T> list, int taskLen = 10, int timeOut = 30) where T : class
        {
            var result = 0;
            //任务量
            var tasks = new Task<int>[taskLen];
            var page = list.Count / taskLen + (list.Count % taskLen > 0 ? 1 : 0);  //每个分得得需要执行的总条数 最有一个执行剩余所有
            for (var ji = 1; ji <= taskLen; ji++)
            {
                //使用分页方法获取待执行数据
                var list01 = list.Skip((ji - 1) * page).Take(page).ToList();
                if (list01.Count <= 0) { break; }
                var task = Task.Run(() =>
                {

                    return func(list01);
                });
                tasks[ji - 1] = task;
            }
            //等待执行
            Task.WaitAll(tasks, 1000 * 1 * timeOut);
            //获取执行成功条数
            result = tasks.Where(b => b.IsCompleted).Sum(b => b.Result);

            return result;
        }
        #endregion

        #region  _Md5 Md5加密

        public static string _Md5(string input, string key = "我爱祖国")
        {
            var hash = string.Empty;

            using (MD5 md5Hash = MD5.Create())
            {

                hash = GetMd5Hash(md5Hash, input + key);
            }
            return hash;
        }

        static string GetMd5Hash(MD5 md5Hash, string input)
        {

            // Convert the input string to a byte array and compute the hash.
            byte[] data = md5Hash.ComputeHash(Encoding.UTF8.GetBytes(input));

            // Create a new Stringbuilder to collect the bytes
            // and create a string.
            StringBuilder sBuilder = new StringBuilder();

            // Loop through each byte of the hashed data 
            // and format each one as a hexadecimal string.
            for (int i = 0; i < data.Length; i++)
            {
                sBuilder.Append(data[i].ToString("x2"));
            }

            // Return the hexadecimal string.
            return sBuilder.ToString().ToUpper();
        }

        #endregion

        #region 邮件

        /// <summary>
        /// 发送邮件
        /// </summary>
        /// <param name="dicToEmail"></param>
        /// <param name="title"></param>
        /// <param name="content"></param>
        /// <param name="name"></param>
        /// <param name="fromEmail"></param>
        /// <returns></returns>
        public static bool _SendEmail(
            Dictionary<string, string> dicToEmail,
            string title, string content,
            string name = "爱留图网", string fromEmail = "841202396@qq.com")
        {
            var isOk = false;
            try
            {
                if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(content)) { return isOk; }

                //设置基本信息
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(name, fromEmail));
                foreach (var item in dicToEmail.Keys)
                {
                    message.To.Add(new MailboxAddress(item, dicToEmail[item]));
                }
                message.Subject = title;
                message.Body = new TextPart("html")
                {
                    Text = content
                };

                //链接发送
                using (var client = new SmtpClient())
                {
                    // For demo-purposes, accept all SSL certificates (in case the server supports STARTTLS)
                    client.ServerCertificateValidationCallback = (s, c, h, e) => true;

                    //采用qq邮箱服务器发送邮件
                    client.Connect("smtp.qq.com", 587, false);

                    // Note: since we don't have an OAuth2 token, disable
                    // the XOAUTH2 authentication mechanism.
                    client.AuthenticationMechanisms.Remove("XOAUTH2");

                    //qq邮箱，密码(安全设置短信获取后的密码)
                    client.Authenticate("841202396@qq.com", "ufiaszkkulbabejh");  

                    client.Send(message);
                    client.Disconnect(true);
                }
                isOk = true;
            }
            catch (Exception ex)
            {

            }
            return isOk;
        }

        #endregion

        #region 读取html模板

        public static async Task<string> _GetHtmlTpl(EnumHelper.EmEmailTpl tpl, string folderPath = @"D:\F\学习\vs2017\netcore\LovePicture.Web\wwwroot\tpl")
        {
            var content = string.Empty;
            if (string.IsNullOrWhiteSpace(folderPath)) { return content; }

            var path = $"{folderPath}/{tpl}.html";
            try
            {
                using (var stream = File.OpenRead(path))
                {
                    using (var reader = new StreamReader(stream))
                    {
                        content = await reader.ReadToEndAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
            return content;
        }
        #endregion
    }
}
